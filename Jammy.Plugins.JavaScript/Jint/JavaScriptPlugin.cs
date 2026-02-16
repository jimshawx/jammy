using Jammy.Interface;
using Jammy.Plugins.Interface;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Jint.Runtime.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Numerics;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript.Jint
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
			var engine = new Engine(cfg => cfg.AllowClr());

			engine.SetValue("console", new JsConsole(logger));

			engine.SetValue("imgui", imguiApi);
			engine.SetValue("jammy", debugger);

			engine.SetValue("Vec2", TypeReference.CreateTypeReference(engine, typeof(Vector2)));

			try 
			{ 
				engine.Execute(code);
			}
			catch (JintException ex)
			{
				logger.LogError($"JavaScript Error:\n{ex}");
				return null;
			}

			return new JavaScriptPlugin(engine, logger);
		}
	}

	public class JavaScriptPlugin : IPlugin
    {
		private readonly Engine engine;
		private readonly ILogger logger;
		private bool scriptIsBroken = false;

		public JavaScriptPlugin(Engine engine, ILogger logger)
		{
			this.engine = engine;
			this.logger = logger;
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
				var fn = engine.GetValue(fnName);
				if (fn is ScriptFunction)
					fn.Call(JsValue.Undefined);
			}
			catch (JintException ex)
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
