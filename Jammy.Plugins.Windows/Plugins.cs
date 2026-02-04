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
			if (plugin == null) return null;
			return new WindowsPluginWindow(plugin, logger);
		}
	}

	internal class WindowsPluginWindow : IPluginWindow
	{
		private System.Windows.Forms.Timer timer;
		private readonly Thread t;
		private Form form;
		private SkiaHostControl skiaControl;
		private ImGuiSkiaRenderer renderer;

		public WindowsPluginWindow(IPlugin plugin, ILogger logger)
		{
			var ss = new SemaphoreSlim(1);
			ss.Wait();

			t = new Thread(() =>
			{
				using var g = Graphics.FromHwnd(IntPtr.Zero);
				float scale = g.DpiX / 96.0f;

				renderer = new ImGuiSkiaRenderer(scale, logger);

				form = new Form();
				form.Width = (int)(800 * scale);
				form.Height = (int)(600 * scale);
				form.Text = $"{plugin.GetType().Name} Window";

				skiaControl = new SkiaHostControl(renderer, plugin, logger);
				ImGuiInput.SetImGuiInput(skiaControl);
				skiaControl.Dock = DockStyle.Fill;
				form.Controls.Add(skiaControl);

				form.Show();

				timer = new System.Windows.Forms.Timer { Interval = 100 };
				timer.Tick += (_, __) => { skiaControl.Invalidate(); };

				ss.Release();

				timer.Start();

				Application.Run(form);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		public void UpdatePlugin(IPlugin plugin)
		{
			if (plugin == null) return;
			skiaControl.UpdatePlugin(plugin);
		}

		public void Close()
		{
			timer?.Stop();
			form?.Close();
			form?.Dispose();
			renderer?.Dispose();
		}
	}
}
