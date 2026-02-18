using Jammy.Core.Types.Types.Breakpoints;
using Jammy.Interface;
using Jammy.Plugins.Interface;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript.ClearScript
{
	public class JavaScriptEngine : IPluginEngine
	{
		private readonly IDebugger debugger;
		private readonly ILogger<JavaScriptEngine> logger;

		public JavaScriptEngine(IDebugger debugger, ILogger<JavaScriptEngine> logger)
		{
			this.debugger = debugger;
			this.logger = logger;
		}

		public bool SupportsExtension(string ext)
		{
			return ext == ".js";
		}

		public IPlugin NewPlugin(string code)
		{
			var engine = new V8ScriptEngine();

			engine.AddHostObject("console", new JsConsole(logger));
			engine.AddHostObject("createCallback", CreateCallback);
			engine.AddHostObject("jammy", WrapperFactory.CreateWrapper(debugger));
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

		public Func<Breakpoint, bool> CreateCallback(object func)
		{
			if (func is ScriptObject scriptFunc)
			{
				return (arg) =>
				{
					var result = scriptFunc.Invoke(false, arg);
					return Convert.ToBoolean(result);
				};
			}

			throw new ArgumentException("Argument must be a JavaScript function");
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

			ExecuteFn("init");
		}

		public void Update()
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
