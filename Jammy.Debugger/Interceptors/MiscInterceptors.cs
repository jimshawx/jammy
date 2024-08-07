using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

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
		private Regs gregs = new Regs();

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() name:{memory.GetString(regs.D[1])}:{regs.D[1]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs(gregs);
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
			}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class InternalLoadSegLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private readonly IOpenFileTracker openFileTracker;

		public InternalLoadSegLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IOpenFileTracker openFileTracker, ILogger<InternalLoadSegLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.openFileTracker = openFileTracker;
		}

		public string Library => "dos.library";
		public string VectorName => "InternalLoadSeg";
		private Regs gregs = new Regs();

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() name:{openFileTracker.GetFileName(regs.D[0])}:{regs.D[0]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs(gregs);
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
			}, memory.UnsafeRead32(regs.SP)));
		}
	}
}
