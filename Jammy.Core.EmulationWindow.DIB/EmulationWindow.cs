using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.DIB
{
	public class EmulationWindow : IEmulationWindow, IDisposable
	{
		private class OwnDCForm : Form
		{
			private const Int32 CS_OWNDC = 0x20;

			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					cp.ClassStyle |= CS_OWNDC;
					return cp;
				}
			}
		}

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int key);

		private readonly ILogger logger;
		private Form emulation;
		private int[] screen; 

		public EmulationWindow(ILogger<EmulationWindow> logger)
		{
			this.logger = logger;

			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				emulation = new OwnDCForm {Name = "Emulation", Text = "Jammy : Alt-Tab or Middle Mouse Click to detach mouse", ControlBox = false, FormBorderStyle = FormBorderStyle.FixedSingle, MinimizeBox = true, MaximizeBox = true};

				if (emulation.Handle == IntPtr.Zero)
					throw new ApplicationException();

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
			if (e.KeyChar == 0x9 && (GetAsyncKeyState((int)VK.VK_MENU)&0x8000)!=0)
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

			RenderTicks();

			var hdc = gfx.GetHdc();
			SetDIBitsToDevice(hdc, 0, 0, (uint)screenWidth, (uint)screenHeight,
				0, 0, 0, (uint)screenHeight,
				screen, ref lpbmi, BITMAPINFO.DIBColorTable.DIB_RGB_COLORS);
			gfx.ReleaseHdc(hdc);
		}

		private DateTime lastTick = DateTime.Now;
		private float[] fpsarr = new float[128];
		private int fpsarrpos=0;
		private void RenderTicks()
		{
			var now = DateTime.Now;
			TimeSpan dt = now - lastTick;
			lastTick = now;

			if (dt > TimeSpan.Zero && dt.Milliseconds <= 1000)
			{
				int so = 20 + 10 * screenWidth;
				int ss = 2;
				var fps = 1000.0f / dt.Milliseconds;
				fpsarr[fpsarrpos++] = fps;
				fpsarrpos &= fpsarr.Length-1;
				var avefps = fpsarr.Sum()/fpsarr.Length;

				for (int i = 0; i <= displayHz*ss; i += 10*ss)
				{
					for (int y = 0; y < 8*ss; y++)
						screen[so + i + y * screenWidth] = 0xffffff;
				}

				for (int i = 0; i < fps*ss; i ++)
				{
					for (int y = 0; y < 3*ss; y++)
						screen[so + i + y * screenWidth] = 0xff0000;
				}

				for (int i = 0; i < avefps * ss; i++)
				{
					for (int y = 0; y < 3 * ss; y++)
						screen[so + i + (4*ss+y) * screenWidth] = 0x0000ff;
				}
			}
		}

		public Types.Types.Point RecentreMouse()
		{
			var centre = new Point(0, 0);

			if (!emulation.IsDisposed)
			{
				emulation.Invoke((Action)delegate()
				{
					//put the cursor back in the middle of the emulation window
					var emuRect = emulation.RectangleToScreen(emulation.ClientRectangle);
					centre = new Point(emuRect.X + emuRect.Width / 2, emuRect.Y + emuRect.Height / 2);
					Cursor.Position = centre;
				});
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