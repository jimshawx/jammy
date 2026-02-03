using ImGuiNET;
using Jammy.Plugins.Interface;
using Jammy.Plugins.Renderer;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.Windows
{
	public class SkiaHostControl : Control
	{
		private readonly IPluginRenderer renderer;
		private IPlugin plugin;
		private readonly ILogger logger;
		private SKImageInfo info;
		private SKSurface surface;
		private SKCanvas canvas;

		private byte[] pixelBuffer;
		private GCHandle pixelHandle;
		private IntPtr pixelPtr;

		private Bitmap gdiBitmap;

		public SkiaHostControl(IPluginRenderer renderer, IPlugin plugin, ILogger logger)
		{
			this.renderer = renderer;
			this.plugin = plugin;
			this.logger = logger;
			this.DoubleBuffered = true;
			this.SetStyle(ControlStyles.AllPaintingInWmPaint |
						  ControlStyles.UserPaint |
						  ControlStyles.OptimizedDoubleBuffer, true);
			this.UpdateStyles();

			ResizeRedraw = true;
			Resize += (_,__) => RecreateSurface();
		}

		private void RecreateSurface()
		{
			if (Width <= 0 || Height <= 0)
				return;

			// Dispose old resources
			surface?.Dispose();
			gdiBitmap?.Dispose();
			if (pixelHandle.IsAllocated)
				pixelHandle.Free();

			// Create new Skia info
			info = new SKImageInfo(
				Width,
				Height,
				SKColorType.Bgra8888, // IMPORTANT: matches GDI+ pixel format
				SKAlphaType.Premul
			);

			// Allocate pixel buffer
			int size = info.BytesSize;
			pixelBuffer = new byte[size];

			// Pin buffer
			pixelHandle = GCHandle.Alloc(pixelBuffer, GCHandleType.Pinned);
			pixelPtr = pixelHandle.AddrOfPinnedObject();

			// Create Skia surface using pinned memory
			surface = SKSurface.Create(info, pixelPtr, info.RowBytes);
			canvas = surface.Canvas;

			// Create GDI+ bitmap that wraps the same memory
			gdiBitmap = new Bitmap(
				info.Width,
				info.Height,
				info.RowBytes,
				PixelFormat.Format32bppPArgb,
				pixelPtr
			);
		}

		public IDisposable Lock()
		{
			return renderer.Lock();
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			if (canvas == null || gdiBitmap == null)
				return;

			using var imgui = Lock();

			canvas.Clear(SKColors.Gray);

			var io = ImGui.GetIO();
			io.DisplaySize = new System.Numerics.Vector2(Width, Height);

			ImGui.NewFrame();
			
			plugin.Render();

			ImGui.Render();

			// Draw ImGui
			renderer.Render(canvas, ImGui.GetDrawData());

			// after drawing test quad (or renderer.Render)
			surface.Flush();

			// Blit directly — no PNG, no encoding, no decoding
			e.Graphics.DrawImage(gdiBitmap, 0, 0);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			surface.Dispose();
			gdiBitmap.Dispose();
			if (pixelHandle.IsAllocated)
				pixelHandle.Free();
		}

		public void UpdatePlugin(IPlugin plugin)
		{
			using var imgui = Lock();
			this.plugin = plugin;
		}
	}
}