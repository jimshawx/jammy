using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Jammy.NativeOverlay.Overlays;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.Window
{
	public static class EnumerableExtensions
	{
		public static void ForEach<T>(this IEnumerable<T> e, Action<T> action) { foreach (var item in e) { action(item); } }
	}

	public class EmulationWindow : IEmulationWindow, IDisposable
	{
		private const string ClassName = "JammyWindowClass";
		private IntPtr hWnd;
		private IntPtr hdc;

		private const int WM_MOVE = 0x0003;
		private const int WM_SIZE = 0x0005;
		private const int WM_ACTIVATE = 0x0006;
		private const int WM_SETFOCUS = 0x0007;
		private const int WM_KILLFOCUS = 0x0008;
		private const int WM_CLOSE = 0x0010;
		private const int WM_PAINT = 0x000F;
		private const int WM_ERASEBKGND = 0x0014;
		private const int WM_SETCURSOR = 0x0020;
		private const int WM_WINDOWPOSCHANGED = 0x0047;
		private const int WM_KEYDOWN = 0x0100;
		private const int WM_KEYUP = 0x0101;
		private const int WM_CHAR = 0x0102;
		private const int WM_SYSKEYDOWN = 0x0104;
		private const int WM_SYSKEYUP = 0x0105;
		private const int WM_SYSCHAR = 0x0106;
		private const int WM_SYSCOMMAND = 0x0112;
		private const int WM_LBUTTONDOWN = 0x0201;
		private const int WM_RBUTTONDOWN = 0x0204;
		private const int WM_MBUTTONDOWN = 0x0207;
		private const int WM_IME_CHAR = 0x0286;

		private RECT emuRect = new RECT();
		private Point EmuPos = new Point();

		private class EventHandlers
		{
			private readonly Dictionary<string, List<dynamic>> events = new Dictionary<string, List<dynamic>>();

			public const string s_mouseClickEvent = "MouseClick";
			public const string s_keyDownEvent = "KeyDown";
			public const string s_keyPressEvent = "KeyPress";
			public const string s_keyUpEvent = "KeyUp";

			public void AddHandler(string eventType, dynamic handler)
			{
				if (!events.ContainsKey(eventType))
					events.Add(eventType, new List<dynamic>());
				events[eventType].Add(handler);
			}
			public void RemoveHandler(string eventType, dynamic handler)
			{
				if (!events.ContainsKey(eventType)) return;
				events[eventType].Remove(handler);
			}

			public IEnumerable<T> GetHandlers<T>(string eventType)
			{
				if (!events.TryGetValue(eventType, out var ls)) return new List<T>();
				return ls.Cast<T>();
			}
		}

		private readonly EventHandlers Events = new EventHandlers();

		private event MouseEventHandler MouseClick
		{
			add => Events.AddHandler(EventHandlers.s_mouseClickEvent, value);
			remove => Events.RemoveHandler(EventHandlers.s_mouseClickEvent, value);

		}
		private event KeyEventHandler KeyDown
		{
			add => Events.AddHandler(EventHandlers.s_keyDownEvent, value);
			remove => Events.RemoveHandler(EventHandlers.s_keyDownEvent, value);
		}

		private event KeyPressEventHandler KeyPress
		{
			add => Events.AddHandler(EventHandlers.s_keyPressEvent, value);
			remove => Events.RemoveHandler(EventHandlers.s_keyPressEvent, value);
		}

		private event KeyEventHandler KeyUp
		{
			add => Events.AddHandler(EventHandlers.s_keyUpEvent, value);
			remove => Events.RemoveHandler(EventHandlers.s_keyUpEvent, value);
		}

		private static Keys ModifierKeys
		{
			get
			{
				Keys modifiers = 0;

				if (GetKeyState((int)Keys.ShiftKey) < 0)
					modifiers |= Keys.Shift;
				if (GetKeyState((int)Keys.ControlKey) < 0)
					modifiers |= Keys.Control;
				if (GetKeyState((int)Keys.Menu) < 0)
					modifiers |= Keys.Alt;
				return modifiers;
			}
		}

		private const int SC_KEYMENU = 0xF100;

		// Window procedure to handle messages
		private bool windowIsActive = false;

		private IntPtr WindowProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
		{
			switch (msg)
			{
				case WM_ACTIVATE:
					windowIsActive = wParam != UIntPtr.Zero;
					break;
				case WM_CLOSE:
					DestroyWindow(hWnd);
					break;

				case WM_ERASEBKGND:
					return 1;

				case WM_SYSKEYDOWN:
				case WM_KEYDOWN:
					{ 
					var h = Events.GetHandlers<KeyEventHandler>(EventHandlers.s_keyDownEvent);
					var k = new KeyEventArgs((Keys)wParam | ModifierKeys);
					h.ForEach(x=>x(new object(), k));

					if ((GetAsyncKeyState((int)VK.VK_MENU) & 0x8000) != 0)
						PostMessage(hWnd, WM_CHAR, (UIntPtr)VK.VK_TAB, IntPtr.Zero);
					}
					break;

				case WM_SYSKEYUP:
				case WM_KEYUP:
					{ 
					var h = Events.GetHandlers<KeyEventHandler>(EventHandlers.s_keyUpEvent);
					var k = new KeyEventArgs((Keys)wParam|ModifierKeys);
					h.ForEach(x => x(new object(), k));
					}
					break;

				case WM_IME_CHAR:
				case WM_SYSCHAR:
				case WM_CHAR:
					{ 
						logger.LogTrace($"{wParam:X8}");
					var h = Events.GetHandlers<KeyPressEventHandler>(EventHandlers.s_keyPressEvent);
					var k = new KeyPressEventArgs((char)wParam);
					h.ForEach(x => x(new object(), k));
					}
					break;

				case WM_LBUTTONDOWN:
					if (!windowIsActive) break;
					{
					var h = Events.GetHandlers<MouseEventHandler>(EventHandlers.s_mouseClickEvent);
					var m = new MouseEventArgs(MouseButtons.Left, 1, (short)(lParam >> 16), (short)(lParam & 0xffff), 0);
					h.ForEach(x => x(new object(), m));
					}
					break;
				case WM_MBUTTONDOWN:
					{
					var h = Events.GetHandlers<MouseEventHandler>(EventHandlers.s_mouseClickEvent);
					var m = new MouseEventArgs(MouseButtons.Middle, 1, (short)(lParam >> 16), (short)(lParam & 0xffff), 0);
					h.ForEach(x => x(new object(), m));
					}
					break;
				case WM_RBUTTONDOWN:
					{
					var h = Events.GetHandlers<MouseEventHandler>(EventHandlers.s_mouseClickEvent);
					var m = new MouseEventArgs(MouseButtons.Right, 1, (short)(lParam >> 16), (short)(lParam & 0xffff), 0);
					h.ForEach(x => x(new object(), m));
					}
					break;

				case WM_MOVE:
				case WM_SIZE:
				case WM_WINDOWPOSCHANGED:
					GetClientRect(hWnd, ref emuRect);
					EmuPos.X = emuRect.left;
					EmuPos.Y = emuRect.top;
					ClientToScreen(hWnd, ref EmuPos);
					emuRect.right = EmuPos.X + emuRect.right - emuRect.left;
					emuRect.bottom = EmuPos.Y + emuRect.bottom - emuRect.top;
					emuRect.left = EmuPos.X;
					emuRect.top = EmuPos.Y;
					break;

				default:
					return DefWindowProc(hWnd, msg, wParam, lParam);
			}
			return IntPtr.Zero;
		}

		public delegate IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam);

		private const int CS_OWNDC = 0x20;

		private const int WS_EX_TOPMOST = 0x00000008;
		private const int WS_VISIBLE =	0x10000000;

		private IntPtr arrow;
		
		private WndProc wndProcDelegate; 

		public void Create()
		{
			//arrow = LoadCursor(IntPtr.Zero, IDC_ARROW);
			wndProcDelegate = new WndProc(WindowProc);

			var wndClass = new WNDCLASSEX();
			wndClass.cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>();
			wndClass.lpszClassName = ClassName;
			wndClass.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate); ;
			wndClass.style = CS_OWNDC;
			wndClass.hInstance = Marshal.GetHINSTANCE(typeof(EmulationWindow).Module);
			//wndClass.hCursor = arrow;

			ushort regResult = RegisterClassEx(ref wndClass);
			if (regResult == 0)
				throw new Exception("Failed to register window class.");

			hWnd = CreateWindowEx(0/*WS_EX_TOPMOST*/, ClassName, "Jammy : Alt-Tab or Middle Mouse Click to detach mouse", WS_VISIBLE, 100,100, 100, 100, IntPtr.Zero, IntPtr.Zero, wndClass.hInstance, IntPtr.Zero);
			if (hWnd == IntPtr.Zero)
				throw new Exception("Failed to create window.");
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct WNDCLASSEX
		{
			public uint cbSize;
			public uint style;
			public IntPtr lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			[MarshalAs(UnmanagedType.LPTStr)] public string lpszMenuName;
			[MarshalAs(UnmanagedType.LPTStr)] public string lpszClassName;
			public IntPtr hIconSm;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MSG
		{
			public IntPtr hWnd;
			public uint message;
			public UIntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public Point pt;
			public int lPrivate;
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern ushort RegisterClassEx(ref WNDCLASSEX lpWndClass);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, [MarshalAs(UnmanagedType.LPUTF8Str)] string lpWindowName, uint dwStyle,
			int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

		[DllImport("user32.dll")]
		private static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

		[DllImport("user32.dll")]
		private static extern IntPtr DispatchMessage(ref MSG lpMsg);

		[DllImport("user32.dll")]
		private static extern int TranslateMessage(ref MSG lpMsg);

		[DllImport("user32.dll")]
		private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr DestroyWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int key);

		[DllImport("user32.dll")]
		private static extern short GetKeyState(int key);

		[DllImport("user32.dll")]
		private static extern IntPtr SetCursor(IntPtr cursor);

		[DllImport("user32.dll")]
		private static extern IntPtr GetDC(IntPtr hWnd);
		
		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

		[DllImport("user32.dll")]
		private static extern void SetCursorPos(int X, int Y);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UpdateWindow(IntPtr hWnd);

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left; 
			public int top; 
			public int right; 
			public int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Point
		{
			public int X;
			public int Y;
		}

		[DllImport("user32.dll")]
		private static extern void GetClientRect(IntPtr hWnd, ref RECT rect);

		[DllImport("user32.dll")]
		private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

		private const IntPtr IDC_ARROW = 32512;

		[DllImport("user32.dll")]
		private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr cursorName);

		[DllImport("gdi32.dll", EntryPoint = "SetDIBitsToDevice", SetLastError = true)]
		private static extern int SetDIBitsToDevice([In] IntPtr hdc, int xDest, int yDest, uint w, uint h, int xSrc,
			int ySrc, uint startScan, uint cLines, [In] int[] lpvBits,
			[In] ref BITMAPINFO lpbmi, BITMAPINFO.DIBColorTable colorUse);

		[DllImport("user32.dll")]
		private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

		[DllImport("user32.dll")]
		private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll")]
		private static extern int AdjustWindowRectEx(ref RECT lpRect, int dwStyle, int bMenu, int dwExStyle);

		[DllImport("user32.dll")]
		private static extern IntPtr GetFocus();

		[DllImport("user32.dll")]
		private static extern int ShowCursor(int show);

		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		private static extern int PostMessage(IntPtr hWnd, int msg, UIntPtr wParam, IntPtr lParam);

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

		private readonly IDiskLightOverlay diskLightOverlay;
		private readonly ITicksOverlay ticksOverlay;
		private readonly ILogger logger;
		// Form emulation;
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
				Create();

				MouseClick += Emulation_MouseClick;
				KeyPress += Emulation_KeyPress;
				KeyDown += Emulation_KeyDown;
				//emulation.Deactivate += Emulation_Deactivate;

				ss.Release();

				// Main message loop
				MSG msg;
				while (GetMessage(out msg, hWnd, 0, 0) > 0)
				{
					TranslateMessage(ref msg);
					DispatchMessage(ref msg);
				}
			});
			//t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		public void Dispose()
		{
			//emulation.Close();
		}

		public bool IsCaptured { get; private set; } = false;

		private void Capture(string where)
		{
			if (!IsCaptured)
			{
				logger.LogTrace($"Capture {where}");
				IsCaptured = true;
				//SetCursor(IntPtr.Zero);
				ShowCursor(0);
			}
		}

		private void Release(string where)
		{
			logger.LogTrace($"Release {where} Was Captured? {IsCaptured}");
			if (IsCaptured)
			{
				IsCaptured = false;
				//SetCursor(arrow);
				ShowCursor(1);
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
			logger.LogTrace($"KeyPress {(int)e.KeyChar:X8}");

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

		private int screenWidth;
		private int screenHeight;
		private BITMAPINFO lpbmi;

		private int displayHz;

		private const uint SWP_NOMOVE = 0x0002;
		private const uint SWP_NOZORDER = 0x0004;
		private const uint SWP_NOREDRAW = 0x0008;
		private const uint SWP_SHOWWINDOW = 0x0040;

		private const int GWL_STYLE = -16;
		private const int GWL_EXSTYLE = -20;

		public void SetPicture(int width, int height)
		{
			var dm = new DEVMODE();
			EnumDisplaySettings(null!, 0, ref dm);
			logger.LogTrace($"Monitor refresh rate is {dm.dmDisplayFrequency}Hz.  Set this as high as possible!");
			displayHz = dm.dmDisplayFrequency;

			screen = new int[width * height];

			screenWidth = width;
			screenHeight = height;

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

			var rect = new RECT { right = width, bottom = height};
			var style = GetWindowLong(hWnd, GWL_STYLE);
			var exstyle = GetWindowLong(hWnd, GWL_EXSTYLE);
			AdjustWindowRectEx(ref rect, style, 0, exstyle);
			SetWindowPos(hWnd, IntPtr.Zero, 0,0, rect.right-rect.left, rect.bottom-rect.top, SWP_NOMOVE|SWP_SHOWWINDOW| SWP_NOZORDER| SWP_NOREDRAW);
			hdc = GetDC(hWnd);
		}

		public void Blit(int[] screen)
		{
			ticksOverlay.Render();
			diskLightOverlay.Render();

			//var hdc = GetDC(hWnd);
			SetDIBitsToDevice(hdc, 0, 0, (uint)screenWidth, (uint)screenHeight,
				0, 0, 0, (uint)screenHeight,
				screen, ref lpbmi, BITMAPINFO.DIBColorTable.DIB_RGB_COLORS);
			//ReleaseDC(hWnd, hdc);
		}

		public Types.Types.Point RecentreMouse()
		{
			//put the cursor back in the middle of the emulation window

			int x = emuRect.left + (emuRect.right - emuRect.left)/2;
			int y = emuRect.top + (emuRect.bottom - emuRect.top) / 2;

			SetCursorPos(x, y);

			return new Types.Types.Point { X = x, Y = y };
		}

		public void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp)
		{
			KeyDown += (sender, e) => addKeyDown(e.KeyValue);
			KeyUp += (sender, e) => addKeyUp(e.KeyValue);
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