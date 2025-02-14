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

	public class OpenScreenTagListLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public OpenScreenTagListLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<OpenScreenTagListLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "intuition.library";
		public string VectorName => "OpenScreenTagList";
		private Regs gregs = new Regs();

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() : {regs.A[1]:X8}");

			var tags = regs.A[1];
			if (tags != 0)
			for (; ; )
			{
				var tag = memory.UnsafeRead32(tags); tags += 4;
				if (tag == 0) break;
				var tagValue = memory.UnsafeRead32(tags); tags += 4;

				string extra = "";
				if (tag == 0x80000047) extra = "SA_LikeWorkbench";
				else if (tag == 0x8000002D) extra = "SA_Type";
				else if (tag == 0x80000028) extra = $"SA_Title {memory.GetString(tagValue)}";
				else if (tag == 0x8000002F) extra = $"SA_PubName {memory.GetString(tagValue)}";
				else if (tag == 0x80000021) extra = "SA_Left";
				else if (tag == 0x80000022) extra = "SA_Top";
				else if (tag == 0x80000023) extra = "SA_Width";
				else if (tag == 0x80000024) extra = "SA_Height";
				else if (tag == 0x80000025) extra = "SA_Depth";
				else if (tag == 0x80000029)
				{
					extra = "SA_Colors";
					var cspec = tagValue;
					for (; ; )
					{
						var idx = memory.UnsafeRead16(cspec); cspec += 2;
						if (idx == 0xffff) break;
						var col = memory.UnsafeRead16(cspec); cspec += 2;
						extra += $"\n{idx:X4} {col:X4}";
					}
					logger.LogTrace("\n");
				}
				else if (tag == 0x80000043)
				{
					extra = "SA_Colors32";
					var cspec = tagValue;
					for (; ; )
					{
						var count = memory.UnsafeRead16(cspec); cspec += 2;
						if (count == 0) break;
						extra += $"\n{count}";
						var idx = memory.UnsafeRead16(cspec); cspec += 2;
						for (int i = 0; i < count; i++)
						{
							var r = memory.UnsafeRead32(cspec); cspec += 4;
							var g = memory.UnsafeRead32(cspec); cspec += 4;
							var b = memory.UnsafeRead32(cspec); cspec += 4;
							extra += $"\n{idx + i:X4} {r:X8} {g:X8} {b:X8}";
						}
						logger.LogTrace("\n");
					}
				}

				logger.LogTrace($"\t{tag:X8} {tagValue:X8} {extra}");
			}

			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs(gregs);
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
			}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class OpenScreenLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public OpenScreenLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
		ILibraryBases libraryBases, ILogger<OpenScreenLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "intuition.library";
		public string VectorName => "OpenScreen";
		private Regs gregs = new Regs();

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() : {regs.A[0]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs(gregs);
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
			}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class LoadRGB32Logger : LVOLoggerBase, ILVOInterceptorAction
	{
		public LoadRGB32Logger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
		ILibraryBases libraryBases, ILogger<LoadRGB32Logger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "graphics.library";
		public string VectorName => "LoadRGB32";
		private Regs gregs = new Regs();

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() : {regs.A[0]:X8} {regs.A[1]:X8}");

			var cspec = regs.A[1];
			if (cspec != 0)
			for (; ; )
			{
				var count = memory.UnsafeRead16(cspec); cspec += 2;
				if (count == 0) break;
				logger.LogTrace($"{count}");
				var idx = memory.UnsafeRead16(cspec); cspec += 2;
				for (int i = 0; i < count; i++)
				{
					var r = memory.UnsafeRead32(cspec); cspec += 4;
					var g = memory.UnsafeRead32(cspec); cspec += 4;
					var b = memory.UnsafeRead32(cspec); cspec += 4;
					var rgb = ((r>>(28-8))&0xf00)| ((g >> (28 - 4)) & 0xf0)| ((b >> 28 ) & 0xf);
					logger.LogTrace($"{idx + i:X4} {r:X8} {g:X8} {b:X8} #{rgb:X3}");
				}
			}
		}
	}

	public class LoadRGB4Logger : LVOLoggerBase, ILVOInterceptorAction
	{
		public LoadRGB4Logger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
		ILibraryBases libraryBases, ILogger<LoadRGB4Logger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "graphics.library";
		public string VectorName => "LoadRGB4";
		private Regs gregs = new Regs();

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() : {regs.A[0]:X8} {regs.A[1]:X8} {regs.D[0]&0xffff:X4}");

			var rgb = regs.A[1];
			logger.LogTrace($"{regs.D[0]&0xffff}");
			for (int i = 0; i < (regs.D[0]&0xffff); i++)
				logger.LogTrace($"{memory.UnsafeRead16(rgb):X4}\n"); rgb += 2;
		}
	}

	public class GetRGB4Logger : LVOLoggerBase, ILVOInterceptorAction
	{
		public GetRGB4Logger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
		ILibraryBases libraryBases, ILogger<GetRGB4Logger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "graphics.library";
		public string VectorName => "GetRGB4";
		private Regs gregs = new Regs();

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() : {regs.A[0]:X8} {regs.D[0]:X8}");
		}
	}

	public class GetRGB32Logger : LVOLoggerBase, ILVOInterceptorAction
	{
		public GetRGB32Logger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
		ILibraryBases libraryBases, ILogger<GetRGB32Logger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "graphics.library";
		public string VectorName => "GetRGB32";
		private Regs gregs = new Regs();

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs(gregs);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() : {regs.A[0]:X8} {regs.D[0]:X8} {regs.D[1]:X8} {regs.A[1]:X8} ");
		}
	}
}
