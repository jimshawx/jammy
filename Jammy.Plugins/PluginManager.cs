using Jammy.Plugins.Interface;
using System.Collections.Generic;
using System.Linq;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins
{
	public class PluginManager : IPluginManager
	{
		private readonly IPluginWindowFactory pluginWindowFactory;

		public PluginManager(IPluginWindowFactory pluginWindowFactory, IEnumerable<IPluginEngine> pluginEngines)
		{
			this.pluginWindowFactory = pluginWindowFactory;

			var luaEngine = pluginEngines.FirstOrDefault(e => e is Lua.LuaEngine);
			var luaplugin = luaEngine.NewPlugin(TestScript.lua);

			var jsEngine = pluginEngines.FirstOrDefault(e => e is JavaScript.JavaScriptEngine);
			var jsplugin = jsEngine.NewPlugin(TestScript.js);

			pluginWindowFactory.CreatePluginWindow(luaplugin);
			pluginWindowFactory.CreatePluginWindow(jsplugin);
		}
	}

	internal static class TestScript
	{
		public static string lua = @"
		function update()
			imgui.Begin(""My Lua Window"")

			if imgui.Button(""Button A"") then
				print(""Button A clicked"")
			end

			if imgui.Button(""Button B"") then
				print(""Button B clicked"")
			end

			imgui.End()
		end
		";

		public static string js = @"
		function update()
		{
			imgui.Begin(""My JS Window"");

			if (imgui.Button(""Button A""))
			{
				console.log(""Button A clicked"");
			}

			if (imgui.Button(""Button B""))
			{
				console.log(""Button B clicked"");
			}

			imgui.End();
		}
		";
	}
}
