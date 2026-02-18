/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Interface
{
	public interface IPlugin
	{
		void Update();
	}

	public interface IPluginManager
	{
		void Start();
		void ReloadPlugin(string name);
		void ReloadAllPlugins();
	}

	public interface IPluginEngine
	{
		IPlugin NewPlugin(string code);
		bool SupportsExtension(string ext);
	}
}
