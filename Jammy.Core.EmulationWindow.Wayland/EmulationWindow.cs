using System.Runtime.InteropServices;
using Jammy.Core.Interface.Interfaces;
using Jammy.NativeOverlay;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.Wayland
{
	public class EmulationWindow : IEmulationWindow, IInputOutput, IDisposable
	{
		[DllImport("emuwayland")]
		private static extern IntPtr emu_window_create(int width, int height);

		[DllImport("emuwayland")]
		private static extern void emu_window_render(IntPtr win, int[] pixels);

		[DllImport("emuwayland")]
		private static extern void emu_window_pump(IntPtr win);

		[DllImport("emuwayland")]
		private static extern void emu_window_wait(IntPtr win);

		[DllImport("emuwayland")]
		private static extern void emu_window_destroy(IntPtr win);

		[DllImport("emuwayland")]
		private static extern void emu_window_set_key_callback(IntPtr win, KeyCallback cb);

		[DllImport("emuwayland")]
		private static extern void emu_window_set_button_callback(IntPtr win, ButtonCallback cb);

		[DllImport("emuwayland")]
		private static extern void emu_window_set_motion_callback(IntPtr win, MotionCallback cb);

		[DllImport("emuwayland")]
		private static extern void emu_window_set_locked_callback(IntPtr win, LockCallback cb);

		[DllImport("emuwayland")]
		private static extern void emu_window_set_unlocked_callback(IntPtr win, LockCallback cb);


		private IntPtr _native;

		public EmulationWindow(IOverlayCollection overlayCollection, ILogger<EmulationWindow> logger)
		{
			this.overlayCollection = overlayCollection;
			this.logger = logger;
		}

		public void Dispose()
		{
			if (_native != IntPtr.Zero)
			{
				emu_window_destroy(_native);
				_native = IntPtr.Zero;
			}
		}

		private int[] screen;
		private IOverlayCollection overlayCollection;
		private readonly ILogger<EmulationWindow> logger;
		private uint screenWidth;
		private uint screenHeight;
		private int displayHz;

		public void SetPicture(int width, int height)
		{
			_native = emu_window_create(width, height);
			if (_native == IntPtr.Zero)
				throw new Exception("Failed to create Wayland window");

			_keyCb = (key, state) =>
			{
				if (state == 1) KeyDown?.Invoke(key);
				else KeyUp?.Invoke(key);
			};

			_buttonCb = (button, state) =>
			{
				if (state == 1) MouseDown?.Invoke(button);
				else MouseUp?.Invoke(button);
			};

			_motionCb = (dx, dy) => MouseMove?.Invoke(dx, dy);

			_lockedCb = () => PointerLocked?.Invoke();
			_unlockedCb = () => PointerUnlocked?.Invoke();

			emu_window_set_key_callback(_native, _keyCb);
			emu_window_set_button_callback(_native, _buttonCb);
			emu_window_set_motion_callback(_native, _motionCb);
			emu_window_set_locked_callback(_native, _lockedCb);
			emu_window_set_unlocked_callback(_native, _unlockedCb);

			screenWidth = (uint)width;
			screenHeight = (uint)height;
			displayHz = 60;

			screen = GC.AllocateArray<int>((int)(screenWidth * screenHeight), true);

			var t = new Thread(WaitEvents);
			t.Start();
		}

		public void Blit(int[] pixels)
		{
			emu_window_render(_native, pixels);
		}

		private void PumpEvents()
		{
			emu_window_pump(_native);
		}

		private void WaitEvents()
		{
			while (true)
				emu_window_wait(_native);
		}

		public bool IsCaptured { get; private set; } = false;

		public Types.Types.Point RecentreMouse()
		{
			//var centre = new Point(0, 0);

			// if (!emulation.IsDisposed)
			// {
			// 	emulation.Invoke((Action)delegate()
			// 	{
			// 		//put the cursor back in the middle of the emulation window
			// 		var emuRect = emulation.RectangleToScreen(emulation.ClientRectangle);
			// 		centre = new Point(emuRect.X + emuRect.Width / 2, emuRect.Y + emuRect.Height / 2);
			// 		Cursor.Position = centre;
			// 	});
			//}

			return new Types.Types.Point { X = 0, Y = 0 };
		}

		public void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp)
		{
			// emulation.KeyDown += (sender, e) => addKeyDown(e.KeyValue);
			// emulation.KeyUp += (sender, e) => addKeyUp(e.KeyValue);
		}

		public bool IsActive()
		{
			return false;
			//return IsCaptured;
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
			return io;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void KeyCallback(int key, int state);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ButtonCallback(int button, int state);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MotionCallback(double dx, double dy);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LockCallback();

		private KeyCallback _keyCb;
		private ButtonCallback _buttonCb;
		private MotionCallback _motionCb;
		private LockCallback _lockedCb;
		private LockCallback _unlockedCb;

		public event Action<int>? KeyDown;
		public event Action<int>? KeyUp;
		public event Action<int>? MouseDown;
		public event Action<int>? MouseUp;
		public event Action<double, double>? MouseMove;
		public event Action? PointerLocked;
		public event Action? PointerUnlocked;
	}
}

