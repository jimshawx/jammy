/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Interface
{
	public interface IPlugin
	{
		void Render();
	}

	public interface IPluginManager
	{
	}

	public interface IPluginWindow
	{
	}

	public interface IPluginWindowFactory
	{
		IPluginWindow CreatePluginWindow(IPlugin plugin);
	}

	public interface IPluginEngine
	{
		IPlugin NewPlugin(string code);
	}
}
