using Jammy.Core.Interface.Interfaces;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;

namespace Jammy.Debugger.Interceptors
{
	public class LoadSegLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public LoadSegLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<LoadSegLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "dos.library";
		public string VectorName => "LoadSeg";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() name:{memory.GetString(regs.D[1])}:{regs.D[1]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs();
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
			}, memory.UnsafeRead32(regs.SP)));
		}
	}
}
