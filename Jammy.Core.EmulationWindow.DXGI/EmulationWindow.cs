using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Jammy.NativeOverlay;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

		private readonly INativeOverlay nativeOverlay;
		private readonly ILogger logger;
		private Form emulation;

		private int screenWidth;
		private int screenHeight;
		private IDXGISwapChain1 swapchain;
		private ID3D11Device device;
		private ID3D11DeviceContext context;
		private ID3D11Texture2D stagingTexture;
		private ID3D11Texture2D d3dBackBuffer;

		// Triple Buffering Arrays
		private int[] backBufferArray;
		private int[] readyBufferArray;
		private int[] frontBufferArray;

		// Render Thread Management
		private Thread renderThread;
		private CancellationTokenSource renderCts;

		private int mouseDX, mouseDY;

		public class AForm : Form
		{
			private readonly ILogger logger;
			private readonly Action<Message> HandleRawMessage;

			public AForm(ILogger logger, Action<Message> rawMessageHandler)
			{
				this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
				this.logger = logger;
				this.HandleRawMessage = rawMessageHandler;
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == WM_INPUT)
					HandleRawMessage(m);

				base.WndProc(ref m);
			}
		}

		private void HandleRawMessage(Message m)
		{
			uint dwSize = (uint)Marshal.SizeOf(typeof(RAWINPUT));
			uint headerSize = (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER));

			int result = GetRawInputData(m.LParam, RID_INPUT, out RAWINPUT raw, ref dwSize, headerSize);

			if (result == -1) return;

			switch (raw.header.dwType)
			{
				case RIM_TYPEMOUSE:
					HandleRawMouse(raw.mouse);
					break;

				case RIM_TYPEKEYBOARD:
					HandleRawKeyboard(raw.keyboard);
					break;
			}
		}

		private void HandleRawMouse(RAWMOUSE mouse)
		{
			// 0x01 = MOUSE_MOVE_RELATIVE
			if ((mouse.usFlags & 0x01) == 0)
			{
				mouseDX += mouse.lLastX;
				mouseDY += mouse.lLastY;
			}

			if ((mouse.ulButtons & RI_MOUSE_LEFT_BUTTON_DOWN) != 0) io.MouseButtons |= InputOutput.MouseButton.MouseLeft;
			else io.MouseButtons &= ~InputOutput.MouseButton.MouseLeft;

			if ((mouse.ulButtons & RI_MOUSE_MIDDLE_BUTTON_DOWN) != 0) io.MouseButtons |= InputOutput.MouseButton.MouseMiddle;
			else io.MouseButtons &= ~InputOutput.MouseButton.MouseMiddle					;

			if ((mouse.ulButtons & RI_MOUSE_RIGHT_BUTTON_DOWN) != 0) io.MouseButtons |= InputOutput.MouseButton.MouseRight;
			else io.MouseButtons &= ~InputOutput.MouseButton.MouseRight;
		}

		private const int RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001 ;
		private const int RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010;
		private const int RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;

		private const int KEYBOARD_OVERRUN_MAKE_CODE = 0xff;

		private void HandleRawKeyboard(RAWKEYBOARD keyboard)
		{
			if (keyboard.MakeCode == KEYBOARD_OVERRUN_MAKE_CODE) return;
			if (keyboard.VKey > 255) return;

			logger.LogTrace($"key {keyboard.VKey} {keyboard.Flags:X4} {keyboard.MakeCode}");

			//L/R shift, L/R ctrl, L/R alt
			if (keyboard.VKey == (ushort)VK.VK_LSHIFT && (keyboard.Flags & 0x02) != 0)
				keyboard.VKey = (ushort)VK.VK_RSHIFT;
			if (keyboard.VKey == (ushort)VK.VK_CONTROL && (keyboard.Flags & 0x02) != 0)
				keyboard.VKey = (ushort)VK.VK_RCONTROL;
			if (keyboard.VKey == (ushort)VK.VK_MENU && (keyboard.Flags & 0x02) != 0)
				keyboard.VKey = (ushort)VK.VK_RMENU;

			// 0 = Key Down, 1 = Key Up, 2 = E0 prefix (extended key), 4 = E1 prefixs
			io.Keyboard[keyboard.VKey] = (keyboard.Flags & 0x01) == 0;

			var keyfn = ((keyboard.Flags & 1) != 0) ? keysUp : keysDown;
			for (int i = 0; i < keysUp.Count; i++) keyfn[i](keyboard.VKey);
		}

		public EmulationWindow(INativeOverlay nativeOverlay, ILogger<EmulationWindow> logger)
		{
			this.nativeOverlay = nativeOverlay;
			this.logger = logger;

			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				emulation = new AForm(logger, HandleRawMessage)
				{
					Name = "Emulation",
					Text = "Jammy : Alt-Tab or Middle Mouse Click to detach mouse",
					ControlBox = false,
					FormBorderStyle = FormBorderStyle.FixedSingle,
					MinimizeBox = true,
					MaximizeBox = true,
					Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
				};
				if (emulation.Handle == IntPtr.Zero)
					throw new ApplicationException();

				ss.Release();

				RegisterRawInput(emulation.Handle);

				emulation.MouseClick += Emulation_MouseClick;
				emulation.KeyPress += Emulation_KeyPress;
				emulation.KeyDown += Emulation_KeyDown;
				emulation.Deactivate += Emulation_Deactivate;
				emulation.Activated += Emulation_Activated;
				emulation.Show();

				Application.Run(emulation);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		public void Dispose()
		{
			StopRenderThread();

			emulation.Close();
			stagingTexture.Dispose();
			d3dBackBuffer.Dispose();
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

		private void Emulation_Activated(object sender, EventArgs e)
		{
			io.Reset();
		}

		private void StopRenderThread()
		{
			if (renderCts != null)
			{
				renderCts.Cancel();
				renderThread?.Join();
				renderCts.Dispose();
				renderCts = null;
			}
		}

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
		private int displayHz;
		[DllImport("user32.dll")]
		private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

		[DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
		public static extern uint timeBeginPeriod(uint uMilliseconds);

		public void SetPicture(int width, int height)
		{
			if (emulation.IsDisposed) return;

			var dm = new DEVMODE();
			EnumDisplaySettings(null!, 0, ref dm);
			logger.LogTrace($"Monitor refresh rate is {dm.dmDisplayFrequency}Hz.  Set this as high as possible!");
			displayHz = dm.dmDisplayFrequency;

			timeBeginPeriod(1);

			emulation.Invoke((Action)delegate
			{
				StopRenderThread();

				screenWidth = width;
				screenHeight = height;
				emulation.ClientSize = new Size(screenWidth, screenHeight);

#if DEBUG
				const bool useDebug = true;
#else
				const bool useDebug = false;
#endif
				DXGI.CreateDXGIFactory2<IDXGIFactory2>(useDebug, out var factory);
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

				backBufferArray = new int[screenWidth * screenHeight];
				readyBufferArray = new int[screenWidth * screenHeight];
				frontBufferArray = new int[screenWidth * screenHeight];

				nativeOverlay.Init(screenWidth, screenHeight);

				var swapDesc = new SwapChainDescription1
				{
					Width = (uint)screenWidth,
					Height = (uint)screenHeight,
					AlphaMode = AlphaMode.Ignore,
					BufferCount = 3,
					BufferUsage = Usage.RenderTargetOutput,
					Flags = SwapChainFlags.AllowTearing,
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

				d3dBackBuffer = swapchain.GetBuffer<ID3D11Texture2D>(0);

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

				renderCts = new CancellationTokenSource();
				renderThread = new Thread(RenderLoop)
				{
					IsBackground = true,
					Name = "JammyDXGIRenderThread",
					Priority = ThreadPriority.Highest
				};
				renderThread.Start();
			});
		}

		private void RenderLoop()
		{
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			long frameInterval = 1000 / displayHz;

			while (!renderCts.Token.IsCancellationRequested)
			{
				long startTime = stopwatch.ElapsedMilliseconds;

				// Only draw if there's a new frame waiting
				if (Interlocked.Exchange(ref newFrameWaiting, 0) == 1)
				{
					frontBufferArray = Interlocked.Exchange(ref readyBufferArray, frontBufferArray);

					// Map and Copy
					var dataBox = context.Map(stagingTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None);

					int rowBytes = screenWidth * sizeof(int);
					if (rowBytes == dataBox.RowPitch)
					{
						Marshal.Copy(frontBufferArray, 0, dataBox.DataPointer, screenWidth * screenHeight);
					}
					else
					{
						for (int y = 0; y < screenHeight; y++)
						{
							IntPtr destRowPtr = IntPtr.Add(dataBox.DataPointer, y * (int)dataBox.RowPitch);
							int srcOffset = y * screenWidth;
							Marshal.Copy(frontBufferArray, srcOffset, destRowPtr, screenWidth);
						}
					}
					context.Unmap(stagingTexture, 0);

					// ... Map, Copy, Unmap ...
					context.CopyResource(d3dBackBuffer, stagingTexture);
					swapchain.Present(0, PresentFlags.AllowTearing);
				}

				// Calculate time spent processing this frame
				long elapsed = stopwatch.ElapsedMilliseconds - startTime;
				long sleepTime = frameInterval - elapsed;

				// If we have time to kill, sleep efficiently
				if (sleepTime > 2) // Never sleep for less than 2ms to avoid scheduler thrashing
				{
					Thread.Sleep((int)sleepTime);
				}
				else
				{
					// We are already running behind (or at max speed), 
					// perform a quick yield to let the emulation core have the CPU
					Thread.Yield();
				}
			}
		}

		public void Blit(int[] screen)
		{
			if (emulation.IsDisposed) return;

			nativeOverlay.Render(backBufferArray);

			// Swap the finished frame into the mailbox
			backBufferArray = Interlocked.Exchange(ref readyBufferArray, backBufferArray);

			// Raise the dirty flag!
			Interlocked.Exchange(ref newFrameWaiting, 1);
		}

		// 0 = No new frame, 1 = New frame waiting
		private int newFrameWaiting = 0;

		public Types.Types.Point RecentreMouse()
		{
			var centre = new Point(0, 0);

			if (!emulation.IsDisposed)
			{
				emulation.BeginInvoke((Action)delegate ()
				{
					var emuRect = emulation.RectangleToScreen(emulation.ClientRectangle);
					centre = new Point(emuRect.X + emuRect.Width / 2, emuRect.Y + emuRect.Height / 2);
					Cursor.Position = centre;
				});
			}

			return new Types.Types.Point { X = centre.X, Y = centre.Y };
		}

		private readonly InputOutput io = new InputOutput();

		public InputOutput GetInputOutput()
		{
			var mouse = Cursor.Position;
			var buttons = Control.MouseButtons;

			io.MouseX = mouse.X;
			io.MouseY = mouse.Y;

			io.MouseDX = mouseDX;
			io.MouseDY = mouseDY;

			io.MouseButtons = 0;
			io.MouseButtons |= (buttons & MouseButtons.Left) != 0 ? InputOutput.MouseButton.MouseLeft : 0;
			io.MouseButtons |= (buttons & MouseButtons.Right) != 0 ? InputOutput.MouseButton.MouseRight : 0;
			io.MouseButtons |= (buttons & MouseButtons.Middle) != 0 ? InputOutput.MouseButton.MouseMiddle : 0;

			mouseDX = mouseDY = 0;

			return io;
		}

		private readonly List<Action<int>> keysDown = new List<Action<int>>();
		private readonly List<Action<int>> keysUp = new List<Action<int>>();

		public void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp)
		{
			//emulation.KeyDown += (sender, e) => addKeyDown(e.KeyValue);
			//emulation.KeyUp += (sender, e) => addKeyUp(e.KeyValue);

			keysDown.Add(addKeyDown);
			keysUp.Add(addKeyUp);
		}

		public bool IsActive()
		{
			return IsCaptured;
		}

		public int[] GetFramebuffer()
		{
			return backBufferArray;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left, Top, Right, Bottom;
		}

		[DllImport("user32.dll")]
		static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTDEVICE
		{
			public ushort usUsagePage;
			public ushort usUsage;
			public uint dwFlags;
			public IntPtr hwndTarget;
		}

		private void RegisterRawInput(nint hwnd)
		{
			var rid = new RAWINPUTDEVICE[2];

			//everyone
			//rid[0].dwFlags = RIDEV_INPUTSINK
			//rid[0].hwndTarget = hwnd;

			// Mouse
			rid[0].usUsagePage = 0x01;
			rid[0].usUsage = 0x02;            // Mouse
			rid[0].dwFlags = 0;               
			rid[0].hwndTarget = IntPtr.Zero; 

			// Keyboard
			rid[1].usUsagePage = 0x01;
			rid[1].usUsage = 0x06;            // Keyboard
			rid[1].dwFlags = 0;               
			rid[1].hwndTarget = IntPtr.Zero;

			RegisterRawInputDevices(rid, 2, (uint)Marshal.SizeOf(rid[0]));
		}

		const int WM_INPUT = 0x00FF;
		const uint RID_INPUT = 0x10000003;
		const uint RIM_TYPEMOUSE = 0;
		const uint RIM_TYPEKEYBOARD = 1;
		const uint RIDEV_INPUTSINK = 0x00000100;

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTHEADER
		{
			public uint dwType;
			public uint dwSize;
			public IntPtr hDevice;
			public IntPtr wParam;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWMOUSE
		{
			public ushort usFlags;
			public uint ulButtons;
			public uint ulRawButtons;
			public int lLastX;
			public int lLastY;
			public uint ulExtraInformation;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWKEYBOARD
		{
			public ushort MakeCode;
			public ushort Flags;
			public ushort Reserved;
			public ushort VKey;
			public uint Message;
			public uint ExtraInformation;
		}

		// Explicit layout union for 64-bit architecture
		[StructLayout(LayoutKind.Explicit)]
		public struct RAWINPUT
		{
			[FieldOffset(0)]
			public RAWINPUTHEADER header;

			// this will be 16 not 24 on 32-bit systems
			[FieldOffset(24)]
			public RAWMOUSE mouse;

			[FieldOffset(24)]
			public RAWKEYBOARD keyboard;
		}

		[DllImport("user32.dll")]
		static extern int GetRawInputData(IntPtr hRawInput, uint uiCommand, out RAWINPUT pData, ref uint pcbSize, uint cbSizeHeader);
	}
}