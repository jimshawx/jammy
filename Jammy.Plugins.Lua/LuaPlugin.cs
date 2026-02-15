using DbUp.Engine;
using Jammy.Interface;
using Jammy.Plugins.Interface;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
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
		private static object imguiApi = ImGuiAPI.Instance;

		public LuaEngine(IDebugger debugger, ILogger<LuaEngine> logger)
		{
			var asm = typeof(ImGuiNET.ImGui).Assembly; // or any type from ImGui.NET
			UserData.RegisterAssembly(asm);

			//UserData.RegisterType(typeof(ImGuiNET.ImGuiListClipper));


			// ImGuiListClipper
			UserData.RegisterType(typeof(ImGuiNET.ImGuiListClipper));
			

			// ImDrawListPtr
			UserData.RegisterType(typeof(ImGuiNET.ImDrawListPtr));
			

			// ImDrawDataPtr
			UserData.RegisterType(typeof(ImGuiNET.ImDrawDataPtr));
			

			// ImGuiIOPtr
			UserData.RegisterType(typeof(ImGuiNET.ImGuiIOPtr));
			

			// ImGuiStylePtr
			UserData.RegisterType(typeof(ImGuiNET.ImGuiStylePtr));
			

			// ImFontPtr
			UserData.RegisterType(typeof(ImGuiNET.ImFontPtr));
			

			// ImFontAtlasPtr
			UserData.RegisterType(typeof(ImGuiNET.ImFontAtlasPtr));
			

			// ImGuiViewportPtr
			UserData.RegisterType(typeof(ImGuiNET.ImGuiViewportPtr));
			



			UserData.RegisterType(typeof(ImGuiNET.ImGui));
			//UserData.RegisterType(imguiApi.GetType());
			UserData.RegisterType(debugger.GetType());
			UserData.RegisterType<System.Numerics.Vector2>();


													   //Danger, Will Robinson!
			//UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
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

			script.Globals["ImGuiListClipper"] =
				(Func<ImGuiNET.ImGuiListClipper>)(() => new ImGuiNET.ImGuiListClipper());

			//script.Globals["ImGuiListClipper"] = DynValue.NewCallback((ctx, args) => DynValue.NewUserData(UserData.Create(new ImGuiNET.ImGuiListClipper())));
			script.Globals["ImGuiListClipper"] = DynValue.NewCallback(new CallbackFunction((ctx, args) => DynValue.FromObject(script, new ImGuiNET.ImGuiListClipper())));
			script.Globals["ImDrawListPtr"] =
				(Func<ImGuiNET.ImDrawListPtr>)(() => new ImGuiNET.ImDrawListPtr());
			script.Globals["ImDrawDataPtr"] =
				(Func<ImGuiNET.ImDrawDataPtr>)(() => new ImGuiNET.ImDrawDataPtr());
			script.Globals["ImGuiIOPtr"] =
				(Func<ImGuiNET.ImGuiIOPtr>)(() => new ImGuiNET.ImGuiIOPtr());
			script.Globals["ImGuiStylePtr"] =
				(Func<ImGuiNET.ImGuiStylePtr>)(() => new ImGuiNET.ImGuiStylePtr());
			script.Globals["ImFontPtr"] =
				(Func<ImGuiNET.ImFontPtr>)(() => new ImGuiNET.ImFontPtr());
			script.Globals["ImFontAtlasPtr"] =
				(Func<ImGuiNET.ImFontAtlasPtr>)(() => new ImGuiNET.ImFontAtlasPtr());
			script.Globals["ImGuiViewportPtr"] =
				(Func<ImGuiNET.ImGuiViewportPtr>)(() => new ImGuiNET.ImGuiViewportPtr());

			//script.Globals["imgui"] = imguiApi;
			script.Globals["ImGui"] = UserData.CreateStatic(typeof(ImGuiNET.ImGui));
			//script.Globals["ImGuiListClipper"] = typeof(ImGuiNET.ImGuiListClipper);


			script.Globals["Vec2"] = typeof(System.Numerics.Vector2);
			script.Globals["print"] = (object o)=>logger.LogTrace($"{o?.ToString()}");
			script.Globals["jammy"] = debugger;

			logger.LogTrace("ImGuiListClipper global is: " + script.Globals["ImGuiListClipper"]);

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

		public void Render()
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
