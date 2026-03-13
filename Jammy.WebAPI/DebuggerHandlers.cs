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
		public UrlActionAttribute(string action, string path, string summary = null, uint version = 1)
		{
			Action = action;
			Path = path;
			Summary = summary;
			Ver = version;
		}
		public string Action { get; }
		public string Path { get; }
		public string Summary { get; }
		public uint Ver { get; }
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

		[UrlAction("GET", "memory", "Get memory contents")]
		public MemoryContent GetMemoryContent()
		{
			debugger.LockEmulation();
			var mem = debugger.GetMemoryContent();
			debugger.UnlockEmulation();
			return mem;
		}

		[UrlAction("POST", "emucontrol", "Set the emulation state go/stop/step/stepout")]
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

		[UrlAction("GET", "clock", "Get the clock")]
		public ClockInfo GetClockInfo()
		{
			return debugger.GetChipClock();
		}

		[UrlAction("GET", "vectors", "Get the exception vectors")]
		public Vectors GetVectors()
		{
			return debugger.GetVectors();
		}

		[UrlAction("GET", "libraries", "Get the libraries that have been loaded")]
		public Libraries GetLibraries()
		{
			return debugger.GetLibraries();
		}

		[UrlAction("GET", "allocations", "Get the memory allocations")]
		public MemoryAllocations GetAllocations()
		{
			return debugger.GetAllocations();
		}

		[UrlAction("GET", "chipregs", "Get the current chip register values")]
		public ChipState GetChipRegs()
		{
			return debugger.GetChipRegs();
		}

		[UrlAction("GET", "copper", "Get a disassembly of the current copper list")]
		public string GetCopperDisassembly()
		{
			return debugger.GetCopperDisassembly();
		}

		[UrlAction("GET", "regs", "Get the CPU registers")]
		public Regs GetRegs()
		{
			return debugger.GetRegs();
		}

		[UrlAction("POST", "command", "Run a command")]
		public void RunCommand(string command)
		{
			debugCommand.ProcessCommand(command);
		}
	}
}
