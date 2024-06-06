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
	public class AllocMemLogger : ILVOInterceptorAction
	{
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
		}

		public void Intercept(LVO lvo, ICPU cpu, IDebugMemoryMapper memory, IAnalyser analyser, ILogger logger)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{memory.UnsafeRead32(regs.SP),-4:X8} {lvo.Name}(), size: {regs.D[0]:X8} flags: {regs.D[1]:X8} {(MEMF)regs.D[1]}");
		}
	}

	public class OpenLibraryLogger : ILVOInterceptorAction
	{
		public void Intercept(LVO lvo, ICPU cpu, IDebugMemoryMapper memory, IAnalyser analyser, ILogger logger)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{memory.UnsafeRead32(regs.SP),-4:X8} {lvo.Name}(), libname {regs.A[1]:X8} {memory.GetString(regs.A[1])} version: {regs.D[0]:X8}");
		}
	}

	public class OpenResourceLogger : ILVOInterceptorAction
	{
		public void Intercept(LVO lvo, ICPU cpu, IDebugMemoryMapper memory, IAnalyser analyser, ILogger logger)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"{lvo.Name}() resName: {regs.A[1]:X8} {memory.GetString(regs.A[1])}");
		}
	}

	public class MakeLibraryLogger : ILVOInterceptorAction
	{
		private HashSet<uint> librariesMade = new HashSet<uint>();

		public void Intercept(LVO lvo, ICPU cpu, IDebugMemoryMapper memory, IAnalyser analyser, ILogger logger)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"{lvo.Name}() vectors: {regs.A[0]:X8} structure: {regs.A[1]:X8} init: {regs.A[2]:X8} dataSize: {regs.D[0]:X8} segList: {regs.D[1]:X8}");

			if (!librariesMade.Contains(regs.A[0]))
			{
				librariesMade.Add(regs.A[0]);
				analyser.ExtractFunctionTable(regs.A[0], NT_Type.NT_LIBRARY, $"unknown_{regs.A[0]}");
				analyser.ExtractStructureInit(regs.A[1]);
			}
		}
	}

	public interface ILVOInterceptorAction
	{
		public void Intercept(LVO lvo, ICPU cpu, IDebugMemoryMapper memory, IAnalyser analyser, ILogger logger);
	}

	public class LVOInterceptor
	{
		public string Library { get; set; }
		public LVO LVO { get; set; }
		public ILVOInterceptorAction Action { get; set; }
	}

	public class LVOInterceptors
	{
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memory;
		private readonly IAnalyser analyser;
		private readonly IAnalysis analysis;
		private readonly ILogger logger;

		private Dictionary<string, uint> libraryBaseAddresses = new Dictionary<string, uint>();
		private List<LVOInterceptor> lvoInterceptors { get; } = new List<LVOInterceptor>();

		public LVOInterceptors(ICPU cpu, IDebugMemoryMapper memory, IAnalyser analyser, IAnalysis analysis, ILogger logger)
		{
			this.cpu = cpu;
			this.memory = memory;
			this.analyser = analyser;
			this.analysis = analysis;
			this.logger = logger;
		}

		public void SetLibraryBaseaddress(string libraryName, uint address)
		{
			libraryBaseAddresses[libraryName] = address;
		}

		public void AddLVOIntercept(string library, string vectorName, ILVOInterceptorAction action)
		{
			SetLibraryBaseaddress( "exec.library", memory.UnsafeRead32(4));

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
				var lvo = lvoInterceptors.SingleOrDefault(x => memory.UnsafeRead32((uint)(libraryBaseAddresses[x.Library] + x.LVO.Offset + 2)) == address);
				if (lvo != null)
				{
					//breakpoints.SignalBreakpoint(address);
					lvo.Action.Intercept(lvo.LVO, cpu, memory, analyser, logger);
				}
			}
		}
	}
}
