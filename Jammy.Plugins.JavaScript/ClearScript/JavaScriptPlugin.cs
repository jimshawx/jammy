using Jammy.Interface;
using Jammy.Plugins.Interface;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Text;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript.ClearScript
{
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
			//var sb = new StringBuilder();
			//foreach (var m in imguiApi.GetType().GetMethods(
			//		BindingFlags.Public |
			//		BindingFlags.Instance |
			//		BindingFlags.DeclaredOnly))
			//{
			//	sb.AppendLine(m.ToString());
			//}
			//Trace.Write(sb.ToString());
		}

		public bool SupportsExtension(string ext)
		{
			return ext == ".js";
		}

		public IPlugin NewPlugin(string code)
		{
			var engine = new V8ScriptEngine();

			engine.AddHostObject("console", new JsConsole(logger));

			engine.AddHostObject("imgui", imguiApi);
			engine.AddHostObject("jammy", debugger);
			engine.AddHostType("Vec2", typeof(Vector2));
			engine.AllowReflection = true;

			try
			{ 
				engine.Execute(code);
			}
			catch (ScriptEngineException ex)
			{
				logger.LogError($"JavaScript Error:\n{ex}");
				return null;
			}

			return new JavaScriptPlugin(engine, logger);
		}
	}

	public class JavaScriptPlugin : IPlugin
    {
		private readonly V8ScriptEngine engine;
		private readonly ILogger logger;
		private bool scriptIsBroken = false;
		private Dictionary<string, object> properties = new Dictionary<string, object>();

		public JavaScriptPlugin(V8ScriptEngine engine, ILogger logger)
		{
			this.engine = engine;
			this.logger = logger;
			//foreach (var property in )
			ExecuteFn("init");
		}

		public void Render()
		{
			ExecuteFn("update");
		}

		private void ExecuteFn(string fnName)
		{
			if (scriptIsBroken) return;

			try
			{
				var fn = ((ScriptObject)engine.Script).GetProperty(fnName);
				if (fn == Undefined.Value) return;
				if (!(fn is ScriptObject)) return;//still not enough, might not be a function
				
				engine.Script[fnName]();
			}
			catch (ScriptEngineException ex)
			{
				logger.LogError($"JavaScript Error in function {fnName}:\n{ex}");
				scriptIsBroken = true;
			}
			catch (Exception ex)
			{
				logger.LogError($"JavaScript unknown exception\n{ex}");
				scriptIsBroken = true;
			}
		}
	}
}
