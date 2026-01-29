using Jammy.Plugins.Interface;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Lua
{
	public class LuaImGui
	{
		public void Begin(string title) => ImGuiNET.ImGui.Begin(title);
		public void End() => ImGuiNET.ImGui.End();

		public bool Button(string label) => ImGuiNET.ImGui.Button(label);
	}

	public class LuaEngine : IPluginEngine
	{
		private readonly ILogger<LuaEngine> logger;
		private LuaImGui imguiApi = new LuaImGui();

		public LuaEngine(ILogger<LuaEngine> logger)
		{
			UserData.RegisterType<LuaImGui>();
			this.logger = logger;
		}

		public IPlugin NewPlugin(string code)
		{
			var script = new Script();
			script.Globals["imgui"] = imguiApi;
			script.Globals["print"] = (object o)=>logger.LogTrace($"{o?.ToString()}");
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
		}

		public void Render()
		{
			DynValue fn = script.Globals.Get("update");
			if (fn.Type == DataType.Function)
				script.Call(fn);
		}
	}
}