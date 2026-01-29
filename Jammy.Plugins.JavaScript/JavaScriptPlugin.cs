using Jammy.Plugins.Interface;
using Jint;
using Jint.Native;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.JavaScript
{
	public class JsImGui
	{
		public void Begin(string title) => ImGuiNET.ImGui.Begin(title);
		public void End() => ImGuiNET.ImGui.End();

		public bool Button(string label) => ImGuiNET.ImGui.Button(label);
	}

	public class JsConsole
	{
		private readonly ILogger logger;

		public JsConsole(ILogger logger)
		{
			this.logger = logger;
		}

		public void log(object o) => logger.LogTrace($"{o?.ToString()}");
	}

	public class JavaScriptEngine : IPluginEngine
	{
		private readonly ILogger<JavaScriptEngine> logger;
		private JsImGui imguiApi = new JsImGui();

		public JavaScriptEngine(ILogger<JavaScriptEngine> logger)
		{
			this.logger = logger;
		}

		public IPlugin NewPlugin(string code)
		{
			var engine = new Engine(cfg => cfg.AllowClr());

			engine.SetValue("console", new JsConsole(logger));

			engine.SetValue("imgui", imguiApi);
			engine.Execute(code);
			return new JavaScriptPlugin(engine);
		}
	}

	public class JavaScriptPlugin : IPlugin
    {
		private readonly Engine engine;

		public JavaScriptPlugin(Engine engine)
		{
			this.engine = engine;
		}

		public void Render()
		{
			var updateFn = engine.GetValue("update");
			updateFn.Call(JsValue.Undefined);
		}
	}
}
