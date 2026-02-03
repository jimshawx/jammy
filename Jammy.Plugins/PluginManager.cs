using Jammy.Plugins.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins
{
	public class PluginManager : IPluginManager
	{
		private readonly IPluginWindowFactory pluginWindowFactory;
		private readonly ILogger<PluginManager> logger;

		public PluginManager(IPluginWindowFactory pluginWindowFactory, IEnumerable<IPluginEngine> pluginEngines,
			ILogger<PluginManager> logger)
		{
			this.pluginWindowFactory = pluginWindowFactory;
			this.logger = logger;
			var luaEngine = pluginEngines.SingleOrDefault(e => e is Lua.LuaEngine);
			var jsEngine = pluginEngines.SingleOrDefault(e => e is JavaScript.JavaScriptEngine);

			var luaplugin = luaEngine.NewPlugin(TestScript.lua);
			var jsplugin = jsEngine.NewPlugin(TestScript.js);

			pluginWindowFactory.CreatePluginWindow(luaplugin);
			pluginWindowFactory.CreatePluginWindow(jsplugin);

			Directory.GetFiles("plugins", "*.lua").ToList().ForEach(f =>
			{
				try 
				{ 
					var code = File.ReadAllText(f);
					var plugin = luaEngine.NewPlugin(code);
					pluginWindowFactory.CreatePluginWindow(plugin);
				}
				catch (Exception ex)
				{
					logger.LogTrace($"Can't load plugin {f}\n{ex}");
				}
			});

			Directory.GetFiles("plugins", "*.js").ToList().ForEach(f =>
			{
				try
				{ 
					var code = File.ReadAllText(f);
					var plugin = jsEngine.NewPlugin(code);
					pluginWindowFactory.CreatePluginWindow(plugin);
				}
				catch (Exception ex)
				{
					logger.LogTrace($"Can't load plugin {f}\n{ex}");
				}
			});

		}
	}

	internal static class TestScript
	{
		public static string lua = @"
		function update()
			imgui.Begin(""My Lua Window"", 64)

			if imgui.Button(""Step"") then
				jammy.Step()
			end

			if imgui.Button(""Step Out"") then
				jammy.StepOut()
			end

			if imgui.Button(""Stop"") then
				jammy.Stop()
			end

			if imgui.Button(""Go"") then
				jammy.Go()
			end

			imgui.End()

			--local x = jammy.GetRegs();
			--print(string.format(""PC: %X"", x.PC));

		end
		";

		public static string js = @"
		function update()
		{
			imgui.Begin(""My JS Window"", 64);

			if (imgui.Button(""Step""))
				jammy.Step();

			if (imgui.Button(""Step Out""))
				jammy.StepOut();

			if (imgui.Button(""Stop""))
				jammy.Stop();

			if (imgui.Button(""Go""))
				jammy.Go();

			//imgui.ShowStyleEditor();
			//imgui.ShowStyleSelector(""Style"");

			imgui.End();

			//var x = jammy.GetRegs();
			//console.log(""PC: "" + x.PC.toString(16));
		}
		";
	}
}
