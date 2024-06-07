using Jammy.Types.Kickstart;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Jammy.Core.Types.Types;
using Jammy.Core.Interface.Interfaces;
using Jammy.Interface;

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

		public LVOLoggerBase(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser, ILibraryBases libraryBases, ILogger logger)
		{
			this.cpu = cpu;
			this.memory = memory;
			this.returnValueSnagger = returnValueSnagger;
			this.analyser = analyser;
			this.libraryBases = libraryBases;
			this.logger = logger;
		}
	}
	public class FReadLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public FReadLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser, ILibraryBases libraryBases, ILogger<FReadLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "dos.library";
		public string VectorName => "FRead";

		public void Intercept(LVO lvo)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{memory.UnsafeRead32(regs.SP),-4:X8} {lvo.Name}() {regs.D[0]:X8} {regs.D[1]:X8} {regs.D[2]:X8} {regs.D[3]:X8}");
		}
	}

	public class AllocMemLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public AllocMemLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser, ILibraryBases libraryBases, ILogger<AllocMemLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		[Flags]
		enum MEMF
		{
			MEMF_ANY = (0),    /* Any type of memory will do */
			MEMF_PUBLIC = (1 << 0),
			MEMF_CHIP = (1 << 1),
			MEMF_FAST = (1 << 2),
			MEMF_LOCAL = (1 << 8), /* Memory that does not go away at RESET */
			MEMF_24BITDMA = (1 << 9),  /* DMAable memory within 24 bits of address */
			MEMF_KICK = (1 << 10), /* Memory that can be used for KickTags */

			MEMF_CLEAR = (1 << 16),    /* AllocMem: NULL out area before return */
			MEMF_LARGEST = (1 << 17),  /* AvailMem: return the largest chunk size */
			MEMF_REVERSE = (1 << 18),  /* AllocMem: allocate from the top down */
			MEMF_TOTAL = (1 << 19),    /* AvailMem: return total size of memory */

			MEMF_NO_EXPUNGE = (1<<31), /*AllocMem: Do not cause expunge on failure */
		}

		public string Library => "exec.library";
		public string VectorName => "AllocMem";

		public void Intercept(LVO lvo)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{memory.UnsafeRead32(regs.SP),-4:X8} {lvo.Name}() size: {regs.D[0]:X8} flags: {regs.D[1]:X8} {(MEMF)regs.D[1]}");
			returnValueSnagger.AddSnagger(ReturnValue);
		}

		private void ReturnValue()
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"returned: {regs.D[0]:X8}");
		}
	}

	public class OpenLibraryLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public OpenLibraryLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser, ILibraryBases libraryBases, ILogger<OpenLibraryLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "exec.library";
		public string VectorName => "OpenLibrary";

		public void Intercept(LVO lvo)
		{
			var regs = cpu.GetRegs();
			string libraryName = memory.GetString(regs.A[1]);
			logger.LogTrace($"@{memory.UnsafeRead32(regs.SP),-4:X8} {lvo.Name}() libname {regs.A[1]:X8} {libraryName} version: {regs.D[0]:X8}");
			returnValueSnagger.AddSnagger(() =>
			{
				var regs = cpu.GetRegs();
				logger.LogTrace($"{libraryName} {regs.D[0]:X8}");
				libraryBases.SetLibraryBaseaddress(libraryName, regs.D[0]);
			});
		}
	}

	public class OpenResourceLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public OpenResourceLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser, ILibraryBases libraryBases, ILogger<OpenResourceLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}
		
		public string Library => "exec.library";
		public string VectorName => "OpenResource";

		public void Intercept(LVO lvo)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"{lvo.Name}() resName: {regs.A[1]:X8} {memory.GetString(regs.A[1])}");
			returnValueSnagger.AddSnagger(ReturnValue);
		}

		private void ReturnValue()
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"returned: {regs.D[0]:X8}");
		}
	}

	public class MakeLibraryLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private HashSet<uint> librariesMade = new HashSet<uint>();

		public MakeLibraryLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser, ILibraryBases libraryBases, ILogger<MakeLibraryLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}
		
		public string Library => "exec.library";
		public string VectorName => "MakeLibrary";

		public void Intercept(LVO lvo)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"{lvo.Name}() vectors: {regs.A[0]:X8} structure: {regs.A[1]:X8} init: {regs.A[2]:X8} dataSize: {regs.D[0]:X8} segList: {regs.D[1]:X8}");

			if (!librariesMade.Contains(regs.A[0]))
			{
				librariesMade.Add(regs.A[0]);
				analyser.ExtractFunctionTable(regs.A[0], NT_Type.NT_LIBRARY, $"unknown_{regs.A[0]}");
				analyser.ExtractStructureInit(regs.A[1]);
			}
			returnValueSnagger.AddSnagger(ReturnValue);
		}


		private void ReturnValue()
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"returned: {regs.D[0]:X8}");
		}
	}

	public interface ILVOInterceptorAction
	{
		public void Intercept(LVO lvo);

		public string Library { get; }
		public string VectorName { get; }
	}

	public interface ILibraryBases
	{
		Dictionary<string, uint> Addresses { get; }
		void SetLibraryBaseaddress(string libraryName, uint address);
	}

	public class LibraryBases : ILibraryBases
	{
		private Dictionary<string, uint> libraryBaseAddresses = new Dictionary<string, uint>();

		public LibraryBases(IDebugMemoryMapper memory)
		{
			SetLibraryBaseaddress("exec.library", memory.UnsafeRead32(4));
		}

		public Dictionary<string, uint> Addresses => libraryBaseAddresses;

		public void SetLibraryBaseaddress(string libraryName, uint address)
		{
			if (address == 0) return;
			libraryBaseAddresses[libraryName] = address;
		}
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

	public class LVOInterceptors : ILVOInterceptors
	{
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memory;
		private readonly IAnalysis analysis;
		private readonly ILibraryBases libraryBases;

		private List<LVOInterceptor> lvoInterceptors { get; } = new List<LVOInterceptor>();

		public LVOInterceptors(IEnumerable<ILVOInterceptorAction> actions, ICPU cpu, IDebugMemoryMapper memory, IAnalysis analysis, ILibraryBases libraryBases)
		{
			this.cpu = cpu;
			this.memory = memory;
			this.analysis = analysis;
			this.libraryBases = libraryBases;
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
					lvoInterceptors.Add(new LVOInterceptor
					{
						Library = library,
						LVO = vector,
						Action = action
					});
				}
			}
		}

		public void CheckLVOAccess(uint address, Size size)
		{
			if (size == Size.Word)
			{
				var lvo = lvoInterceptors
					.Where(x => libraryBases.Addresses.ContainsKey(x.Library))
					.SingleOrDefault(x => memory.UnsafeRead32((uint)(libraryBases.Addresses[x.Library] + x.LVO.Offset + 2)) == address);
				if (lvo != null)
				{
					//breakpoints.SignalBreakpoint(address);
					lvo.Action.Intercept(lvo.LVO);
				}
			}
		}
	}

	public interface IReturnValueSnagger
	{
		void AddSnagger(Action snagAction);
		void CheckSnaggers();
	}

	public class ReturnValueSnagger : IReturnValueSnagger
	{
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memoryMapper;
		private readonly Dictionary<uint, List<Action>> snaggers = new Dictionary<uint, List<Action>>();

		public ReturnValueSnagger(ICPU cpu, IDebugMemoryMapper memoryMapper)
		{
			this.cpu = cpu;
			this.memoryMapper = memoryMapper;
		}

		public void AddSnagger(Action snagAction)
		{
			var regs = cpu.GetRegs();
			if (!snaggers.ContainsKey(regs.A[7]))
				snaggers.Add(regs.A[7], new List<Action> { snagAction });
			else
				snaggers[regs.A[7]].Add(snagAction);
		}

		public void CheckSnaggers()
		{
			if (snaggers.Count == 0) return;

			var regs = cpu.GetRegs();
			ushort ins = memoryMapper.UnsafeRead16(regs.PC);
			if (ins != 0x4e75 && ins != 0x4e73) return;//rts or rte

			foreach (var snagger in snaggers)
			{
				if (regs.A[7] >= snagger.Key)
				{
					foreach (var act in snagger.Value)
						act();
					snaggers.Remove(snagger.Key);
					return;
				}
			}
		}
	}
}
