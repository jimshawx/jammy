using Jammy.Interface;
using Jammy.Plugins.Interface;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using System;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Lua
{
	public class LuaEngine : IPluginEngine
	{
		private readonly IDebugger debugger;
		private readonly ILogger<LuaEngine> logger;

		public LuaEngine(IDebugger debugger, ILogger<LuaEngine> logger)
		{
			UserData.RegisterType(debugger.GetType());

			this.debugger = debugger;
			this.logger = logger;
		}

		public bool SupportsExtension(string ext)
		{
			return ext == ".lua";
		}

		public IPlugin NewPlugin(string code)
		{
			var script = new Script();

			script.Globals["print"] = (object o)=>logger.LogTrace($"{o?.ToString()}");
			script.Globals["jammy"] = debugger;

			try 
			{ 
				script.DoString(code);
			}
			catch (InterpreterException ex)
			{
				logger.LogError($"Lua Script Error:\n{ex}");
				return null;
			}
			return new LuaPlugin(script, logger);
		}
	}

	public class LuaPlugin : IPlugin
	{
		private Script script;
		private readonly ILogger logger;
		private bool scriptIsBroken = false;

		public LuaPlugin(Script script, ILogger logger)
		{
			this.script = script;
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
				var fn = script.Globals.Get(fnName);
				if (fn.Type == DataType.Function)
					script.Call(fn);
			}
			catch (InterpreterException ex)
			{
				logger.LogError($"Lua Script Error in function {fnName}:\n{ex}");
				scriptIsBroken = true;
			}
			catch (Exception ex)
			{
				logger.LogError($"Lua Script unknown exception\n{ex}");
				scriptIsBroken = true;
			}
		}
	}
}
