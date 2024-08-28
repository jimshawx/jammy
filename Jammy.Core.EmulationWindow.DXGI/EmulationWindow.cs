using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Microsoft.Extensions.Logging;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Direct3D;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.DX
{
	public class EmulationWindow : IEmulationWindow, IDisposable
	{
		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int key);

		private readonly ILogger logger;
		private Form emulation;

		public EmulationWindow(ILogger<EmulationWindow> logger)
		{
			this.logger = logger;

			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				emulation = new Form {Name = "Emulation", Text = "Jammy : Alt-Tab or Middle Mouse Click to detach mouse", ControlBox = false, FormBorderStyle = FormBorderStyle.FixedSingle, MinimizeBox = true, MaximizeBox = true};
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

		//private Bitmap bitmap;
		//private PictureBox picture;
		private int screenWidth;
		private int screenHeight;
		private IDXGISwapChain1 swapchain;
		private ID3D11Texture2D surface;
		private ID3D11Device device;
		private int[] screen;

		public void SetPicture(int width, int height)
		{
			if (emulation.IsDisposed) return;

			emulation.Invoke((Action)delegate
			{
				screenWidth = width;
				screenHeight = height;

				emulation.ClientSize = new Size(screenWidth, screenHeight);
				//bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppRgb);
				//picture = new PictureBox {Image = bitmap, ClientSize = new Size(screenWidth, screenHeight), Enabled = false};

				//try to scale the box
				//picture.SizeMode = PictureBoxSizeMode.StretchImage;
				//int scaledHeight = (SCREEN_HEIGHT * 6) / 5;
				//emulation.ClientSize = new System.Drawing.Size(SCREEN_WIDTH, scaledHeight);
				//picture.ClientSize = new System.Drawing.Size(SCREEN_WIDTH, scaledHeight);

				//emulation.Controls.Add(picture);

				DXGI.CreateDXGIFactory2<IDXGIFactory2>(true, out var factory);
				if (factory == null)
					throw new ApplicationException();

				factory.EnumAdapters(0, out var adapter);
				if (adapter == null)
					throw new ApplicationException();

				var featureLevels = new FeatureLevel[]
								{
							FeatureLevel.Level_11_1,
							FeatureLevel.Level_11_0,
								};

				D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug, featureLevels, out device);
				if (device == null)
					throw new ApplicationException();

				screen = new int[screenWidth * screenHeight];

				surface = device.CreateTexture2D(new Texture2DDescription
				{
					Format = Format.B8G8R8A8_UNorm,
					Width = screenWidth ,
					Height = screenHeight ,
					CPUAccessFlags = CpuAccessFlags.Write | CpuAccessFlags.Read,
					MipLevels = 1,
					ArraySize = 1,
					BindFlags = BindFlags.None,
					MiscFlags = ResourceOptionFlags.None,
					SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
					Usage = ResourceUsage.Default
				});
				if (surface == null)
					throw new ApplicationException();

				swapchain = factory.CreateSwapChainForHwnd(
					device,
					emulation.Handle,
					new SwapChainDescription1
					{
						Width = screenWidth,
						Height = screenHeight,
						AlphaMode = AlphaMode.Ignore,
						BufferCount = 2,
						BufferUsage = Usage.RenderTargetOutput ,
						Flags = 0,//SwapChainFlags.AllowTearing|SwapChainFlags.GdiCompatible ,
						Format = Format.B8G8R8A8_UNorm,
						SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
						Scaling = Scaling.Stretch,
						Stereo = false,
						SwapEffect = SwapEffect.Discard
					},
					null,
					null);

				emulation.Show();
			});
		}

		public bool PowerLight { private get; set; }
		public bool DiskLight { private get; set; }

		public void Blit(int[] screen)
		{
			if (emulation.IsDisposed) return;

			emulation.Invoke((Action)delegate
			{

				var surface = swapchain.GetBuffer<IDXGISurface2>(0);
				
				var rt = device.CreateRenderTargetView(surface.QueryInterface<ID3D11Resource>(), null);
				

				var map = surface.Map(Vortice.DXGI.MapFlags.Write | Vortice.DXGI.MapFlags.Discard);
				
				var map2 = surface.MapDataStream(Vortice.DXGI.MapFlags.Write | Vortice.DXGI.MapFlags.Discard);

				foreach (var c in screen)
					map2.Write<int>(c);

				surface.Unmap();

				//Marshal.Copy(screen, 0, map2.BasePointer, screenWidth * screenHeight);

				surface.Release();

				swapchain.Present(1,0);
			});
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