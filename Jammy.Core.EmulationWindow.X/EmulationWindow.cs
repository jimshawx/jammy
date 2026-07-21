using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Jammy.NativeOverlay;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Jammy.Core.EmulationWindow.X.EmulationWindow;
using KeySym = ushort;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.X
{
	public class EmulationWindow : IEmulationWindow, IInputOutput, IDisposable
	{
		private const string X11Library = "libX11.so.6";

		[DllImport(X11Library)]
		private static extern IntPtr XOpenDisplay(string displayName);
		
		[DllImport(X11Library)]
		private static extern IntPtr XServerVendor(IntPtr display);

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

		[DllImport(X11Library, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool XGetEventData(IntPtr display, ref XGenericEventCookie cookie);

		[DllImport(X11Library, CallingConvention = CallingConvention.Cdecl)]
		public static extern void XFreeEventData(IntPtr display, ref XGenericEventCookie cookie);

		[DllImport("libX11.so.6")]
		public static extern int XGrabPointer(IntPtr display, IntPtr grab_window, bool owner_events,
									  uint event_mask, int pointer_mode, int keyboard_mode,
									  IntPtr confine_to, IntPtr cursor, IntPtr time);

		[DllImport("libX11.so.6")]
		public static extern int XUngrabPointer(IntPtr display, IntPtr time);

		[DllImport("libX11.so.6")]
		public static extern IntPtr XCreateBitmapFromData(IntPtr display, IntPtr drawable, byte[] data, int width, int height);

		[DllImport("libX11.so.6")]
		public static extern IntPtr XCreatePixmapCursor(IntPtr display, IntPtr source, IntPtr mask, ref XColor foreground_color, ref XColor background_color, int x, int y);

		[DllImport("libX11.so.6")]
		public static extern int XDefineCursor(IntPtr display, IntPtr window, IntPtr cursor);

		[DllImport("libX11.so.6")]
		public static extern int XFreePixmap(IntPtr display, IntPtr pixmap);

		private const string XiLibrary = "libXi.so.6";

		// --- libXi (XInput2) ---
		[DllImport(XiLibrary, CallingConvention = CallingConvention.Cdecl)]
		public static extern int XISelectEvents(IntPtr display, IntPtr window, ref XIEventMask masks, int num_masks);

		[DllImport(XiLibrary, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr XIQueryDevice(IntPtr display, int deviceid, out int ndevices_return);

		[DllImport(XiLibrary, CallingConvention = CallingConvention.Cdecl)]
		public static extern void XIFreeDeviceInfo(IntPtr info);

		[DllImport("libXi.so.6", CallingConvention = CallingConvention.Cdecl)]
		public static extern int XIGrabDevice(
			IntPtr display,
			int deviceid,
			IntPtr grab_window,
			IntPtr time,
			IntPtr cursor,
			int grab_mode,
			int paired_device_mode,
			bool owner_events,
			ref XIEventMask mask
		);
		[DllImport("libXi.so.6", CallingConvention = CallingConvention.Cdecl)]
		public static extern int XIUngrabDevice(IntPtr display, int deviceid, IntPtr time);

		[DllImport("libXfixes.so.3", CallingConvention = CallingConvention.Cdecl)]
		public static extern void XFixesHideCursor(IntPtr display, IntPtr window);

		[DllImport("libXfixes.so.3", CallingConvention = CallingConvention.Cdecl)]
		public static extern void XFixesShowCursor(IntPtr display, IntPtr window);

		[DllImport("libXfixes.so.3")]
		public static extern bool XFixesQueryExtension(IntPtr display, out int event_base, out int error_base);

		// Equivalent to #define XIMaskLen(event) (((event) >> 3) + 1)
		public static int XIMaskLen(int eventType)
		{
			return (eventType >> 3) + 1;
		}

		// Equivalent to #define XISetMask(mask, event) (mask)[(event) >> 3] |= (1 << ((event) & 7))
		public static void XISetMask(byte[] mask, int eventType)
		{
			mask[eventType >> 3] |= (byte)(1 << (eventType & 7));
		}

		// Equivalent to #define XIMaskIsSet(mask, event) ((mask)[(event) >> 3] & (1 << ((event) & 7)))
		public static bool XIMaskIsSet(byte[] mask, int eventType)
		{
			return (mask[eventType >> 3] & (1 << (eventType & 7))) != 0;
		}

		[DllImport("libXi.so.6")]
		public static extern int XChangeDeviceProperty(
			IntPtr display,
			int deviceid,
			IntPtr property,
			IntPtr type,
			int format,
			int mode,
			ref byte data,
			int nelements
		);


		[DllImport("libX11.so.6")]
		public static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

		//[DllImport("libXi.so.6")]
		//public static extern int XIFreeDeviceInfo(IntPtr info);

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
			[FieldOffset(0)] public XGenericEventCookie xcookie;
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
		public struct XColor
		{
			public IntPtr pixel;
			public ushort red, green, blue;
			public byte flags, pad;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XGenericEventCookie
		{
			public int type;            // Always GenericEvent (35)
			public IntPtr serial;       // # of last request processed
			public int send_event;      // true if from SendEvent request
			public IntPtr display;      // Display the event was read from
			public int extension;       // major opcode of extension that caused the event
			public int evtype;          // actual event type (e.g., XI_RawMotion)
			public uint cookie;         // unique event cookie
			public IntPtr data;         // pointer to the actual XIRawEvent data
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XIEventMask
		{
			public int deviceid;        // The device to listen to (usually 1 for XIAllMasterDevices)
			public int mask_len;        // Length of the mask array in bytes
			public IntPtr mask;         // Pointer to the unmanaged byte array mask
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XIValuatorState
		{
			public int mask_len;        // Length of the bitmask in bytes
			public IntPtr mask;         // Pointer to the bitmask array (indicates which axes updated)
			public IntPtr values;       // Pointer to an array of double values for the updated axes
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XIRawEvent
		{
			public int type;
			public IntPtr serial;
			public int send_event;
			public IntPtr display;
			public int extension;
			public int evtype;
			public IntPtr time;
			public int deviceid;
			public int sourceid;
			public int detail;
			public int flags;
			public XIValuatorState valuators;
			public IntPtr raw_values;   // Pointer to raw doubles (unaccelerated hardware values)
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XIDeviceInfo
		{
			public int deviceid;
			public IntPtr name;       // CRITICAL FIX: 'name' is here in C
			public int use;
			public int attachment;
			public int enabled;
			public int num_classes;
			public IntPtr classes;
		}

		// The generic header that all XI class structs start with
		[StructLayout(LayoutKind.Sequential)]
		public struct XIAnyClassInfo
		{
			public int type;
			public int sourceid;
		}

		// The specific class struct for movement axes (Valuators)
		[StructLayout(LayoutKind.Sequential)]
		public struct XIValuatorClassInfo
		{
			public int type;        // Inherited from AnyClassInfo
			public int sourceid;    // Inherited from AnyClassInfo
			public int number;      // Axis number (0 for X, 1 for Y)
			public IntPtr label;    // Atom representing axis name
			public double min;
			public double max;
			public double value;
			public int resolution;
			public int mode;        // XIModeRelative (0) or XIModeAbsolute (1)
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct XIEvent
		{
			public int type;
			public int serial;
			public bool send_event;
			public IntPtr display;
			public int extension;
			public int evtype;
			public uint cookie;
			public IntPtr data;
		}


	[StructLayout(LayoutKind.Sequential)]
	public struct XIDeviceEvent
	{
		public int type;
		public IntPtr serial;        // unsigned long
		public int send_event;       // Xlib 'Bool' is an int (32-bit), not a 1-byte bool
		public IntPtr display;       // Display*
		public int extension;
		public int evtype;
		public IntPtr time;          // Time is unsigned long
		public int deviceid;
		public int sourceid;
		public int detail;
		public IntPtr root;          // Window is unsigned long
		public IntPtr @event;        // Window is unsigned long
		public IntPtr child;         // Window is unsigned long
		public double root_x;
		public double root_y;
		public double event_x;
		public double event_y;
		public int flags;
		public XIButtonState buttons;
		public XIValuatorState valuators;
		public XIModifierState mods;
		public XIGroupState group;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct XIButtonState
	{
		public int mask_len;
		public IntPtr mask;
	}

	//[StructLayout(LayoutKind.Sequential)]
	//public struct XIValuatorState
	//{
	//	public int mask_len;
	//	public IntPtr mask;          // unsigned char*
	//	public IntPtr values;        // double*
	//}

	[StructLayout(LayoutKind.Sequential)]
	public struct XIModifierState
	{
		public int base_mods;
		public int latched_mods;
		public int locked_mods;
		public int effective_mods;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct XIGroupState
	{
		public int base_group;
		public int latched_group;
		public int locked_group;
		public int effective_group;
	}

	// These are required to maintain the struct alignment for XIDeviceEvent
	//[StructLayout(LayoutKind.Sequential)]
	//	public struct XIButtonState { public int mask_len; public IntPtr mask; }

	//	[StructLayout(LayoutKind.Sequential)]
	//	public struct XIModifierState { public int base_mods; public int latched_mods; public int locked_mods; public int base_group; public int latched_group; public int locked_group; }

		private const int KeyPress = 2;
		private const int KeyRelease = 3;
		private const int ButtonPress = 4;
		private const int ButtonRelease = 5;
		private const int MotionNotify = 6;
		private const int GenericEvent = 35;
		private const int XI_RawMotion = 17;
		private const int XI_Motion = 6;

		private const int FocusIn = 9;
		private const int FocusOut = 10;

		private const long KeyPressMask = 1 << 0;
		private const long KeyReleaseMask = 1 << 1;
		private const long ButtonPressMask = 1 << 2;
		private const long ButtonReleaseMask = 1 << 3;
		private const long PointerMotionMask = 1 << 6;
		private const long FocusChangeMask = 1L << 21;

		// Device IDs
		private const int XIAllDevices = 0;
		private const int XIAllMasterDevices = 1;

		// Class Types
		private const int XIKeyClass = 0;
		private const int XIButtonClass = 1;
		private const int XIValuatorClass = 2; // This is the axis class we are looking for

		// Device Modes
		private const int XIModeRelative = 0; // Real hardware mouse
		private const int XIModeAbsolute = 1; // WSL2 Virtual Tablet / Touchscreen

		private readonly INativeOverlay nativeOverlay;
		private readonly ILogger logger;

		private int[] screen;

		public EmulationWindow(INativeOverlay nativeOverlay, ILogger<EmulationWindow> logger)
		{
			this.nativeOverlay = nativeOverlay;
			this.logger = logger;
		}

		public void Dispose()
		{
			XDestroyImage(ref ximage);
			XFreeGC(xdisplay, gc);
			XDestroyWindow(xdisplay, xwindow);
			XCloseDisplay(xdisplay);
		}

		public void Blit(int[] screen)
		{
			nativeOverlay.Render(screen);

			XPutImage(xdisplay, xwindow, gc, ref ximage, 0, 0, 0, 0, screenWidth, screenHeight);
			XFlush(xdisplay);
		}

		public bool IsCaptured { get; private set; } = false;
		private uint screenWidth;
		private uint screenHeight;

		private int displayHz;

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

		//const uint ButtonPressMask = 1 << 2;
		//const uint ButtonReleaseMask = 1 << 3;
		//const uint PointerMotionMask = 1 << 6;
		const int GrabModeAsync = 1;
		IntPtr invisibleCursor;
		XIEventMask mask;
		private void BlankMouse()
		{
			
			// Create a 1x1 array of zero (transparent)
			byte[] emptyData = new byte[] { 0 };

			// Create a blank pixmap on the root window
			IntPtr blankPixmap = XCreateBitmapFromData(xdisplay, xwindow, emptyData, 1, 1);

			XColor black = new XColor(); // Default 0 values are fine
			invisibleCursor = XCreatePixmapCursor(xdisplay, blankPixmap, blankPixmap, ref black, ref black, 0, 0);

			// Apply it to your window
			XDefineCursor(xdisplay, xwindow, invisibleCursor);

			// Cleanup the pixmap memory
			//XFreePixmap(xdisplay, blankPixmap);
			return;
			
			//XFixesHideCursor(xdisplay, xwindow);
			//XFlush(xdisplay);
		}

		public void SetPicture(int width, int height)
		{
			//xdisplay = XOpenDisplay("192.168.148.240:0.0");
			xdisplay = XOpenDisplay(null);

			// Put this in SetPicture right after connecting to the display
			XFixesQueryExtension(xdisplay, out int evBase, out int errBase);

			logger.LogTrace($"Current Environment DISPLAY variable: {Environment.GetEnvironmentVariable("DISPLAY")}");

			if (xdisplay != IntPtr.Zero)
			{
				IntPtr vendorPtr = XServerVendor(xdisplay);
				string vendor = Marshal.PtrToStringAnsi(vendorPtr);
				logger.LogTrace($"Connected to X Server Vendor: {vendor}");
			}

			var rootWindow = XRootWindow(xdisplay, XDefaultScreen(xdisplay));
			xwindow = XCreateSimpleWindow(xdisplay, rootWindow, 10, 10, (uint)width, (uint)height, 1, 0, 0xFFFFFF);
			XStoreName(xdisplay, xwindow, "Jammy : Alt-Tab or Middle Mouse Click to detach mouse");
			XSelectInput(xdisplay, xwindow, KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | FocusChangeMask);

			//IsRunningAbsoluteMouse(xdisplay);



			//byte[] maskBytes = new byte[XIMaskLen(XI_Motion)];
			//XISetMask(maskBytes, XI_Motion);
			byte[] maskBytes = new byte[XIMaskLen(XI_RawMotion)];
			XISetMask(maskBytes, XI_RawMotion);

			mask = new XIEventMask();
			mask.deviceid = XIAllDevices;
			mask.mask_len = maskBytes.Length;
			mask.mask = Marshal.AllocHGlobal(maskBytes.Length);

			// Copy managed byte array to unmanaged memory
			Marshal.Copy(maskBytes, 0, mask.mask, maskBytes.Length);

			// Register with root window
			//IntPtr rootWindow = XDefaultRootWindow(xdisplay);
			XISelectEvents(xdisplay, rootWindow, ref mask, 1);

			// Free the unmanaged memory after registering
			//Marshal.FreeHGlobal(mask.mask);

























			gc = XCreateGC(xdisplay, xwindow, 0, IntPtr.Zero);

			XMapWindow(xdisplay, xwindow);
			XClearWindow(xdisplay, xwindow);
			XFlush(xdisplay);

			//ForceRelativeMode(xdisplay, 6);
			BlankMouse();

			screenWidth = (uint)width;
			screenHeight = (uint)height;
			displayHz = 60;

			var xvis = new XVisualInfo { depth = 24 };
			int items;
			var xptr = XGetVisualInfo(xdisplay, VisualBitsPerRGBMask, ref xvis, out items);
			if (xptr != 0)
			{
				var xvis2 = Marshal.PtrToStructure<XVisualInfo>(xptr);
			}
			var ximagePtr = XCreateImage(xdisplay, IntPtr.Zero, 24, ZPixmap, 0, IntPtr.Zero, screenWidth, screenHeight, 32, 0);
			ximage = Marshal.PtrToStructure<XImage>(ximagePtr);

			screen = GC.AllocateArray<int>((int)(screenWidth * screenHeight), true);
			ximage.data = Marshal.UnsafeAddrOfPinnedArrayElement(screen, 0);
			nativeOverlay.Init(width, height);

			var t = new Thread(XEventHandler);
			t.Start();
		}

		private readonly InputOutput io = new InputOutput();

		public InputOutput GetInputOutput()
		{
			return io;
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
						VK vk = 0;
						KeySym ksym = XLookupKeysym(ref xevent.xkey, 0);

						//https://wiki.linuxquestions.org/wiki/List_of_keysyms
						if ((ksym & 0xff00) == 0)
						{
							vk = keylook[ksym & 0xff];
							RunKeyDown(vk);
							//io.Keyboard[ksym] = 1;
							//io.DebKeyboard[ksym] = 1;
						}
						else if ((ksym & 0xff00) == 0xff00)
						{
							vk = keylook2[ksym & 0xff];
							RunKeyDown(vk);
							//io.Keyboard[ksym & 0xff] = 1;
							//io.DebKeyboard[ksym & 0xff] = 1;
						}
						logger.LogTrace($"keydown {xevent.xkey.keycode} {ksym:X4} {vk}");

						}
						break;
					case KeyRelease:
						{
						VK vk = 0;
						KeySym ksym = XLookupKeysym(ref xevent.xkey, 0);

						if ((ksym & 0xff00) == 0)
						{
							vk = keylook[ksym & 0xff];
							//io.Keyboard[ksym] = 1;
							//io.DebKeyboard[ksym] = 1;
							RunKeyUp(vk);
						}
						else if ((ksym & 0xff00) == 0xff00)
						{
							vk = keylook2[ksym & 0xff];
							//io.Keyboard[ksym & 0xff] = 1;
							//io.DebKeyboard[ksym & 0xff] = 1;
							RunKeyUp(vk);
						}
						logger.LogTrace($"keyup {xevent.xkey.keycode} {ksym:X4} {vk}");

						}
						break;
					case ButtonPress:
						//Console.WriteLine($"mousedown {xevent.xbutton.button:X8}");
						switch (xevent.xbutton.button&0xff)
						{
							case 1: io.MouseButtons |= InputOutput.MouseButton.MouseLeft; break;
							case 2: io.MouseButtons |= InputOutput.MouseButton.MouseMiddle; break;
							case 3: io.MouseButtons |= InputOutput.MouseButton.MouseRight; break;
						}
						break;
					case ButtonRelease:
						//Console.WriteLine($"mouseup {xevent.xbutton.button:X8}");
						switch (xevent.xbutton.button&0xff)
						{
							case 1: io.MouseButtons &= ~InputOutput.MouseButton.MouseLeft; break;
							case 2: io.MouseButtons &= ~InputOutput.MouseButton.MouseMiddle; break;
							case 3: io.MouseButtons &= ~InputOutput.MouseButton.MouseRight; break;
						}
						break;
					case MotionNotify:
						//Console.WriteLine($"mousemove {xevent.xmotion.x} {xevent.xmotion.y}");
						io.MouseX = xevent.xmotion.x;
						io.MouseY = xevent.xmotion.y;
						break;
					case FocusIn:
						logger.LogTrace("focus in");
						XWarpPointer(xdisplay, IntPtr.Zero, xwindow, 0, 0, 0, 0, (int)screenWidth / 2, (int)screenHeight / 2);

						XFixesHideCursor(xdisplay, xwindow);
						// Grab the pointer
						var err = XGrabPointer(
							xdisplay,
							xwindow,
							false,
							(uint)(ButtonPressMask | ButtonReleaseMask | PointerMotionMask ),
							GrabModeAsync,
							GrabModeAsync,
							xwindow, // Confine it to your window
							invisibleCursor, // Use the invisible cursor we created
							IntPtr.Zero // CurrentTime
						);
						if (err != 0) logger.LogTrace($"grab {err}");
						XFlush(xdisplay);
						break;;

						////// Define the ID
						////const int XI_RawMotion = 17;

						////// 1. Calculate the required length ( (17 >> 3) + 1 = 3 )
						////int maskLen = XIMaskLen(XI_RawMotion);

						////// 2. Create the array
						////byte[] maskBytes = new byte[maskLen];

						////// 3. Set the bit for RawMotion
						////XISetMask(maskBytes, XI_RawMotion);

						////IntPtr grabMask = Marshal.AllocHGlobal(maskBytes.Length);
						////Marshal.Copy(maskBytes, 0, grabMask, maskBytes.Length);
						////break;

						//int status = XIGrabDevice(
						//		xdisplay,
						//		2, // Master Pointer ID
						//		xwindow,
						//		IntPtr.Zero, // CurrentTime
						//		IntPtr.Zero,//invisibleCursor,
						//		1, // GrabModeAsync
						//		1, // GrabModeAsync
						//		false,
						//		ref mask // PASS YOUR EXISTING MASK STRUCT HERE
						//	);

						////Marshal.FreeHGlobal(grabMask);
						//logger.LogTrace($"grab {status}");
						//break;
					case FocusOut:
						logger.LogTrace("focus out");
						//break;

						XUngrabPointer(xdisplay,IntPtr.Zero);
						XFixesShowCursor(xdisplay, xwindow);
						XFlush(xdisplay);
						break;
						//XIUngrabDevice(xdisplay, 2, IntPtr.Zero);
						//break;

					case GenericEvent:
						// 1. Fetch the extended event data (the "cookie") from the X Server
						if (XGetEventData(xdisplay, ref xevent.xcookie))
						{
							const int XI_RawMotion = 17;


							//if (xevent.xcookie.evtype == XI_RawMotion)
							//{
							//	IntPtr basePtr = xevent.xcookie.data;

							//	// Read the IDs to see where the event originated
							//	int deviceId = Marshal.ReadInt32(basePtr, 48);
							//	int sourceId = Marshal.ReadInt32(basePtr, 52);

							//	IntPtr maskPtr = Marshal.ReadIntPtr(basePtr, 72);
							//	IntPtr rawValuesPtr = Marshal.ReadIntPtr(basePtr, 88);

							//	if (maskPtr != IntPtr.Zero && rawValuesPtr != IntPtr.Zero)
							//	{
							//		byte maskByte = Marshal.ReadByte(maskPtr);

							//		int valIndex = 0;
							//		double dx = 0, dy = 0;

							//		if ((maskByte & (1 << 0)) != 0)
							//		{
							//			dx = Marshal.PtrToStructure<double>(IntPtr.Add(rawValuesPtr, valIndex * sizeof(double)));
							//			valIndex++;
							//		}
							//		if ((maskByte & (1 << 1)) != 0)
							//		{
							//			dy = Marshal.PtrToStructure<double>(IntPtr.Add(rawValuesPtr, valIndex * sizeof(double)));
							//		}

							//		logger.LogTrace($"Device: {deviceId} | Source: {sourceId} | {dx}, {dy}");
							//	}
							//}
							if (xevent.xcookie.evtype == XI_RawMotion)
							{
								//if (XGetEventData(xdisplay, ref xevent.xcookie))
								{
									XIRawEvent rawEvent = Marshal.PtrToStructure<XIRawEvent>(xevent.xcookie.data);

									if (rawEvent.raw_values != IntPtr.Zero && rawEvent.valuators.mask != IntPtr.Zero)
									{
										// 1. Safely read the first byte of the mask
										byte maskByte = Marshal.ReadByte(rawEvent.valuators.mask);

										double dx = 0;
										double dy = 0;
										int valIndex = 0;

										// 1-element buffer to safely marshal doubles out of memory
										double[] tempBuffer = new double[1];

										// Axis 0 = X
										if ((maskByte & (1 << 0)) != 0)
										{
											Marshal.Copy(IntPtr.Add(rawEvent.raw_values, valIndex * 8), tempBuffer, 0, 1);
											dx = tempBuffer[0];
											valIndex++;
										}

										// Axis 1 = Y
										if ((maskByte & (1 << 1)) != 0)
										{
											Marshal.Copy(IntPtr.Add(rawEvent.raw_values, valIndex * 8), tempBuffer, 0, 1);
											dy = tempBuffer[0];
										}

										io.MouseDX = (int)dx;
										io.MouseDY = (int)dy;
										logger.LogTrace($"{dx},{dy}");
									}

									//XFreeEventData(xdisplay, ref xevent.xcookie);
								}
							}
							else if (xevent.xcookie.evtype == XI_Motion)
							{
								XIEvent xiEvent = Marshal.PtrToStructure<XIEvent>(xevent.xcookie.data);

								// This is the structure that contains the relative delta values
								// when the server is in Relative Mode
								XIDeviceEvent devEvent = Marshal.PtrToStructure<XIDeviceEvent>(xevent.xcookie.data);

								// Ensure the valuators array actually contains data
								if (devEvent.valuators.values != IntPtr.Zero)
								{
									// The mask determines which axes are present, but assuming a standard 
									// relative motion event with both X and Y present:
									double dx = Marshal.PtrToStructure<double>(devEvent.valuators.values);
									double dy = Marshal.PtrToStructure<double>(IntPtr.Add(devEvent.valuators.values, sizeof(double)));

									io.MouseDX = (int)dx;
									io.MouseDY = (int)dy;
								}

							}

							// 5. CRITICAL: Free the event data to prevent memory leaks!
							XFreeEventData(xdisplay, ref xevent.xcookie);
						}
						break;
					default:
						Console.WriteLine("Unhandled XEvent type: " + xevent.type);
						break;
				}
			}
		}

		private double GetRawDelta(XIRawEvent rawEvent, int axisIndex)
		{
			// 1. Copy the mask from unmanaged memory
			byte[] mask = new byte[rawEvent.valuators.mask_len];
			Marshal.Copy(rawEvent.valuators.mask, mask, 0, mask.Length);

			// 2. Check if the specific axis (0 for X, 1 for Y) has updated in this event
			if (!XIMaskIsSet(mask, axisIndex))
			{
				return 0.0;
			}

			// 3. If it did update, we need to find its index in the compressed double array.
			// We count how many bits were set *before* our target axis.
			int valueCount = 0;
			for (int i = 0; i < axisIndex; i++)
			{
				if (XIMaskIsSet(mask, i))
				{
					valueCount++;
				}
			}

			// 4. Read the double at the calculated offset from unmanaged memory
			// raw_values is un-accelerated device data. values is accelerated screen data.
			// Use raw_values for raw mouse input.
			IntPtr ptr = IntPtr.Add(rawEvent.raw_values, valueCount * sizeof(double));
			//IntPtr ptr = IntPtr.Add(rawEvent.valuators.values, valueCount * sizeof(double));

			// Read and return the double
			double[] result = new double[1];
			Marshal.Copy(ptr, result, 0, 1);
			return result[0];
		}
		// Atom types
		public static readonly IntPtr XA_INTEGER = (IntPtr)19; // XA_INTEGER = 19

		// Property modes
		public const int PropModeReplace = 0;
		public const int PropModePrepend = 1;
		public const int PropModeAppend = 2;
		private void ForceRelativeMode(IntPtr display, int deviceId)
		{
			// The atom for "Device Mode"
			IntPtr modeAtom = XInternAtom(display, "Device Mode", false);

			if (modeAtom == IntPtr.Zero)
				modeAtom = XInternAtom(display, "Rel Mode", false);

			IntPtr typeAtom = XInternAtom(display, "INTEGER", false);

			// 0 = Absolute, 1 = Relative
			byte mode = 1;

			// Set the property on the device
			XChangeDeviceProperty(
				display,
				deviceId,
				modeAtom,
				typeAtom,
				8,
				PropModeReplace,
				ref mode,
				1
			);
			XFlush(display);
		}
		//private bool IsRunningAbsoluteMouse(IntPtr xdisplay)
		//{
		//	// 1. Query the master devices
		//	//IntPtr infoPtr = XIQueryDevice(xdisplay, XIAllMasterDevices, out int num_devices);
		//	IntPtr infoPtr = XIQueryDevice(xdisplay, XIAllDevices, out int num_devices);

		//	if (infoPtr == IntPtr.Zero)
		//		return false;

		//	bool requiresAbsoluteHack = false;
		//	int infoSize = Marshal.SizeOf<XIDeviceInfo>();

		//	// 2. XIQueryDevice returns a pointer to an array of XIDeviceInfo structs
		//	for (int i = 0; i < num_devices; i++)
		//	{
		//		// Calculate the address of the current XIDeviceInfo struct
		//		IntPtr currentDeviceInfoPtr = IntPtr.Add(infoPtr, i * infoSize);
		//		XIDeviceInfo deviceInfo = Marshal.PtrToStructure<XIDeviceInfo>(currentDeviceInfoPtr);

		//		string deviceName = "Unknown Device";
		//		if (deviceInfo.name != IntPtr.Zero)
		//			deviceName = Marshal.PtrToStringAnsi(deviceInfo.name);
		//		logger.LogTrace($"{deviceName} {deviceInfo.deviceid}");

		//		// 3. deviceInfo.classes is a pointer to an array of pointers (IntPtr[])
		//		for (int c = 0; c < deviceInfo.num_classes; c++)
		//		{
		//			// Read the specific pointer at index c
		//			IntPtr classPtr = Marshal.ReadIntPtr(deviceInfo.classes, c * IntPtr.Size);

		//			// First, marshal just the header to see what type of class this is
		//			XIAnyClassInfo anyClass = Marshal.PtrToStructure<XIAnyClassInfo>(classPtr);

		//			// We only care about Valuators (Movement axes)
		//			if (anyClass.type == XIValuatorClass)
		//			{
		//				// Re-marshal the same pointer into the full Valuator struct
		//				XIValuatorClassInfo valuator = Marshal.PtrToStructure<XIValuatorClassInfo>(classPtr);

		//				// If any axis on the master pointer reports as absolute, flag it
		//				if (valuator.mode == XIModeAbsolute)
		//				{
		//					requiresAbsoluteHack = true;
		//				}
		//			}
		//		}
		//	}

		//	// 4. Clean up the unmanaged memory allocated by libXi
		//	XIFreeDeviceInfo(infoPtr);

		//	return requiresAbsoluteHack;
		//}

		public Types.Types.Point RecentreMouse()
		{
			return new Types.Types.Point { X = 0, Y = 0 };

			//put the cursor back in the middle of the emulation window

			//can't be done on wslg or any wayland layer emulating X11

			//IntPtr root;
			//int x, y;
			//uint width, height;
			//uint borderWidth;
			//uint depth;
			//XGetGeometry(xdisplay, xwindow, out root,
			//  out x,  out y, out width, out height,
			//  out borderWidth, out depth);


			//XWindowAttributes attr = new XWindowAttributes();
			//XGetWindowAttributes(xdisplay, xwindow, ref attr);

			//Console.WriteLine($"{x} {y} {width} {height}");
			////int cx = 0;
			////int cy = 0;

			//int cx = (int)(width / 2);
			//int cy = (int)(height / 2);
			//int err = XWarpPointer(xdisplay, IntPtr.Zero, rootWindow, 0, 0, 0, 0, cx, cy);
			//Console.WriteLine($"{err}");

			//return new Types.Types.Point { X = cx, Y = cy };
		}

		private List<Tuple<Action<int>,Action<int>>> keyhandlers = new List<Tuple<Action<int>, Action<int>>>();

		public void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp)
		{
			keyhandlers.Add(new Tuple<Action<int>, Action<int>>(addKeyDown, addKeyUp));
		}

		private void RunKeyDown(VK vk)
		{
			io.Keyboard[(int)vk] = true;
			foreach (var k in keyhandlers)
				if (k.Item1 != null) k.Item1((int)vk);
		}

		private void RunKeyUp(VK vk)
		{
			io.Keyboard[(int)vk] = false;
			foreach (var k in keyhandlers)
				if (k.Item2 != null) k.Item2((int)vk);
		}

		public bool IsActive()
		{
			return true;//IsCaptured;
		}

		public int[] GetFramebuffer()
		{
			return screen;
		}
	
		private readonly VK[] keylook = 
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
		 VK.VK_SPACE, //0x20
		(VK)'1',(VK)'2',(VK)'3',(VK)'4',(VK)'5',(VK)'6',VK.VK_OEM_7,(VK)'8',(VK)'9',(VK)'0',
		 0,
		 VK.VK_OEM_COMMA,//VK.VK_PLUS,
		 VK.VK_OEM_MINUS,//VK.VK_LESS_THAN,
		 VK.VK_OEM_PERIOD,//VK.VK_GREATER_THAN,
		 VK.VK_OEM_2,//VK.VK_QUESTION_MARK,
		(VK)'0', //0x30
		(VK)'1', (VK)'2', (VK)'3', (VK)'4', (VK)'5', (VK)'6',(VK) '7',(VK) '8', (VK)'9',
		 0,//VK.VK_SEMI_COLON,
		 VK.VK_OEM_1,//VK.VK_SEMI_COLON,
		 0,//VK.VK_LESS_THAN,
		 VK.VK_OEM_PLUS,//VK.VK_GREATER_THAN,
		 0,//VK.VK_QUESTION_MARK,
		 0,//VK.VK_SINGLE_QUOTE,
		 0,//VK.VK_SINGLE_QUOTE,//0x40
		(VK)'A',(VK)'B',(VK)'C',(VK)'D',(VK)'E',(VK)'F',(VK)'G',(VK)'H',(VK)'I',(VK)'J',(VK)'K',(VK)'L',(VK)'M',
		(VK)'N',(VK)'O',(VK)'P',(VK)'Q',(VK)'R',(VK)'S',(VK)'T',(VK)'U',(VK)'V',(VK)'W',(VK)'X',(VK)'Y',(VK)'Z',
		 VK.VK_OEM_4,//VK.VK_OPEN_SQR_BRACKET,
		 VK.VK_OEM_5,//VK.VK_RSX,
		 VK.VK_OEM_6,//VK.VK_CLOSE_SQR_BRACKET,
		0,// VK.VK_SQUIGLE,
		 0,//VK.VK_MINUS,
		 VK.VK_OEM_3,//VK.VK_SQUIGLE,//0x60
		(VK)'A',(VK)'B',(VK)'C',(VK)'D',(VK)'E',(VK)'F',(VK)'G',(VK)'H',(VK)'I',(VK)'J',(VK)'K',(VK)'L',(VK)'M',
		(VK)'N',(VK)'O',(VK)'P',(VK)'Q',(VK)'R',(VK)'S',(VK)'T',(VK)'U',(VK)'V',(VK)'W',(VK)'X',(VK)'Y',(VK)'Z',
		0,// VK.VK_OPEN_SQR_BRACKET,
		 0,//VK.VK_RSX,
		0,// VK.VK_CLOSE_SQR_BRACKET,
		 0,//VK.VK_SQUIGLE,
		];

		VK[] keylook2 = 
		[
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		0,
		VK.VK_BACK,//0x8
		VK.VK_TAB,
		0,
		0,
		0,
		VK.VK_RETURN,//0xd
		0,
		0,
		0,//0x10
		0,
		0,
		0,//pause key
		0,//scroll lock
		0,//VK.VK_SYSRQ,//0x15
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
		VK.VK_HOME,//0x50
		VK.VK_LEFT,
		VK.VK_UP,
		VK.VK_RIGHT,
		VK.VK_DOWN,
		VK.VK_PRIOR,
		VK.VK_NEXT,
		VK.VK_END,//0x57
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
		0,//VK.VK_KEYPAD_ENTER,
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
		0,//VK.VK_KEYPAD_PLUS,
		0,
		0,//VK.VK_KEYPAD_MINUS,
		0,//VK.VK_KEYPAD_FULL_STOP,
		0,//keypad divide
		VK.VK_NUMPAD0,//0xb0
		VK.VK_NUMPAD1,
		VK.VK_NUMPAD2,
		VK.VK_NUMPAD3,
		VK.VK_NUMPAD4,
		VK.VK_NUMPAD5,
		VK.VK_NUMPAD6,
		VK.VK_NUMPAD7,
		VK.VK_NUMPAD8,
		VK.VK_NUMPAD9,
		0,
		0,
		0,
		0,
		VK.VK_F1,
		VK.VK_F2,
		VK.VK_F3,//0xc0
		VK.VK_F4,
		VK.VK_F5,
		VK.VK_F6,
		VK.VK_F7,
		VK.VK_F8,
		VK.VK_F9,
		VK.VK_F10,
		VK.VK_F11,
		VK.VK_F12,
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
		VK.VK_LSHIFT,
		VK.VK_RSHIFT,
		VK.VK_LCONTROL,
		VK.VK_RCONTROL,
		VK.VK_CAPITAL,
		0,
		0,
		0,
		0,//VK.VK_LEFT_ALT,
		0,//VK.VK_RIGHT_ALT,
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

/*

#include <stdio.h>
#include <stdlib.h>
#include <time.h>

#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <X11/Xos.h>
#define XK_MISCELLANY
#define XK_LATIN1
#include <X11/keysymdef.h>

#include "buildefs.h"
#include "lib_gimc.h"
#include "lib_gdi.h"
#include "lib_mem.h"
#include "lib_3d.h"
#include "lib_lig.h"
#include "lib_uci.h"
#include "lib_obj.h"
#include "lib_spr.h"
#define NO_WINDOWS
#include "winmain.h"

#include "rendver.h"

SYSTEM_CONFIG system_status;

FILE *lfile;

Display *xdisplay;
Window xwindow;
GC gc;
int xscreen;
//Pixmap gray;
XImage *ximage;
Visual xvisual;

int main(int argc, char **argv)
{
  XSizeHints size_hints;

  DEBUGOUT("hello mum\n");

  lfile = fopen("log.txt", "w");
  if (lfile) setbuf(lfile, NULL);

#ifdef MEM_STATIC_MEMORY
  MEM_init_malloc_manager();
#endif

 xdisplay = XOpenDisplay(NULL);
 xscreen = DefaultScreen(xdisplay);
 xwindow = XCreateSimpleWindow(xdisplay, RootWindow(xdisplay, xscreen), 0,0,640,480,2,1,WhitePixel(xdisplay,xscreen));

 gc = XCreateGC(xdisplay, xwindow,0,0);

  XSetWindowBackground(xdisplay, xwindow, WhitePixel(xdisplay, xscreen));

  size_hints.flags = USSize | PMinSize | PMaxSize; // USPosition;
//  size_hints.x = 0;
//  size_hints.y = 0;
  size_hints.width = 640;
  size_hints.height = 480;
  size_hints.min_width = 640;
  size_hints.min_height = 480;
  size_hints.max_width = 640;
  size_hints.max_height = 480;

  XSetStandardProperties(xdisplay, xwindow, "KickFlip", "KickFlip", NULL, argv, argc, &size_hints);

  XMapWindow(xdisplay, xwindow);
  XClearWindow(xdisplay, xwindow);
  XFlush(xdisplay);

  ximage = XCreateImage(xdisplay, &xvisual, 16, ZPixmap, 0, NULL, 640,480, 16, 0);
//  ximage = XCreateImage(xdisplay, &xvisual, 32, ZPixmap, 0, NULL, 640,480, 32, 0);

  XSelectInput(xdisplay, xwindow, KeyPress | KeyRelease | ButtonPressMask | ButtonReleaseMask);

  APP_activate();

  if (GDI_init())
    return -1;

  APP_set_states(APP_startup());

  APP_main();

  if (lfile) fclose(lfile);

  GDI_deinit();

  XDestroyImage(ximage);
  XFreeGC(xdisplay, gc);
  XDestroyWindow(xdisplay, xwindow);
  XCloseDisplay(xdisplay);

 return 0;
 }

int min(int x, int y)
{
  if (y < x)
   return y;
return x;
}

int max(int x, int y)
{
  if (x > y)
   return x;
return y;
}

ULONG WIN_get_time()
{
return (clock() * 1000) / CLOCKS_PER_SEC;
}

unsigned char keylook[256]=
{
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,//0x10
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_NULL,
 KS_SPACE_BAR, //0x20
 KS_1,
 KS_2,
 KS_3,
 KS_4,
 KS_5,
 KS_6,
 KS_7,
 KS_8,
 KS_9,
 KS_0,
 KS_NULL,
 KS_PLUS,
 KS_LESS_THAN,
 KS_GREATER_THAN,
 KS_QUESTION_MARK,
 KS_0, //0x30
 KS_1,
 KS_2,
 KS_3,
 KS_4,
 KS_5,
 KS_6,
 KS_7,
 KS_8,
 KS_9,
 KS_SEMI_COLON,
 KS_SEMI_COLON,
 KS_LESS_THAN,
 KS_GREATER_THAN,
 KS_QUESTION_MARK,
 KS_SINGLE_QUOTE,
 KS_SINGLE_QUOTE,//0x40
 KS_A,
 KS_B,
 KS_C,
 KS_D,
 KS_E,
 KS_F,
 KS_G,
 KS_H,
 KS_I,
 KS_J,
 KS_K,
 KS_L,
 KS_M,
 KS_N,
 KS_O,
 KS_P,//0x50
 KS_Q,
 KS_R,
 KS_S,
 KS_T,
 KS_U,
 KS_V,
 KS_W,
 KS_X,
 KS_Y,
 KS_Z,
 KS_OPEN_SQR_BRACKET,
 KS_RSX,
 KS_CLOSE_SQR_BRACKET,
 KS_SQUIGLE,
 KS_MINUS,
 KS_SQUIGLE,//0x60
 KS_A,
 KS_B,
 KS_C,
 KS_D,
 KS_E,
 KS_F,
 KS_G,
 KS_H,
 KS_I,
 KS_J,
 KS_K,
 KS_L,
 KS_M,
 KS_N,
 KS_O,
 KS_P,//0x70
 KS_Q,
 KS_R,
 KS_S,
 KS_T,
 KS_U,
 KS_V,
 KS_W,
 KS_X,
 KS_Y,
 KS_Z,
 KS_OPEN_SQR_BRACKET,
 KS_RSX,
 KS_CLOSE_SQR_BRACKET,
 KS_SQUIGLE,
};

unsigned char keylook2[256]=
{
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_BACKSPACE,//0x8
KS_TAB,
KS_NULL,
KS_NULL,
KS_NULL,
KS_ENTER,//0xd
KS_NULL,
KS_NULL,
KS_NULL,//0x10
KS_NULL,
KS_NULL,
KS_NULL,//pause key
KS_NULL,//scroll lock
KS_SYSRQ,//0x15
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0x20
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0x30
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0x40
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_HOME,//0x50
KS_CURSOR_LEFT,
KS_CURSOR_UP,
KS_CURSOR_RIGHT,
KS_CURSOR_DOWN,
KS_PAGE_UP,
KS_PAGE_DOWN,
KS_END,//0x57
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0x60
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0x70
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0x80
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_KEYPAD_ENTER,
KS_NULL,
KS_NULL,
KS_NULL,//0x90
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0xa0
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//keypad times
KS_KEYPAD_PLUS,
KS_NULL,
KS_KEYPAD_MINUS,
KS_KEYPAD_FULL_STOP,
KS_NULL,//keypad divide
KS_KEYPAD_0,//0xb0
KS_KEYPAD_1,
KS_KEYPAD_2,
KS_KEYPAD_3,
KS_KEYPAD_4,
KS_KEYPAD_5,
KS_KEYPAD_6,
KS_KEYPAD_7,
KS_KEYPAD_8,
KS_KEYPAD_9,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_F1,
KS_F2,
KS_F3,//0xc0
KS_F4,
KS_F5,
KS_F6,
KS_F7,
KS_F8,
KS_F9,
KS_F10,
KS_F11,
KS_F12,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0xd0
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0xe0
KS_LEFT_SHIFT,
KS_RIGHT_SHIFT,
KS_LEFT_CTRL,
KS_RIGHT_CTRL,
KS_CAPS_LOCK,
KS_NULL,
KS_NULL,
KS_NULL,
KS_LEFT_ALT,
KS_RIGHT_ALT,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,//0xf0
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL,
KS_NULL
};

void handle_all_messages()
{
//  char keys[32];
//  SLONG x,y;
//  XQueryKeymap(xdisplay, keys);
//  for (x = 0; x < 32; x++)
//  for (y = 0; y < 8; y++)
//    system_status.keyboard[keylook[(x<<3)+y]] = keys[x]&y?1:0;
//  system_status.deb_keyboard[keylook[(x<<3)+y]] = keys[x]&y?1:0;

 XEvent ev;
 ULONG x;

 for (x =0; x < 256; x++)
   system_status.deb_keyboard[x] = 0;

  if (XPending(xdisplay))
  {
      do
      {
	XNextEvent(xdisplay, out ev);
	switch (ev.type)
	  {
	  case ButtonRelease:
	    switch (ev.xbutton.button)
	    {
	      case 1:
		system_status.mouse[0].buttons &= ~MOUSE_LBUTTON; break;
	      case 2:
		system_status.mouse[0].buttons &= ~MOUSE_MBUTTON; break;
	      case 3:
		system_status.mouse[0].buttons &= ~MOUSE_RBUTTON; break;
	    }
	    break;
	  case ButtonPress:
	    switch (ev.xbutton.button)
	    {
	      case 1:
		system_status.mouse[0].buttons |= MOUSE_LBUTTON; break;
	      case 2:
		system_status.mouse[0].buttons |= MOUSE_MBUTTON; break;
	      case 3:
		system_status.mouse[0].buttons |= MOUSE_RBUTTON; break;
	    }
	    break;

	  case MotionNotify:
	    system_status.mouse[0].mouse_x = ev.xmotion.x;
	    system_status.mouse[0].mouse_y = ev.xmotion.y;
	    system_status.mouse[0].mouse_dx += system_status.mouse[0].mouse_x;
	    system_status.mouse[0].mouse_dy += system_status.mouse[0].mouse_y;
	    break;

	  case KeyRelease:
	    {
	     KeySym ksym;
	     //KeyCode code;
	     ksym = XLookupKeysym(&ev.xkey, 0);
	     //code = XKeysymToKeycode(xdisplay, ksym);
	     //system_status.keyboard[code&0xff] = 0;
	     
             if ((ksym & 0xff00) == 0)
	       {
	       ksym = keylook[ksym];
	       system_status.keyboard[ksym] = 0;
	       }
	     else if ((ksym & 0xff00) == 0xff00)
	       {
		 ksym = keylook2[ksym&0xff];
		 system_status.keyboard[ksym&0xff] = 0;
	       }
	       
	    }
	    break;

	  case KeyPress:
	    {
	      KeySym ksym;
	      //KeyCode code;
	      ksym = XLookupKeysym(&ev.xkey, 0);
	      //code = XKeysymToKeycode(xdisplay, ksym);
	      //system_status.keyboard[code&0xff] = 1;
	      //system_status.deb_keyboard[code&0xff] = 1;
	      
              if ((ksym & 0xff00) == 0)
	      {
 	        ksym = keylook[ksym&0xff];
	        system_status.keyboard[ksym] = 1;
	        system_status.deb_keyboard[ksym] = 1;
	      }
	      else if ((ksym & 0xff00) == 0xff00)
		{
		  ksym = keylook2[ksym&0xff];
		  system_status.keyboard[ksym&0xff] = 1;
		  system_status.deb_keyboard[ksym&0xff] = 1;
		}
		
	    }
	  break;

      }

     } while (XEventsQueued(xdisplay, QueuedAlready));
	      if (system_status.deb_keyboard[KS_F5])
		HWI_screenshot();
  }
}

void WIN_blit(void *src)
{
   ximage->data = src;

   XPutImage(xdisplay, xwindow, gc, ximage, 0,0,0,0,640,480);

   XFlush(xdisplay);
}

void WIN_set_blit_info(ULONG render_depth)
{
}

int sound_on = 0;

*/