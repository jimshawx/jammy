using Jammy.Plugins.Interface;
using Jammy.Plugins.Renderer;
using System.Threading;
using System.Windows.Forms;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Windows
{
	public class WindowsPluginWindowFactory : IPluginWindowFactory
	{
		private readonly IPluginRenderer renderer;
		public WindowsPluginWindowFactory()
		{
			renderer = new ImGuiSkiaRenderer();
		}

		public IPluginWindow CreatePluginWindow(IPlugin plugin)
		{
			return new WindowsPluginWindow(renderer, plugin);
		}
	}

	internal class WindowsPluginWindow : IPluginWindow
	{
		System.Windows.Forms.Timer timer;
		Thread t;
		Form form;

		public WindowsPluginWindow(IPluginRenderer renderer, IPlugin plugin)
		{
			t = new Thread(() =>
			{
				form = new Form();
				form.Width = 800;
				form.Height = 600;
				form.Text = "Lua Plugin Window";

				//ImGui.StyleColorsLight();

				//io.FontGlobalScale = 2.0f;

				//var style = ImGui.GetStyle();
				//style.ScaleAllSizes(2.0f);

				var sk = new SkiaHostControl(renderer, plugin);
				ImGuiInput.SetImGuiInput(sk);
				sk.Dock = DockStyle.Fill;
				form.Controls.Add(sk);
				//form.Paint += (s, e) =>
				//{
				//	var canvas = e.Graphics;
				//	renderer.Render(canvas, ImGui.GetDrawData());
				//};	

				form.Show();

				timer = new System.Windows.Forms.Timer { Interval = 100 };
				timer.Tick += (_, __) => { sk.Invalidate(); };
				timer.Start();

				Application.Run(form);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
		}
	}
}
