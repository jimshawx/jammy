using Jammy.Interface;
using Jammy.Types.Debugger;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Net.Http;

namespace Jammy.WebAPI
{
	public class UrlPathAttribute: Attribute
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
		private readonly ILogger<DebuggerHandlers> logger;

		public DebuggerHandlers(IDebugger debugger, ILogger<DebuggerHandlers> logger)
		{
			this.debugger = debugger;
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

		[UrlAction("PUT","testput")]
		public void TestPut(string s)
		{
			Trace.WriteLine($"Test called with {s}", s);
		}

		[UrlAction("GET", "testget")]
		public string TestGet()
		{
			return "Hello";
		}
	}
}
