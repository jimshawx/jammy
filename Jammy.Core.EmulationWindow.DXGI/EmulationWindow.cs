using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Jammy.NativeOverlay.Overlays;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.DX
{
	public class EmulationWindow : IEmulationWindow, IDisposable
	{
		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int key);

		private readonly IDiskLightOverlay diskLightOverlay;
		private readonly ITicksOverlay ticksOverlay;
		private readonly ILogger logger;
		private Form emulation;

		private int screenWidth;
		private int screenHeight;
		private IDXGISwapChain1 swapchain;
		private ID3D11Device device;
		private ID3D11DeviceContext context;
		private ID3D11Texture2D stagingTexture;
		private ID3D11Texture2D backBuffer;
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
				emulation = new Form
				{
					Name = "Emulation",
					Text = "Jammy : Alt-Tab or Middle Mouse Click to detach mouse",
					ControlBox = false,
					FormBorderStyle = FormBorderStyle.FixedSingle,
					MinimizeBox = true,
					MaximizeBox = true
				};
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
			stagingTexture.Dispose();
			backBuffer.Dispose();
			swapchain.Dispose();
			context.Dispose();
			device.Dispose();
		}

		public bool IsCaptured { get; private set; } = false;

		private void Capture(string where)
		{
			if (!IsCaptured)
			{
				IsCaptured = true;
				Cursor.Hide();
				Cursor.Clip = emulation.RectangleToScreen(emulation.ClientRectangle);
			}
		}

		private void Release(string where)
		{
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
		}

		private void Emulation_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyValue == (int)VK.VK_TAB && (GetAsyncKeyState((int)VK.VK_MENU) & 0x8000) != 0)
				Release("DnAltTab");
		}

		private void Emulation_Deactivate(object sender, EventArgs e)
		{
			Release("Deactivate");
		}

		public void SetPicture(int width, int height)
		{
			if (emulation.IsDisposed) return;

			emulation.Invoke((Action)delegate
			{
				screenWidth = width;
				screenHeight = height;
				emulation.ClientSize = new Size(screenWidth, screenHeight);

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

				D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.BgraSupport, featureLevels, out device, out context);
				if (device == null || context == null)
					throw new ApplicationException();

				screen = new int[screenWidth * screenHeight];

				var swapDesc = new SwapChainDescription1
				{
					Width = (uint)screenWidth,
					Height = (uint)screenHeight,
					AlphaMode = AlphaMode.Ignore,
					BufferCount = 3,
					BufferUsage = Usage.RenderTargetOutput,
					Flags = 0,
					Format = Format.B8G8R8A8_UNorm,
					SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
					Scaling = Scaling.Stretch,
					Stereo = false,
					SwapEffect = SwapEffect.FlipDiscard
				};

				swapchain = factory.CreateSwapChainForHwnd(
					device,
					emulation.Handle,
					swapDesc,
					null,
					null);

				backBuffer = swapchain.GetBuffer<ID3D11Texture2D>(0);

				stagingTexture = device.CreateTexture2D(new Texture2DDescription
				{
					Format = Format.B8G8R8A8_UNorm,
					Width = (uint)screenWidth,
					Height = (uint)screenHeight,
					CPUAccessFlags = CpuAccessFlags.Write,
					MipLevels = 1,
					ArraySize = 1,
					BindFlags = BindFlags.None,
					MiscFlags = ResourceOptionFlags.None,
					SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
					Usage = ResourceUsage.Staging
				});

				emulation.Show();
			});
		}


		public void Blit(int[] screen)
		{
			if (emulation.IsDisposed) return;

			ticksOverlay.Render();
			diskLightOverlay.Render();

			emulation.Invoke((Action)delegate
			{
				// Map the staging texture for writing
				var dataBox = context.Map(stagingTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None);

				//unsafe
				//{
				//	byte* dest = (byte*)dataBox.DataPointer;
				//	fixed (int* src = screen)
				//	{
				//		int rowBytes = screenWidth * sizeof(int);
				//		for (int y = 0; y < screenHeight; y++)
				//		{
				//			Buffer.MemoryCopy(
				//				src + y * screenWidth,
				//				dest + y * dataBox.RowPitch,
				//				rowBytes,
				//				rowBytes);
				//		}
				//	}
				//}

				int rowBytes = screenWidth * sizeof(int);
				for (int y = 0; y < screenHeight; y++)
				{
					IntPtr destRowPtr = IntPtr.Add(dataBox.DataPointer, y * (int)dataBox.RowPitch);
					int srcOffset = y * screenWidth;
					Marshal.Copy(screen, srcOffset, destRowPtr, screenWidth);
				}

				context.Unmap(stagingTexture, 0);

				// Copy the staging texture to the back buffer
				context.CopyResource(backBuffer, stagingTexture);

				swapchain.Present(0, 0);
			});
		}

		public Types.Types.Point RecentreMouse()
		{
			var centre = new Point(0, 0);

			if (!emulation.IsDisposed)
			{
				emulation.Invoke((Action)delegate ()
				{
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
		}

		public int[] GetFramebuffer()
		{
			return screen;
		}
	}
}