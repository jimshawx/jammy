using Jammy.Plugins.Interface;
using Jammy.Plugins.Renderer;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Windows
{
	public class WindowsPluginWindowFactory : IPluginWindowFactory
	{
		private readonly ILogger<WindowsPluginWindowFactory> logger;

		public WindowsPluginWindowFactory(ILogger<WindowsPluginWindowFactory> logger)
		{
			this.logger = logger;
		}

		public IPluginWindow CreatePluginWindow(IPlugin plugin)
		{
			return new WindowsPluginWindow(plugin, logger);
		}
	}

	internal class WindowsPluginWindow : IPluginWindow
	{
		private System.Windows.Forms.Timer timer;
		private readonly Thread t;
		private Form form;

		public WindowsPluginWindow(IPlugin plugin, ILogger logger)
		{
			var ss = new SemaphoreSlim(1);
			ss.Wait();

			t = new Thread(() =>
			{
				using var g = Graphics.FromHwnd(IntPtr.Zero);
				float scale = g.DpiX / 96.0f;

				var renderer = new ImGuiSkiaRenderer(scale, logger);

				form = new Form();
				form.Width = 800;
				form.Height = 600;
				form.Text = $"{plugin.GetType().Name} Window";

				var sk = new SkiaHostControl(renderer, plugin, logger);
				ImGuiInput.SetImGuiInput(sk);
				sk.Dock = DockStyle.Fill;
				form.Controls.Add(sk);

				form.Show();

				timer = new System.Windows.Forms.Timer { Interval = 100 };
				timer.Tick += (_, __) => { sk.Invalidate(); };
				timer.Start();

				ss.Release();

				Application.Run(form);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}
	}
}
