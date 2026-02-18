using Jammy.Core.Types;
using Jammy.Interface;
using Jammy.Types;
using Jammy.Types.Debugger;
using Microsoft.Extensions.Logging;
using System;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.WebAPI
{
	public class UrlPathAttribute : Attribute
	{
		public UrlPathAttribute(string path)
		{
			Path = path;
		}

		public string Path { get; }
	}

	public class UrlActionAttribute : Attribute
	{
		public UrlActionAttribute(string action, string path)
		{
			Action = action;
			Path = path;
		}
		public string Action { get; }
		public string Path { get; }
	}

	[UrlPath("debugger")]
	public class DebuggerHandlers
	{
		private readonly IDebugger debugger;
		private readonly IDebugCommand debugCommand;
		private readonly ILogger<DebuggerHandlers> logger;

		public DebuggerHandlers(IDebugger debugger, IDebugCommand debugCommand,
			ILogger<DebuggerHandlers> logger)
		{
			this.debugger = debugger;
			this.debugCommand = debugCommand;
			this.logger = logger;
		}

		[UrlAction("GET", "memory")]
		public MemoryContent GetMemoryContent()
		{
			debugger.LockEmulation();
			var mem = debugger.GetMemoryContent();
			debugger.UnlockEmulation();
			return mem;
		}

		[UrlAction("POST", "emucontrol")]
		public void EmuControl(string command)
		{
			if (command == "go")
				debugger.Go();
			else if (command == "stop")
				debugger.Stop();
			else if (command == "step")
				debugger.Step();
			else if (command == "stepout")
				debugger.StepOut();
		}

		[UrlAction("GET", "clock")]
		public ClockInfo GetClockInfo()
		{
			return debugger.GetChipClock();
		}

		[UrlAction("GET", "vectors")]
		public Vectors GetVectors()
		{
			return debugger.GetVectors();
		}

		[UrlAction("GET", "libraries")]
		public Libraries GetLibraries()
		{
			return debugger.GetLibraries();
		}

		[UrlAction("GET", "allocations")]
		public MemoryAllocations GetAllocations()
		{
			return debugger.GetAllocations();
		}

		[UrlAction("GET", "chipregs")]
		public ChipState GetChipRegs()
		{
			return debugger.GetChipRegs();
		}

		[UrlAction("GET", "copper")]
		public string GetCopperDisassembly()
		{
			return debugger.GetCopperDisassembly();
		}

		[UrlAction("GET", "regs")]
		public Regs GetRegs()
		{
			return debugger.GetRegs();
		}

		[UrlAction("POST", "command")]
		public void RunCommand(string command)
		{
			debugCommand.ProcessCommand(command);
		}
	}
}
