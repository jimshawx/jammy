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
		private enum PluginType
		{
			Js,
			Lua
		}

		private class Plugin
		{
			public string Name { get; set; }
			public string File { get; set; }
			public IPluginWindow Window { get; set; }
			public PluginType Type { get; set; }
		}

		private readonly IPluginWindowFactory pluginWindowFactory;
		private readonly ILogger<PluginManager> logger;
		private readonly IPluginEngine luaEngine;
		private readonly IPluginEngine jsEngine;

		private readonly List<Plugin> activePlugins = new List<Plugin>();

		public PluginManager(IPluginWindowFactory pluginWindowFactory, IEnumerable<IPluginEngine> pluginEngines,
			ILogger<PluginManager> logger)
		{
			this.pluginWindowFactory = pluginWindowFactory;
			this.logger = logger;
			luaEngine = pluginEngines.SingleOrDefault(e => e is Lua.LuaEngine);
			jsEngine = pluginEngines.SingleOrDefault(e => e is JavaScript.JavaScriptEngine);
		}

		public void Start()
		{
			Directory.GetFiles("plugins", "*.lua").ToList().ForEach(f =>
			{
				try 
				{ 
					var code = File.ReadAllText(f);
					var plugin = luaEngine.NewPlugin(code);
					activePlugins.Add(new Plugin
					{ 
						File = f,
						Name = Path.GetFileName(f),
						Window = pluginWindowFactory.CreatePluginWindow(plugin),
						Type = PluginType.Lua
					});
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
					activePlugins.Add(new Plugin
					{
						File = f,
						Name = Path.GetFileName(f),
						Window = pluginWindowFactory.CreatePluginWindow(plugin),
						Type = PluginType.Js
					});
				}
				catch (Exception ex)
				{
					logger.LogTrace($"Can't load plugin {f}\n{ex}");
				}
			});
		}

		public void ReloadPlugin(string name)
		{
			var plugin = activePlugins.SingleOrDefault(p => p.Name == name);
			if (plugin == null)
			{
				logger.LogTrace($"No such plugin as {name}");
				return;
			}
			
			string code;
			try
			{ 
				code = File.ReadAllText(plugin.File);
			}
			catch (Exception ex)
			{
				logger.LogTrace($"Can't load plugin {plugin.File}\n{ex}");
				return;
			}

			if (plugin.Type == PluginType.Lua)
				plugin.Window.UpdatePlugin(luaEngine.NewPlugin(code));
			else if (plugin.Type == PluginType.Js)
				plugin.Window.UpdatePlugin(jsEngine.NewPlugin(code));
		}

		public void ReloadAllPlugins()
		{
			foreach (var plugin in activePlugins)
				ReloadPlugin(plugin.Name);
		}
	}
}
