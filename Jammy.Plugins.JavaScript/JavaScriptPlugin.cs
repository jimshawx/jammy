using Jammy.Interface;
using Jammy.Plugins.Interface;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript
{
	public class JsConsole
	{
		private readonly ILogger logger;

		public JsConsole(ILogger logger)
		{
			this.logger = logger;
		}

		public void log(object o) => logger.LogTrace($"{o?.ToString()}");

		public void assert() { }
		//Log an error message to console if the first argument is false.

		public void clear() { }
		//Clear the console

		public void count() { }
		//Log the number of times this line has been called with the given label.

		public void countReset() { }
		//Resets the value of the counter with the given label.

		public void debug() { }
		//Outputs a message to the console with the debug log level.

		public void dir() { }
		//Displays an interactive listing of the properties of a specified JavaScript object. This listing lets you use disclosure triangles to examine the contents of child objects.

		public void dirxml() { }
		//Displays an XML/HTML Element representation of the specified object if possible or the JavaScript Object view if it is not possible.

		public void error() { }
		//Outputs a message to the console with the error log level.

		public void exception() { } //Non-standard Deprecated
		//An alias for public void error().

		public void group() { }
		//Creates a new inline group, indenting all following output by another level.To move back out a level, call public void groupEnd().

		public void groupCollapsed() { }
		//Creates a new inline group, indenting all following output by another level.However, unlike public void group() this starts with the inline group collapsed requiring the use of a disclosure button to expand it. To move back out a level, call public void groupEnd().

		public void groupEnd() { }
		//Exits the current inline group.

		public void info() { }
		//Outputs a message to the console with the info log level.

		public void log() { }
		//Outputs a message to the public void 

		public void profile() { } //Non-standard
		//Starts the browser's built-in profiler (for example, the Firefox performance tool). You can specify an optional name for the profile.

		public void profileEnd() { } //Non-standard
		//Stops the profiler. You can see the resulting profile in the browser's performance tool (for example, the Firefox performance tool).

		public void table() { }
		//Displays tabular data as a table.

		public void time() { }
		//Starts a timer with a name specified as an input parameter.Up to 10,000 simultaneous timers can run on a given page.

		public void timeEnd() { }
		//Stops the specified timer and logs the elapsed time in milliseconds since it started.

		public void timeLog() { }
		//Logs the value of the specified timer to the public void 

		public void timeStamp() { } //Non-standard
		//Adds a marker to the browser performance tool's timeline (Chrome or Firefox).

		public void trace() { }
		//Outputs a stack trace.

		public void warn() { }
		//Outputs a message to the console with the warning log level.
	}

	public class JavaScriptEngine : IPluginEngine
	{
		private readonly IDebugger debugger;
		private readonly ILogger<JavaScriptEngine> logger;
		private static object imguiApi = ImGuiAPI.Instance;

		public JavaScriptEngine(IDebugger debugger, ILogger<JavaScriptEngine> logger)
		{
			this.debugger = debugger;
			this.logger = logger;

			//log the methods we are proxying
			var sb = new StringBuilder();
			foreach (var m in imguiApi.GetType().GetMethods(
					BindingFlags.Public |
					BindingFlags.Instance |
					BindingFlags.DeclaredOnly))
			{
				sb.AppendLine(m.ToString());
			}
			Trace.Write(sb.ToString());
		}

		public IPlugin NewPlugin(string code)
		{
			var engine = new Engine(cfg => cfg.AllowClr());

			engine.SetValue("console", new JsConsole(logger));

			engine.SetValue("imgui", imguiApi);
			engine.SetValue("jammy", debugger);

			engine.Execute(code);
			return new JavaScriptPlugin(engine);
		}
	}

	public class JavaScriptPlugin : IPlugin
    {
		private readonly Engine engine;

		public JavaScriptPlugin(Engine engine)
		{
			this.engine = engine;
			var initFn = engine.GetValue("init");
			if (initFn is ScriptFunction)
				initFn.Call(JsValue.Undefined);
		}

		public void Render()
		{
			var updateFn = engine.GetValue("update");
			if (updateFn is ScriptFunction)
				updateFn.Call(JsValue.Undefined);
		}
	}
}
