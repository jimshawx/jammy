using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Jammy.NativeOverlay;
using Jammy.NativeOverlay.Overlays;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.InteropServices;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.DIB
{
	public class EmulationWindow : IEmulationWindow, IDisposable
	{
		private class OwnDCForm : Form
		{
			private const Int32 CS_OWNDC = 0x20;

			private const int WM_MOVE = 0x0003;
			private const int WM_SIZE = 0x0005;
			private const int WM_SETFOCUS = 0x0007;
			private const int WM_KILLFOCUS = 0x0008;
			private const int WM_PAINT = 0x000F;
			private const int WM_ERASEBKGND = 0x0014;
			private const int WM_SETCURSOR = 0x0020;
			private const int WM_WINDOWPOSCHANGED = 0x0047;

			[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
			public Rectangle ScreenRect { get; private set; } = new Rectangle();

			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					cp.ClassStyle |= CS_OWNDC;
					return cp;
				}
			}

			protected override void OnPaint(PaintEventArgs e)
			{
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == WM_ERASEBKGND)
				{ 
					m.Result = 1;
					return;
				}

				if (m.Msg == WM_SETCURSOR)
					m.Result = 0;

				//instant 50% frame-rate loss, due to flood of WM_PAINT messages
				//if (m.Msg == WM_PAINT)
				//{
				//	m.Result = 0;
				//	return;
				//}

				base.WndProc(ref m);

				if (m.Msg == WM_MOVE || m.Msg == WM_SIZE || m.Msg == WM_WINDOWPOSCHANGED)
					ScreenRect = RectangleToScreen(ClientRectangle);
			}
		}

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int key);

		private readonly IDiskLightOverlay diskLightOverlay;
		private readonly ITicksOverlay ticksOverlay;
		private readonly ILogger logger;
		private Form emulation;
		private int[] screen;

		public EmulationWindow(IDiskLightOverlay diskLightOverlay, ITicksOverlay ticksOverlay, ILogger<EmulationWindow> logger)
		{
			this.diskLightOverlay = diskLightOverlay;
			this.ticksOverlay = ticksOverlay;
			this.logger = logger;

			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				emulation = new OwnDCForm { Name = "Emulation", Text = "Jammy : Alt-Tab or Middle Mouse Click to detach mouse", ControlBox = false, FormBorderStyle = FormBorderStyle.FixedSingle, MinimizeBox = true, MaximizeBox = true };

				if (emulation.Handle == IntPtr.Zero)
					throw new ApplicationException();

				//todo: switch to RAW Input
				//https://ph3at.github.io/posts/Windows-Input/

				ss.Release();

				emulation.MouseClick += Emulation_MouseClick;
				emulation.KeyPress += Emulation_KeyPress;
				emulation.KeyDown += Emulation_KeyDown;
				emulation.Deactivate += Emulation_Deactivate;

				emulation.Show();

				Application.Run(emulation);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		public void Dispose()
		{
			emulation.Close();
		}

		public bool IsCaptured { get; private set; } = false;

		private void Capture(string where)
		{
			if (!IsCaptured)
			{
				//logger.LogTrace($"Capture {where}");
				IsCaptured = true;
				Cursor.Hide();
				Cursor.Clip = emulation.RectangleToScreen(emulation.ClientRectangle);
			}
		}

		private void Release(string where)
		{
			//logger.LogTrace($"Release {where} Was Captured? {IsCaptured}");
			if (IsCaptured)
			{
				IsCaptured = false;
				Cursor.Show();
				Cursor.Clip = new Rectangle(0, 0, 0, 0);
			}
		}

		private void Emulation_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (Control.MouseButtons == (MouseButtons.Left | MouseButtons.Right))
					Release("Click");
				else
					Capture("Click");
			}

			if (e.Button == MouseButtons.Middle)
			{
				Release("Middle");
			}
		}

		private void Emulation_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 0x9 && (GetAsyncKeyState((int)VK.VK_MENU) & 0x8000) != 0)
				Release("AltTab");

			//if (e.KeyChar == 0x1B)
			//	Release("KeyPress");
		}

		private void Emulation_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyValue == (int)VK.VK_TAB && (GetAsyncKeyState((int)VK.VK_MENU) & 0x8000) != 0)
				Release("DnAltTab");

			//if (e.KeyValue == (int)VK.VK_ESCAPE)
			//	Release("DnKeyPress");
		}

		private void Emulation_Deactivate(object sender, EventArgs e)
		{
			Release("Deactivate");
		}

		private int screenWidth;
		private int screenHeight;
		private Graphics gfx;
		private BITMAPINFO lpbmi;

		private int displayHz;

		public void SetPicture(int width, int height)
		{
			if (emulation.IsDisposed) return;

			var dm = new DEVMODE();
			EnumDisplaySettings(null!, 0, ref dm);
			logger.LogTrace($"Monitor refresh rate is {dm.dmDisplayFrequency}Hz.  Set this as high as possible!");
			displayHz = dm.dmDisplayFrequency;

			screen = new int[width * height];

			lpbmi.biSize = 40;
			lpbmi.biWidth = width;
			lpbmi.biHeight = -height;
			lpbmi.biPlanes = 1;
			lpbmi.biBitCount = 32;
			lpbmi.biCompression = (uint)BI_RGB;
			lpbmi.biSizeImage = (uint)(height * (width * lpbmi.biBitCount / 8));
			lpbmi.biXPelsPerMeter = 0;
			lpbmi.biYPelsPerMeter = 0;
			lpbmi.biClrUsed = 0;
			lpbmi.biClrImportant = 0;
			lpbmi.cols = null!;

			emulation.Invoke((Action)delegate
			{
				screenWidth = width;
				screenHeight = height;

				emulation.ClientSize = new Size(screenWidth, screenHeight);
				emulation.Show();

				gfx = Graphics.FromHwnd(emulation.Handle);
			});
		}

		[DllImport("gdi32.dll", EntryPoint = "SetDIBitsToDevice", SetLastError = true)]
		private static extern int SetDIBitsToDevice([In] IntPtr hdc, int xDest, int yDest, uint w, uint h, int xSrc,
			int ySrc, uint startScan, uint cLines, [In] int[] lpvBits,
			[In] ref BITMAPINFO lpbmi, BITMAPINFO.DIBColorTable colorUse);

		[DllImport("user32.dll")]
		private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

		[StructLayout(LayoutKind.Sequential)]
		private struct BITMAPINFO
		{
			public uint biSize;
			public int biWidth, biHeight;
			public short biPlanes, biBitCount;
			public uint biCompression, biSizeImage;
			public int biXPelsPerMeter, biYPelsPerMeter;
			public uint biClrUsed, biClrImportant;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
			public uint[] cols;

			public enum DIBColorTable
			{
				DIB_RGB_COLORS = 0,    /* color table in RGBs */
				DIB_PAL_COLORS
			};    /* color table in palette indices */
		}

		private const uint BI_RGB = 0;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		private struct DEVMODE
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string dmDeviceName;
			public short dmSpecVersion;
			public short dmDriverVersion;
			public short dmSize;
			public short dmDriverExtra;
			public int dmFields;
			public short dmOrientation;
			public short dmPaperSize;
			public short dmPaperLength;
			public short dmPaperWidth;
			public short dmScale;
			public short dmCopies;
			public short dmDefaultSource;
			public short dmPrintQuality;
			public short dmColor;
			public short dmDuplex;
			public short dmYResolution;
			public short dmTTOption;
			public short dmCollate;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string dmFormName;
			public short dmUnusedPadding;
			public short dmBitsPerPel;
			public int dmPelsWidth;
			public int dmPelsHeight;
			public int dmDisplayFlags;
			public int dmDisplayFrequency;
		}

		public void Blit(int[] screen)
		{
			if (emulation.IsDisposed) return;

			ticksOverlay.Render();
			diskLightOverlay.Render();

			var hdc = gfx.GetHdc();
			SetDIBitsToDevice(hdc, 0, 0, (uint)screenWidth, (uint)screenHeight,
				0, 0, 0, (uint)screenHeight,
				screen, ref lpbmi, BITMAPINFO.DIBColorTable.DIB_RGB_COLORS);
			gfx.ReleaseHdc(hdc);
		}

		public Types.Types.Point RecentreMouse()
		{
			var centre = new Point(0, 0);

			if (!emulation.IsDisposed)
			{
				//put the cursor back in the middle of the emulation window
				var emuRect = ((OwnDCForm)emulation).ScreenRect;
				centre = new Point(emuRect.X + emuRect.Width / 2, emuRect.Y + emuRect.Height / 2);
				Cursor.Position = centre;
			}

			return new Types.Types.Point { X = centre.X, Y = centre.Y };
		}

		public void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp)
		{
			emulation.KeyDown += (sender, e) => addKeyDown(e.KeyValue);
			emulation.KeyUp += (sender, e) => addKeyUp(e.KeyValue);
		}

		public bool IsActive()
		{
			return IsCaptured;
			//this is good but slow
			//return Form.ActiveForm == emulation;
		}

		public int[] GetFramebuffer()
		{
			return screen;
		}
	}
}