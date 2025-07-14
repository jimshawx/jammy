using Jammy.Core.Interface.Interfaces;
using Jammy.NativeOverlay.Overlays;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.X
{
	public class EmulationWindow : IEmulationWindow, IDisposable
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


		[StructLayout(LayoutKind.Explicit)]
		private struct XEvent
		{
			[FieldOffset(0)] public int type;
			[FieldOffset(0)] public XKeyEvent xkey;
			[FieldOffset(0)] public XButtonEvent xbutton;
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

		private const int KeyPress = 2;
		private const int KeyRelease = 3;
		private const int ButtonPress = 4;
		private const int ButtonRelease = 5;

		private const long KeyPressMask = 1 << 0;
		private const long KeyReleaseMask = 1 << 1;
		private const long ButtonPressMask = 1 << 2;
		private const long ButtonReleaseMask = 1 << 3;
		private readonly IDiskLightOverlay diskLightOverlay;
		private readonly ITicksOverlay ticksOverlay;
		private readonly ILogger logger;

		private int[] screen;

		public EmulationWindow(IDiskLightOverlay diskLightOverlay, ITicksOverlay ticksOverlay, ILogger<EmulationWindow> logger)
		{
			this.diskLightOverlay = diskLightOverlay;
			this.ticksOverlay = ticksOverlay;
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
			ticksOverlay.Render();
			diskLightOverlay.Render();

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

		public void SetPicture(int width, int height)
		{
			xdisplay = XOpenDisplay(IntPtr.Zero);
			var rootWindow = XRootWindow(xdisplay, XDefaultScreen(xdisplay));
			xwindow = XCreateSimpleWindow(xdisplay, rootWindow, 10, 10, (uint)width, (uint)height, 1, 0, 0xFFFFFF);
			XStoreName(xdisplay, xwindow, "Jammy : Alt-Tab or Middle Mouse Click to detach mouse");
			XSelectInput(xdisplay, xwindow, KeyPress | KeyRelease | ButtonPressMask | ButtonReleaseMask);

			gc = XCreateGC(xdisplay, xwindow, 0, IntPtr.Zero);
			//gc = XDefaultGC(xdisplay, 0);

			XMapWindow(xdisplay, xwindow);
			XClearWindow(xdisplay, xwindow);
			XFlush(xdisplay);

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

			// XEvent xevent;
			// while (true)
			// {
			// 	XNextEvent(display, out xevent);

			// 	if (xevent.type == Expose)
			// 	{
			// 		Console.WriteLine("Window exposed");
			// 	}

			// }
			//screen = new int[screenWidth * screenHeight];
			screen = GC.AllocateArray<int>((int)(screenWidth * screenHeight), true);
			ximage.data = Marshal.UnsafeAddrOfPinnedArrayElement(screen, 0);
		}

		// private void XEventHandler()
		// {
		// 	XEvent xevent;
		// 	while (true)
		// 	{
		// 		XNextEvent(xdisplay, out xevent);

		// 		if (xevent.type == Expose)
		// 		{
		// 			Console.WriteLine("Window exposed");
		// 		}

		// }

		public Types.Types.Point RecentreMouse()
		{
			//put the cursor back in the middle of the emulation window

			int x = 0;
			int y = 0;
			return new Types.Types.Point { X = x, Y = y };
		}

		public void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp)
		{

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