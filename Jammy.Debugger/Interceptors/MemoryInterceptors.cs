using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Disassembler;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Debugger.Interceptors
{
	[Flags]
	public enum MEMF
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

		MEMF_NO_EXPUNGE = (1 << 31), /*AllocMem: Do not cause expunge on failure */
	}

	public interface IAllocatedMemoryTracker
	{
		void Free(uint address, uint size);
		void Allocate(uint address, uint size, MEMF type);
	}

	public class AllocatedMemoryTracker : IAllocatedMemoryTracker
	{
		private const uint MIN_LOGGED_SIZE = 0;

		private readonly Dictionary<uint, (uint size, MEMF type)> allocations = new Dictionary<uint, (uint size, MEMF type)>();
		private readonly IAnalysis analysis;
		private readonly IDisassemblyRanges disassemblyRanges;
		private readonly ILogger<AllocatedMemoryTracker> logger;

		public AllocatedMemoryTracker(IAnalysis analysis,
			IDisassemblyRanges disassemblyRanges,
			ILogger<AllocatedMemoryTracker> logger)
		{
			this.analysis = analysis;
			this.disassemblyRanges = disassemblyRanges;
			this.logger = logger;
		}

		public void Allocate(uint address, uint size, MEMF type)
		{
			if (size < MIN_LOGGED_SIZE) return;

			if (allocations.TryGetValue(address, out var existing))
			{	
				logger.LogTrace($"*** Realloc {address:X8} {existing.size:X8} {existing.type} -> {size:X8} {type}");
				allocations[address] = (size,type);
				return;
			}
			allocations.Add(address, (size,type));
			//analysis.SetMemType(address, size, MemType.Byte);
			disassemblyRanges.Add(address, size);
		}

		public void Free(uint address, uint size)
		{
			if (size < MIN_LOGGED_SIZE) return;

			if (size == 0) 
			{
				logger.LogTrace($"*** FreeMem({address:X8}, 0) ignored");
				return;
			}

			if (allocations.TryGetValue(address, out var alloc))
			{
				if (alloc.size != size)
				{ 
					logger.LogTrace($"*** FreeMem({address:X8}, {size:X8}) size mismatch ({alloc.size:X8})");
					//retain the un-freed part
					allocations.Add(address + size, (alloc.size - size, alloc.type));
				}

				allocations.Remove(address);
			}
			else
			{
				logger.LogTrace($"*** FreeMem({address:X8}, {size:X8}) not allocated");
			}
		}
	}

	public class AllocMemLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public AllocMemLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IAllocatedMemoryTracker allocatedMemoryTracker, ILogger<AllocMemLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.allocatedMemoryTracker = allocatedMemoryTracker;
		}

		public string Library => "exec.library";
		public string VectorName => "AllocMem";
		private Regs gregs = new Regs();
		private readonly IAllocatedMemoryTracker allocatedMemoryTracker;

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() size: {regs.D[0]:X8} flags: {regs.D[1]:X8} {(MEMF)regs.D[1]}");
			uint size = regs.D[0];
			MEMF type = (MEMF)regs.D[1];
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs(gregs);
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8} {(regs.D[0]==0?"*** OUT OF MEMORY ***":"")}");
				allocatedMemoryTracker.Allocate(regs.D[0], size, type);
			}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class FreeMemLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public FreeMemLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IAllocatedMemoryTracker allocatedMemoryTracker, ILogger<FreeMemLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.allocatedMemoryTracker = allocatedMemoryTracker;
		}

		public string Library => "exec.library";
		public string VectorName => "FreeMem";
		private Regs gregs = new Regs();
		private readonly IAllocatedMemoryTracker allocatedMemoryTracker;

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() address: {regs.A[1]:X8} size: {regs.D[0]:X8}");
			allocatedMemoryTracker.Free(regs.A[1], regs.D[0]);
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs(gregs);
				logger.LogTrace($"{lvo.Name} returned");
			}, memory.UnsafeRead32(regs.SP)));
		}
	}
}
