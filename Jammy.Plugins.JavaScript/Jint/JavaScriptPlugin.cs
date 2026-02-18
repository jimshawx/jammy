using Jammy.Interface;
using Jammy.Plugins.Interface;
using Jint;
using Jint.Native;
using Jint.Native.Function;
using Microsoft.Extensions.Logging;
using System;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript.Jint
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

		private Engine engine = null;
		public IPlugin NewPlugin(string code)
		{
			engine = new Engine(cfg => cfg.AllowClr());

			engine.SetValue("console", new JsConsole(logger));
			engine.SetValue("jammy", debugger);
			engine.SetValue("createCallback", CreateCallback);

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

		public Func<object, bool> CreateCallback(object func)
		{
			return JintCallback.Wrap<object, bool>(engine, func);
		}
	}

	public static class JintCallback
	{
		public static Func<T, TResult> Wrap<T, TResult>(
			Engine engine,
			object jsFuncObj)
		{
			var jsFunc = jsFuncObj as Func<JsValue, JsValue[], JsValue>;
			if (jsFunc == null)
				throw new ArgumentException("Value is not a JS function", nameof(jsFuncObj));

			return arg =>
			{
				var jsArg = JsValue.FromObject(engine, arg);

				var result = jsFunc(
					JsValue.Undefined,      // thisArg
					[jsArg]					// arguments[]
				);

				return result.ToObject() is TResult typed
					? typed
					: (TResult)Convert.ChangeType(result.ToObject(), typeof(TResult));
			};
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

		public void Update()
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
