using Jammy.Core.Interface.Interfaces;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System;

namespace Jammy.Debugger.Interceptors
{
	public class AllocMemLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public AllocMemLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<AllocMemLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
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

			MEMF_NO_EXPUNGE = (1 << 31), /*AllocMem: Do not cause expunge on failure */
		}

		public string Library => "exec.library";
		public string VectorName => "AllocMem";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() size: {regs.D[0]:X8} flags: {regs.D[1]:X8} {(MEMF)regs.D[1]}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs();
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8} {(regs.D[0]==0?"*** OUT OF MEMORY ***":"")}");
			}, memory.UnsafeRead32(regs.SP)));
		}
	}
}
