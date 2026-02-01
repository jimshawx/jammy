using Jammy.Interface;
using Jammy.Plugins.Interface;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Lua
{
	public class LuaEngine : IPluginEngine
	{
		private readonly IDebugger debugger;
		private readonly ILogger<LuaEngine> logger;
		private static object imguiApi = ImGuiAPI.Instance;

		public LuaEngine(IDebugger debugger, ILogger<LuaEngine> logger)
		{
			UserData.RegisterType(imguiApi.GetType());
			UserData.RegisterType(debugger.GetType());
			//Danger, Will Robinson!
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
			this.debugger = debugger;
			this.logger = logger;
		}

		public IPlugin NewPlugin(string code)
		{
			var script = new Script();
			script.Globals["imgui"] = imguiApi;
			script.Globals["print"] = (object o)=>logger.LogTrace($"{o?.ToString()}");
			script.Globals["jammy"] = debugger;
			script.DoString(code);
			return new LuaPlugin(script);
		}
	}

	public class LuaPlugin : IPlugin
	{
		private Script script;

		public LuaPlugin(Script script)
		{
			this.script = script;
			var fn = script.Globals.Get("init");
			if (fn.Type == DataType.Function)
				script.Call(fn);
		}

		public void Render()
		{
			var fn = script.Globals.Get("update");
			if (fn.Type == DataType.Function)
				script.Call(fn);
		}
	}
}