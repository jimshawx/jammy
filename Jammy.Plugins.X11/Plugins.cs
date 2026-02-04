
using ImGuiNET;
using Jammy.Plugins.Interface;
using Jammy.Plugins.Renderer;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using KeySym = ushort;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Plugins.X11
{
	public class X11PluginWindowFactory : IPluginWindowFactory
	{
		private readonly ILogger<X11PluginWindowFactory> logger;

		public X11PluginWindowFactory(ILogger<X11PluginWindowFactory> logger)
		{
			this.logger = logger;
		}

		public IPluginWindow CreatePluginWindow(IPlugin plugin)
		{
			if (plugin == null) return null;
			return new X11PluginWindow(plugin, logger);
		}
	}

	internal class X11PluginWindow : IPluginWindow
	{
		private const string X11Library = "libX11.so.6";

		[DllImport(X11Library)]
		private static extern IntPtr XOpenDisplay(IntPtr displayName);

		[DllImport(X11Library)]
		private static extern IntPtr XCreateSimpleWindow(IntPtr display, IntPtr rootWindow, int x, int y, uint width, uint height, uint borderWidth, ulong border, ulong background);

		[DllImport(X11Library)]
		private static extern void XMapWindow(IntPtr display, IntPtr window);

		[DllImport(X11Library)]
		private static extern void XStoreName(IntPtr display, IntPtr window, string windowName);

		[DllImport(X11Library)]
		private static extern void XClearWindow(IntPtr display, IntPtr window);

		[DllImport(X11Library)]
		private static extern void XDestroyWindow(IntPtr display, IntPtr window);

		[DllImport(X11Library)]
		private static extern void XCloseDisplay(IntPtr display);

		[DllImport(X11Library)]
		private static extern void XDestroyImage(ref XImage ximage);

		[DllImport(X11Library)]
		private static extern void XFlush(IntPtr display);

		[DllImport(X11Library)]
		private static extern void XSelectInput(IntPtr display, IntPtr window, long eventMask);

		[DllImport(X11Library)]
		private static extern void XNextEvent(IntPtr display, out XEvent xevent);

		[DllImport(X11Library)]
		private static extern IntPtr XCreateGC(IntPtr display, IntPtr window, uint valueMask, IntPtr values);

		[DllImport(X11Library)]
		private static extern IntPtr XPutImage(IntPtr display, IntPtr window, IntPtr gc, ref XImage ximage, int src_x, int src_y, int dest_x, int dest_y, uint width, uint height);

		[DllImport(X11Library)]
		private static extern void XFreeGC(IntPtr display, IntPtr gc);

		[DllImport(X11Library)]
		private static extern IntPtr XDefaultScreen(IntPtr display);

		[DllImport(X11Library)]
		private static extern IntPtr XDefaultGC(IntPtr display, int parm);

		[DllImport(X11Library)]
		private static extern IntPtr XRootWindow(IntPtr display, IntPtr screen);

		[DllImport(X11Library)]
		private static extern IntPtr XGetVisualInfo(IntPtr display, int vinfo_mask, ref XVisualInfo vinfo_template, out int nitems_return);

		[DllImport(X11Library)]
		private static extern IntPtr XCreateImage(IntPtr xdisplay, IntPtr xvisual, uint bpp, int format, int offset, IntPtr data, uint width, uint height, int bitmap_pad, int bytes_per_line);
		private const int ZPixmap = 2;

		[DllImport(X11Library)]
		private static extern KeySym XLookupKeysym(ref XKeyEvent e, int index);

		[DllImport(X11Library)]
		private static extern int XWarpPointer(IntPtr display, IntPtr src_w, IntPtr dest_w, int src_x, int src_y, uint src_width, uint src_height, int dest_x, int dest_y);

		[DllImport(X11Library)]
		private static extern int XGetGeometry(IntPtr display, IntPtr d, out IntPtr root_return, out int x_return, out int y_return,
			out uint width_return, out uint height_return, out uint border_width_return, out uint depth_return);

		[DllImport(X11Library)]
		private static extern int XGetWindowAttributes(IntPtr display, IntPtr window, ref XWindowAttributes attr);


		[StructLayout(LayoutKind.Sequential)]
		public struct Display
		{
			public IntPtr ext_data;   // hook for extension to hang data
			public IntPtr private1;   // private to the display
			public int fd;            // Network socket
			public int private2;      // private to the display
			public int proto_major_version; // major version of the protocol
			public int proto_minor_version; // minor version of the protocol
			public IntPtr vendor;     // vendor of the server
			public IntPtr private3;   // private to the display
			public int private4;      // private to the display
			public int private5;      // private to the display
			public int private6;      // private to the display
			public IntPtr resource_alloc; // private allocator
			public int byte_order;    // screen byte order
			public int bitmap_unit;   // padding boundary
			public int bitmap_pad;    // pad bits
			public int bitmap_bit_order;  // bit order
			public int nformats;      // number of pixmap formats
			public IntPtr pixmap_format; // pixmap format
			public int private8;      // private to the display
			public int release;       // release of the protocol
			public IntPtr private9;   // private to the display
			public IntPtr private10;  // private to the display
			public IntPtr private11;  // private to the display
			public IntPtr private12;  // private to the display
			public IntPtr private13;  // private to the display
			public int private14;     // private to the display
			public IntPtr default_screen; // default screen
			public IntPtr screens;    // screens
			public int nscreens;      // number of screens
			public IntPtr private15;  // private to the display
			public int private16;     // private to the display
			public int min_keycode;   // minimum keycode
			public int max_keycode;   // maximum keycode
			public IntPtr private17;  // private to the display
			public IntPtr private18;  // private to the display
			public IntPtr private19;  // private to the display
			public IntPtr private20;  // private to the display
			public IntPtr private21;  // private to the display
			public IntPtr private22;  // private to the display
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XImage
		{
			public int width;
			public int height;
			public int xoffset;
			public int format;
			public IntPtr data;
			public int byte_order;
			public int bitmap_unit;
			public int bitmap_bit_order;
			public int bitmap_pad;
			public int depth;
			public int bytes_per_line;
			public int bits_per_pixel;
			public IntPtr red_mask;
			public IntPtr green_mask;
			public IntPtr blue_mask;
			public IntPtr obdata;
			public IntPtr f;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XWindowAttributes
		{
			int x, y;                       // Window position relative to parent
			int width, height;              // Window dimensions
			int border_width;               // Border width in pixels
			int depth;                      // Number of bits per pixel
			IntPtr visual;                 // Pointer to visual structure
			IntPtr root;                    // Root window ID
			int @class;                      // InputOutput or InputOnly
			int bit_gravity;                // Bit gravity
			int win_gravity;                // Window gravity
			int backing_store;              // Backing store hint
			ulong backing_planes;   // Planes to be preserved
			ulong backing_pixel;    // Pixel value for background
			bool save_under;                // Save-under flag
			IntPtr colormap;              // Associated colormap
			bool map_installed;             // True if colormap is installed
			int map_state;                  // IsUnmapped, IsUnviewable, IsViewable
			long all_event_masks;           // All events selected on this window
			long your_event_mask;           // Events selected by this client
			long do_not_propagate_mask;     // Events not to propagate
			bool override_redirect;         // Override-redirect flag
			IntPtr screen;                 // Pointer to screen structure
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct XEvent
		{
			[FieldOffset(0)] public int type;
			[FieldOffset(0)] public XKeyEvent xkey;
			[FieldOffset(0)] public XButtonEvent xbutton;
			[FieldOffset(0)] public XMotionEvent xmotion;
			[FieldOffset(0)] public XConfigureEvent xconfigure;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XKeyEvent
		{
			public int type;
			public IntPtr serial;
			public bool send_event;
			public IntPtr display;
			public IntPtr window;
			public IntPtr root;
			public IntPtr subwindow;
			public IntPtr time;
			public int x, y;
			public int x_root, y_root;
			public uint state;
			public uint keycode;
			public bool same_screen;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XButtonEvent
		{
			public int type;
			public IntPtr serial;
			public bool send_event;
			public IntPtr display;
			public IntPtr window;
			public IntPtr root;
			public IntPtr subwindow;
			public IntPtr time;
			public int x, y;
			public int x_root, y_root;
			public uint state;
			public uint button;
			public bool same_screen;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XMotionEvent
		{
			public int type;
			public IntPtr serial;
			public bool send_event;
			public IntPtr display;
			public IntPtr window;
			public IntPtr root;
			public IntPtr subwindow;
			public IntPtr time;
			public int x, y;
			public int x_root, y_root;
			public uint state;
			public byte is_hint;
			public bool same_screen;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XConfigureEvent
		{
			public int type;               // Event type (ConfigureNotify)
			public IntPtr serial;   // Last request processed by the server
			public bool send_event;        // True if sent by SendEvent
			public IntPtr display;       // Display the event was read from
			public IntPtr @event;           // Window whose size/position changed
			public IntPtr window;          // Same as 'event' for top-level windows
			public int x, y;               // New window position (relative to parent)
			public int width, height;      // New window size
			public int border_width;       // New border width
			public IntPtr above;           // Window above this one in stacking order
			public bool override_redirect; // True if override-redirect is set
		}

		private const int KeyPress = 2;
		private const int KeyRelease = 3;
		private const int ButtonPress = 4;
		private const int ButtonRelease = 5;
		private const int MotionNotify = 6;
		private const int ConfigureNotify = 22;

		private const long KeyPressMask = 1 << 0;
		private const long KeyReleaseMask = 1 << 1;
		private const long ButtonPressMask = 1 << 2;
		private const long ButtonReleaseMask = 1 << 3;
		private const long PointerMotionMask = 1 << 6;
		private const long StructureNotifyMask = 1 << 17;

		private IPlugin plugin;
		private readonly ILogger logger;

		private int[] screen;

		uint screenWidth = 800;
		uint screenHeight = 600;
		private ImGuiSkiaRenderer renderer;

		public X11PluginWindow(IPlugin plugin, ILogger logger)
		{
			this.plugin = plugin;
			this.logger = logger;

			//X is doing its own scaling on my system
			float scale = 1.0f;

			renderer = new ImGuiSkiaRenderer(scale, logger);

			xdisplay = XOpenDisplay(IntPtr.Zero);
			var rootWindow = XRootWindow(xdisplay, XDefaultScreen(xdisplay));
			xwindow = XCreateSimpleWindow(xdisplay, rootWindow, 10, 10, screenWidth, screenHeight, 1, 0, 0xFFFFFF);
			XStoreName(xdisplay, xwindow, $"{plugin.GetType().Name} Window");
			XSelectInput(xdisplay, xwindow, KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | StructureNotifyMask);

			gc = XCreateGC(xdisplay, xwindow, 0, IntPtr.Zero);

			XMapWindow(xdisplay, xwindow);
			XClearWindow(xdisplay, xwindow);
			XFlush(xdisplay);

			RecreateSurface();

			var w = new System.Timers.Timer(100);
			w.Elapsed += (_, __) => OnPaint();
			w.AutoReset = true;
			w.Enabled = true;
			w.Start();

			var t = new Thread(XEventHandler);
			t.Start();
		}

		private SKImageInfo info;
		private SKSurface surface;
		private SKCanvas canvas;

		private IntPtr pixelPtr;
		private IntPtr ximagePtr;

		private void RecreateSurface()
		{
			int Width = (int)screenWidth;
			int Height = (int)screenHeight;

			// Dispose old resources
			if (ximagePtr != IntPtr.Zero)
				XDestroyImage(ref ximage);
			surface?.Dispose();

			ximagePtr = XCreateImage(xdisplay, IntPtr.Zero, 24, ZPixmap, 0, IntPtr.Zero, screenWidth, screenHeight, 32, 0);
			ximage = Marshal.PtrToStructure<XImage>(ximagePtr);

			// Create new Skia info
			info = new SKImageInfo(
				Width,
				Height,
				SKColorType.Bgra8888, // IMPORTANT: matches XImage ZPixmap pixel format
				SKAlphaType.Premul
			);

			screen = GC.AllocateArray<int>((int)(screenWidth * screenHeight), true);
			pixelPtr = Marshal.UnsafeAddrOfPinnedArrayElement(screen, 0);

			// Create Skia surface using pinned memory
			surface = SKSurface.Create(info, pixelPtr, info.RowBytes);
			canvas = surface.Canvas;

			// Create XImage that wraps the same memory
			ximage.data = pixelPtr;
		}

		public IDisposable Lock()
		{
			return renderer.Lock();
		}

		private void OnPaint()
		{
			//if (canvas == null || gdiBitmap == null)
			//	return;
			int Width = (int)screenWidth;
			int Height = (int)screenHeight;

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

			// Blit
			XPutImage(xdisplay, xwindow, gc, ref ximage, 0, 0, 0, 0, screenWidth, screenHeight);
			XFlush(xdisplay);

		}

		public void UpdatePlugin(IPlugin plugin)
		{
			if (plugin == null) return;
			this.plugin = plugin;
		}

		public void Close()
		{
			XDestroyImage(ref ximage);
			XFreeGC(xdisplay, gc);
			XDestroyWindow(xdisplay, xwindow);
			XCloseDisplay(xdisplay);
		}

		private IntPtr xdisplay;
		private IntPtr xwindow;
		private XImage ximage;
		private IntPtr gc;

		private const int VisualNoMask = 0x0;
		private const int VisualIDMask = 0x1;
		private const int VisualScreenMask = 0x2;
		private const int VisualDepthMask = 0x4;
		private const int VisualClassMask = 0x8;
		private const int VisualRedMaskMask = 0x10;
		private const int VisualGreenMaskMask = 0x20;
		private const int VisualBlueMaskMask = 0x40;
		private const int VisualColormapSizeMask = 0x80;
		private const int VisualBitsPerRGBMask = 0x100;
		private const int VisualAllMask = 0x1FF;

		[StructLayout(LayoutKind.Sequential)]
		private struct XVisualInfo
		{
			public IntPtr visual;
			public UInt64 visualid;
			public int screen;
			public uint depth;
			public int @class;
			public uint red_mask;
			public uint green_mask;
			public uint blue_mask;
			public int colormap_size;
			public int bits_per_rgb;
		}

		private void XEventHandler()
		{
			XEvent xevent;
			while (true)
			{
				XNextEvent(xdisplay, out xevent);

				switch (xevent.type)
				{
					case KeyPress:
						{
							using (var imgui = Lock())
							{
								ImGuiKey vk = 0;
								KeySym ksym = XLookupKeysym(ref xevent.xkey, 0);

								var io = ImGui.GetIO();

								//https://wiki.linuxquestions.org/wiki/List_of_keysyms
								if ((ksym & 0xff00) == 0)
									vk = keylook[ksym & 0xff];
								else if ((ksym & 0xff00) == 0xff00)
									vk = keylook2[ksym & 0xff];

								io.AddKeyEvent(vk, true);

								//io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
								//io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
								//io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);


								Trace.WriteLine($"keydown {xevent.xkey.keycode} {ksym:X4} {vk}");
							}
						}
						break;
					case KeyRelease:
						{
							using (var imgui = Lock())
							{
								ImGuiKey vk = 0;
								KeySym ksym = XLookupKeysym(ref xevent.xkey, 0);

								var io = ImGui.GetIO();

								if ((ksym & 0xff00) == 0)
									vk = keylook[ksym & 0xff];
								else if ((ksym & 0xff00) == 0xff00)
									vk = keylook2[ksym & 0xff];

								io.AddKeyEvent(vk, false);

								//io.AddKeyEvent(ImGuiKey.ModCtrl, e.Control);
								//io.AddKeyEvent(ImGuiKey.ModShift, e.Shift);
								//io.AddKeyEvent(ImGuiKey.ModAlt, e.Alt);
								Trace.WriteLine($"keyup {xevent.xkey.keycode} {ksym:X4} {vk}");
							}
						}
						break;
					case ButtonPress:
						{
							using (var imgui = Lock())
							{
								if ((xevent.xbutton.button & 0xff) == 1)
									ImGui.GetIO().AddMouseButtonEvent(0, true);
								if ((xevent.xbutton.button & 0xff) == 3)
									ImGui.GetIO().AddMouseButtonEvent(1, true);
								if ((xevent.xbutton.button & 0xff) == 2)
									ImGui.GetIO().AddMouseButtonEvent(2, true);
							}
						}
						break;
					case ButtonRelease:
						{
							using (var imgui = Lock())
							{
								if ((xevent.xbutton.button & 0xff) == 1)
									ImGui.GetIO().AddMouseButtonEvent(0, false);
								if ((xevent.xbutton.button & 0xff) == 3)
									ImGui.GetIO().AddMouseButtonEvent(1, false);
								if ((xevent.xbutton.button & 0xff) == 2)
									ImGui.GetIO().AddMouseButtonEvent(2, false);
							}
						}
						break;
					case MotionNotify:
						{
							using (var imgui = Lock())
							{
								ImGui.GetIO().AddMousePosEvent(xevent.xmotion.x, xevent.xmotion.y);
							}
						}
						break;
					case ConfigureNotify:
						{
							XConfigureEvent xce = xevent.xconfigure;

							if (screenWidth == (uint)xce.width && screenHeight == (uint)xce.height)
								break;

							Trace.WriteLine($"Resize from {screenWidth}x{screenHeight} to {xce.width}x{xce.height}");

							using (var imgui = Lock())
							{
								screenWidth = (uint)xce.width;
								screenHeight = (uint)xce.height;
								//RecreateSurface();
								Trace.WriteLine($"configurenotify {xce.width} {xce.height}");
							}
						}
						break;
					default:
						Console.WriteLine("Unhandled XEvent type: " + xevent.type);
						break;
				}
			}
		}

		private readonly ImGuiKey[] keylook =
		[
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,//0x10
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 0,
		 ImGuiKey.Space, //0x20
		ImGuiKey._1,ImGuiKey._2,ImGuiKey._3,ImGuiKey._4,ImGuiKey._5,ImGuiKey._6,ImGuiKey._7,ImGuiKey._8,ImGuiKey._9,ImGuiKey._0,
		 0,
		 ImGuiKey.Comma,//ImGuiKey.PLUS,
		 ImGuiKey.Minus,//ImGuiKey.LESS_THAN,
		 ImGuiKey.Period,//ImGuiKey.GREATER_THAN,
		 ImGuiKey.Slash,//ImGuiKey.QUESTION_MARK,
		ImGuiKey._0, //0x30
		ImGuiKey._1, ImGuiKey._2, ImGuiKey._3, ImGuiKey._4, ImGuiKey._5, ImGuiKey._6,ImGuiKey._7,ImGuiKey._8, ImGuiKey._9,
		 0,//ImGuiKey.SEMI_COLON,
		 ImGuiKey.Semicolon,//ImGuiKey.SEMI_COLON,
		 0,//ImGuiKey.LESS_THAN,
		 ImGuiKey.Equal,//ImGuiKey.GREATER_THAN,
		 0,//ImGuiKey.QUESTION_MARK,
		 0,//ImGuiKey.SINGLE_QUOTE,
		 0,//ImGuiKey.SINGLE_QUOTE,//0x40
		ImGuiKey.A,ImGuiKey.B,ImGuiKey.C,ImGuiKey.D,ImGuiKey.E,ImGuiKey.F,ImGuiKey.G,ImGuiKey.H,ImGuiKey.I,ImGuiKey.J,ImGuiKey.K,ImGuiKey.L,ImGuiKey.M,
		ImGuiKey.N,ImGuiKey.O,ImGuiKey.P,ImGuiKey.Q,ImGuiKey.R,ImGuiKey.S,ImGuiKey.T,ImGuiKey.U,ImGuiKey.V,ImGuiKey.W,ImGuiKey.X,ImGuiKey.Y,ImGuiKey.Z,
		 ImGuiKey.LeftBracket,//ImGuiKey.OPEN_SQR_BRACKET,
		 0,//ImGuiKey.RSX,
		 ImGuiKey.RightBracket,//ImGuiKey.CLOSE_SQR_BRACKET,
		0,// ImGuiKey.SQUIGLE,
		0,//ImGuiKey.MINUS,
		0,//ImGuiKey.SQUIGLE,//0x60
		ImGuiKey.A,ImGuiKey.B,ImGuiKey.C,ImGuiKey.D,ImGuiKey.E,ImGuiKey.F,ImGuiKey.G,ImGuiKey.H,ImGuiKey.I,ImGuiKey.J,ImGuiKey.K,ImGuiKey.L,ImGuiKey.M,
		ImGuiKey.N,ImGuiKey.O,ImGuiKey.P,ImGuiKey.Q,ImGuiKey.R,ImGuiKey.S,ImGuiKey.T,ImGuiKey.U,ImGuiKey.V,ImGuiKey.W,ImGuiKey.X,ImGuiKey.Y,ImGuiKey.Z,
		0,// ImGuiKey.OPEN_SQR_BRACKET,
		 0,//ImGuiKey.RSX,
		0,// ImGuiKey.CLOSE_SQR_BRACKET,
		 0,//ImGuiKey.SQUIGLE,
		];

		ImGuiKey[] keylook2 =
		[
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		ImGuiKey.Backspace,//0x8
		ImGuiKey.Tab,
		0,
		0,
		0,
		ImGuiKey.Enter,//0xd
		0,
		0,
		0,//0x10
		0,
		0,
		0,//pause key
		0,//scroll lock
		0,//ImGuiKey.SYSRQ,//0x15
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0x20
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0x30
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0x40
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		ImGuiKey.Home,//0x50
		ImGuiKey.LeftArrow,
		ImGuiKey.UpArrow,
		ImGuiKey.RightArrow,
		ImGuiKey.DownArrow,
		ImGuiKey.PageUp,
		ImGuiKey.PageDown,
		ImGuiKey.End,//0x57
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0x60
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0x70
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0x80
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//ImGuiKey.KEYPAD_ENTER,
		0,
		0,
		0,//0x90
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0xa0
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//keypad times
		0,//ImGuiKey.KEYPAD_PLUS,
		0,
		0,//ImGuiKey.KEYPAD_MINUS,
		0,//ImGuiKey.KEYPAD_FULL_STOP,
		0,//keypad divide
		ImGuiKey.Keypad0,//0xb0
		ImGuiKey.Keypad1,
		ImGuiKey.Keypad2,
		ImGuiKey.Keypad3,
		ImGuiKey.Keypad4,
		ImGuiKey.Keypad5,
		ImGuiKey.Keypad6,
		ImGuiKey.Keypad7,
		ImGuiKey.Keypad8,
		ImGuiKey.Keypad9,
		0,
		0,
		0,
		0,
		ImGuiKey.F1,
		ImGuiKey.F2,
		ImGuiKey.F3,//0xc0
		ImGuiKey.F4,
		ImGuiKey.F5,
		ImGuiKey.F6,
		ImGuiKey.F7,
		ImGuiKey.F8,
		ImGuiKey.F9,
		ImGuiKey.F10,
		ImGuiKey.F11,
		ImGuiKey.F12,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0xd0
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,//0xe0
		ImGuiKey.LeftShift,
		ImGuiKey.RightShift,
		ImGuiKey.LeftCtrl,
		ImGuiKey.RightCtrl,
		ImGuiKey.CapsLock,
		0,
		0,
		0,
		0,//ImGuiKey.LEFT_ALT,
		0,//ImGuiKey.RIGHT_ALT,
		0,
		0,
		0,
		0,
		0,
		0,//0xf0
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0
		];
	}
}

