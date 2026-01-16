using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Debugger
{
	public class LVOLoggerBase
	{
		protected readonly ICPU cpu;
		protected readonly IDebugMemoryMapper memory;
		protected readonly IReturnValueSnagger returnValueSnagger;
		protected readonly IAnalyser analyser;
		protected readonly ILibraryBases libraryBases;
		protected readonly ILogger logger;

		public LVOLoggerBase(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger logger)
		{
			this.cpu = cpu;
			this.memory = memory;
			this.returnValueSnagger = returnValueSnagger;
			this.analyser = analyser;
			this.libraryBases = libraryBases;
			this.logger = logger;
		}
	}

	public interface ILVOInterceptorAction
	{
		public void Intercept(LVO lvo, uint pc);

		public string Library { get; }
		public string VectorName { get; }
	}

	public interface ILVOInterceptors
	{
		void CheckLVOAccess(uint address, Size size);
	}

	public class LVOInterceptor
	{
		public string Library { get; set; }
		public LVO LVO { get; set; }
		public ILVOInterceptorAction Action { get; set; }
	}

	public interface ILVOInterceptorCollection
	{
		void AddInterceptor(LVOInterceptor interceptor);
		Dictionary<uint, LVOInterceptor> ActiveLVOInterceptors { get; }
		void UpdateActiveLVOInterceptors(Dictionary<string, uint> libraryBaseAddresses);
	}

	public class LVOInterceptorCollection : ILVOInterceptorCollection
	{
		private readonly List<LVOInterceptor> lvoInterceptors = new List<LVOInterceptor>();
		private readonly Dictionary<uint, LVOInterceptor> activeLvoInterceptors = new Dictionary<uint, LVOInterceptor>();
		private readonly IDebugMemoryMapper memory;

		public LVOInterceptorCollection(IDebugMemoryMapper memory)
		{
			this.memory = memory;
		}

		public void AddInterceptor(LVOInterceptor interceptor)
		{
			lvoInterceptors.Add(interceptor);
		}

		//opened a new library or library base changed
		public void UpdateActiveLVOInterceptors(Dictionary<string, uint> libraryBaseAddresses)
		{
			activeLvoInterceptors.Clear();
			foreach (var interceptor in lvoInterceptors)
			{
				if (libraryBaseAddresses.ContainsKey(interceptor.Library))
				{
					uint address = memory.UnsafeRead32((uint)(libraryBaseAddresses[interceptor.Library] + interceptor.LVO.Offset + 2));
					activeLvoInterceptors[address] = interceptor;
				}
			}
		}

		public Dictionary<uint, LVOInterceptor> ActiveLVOInterceptors => activeLvoInterceptors;
	}

	public class LVOInterceptors : ILVOInterceptors
	{
		private readonly ILVOInterceptorCollection lvoInterceptorCollection;
		private readonly IAnalysis analysis;

		public LVOInterceptors(IEnumerable<ILVOInterceptorAction> actions,
			ILVOInterceptorCollection lvoInterceptorCollection,	IAnalysis analysis)
		{
			
			this.lvoInterceptorCollection = lvoInterceptorCollection;
			this.analysis = analysis;
			foreach (var act in actions)
				AddLVOIntercept(act.Library, act.VectorName, act);
		}

		private void AddLVOIntercept(string library, string vectorName, ILVOInterceptorAction action)
		{
			var lvos = analysis.GetLVOs();
			if (lvos.TryGetValue(library, out var lib))
			{
				var vector = lib.LVOs.SingleOrDefault(x => x.Name == vectorName);
				if (vector != null)
				{
					lvoInterceptorCollection.AddInterceptor(new LVOInterceptor
					{
						Library = library,
						LVO = vector,
						Action = action
					});
				}
			}
		}

		public void CheckLVOAccess(uint pc, Size size)
		{
			if (size == Size.Word)
			{
				if (!lvoInterceptorCollection.ActiveLVOInterceptors.TryGetValue(pc, out var lvo))
					return;

				lvo.Action.Intercept(lvo.LVO, pc);
			}
		}
	}

	public interface ISnagger
	{
		bool IsHit(IDebugMemoryMapper memoryMapper, uint pc, uint sp);
		void Act();
	}

	public class RtsSnagger : ISnagger
	{
		private Action act;
		private uint sp;
		public RtsSnagger(Action action, uint sp)
		{
			this.act = action;
			this.sp = sp;
		}

		public bool IsHit(IDebugMemoryMapper memoryMapper, uint pc, uint sp)
		{
			ushort ins = memoryMapper.UnsafeRead16(pc);
			if (ins != 0x4e75 && ins != 0x4e73) return false;//rts or rte
			return sp == this.sp;
		}

		public void Act()
		{
			act();
		}
	}

	public class ReturnAddressSnagger :ISnagger
	{
		private Action act;
		private uint address;

		public ReturnAddressSnagger(Action action, uint address)
		{
			this.act = action;
			this.address = address;
		}

		public bool IsHit(IDebugMemoryMapper memoryMapper, uint pc, uint sp)
		{
			return pc == address;
		}

		public void Act()
		{
			act();
		}
	}

	public interface IReturnValueSnagger
	{
		void AddSnagger(ISnagger snagger);
		void CheckSnaggers(uint pc, uint sp);
	}

	public class ReturnValueSnagger : IReturnValueSnagger
	{
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memoryMapper;
		private readonly List<ISnagger> snaggers = new List<ISnagger>();

		public ReturnValueSnagger(ICPU cpu, IDebugMemoryMapper memoryMapper)
		{
			this.cpu = cpu;
			this.memoryMapper = memoryMapper;
		}

		public void AddSnagger(ISnagger snagger)
		{
			snaggers.Insert(0, snagger);
		}

		public void CheckSnaggers(uint pc, uint sp)
		{
			if (snaggers.Count == 0) return;

			int i = 0;
			foreach (var snagger in snaggers)
			{
				if (snagger.IsHit(memoryMapper, pc, sp))
				{
					snagger.Act();
					snaggers.RemoveAt(i);
					break;
				}
				i++;
			}
		}
	}
}
