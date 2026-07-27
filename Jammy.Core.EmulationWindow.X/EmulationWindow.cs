using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Jammy.NativeOverlay;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using KeySym = ushort;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.X
{
	public class EmulationWindow : IEmulationWindow, IInputOutput, IDisposable
	{
		private const string X11Library = "libX11.so.6";
		private const string XiLibrary = "libXi.so.6";

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

		[DllImport(X11Library)]
		private static extern bool XGetEventData(IntPtr display, ref XGenericEventCookie cookie);

		[DllImport(X11Library)]
		private static extern void XFreeEventData(IntPtr display, ref XGenericEventCookie cookie);

		[DllImport(X11Library)]
		private static extern int XGrabPointer(IntPtr display, IntPtr grab_window, bool owner_events,
									  uint event_mask, int pointer_mode, int keyboard_mode,
									  IntPtr confine_to, IntPtr cursor, IntPtr time);

		[DllImport(X11Library)]
		private static extern int XUngrabPointer(IntPtr display, IntPtr time);

		[DllImport(X11Library)]
		private static extern IntPtr XCreateBitmapFromData(IntPtr display, IntPtr drawable, byte[] data, int width, int height);

		[DllImport(X11Library)]
		private static extern IntPtr XCreatePixmapCursor(IntPtr display, IntPtr source, IntPtr mask, ref XColor foreground_color, ref XColor background_color, int x, int y);

		[DllImport(X11Library)]
		private static extern int XDefineCursor(IntPtr display, IntPtr window, IntPtr cursor);

		[DllImport(X11Library)]
		private static extern int XFreePixmap(IntPtr display, IntPtr pixmap);

		[DllImport(X11Library)]
		private static extern int XFreeCursor(IntPtr display, IntPtr cursor);

		// libXi (XInput2)

		[DllImport(XiLibrary)]
		private static extern int XISelectEvents(IntPtr display, IntPtr window, ref XIEventMask masks, int num_masks);

		[DllImport(XiLibrary)]
		private static extern IntPtr XIQueryDevice(IntPtr display, int deviceid, out int ndevices_return);

		[DllImport(XiLibrary)]
		private static extern void XIFreeDeviceInfo(IntPtr info);

		[DllImport(XiLibrary)]
		private static extern int XIGrabDevice(
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

		[DllImport(XiLibrary)]
		private static extern int XIUngrabDevice(IntPtr display, int deviceid, IntPtr time);

		[DllImport("libXfixes.so.3")]
		private static extern void XFixesHideCursor(IntPtr display, IntPtr window);

		[DllImport("libXfixes.so.3")]
		private static extern void XFixesShowCursor(IntPtr display, IntPtr window);

		[DllImport("libXfixes.so.3")]
		private static extern bool XFixesQueryExtension(IntPtr display, out int event_base, out int error_base);

		// #define XIMaskLen(event) (((event) >> 3) + 1)
		private static int XIMaskLen(int eventType)
		{
			return (eventType >> 3) + 1;
		}

		// #define XISetMask(mask, event) (mask)[(event) >> 3] |= (1 << ((event) & 7))
		private static void XISetMask(byte[] mask, int eventType)
		{
			mask[eventType >> 3] |= (byte)(1 << (eventType & 7));
		}

		// #define XIMaskIsSet(mask, event) ((mask)[(event) >> 3] & (1 << ((event) & 7)))
		private static bool XIMaskIsSet(byte[] mask, int eventType)
		{
			return (mask[eventType >> 3] & (1 << (eventType & 7))) != 0;
		}

		[DllImport(XiLibrary)]
		private static extern int XChangeDeviceProperty(
			IntPtr display,
			int deviceid,
			IntPtr property,
			IntPtr type,
			int format,
			int mode,
			ref byte data,
			int nelements
		);


		[DllImport(X11Library)]
		private static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);

		[StructLayout(LayoutKind.Sequential)]
		private struct Display
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
		private struct XColor
		{
			public IntPtr pixel;
			public ushort red, green, blue;
			public byte flags, pad;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XGenericEventCookie
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
		private struct XIEventMask
		{
			public int deviceid;        // The device to listen to (usually 1 for XIAllMasterDevices)
			public int mask_len;        // Length of the mask array in bytes
			public IntPtr mask;         // Pointer to the unmanaged byte array mask
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XIValuatorState
		{
			public int mask_len;        // Length of the bitmask in bytes
			public IntPtr mask;         // Pointer to the bitmask array (indicates which axes updated)
			public IntPtr values;       // Pointer to an array of double values for the updated axes
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XIRawEvent
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
		private struct XIDeviceInfo
		{
			public int deviceid;
			public IntPtr name;
			public int use;
			public int attachment;
			public int enabled;
			public int num_classes;
			public IntPtr classes;
		}

		// The generic header that all XI class structs start with
		[StructLayout(LayoutKind.Sequential)]
		private struct XIAnyClassInfo
		{
			public int type;
			public int sourceid;
		}

		// The specific class struct for movement axes (Valuators)
		[StructLayout(LayoutKind.Sequential)]
		private struct XIValuatorClassInfo
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
		private struct XIEvent
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
		private struct XIDeviceEvent
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
		private struct XIButtonState
		{
			public int mask_len;
			public IntPtr mask;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XIModifierState
		{
			public int base_mods;
			public int latched_mods;
			public int locked_mods;
			public int effective_mods;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct XIGroupState
		{
			public int base_group;
			public int latched_group;
			public int locked_group;
			public int effective_group;
		}

		private const int KeyPress = 2;
		private const int KeyRelease = 3;
		private const int ButtonPress = 4;
		private const int ButtonRelease = 5;
		private const int MotionNotify = 6;
		private const int GenericEvent = 35;
		private const int FocusIn = 9;
		private const int FocusOut = 10;
		private const int DestroyNotify = 17;
		private const int UnmapNotify = 18;
		private const int MapNotify = 19;
		private const int ReparentNotify = 21;
		private const int ConfigureNotify = 22;

		private const long KeyPressMask = 1 << 0;
		private const long KeyReleaseMask = 1 << 1;
		private const long ButtonPressMask = 1 << 2;
		private const long ButtonReleaseMask = 1 << 3;
		private const long PointerMotionMask = 1 << 6;
		private const long FocusChangeMask = 1L << 21;
		private const long StructureNotifyMask = 1L << 17;

		private const int XI_RawMotion = 17;
		private const int XI_Motion = 6;

		// Device IDs
		private const int XIAllDevices = 0;
		private const int XIAllMasterDevices = 1;

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
			XFreeCursor(xdisplay, invisibleCursor);
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

		private const int GrabModeAsync = 1;
		private IntPtr invisibleCursor;

		private void BlankMouse()
		{
			byte[] emptyData = { 0 };
			IntPtr blankPixmap = XCreateBitmapFromData(xdisplay, xwindow, emptyData, 1, 1);
			XColor black = new XColor();
			invisibleCursor = XCreatePixmapCursor(xdisplay, blankPixmap, blankPixmap, ref black, ref black, 0, 0);
			XFreePixmap(xdisplay, blankPixmap);
		}

		public void SetPicture(int width, int height)
		{
			xdisplay = XOpenDisplay(null);

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
			XSelectInput(xdisplay, xwindow, KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | FocusChangeMask | StructureNotifyMask);

			byte[] maskBytes = new byte[XIMaskLen(XI_RawMotion)];
			XISetMask(maskBytes, XI_RawMotion);

			var mask = new XIEventMask();
			mask.deviceid = XIAllDevices;
			mask.mask_len = maskBytes.Length;
			mask.mask = Marshal.AllocHGlobal(maskBytes.Length);
			Marshal.Copy(maskBytes, 0, mask.mask, maskBytes.Length);
			XISelectEvents(xdisplay, rootWindow, ref mask, 1);
			Marshal.FreeHGlobal(mask.mask);

			gc = XCreateGC(xdisplay, xwindow, 0, IntPtr.Zero);

			XMapWindow(xdisplay, xwindow);
			XClearWindow(xdisplay, xwindow);
			XFlush(xdisplay);

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
		private int mouseDX, mouseDY;

		public InputOutput GetInputOutput()
		{

			io.MouseDX = mouseDX;
			io.MouseDY = mouseDY;

			mouseDX = mouseDY = 0;

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
						}
						else if ((ksym & 0xff00) == 0xff00)
						{
							vk = keylook2[ksym & 0xff];
							RunKeyDown(vk);
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
							RunKeyUp(vk);
						}
						else if ((ksym & 0xff00) == 0xff00)
						{
							vk = keylook2[ksym & 0xff];
							RunKeyUp(vk);
						}
						logger.LogTrace($"keyup {xevent.xkey.keycode} {ksym:X4} {vk}");
						}
						break;

					case ButtonPress:
						byte buttons = (byte)xevent.xbutton.button;
						if (!IsCaptured && buttons == 1)
						{
							IsCaptured = true;
							CaptureMouse();
							break;
						}
						else if (IsCaptured && buttons == 3)
						{
							IsCaptured = false;
							ReleaseMouse();
							break;
						}

						switch (xevent.xbutton.button&0xff)
						{
							case 1: io.MouseButtons |= InputOutput.MouseButton.MouseLeft; break;
							case 2: io.MouseButtons |= InputOutput.MouseButton.MouseMiddle; break;
							case 3: io.MouseButtons |= InputOutput.MouseButton.MouseRight; break;
						}
						break;

					case ButtonRelease:
						switch (xevent.xbutton.button&0xff)
						{
							case 1: io.MouseButtons &= ~InputOutput.MouseButton.MouseLeft; break;
							case 2: io.MouseButtons &= ~InputOutput.MouseButton.MouseMiddle; break;
							case 3: io.MouseButtons &= ~InputOutput.MouseButton.MouseRight; break;
						}
						break;

					case MotionNotify:
						//io.MouseX = xevent.xmotion.x;
						//io.MouseY = xevent.xmotion.y;
						break;

					case FocusIn:
						break;

					case UnmapNotify:
					case DestroyNotify:
					case FocusOut:
						if (IsCaptured)
						{
							IsCaptured = false;
							ReleaseMouse();
						}
						break;

					case MapNotify:
					case ReparentNotify:
					case ConfigureNotify:
						break;

					case GenericEvent:
						if (XGetEventData(xdisplay, ref xevent.xcookie))
						{
							if (xevent.xcookie.evtype == XI_RawMotion)
							{
								if (IsCaptured)
								{
									XWarpPointer(xdisplay, IntPtr.Zero, xwindow, 0, 0, 0, 0,
												 (int)screenWidth / 2, (int)screenHeight / 2);
								}

								var rawEvent = Marshal.PtrToStructure<XIRawEvent>(xevent.xcookie.data);

								if (rawEvent.raw_values != IntPtr.Zero && rawEvent.valuators.mask != IntPtr.Zero)
								{
									byte maskByte = Marshal.ReadByte(rawEvent.valuators.mask);

									double dx = 0;
									double dy = 0;
									int valIndex = 0;

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

									mouseDX += (int)dx;
									mouseDY += (int)dy;
									//logger.LogTrace($"XI_RawMotion {dx},{dy}");
								}
							}
							XFreeEventData(xdisplay, ref xevent.xcookie);
						}
						break;

					default:
						Console.WriteLine("Unhandled XEvent type: " + xevent.type);
						break;
				}
			}
		}

		// Property modes
		public const int PropModeReplace = 0;
		public const int PropModePrepend = 1;
		public const int PropModeAppend = 2;

		private void CaptureMouse()
		{
			logger.LogTrace("Capture");

			XWarpPointer(xdisplay, IntPtr.Zero, xwindow, 0, 0, 0, 0, (int)screenWidth / 2, (int)screenHeight / 2);

			//XFixesHideCursor(xdisplay, xwindow);

			int err = XGrabPointer(
				xdisplay,
				xwindow,
				true,
				(uint)(ButtonPressMask | ButtonReleaseMask | PointerMotionMask),
				GrabModeAsync,
				GrabModeAsync,
				xwindow,
				invisibleCursor,
				IntPtr.Zero
			);

			if (err != 0) logger.LogTrace($"grab {err}");

			XFlush(xdisplay);
		}

		private void ReleaseMouse()
		{
			logger.LogTrace("Release");

			XUngrabPointer(xdisplay, IntPtr.Zero);
			//XFixesShowCursor(xdisplay, xwindow);
			XFlush(xdisplay);
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
			return IsCaptured;
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
