using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Jammy.NativeOverlay;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.GDI
{
	public class EmulationWindow : IEmulationWindow, IDisposable
	{
		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int key);

		private readonly INativeOverlay nativeOverlay;
		private readonly ILogger logger;
		private Form emulation;
		private int[] screen;

		public class AForm : Form
		{
			private readonly Action<Message> HandleRawMessage;

			public AForm(Action<Message> rawMessageHandler)
			{
				this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
				this.HandleRawMessage = rawMessageHandler;
			}

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == WM_INPUT)
					HandleRawMessage(m);

				base.WndProc(ref m);
			}
		}

		public EmulationWindow(INativeOverlay nativeOverlay, ILogger<EmulationWindow> logger)
		{
			this.nativeOverlay = nativeOverlay;
			this.logger = logger;

			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				emulation = new AForm(HandleRawMessage) {Name = "Emulation", Text = "Jammy : Alt-Tab or Middle Mouse Click to detach mouse", ControlBox = false, FormBorderStyle = FormBorderStyle.FixedSingle, MinimizeBox = true, MaximizeBox = true};

				if (emulation.Handle == IntPtr.Zero)
					throw new ApplicationException();

				ss.Release();

				RegisterRawInput(emulation.Handle);

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

		private Bitmap bitmap;
		private PictureBox picture;
		private int screenWidth;
		private int screenHeight;

		public void SetPicture(int width, int height)
		{
			if (emulation.IsDisposed) return;

			screen = new int[width * height];
			nativeOverlay.Init(width, height);

			emulation.Invoke((Action)delegate
			{
				screenWidth = width;
				screenHeight = height;

				emulation.ClientSize = new Size(screenWidth, screenHeight);
				bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppRgb);
				picture = new PictureBox {Image = bitmap, ClientSize = new Size(screenWidth, screenHeight), Enabled = false};

				//try to scale the box
				//picture.SizeMode = PictureBoxSizeMode.StretchImage;
				//int scaledHeight = (SCREEN_HEIGHT * 6) / 5;
				//emulation.ClientSize = new System.Drawing.Size(SCREEN_WIDTH, scaledHeight);
				//picture.ClientSize = new System.Drawing.Size(SCREEN_WIDTH, scaledHeight);

				emulation.Controls.Add(picture);
				emulation.Show();
			});
		}

		public void Blit(int[] screen)
		{
			if (emulation.IsDisposed) return;

			nativeOverlay.Render(screen);

			emulation.Invoke((Action)delegate
			{
				var bitmapData = bitmap.LockBits(new Rectangle(0, 0, screenWidth, screenHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
				Marshal.Copy(screen, 0, bitmapData.Scan0, screen.Length);
				bitmap.UnlockBits(bitmapData);
				picture.Refresh();
			});
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
			//this is good but slow
			//return Form.ActiveForm == emulation;
		}

		public int[] GetFramebuffer()
		{
			return screen;
		}

		private readonly InputOutput io = new InputOutput();

		public InputOutput GetInputOutput()
		{
			io.MouseDX = mouseDX;
			io.MouseDY = mouseDY;

			mouseDX = mouseDY = 0;

			return io;
		}

		private int mouseDX, mouseDY;

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
			else io.MouseButtons &= ~InputOutput.MouseButton.MouseMiddle;

			if ((mouse.ulButtons & RI_MOUSE_RIGHT_BUTTON_DOWN) != 0) io.MouseButtons |= InputOutput.MouseButton.MouseRight;
			else io.MouseButtons &= ~InputOutput.MouseButton.MouseRight;
		}

		private const int RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
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