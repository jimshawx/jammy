using Jammy.Plugins.Interface;
using Jint;
using Jint.Native;

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

	public class JavaScriptEngine : IPluginEngine
	{
		private JsImGui imguiApi = new JsImGui();

		public JavaScriptEngine()
		{
		}

		public IPlugin NewPlugin(string code)
		{
			var engine = new Engine(cfg => cfg.AllowClr());
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
