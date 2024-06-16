using Jammy.Core.Interface.Interfaces;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Jammy.Debugger.Interceptors
{
	public interface IOpenFileTracker
	{
		public void Open(uint handle, string name);
		public void Close(uint handle);
		public string GetFileName(uint handle);
	}

	public class OpenFileTracker : IOpenFileTracker
	{
		private Dictionary<uint, string> openFiles = new Dictionary<uint, string>();

		public void Open(uint handle, string name)
		{
			openFiles[handle] = name;
		}

		public void Close(uint handle)
		{
			if (openFiles.ContainsKey(handle))
				openFiles.Remove(handle);
		}

		public string GetFileName(uint handle)
		{
			if (!openFiles.ContainsKey(handle)) return "unknown";
			return openFiles[handle];
		}
	}

	public class ReadLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private readonly IOpenFileTracker fileTracker;

		public ReadLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IOpenFileTracker fileTracker, ILogger<ReadLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.fileTracker = fileTracker;
		}

		public string Library => "dos.library";
		public string VectorName => "Read";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() file: {fileTracker.GetFileName(regs.D[1])}:{regs.D[1]:X8} buffer: {regs.D[2]:X8} length: {regs.D[3]:X8}");
		}
	}

	public class OpenLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private readonly IOpenFileTracker fileTracker;

		public OpenLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IOpenFileTracker fileTracker, ILogger<OpenLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.fileTracker = fileTracker;
		}

		public string Library => "dos.library";
		public string VectorName => "Open";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			string filename = memory.GetString(regs.D[1]);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() name:{filename}:{regs.D[1]:X8} flags: {regs.D[2]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs();
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
				fileTracker.Open(regs.D[0], filename);
			}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class CloseLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private readonly IOpenFileTracker fileTracker;

		public CloseLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IOpenFileTracker fileTracker, ILogger<CloseLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.fileTracker = fileTracker;
		}

		public string Library => "dos.library";
		public string VectorName => "Close";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() file: file: {fileTracker.GetFileName(regs.D[1])}:{regs.D[1]:X8}");
			fileTracker.Close(regs.D[1]);
		}
	}
}
