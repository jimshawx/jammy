using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Tests
{
	public class TestKickLogo
	{
		[Test(Description = "Kickstart Logo")]
		public void TestLogo()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.Build();

			ServiceProvider serviceProvider0 = new ServiceCollection()
				.AddLogging(x =>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					x.AddDebug();
				})
				.AddSingleton<IKickstartROM, KickstartROM>()
				.AddSingleton<TestMemory>()
				.AddSingleton<ITestMemory>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryMapper>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryManager, MemoryManager>()
				.AddSingleton<IKickLogo, KickLogo>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation").Bind(o))
				.BuildServiceProvider();

			var kicklogo = serviceProvider0.GetRequiredService<IKickLogo>();
			kicklogo.KSLogo();
			Thread.Sleep(3000);
			kicklogo.Close();
		}
	}

	public interface IKickLogo
	{
		void KSLogo(); 
		void Close();
	}

	public class KickLogo : IKickLogo
	{
		[DllImport("gdi32.dll")]
		static extern uint FloodFill(IntPtr hdc, int x, int y, int color);

		[DllImport("gdi32.dll")]
		static extern uint ExtFloodFill(IntPtr hdc, int x, int y, int color, uint type);

		[DllImport("gdi32.dll")]
		static extern uint SetDCBrushColor(IntPtr hdc, int color);

		[DllImport("gdi32.dll")]
		static extern uint SetDCPenColor(IntPtr hdc, int color);

		[DllImport("gdi32.dll")]
		static extern int GetStockObject(int obj);

		[DllImport("gdi32.dll")]
		static extern uint SelectObject(IntPtr hdc, int obj);

		[DllImport("gdi32.dll")]
		static extern uint LineTo(IntPtr hdc, int x, int y);

		[DllImport("gdi32.dll")]
		static extern uint MoveToEx(IntPtr hdc, int x, int y, IntPtr lppt);

		[DllImport("gdi32.dll")]
		static extern int GetPixel(IntPtr hdc, int nXPos, int nYPos);

		[DllImport("gdi32.dll")]
		static extern int SetPixel(IntPtr hdc, int nXPos, int nYPos, int color);

		private ILogger logger;

		private readonly IKickstartROM kickstart;

		public KickLogo(IKickstartROM kickstart, ILogger<KickLogo> logger)
		{
			this.kickstart = kickstart;
			this.logger = logger;
		}

		private Form form;

		const int DC_BRUSH          = 18;
		const int DC_PEN            = 19;

		const int FLOODFILLBORDER   = 0;
		const int FLOODFILLSURFACE  = 1;

		public void KSLogo()
		{
			var range = kickstart.MappedRange().First();
			var ram = ((IBulkMemoryRead)kickstart).ReadBulk().First();
			for (ulong i = 0; i < range.Length- (ulong)kslogo.Length; i++)
			{
				if (kslogo.SequenceEqual(ram.Memory.Skip((int)i).Take(kslogo.Length)))
				{
					logger.LogTrace($"Found the kickstart logo at {i+range.Start:X8}");
					break;
				}
			}

			int k = 0;
			byte b0, b1;
			int mode = 0; //0 unknown, 1 polyline start, 2 polyline, 3 fill
			const int ox = 70, oy = 40;
			form = new Form { ClientSize = new Size(320, 200) };
			var bitmap = new Bitmap(320, 200, PixelFormat.Format32bppRgb);
			var picture = new PictureBox { Size = new Size(320, 200), Image = bitmap };

			form.Controls.Add(picture);
			form.Show();

			int[] colours = [0xffffff, 0x000000, 0xcc7777, 0xbbbbbb];

			var g = picture.CreateGraphics();
			int dx = 0, dy = 0;

			var hdc = g.GetHdc();
			SelectObject(hdc, (int)bitmap.GetHbitmap());

			int dcbrush = GetStockObject(DC_BRUSH);
			int dcpen = GetStockObject(DC_PEN);

			SetDCBrushColor(hdc, colours[0]);
			SetDCPenColor(hdc, colours[1]);

			SelectObject(hdc, dcbrush);
			SelectObject(hdc, dcpen);

			for (; ; )
			{
				b0 = kslogo[k++];
				b1 = kslogo[k++];
				if (b0 == 0xff && b1 == 0xff) break;
				if (b0 == 0xfe)
				{
					logger.LogTrace($"fill colour {b1} {colours[b1]:X6}");
					SelectObject(hdc, dcbrush);
					SetDCBrushColor(hdc, colours[b1]);
					mode = 3;
				}
				else if (b0 == 0xff)
				{
					logger.LogTrace($"colour {b1} {colours[b1]:X6}");
					SelectObject(hdc, dcpen);
					SetDCPenColor(hdc, colours[b1]);
					mode = 1;
				}
				else
				{
					if (mode == 0) logger.LogTrace("unknown mode");
					else if (mode == 1)
					{
						logger.LogTrace($"move {ox + b0},{oy + b1}");
						dx = ox + b0;
						dy = oy + b1;
						MoveToEx(hdc, dx, dy, IntPtr.Zero);

						mode = 2;
					}
					else if (mode == 2)
					{
						logger.LogTrace($"draw {ox + b0},{oy + b1} // {ox + b0 - dx},{oy + b1 - dy}");
						int nx = ox + b0, ny = oy + b1;

						LineTo(hdc, nx, ny);

						dx = nx;
						dy = ny;
					}
					else if (mode == 3)
					{
						logger.LogTrace($"fill {ox + b0},{oy + b1}");

						int pix = GetPixel(hdc, ox + b0, oy + b1);
						ExtFloodFill(hdc, ox + b0, oy + b1, pix, FLOODFILLSURFACE);

						mode = 0;
					}
				}
			}
			g.ReleaseHdc();
		}

		public void Close()
		{
			form.Close();
			form.Dispose();
		}

		//ks logo
		private static readonly byte[] kslogo =
		[
			0xFF, 0x01, 0x23, 0x0B, 0x3A, 0x0B, 0x3A, 0x21, 0x71, 0x21, 0x71, 0x0B, 0x7D, 0x0B, 0x88, 0x16, 0x88, 0x5E, 0x7F, 0x5E, 0x7F, 0x38, 0x40, 0x38,
			0x3E, 0x36, 0x35, 0x36, 0x34, 0x38, 0x2D, 0x38, 0x2D, 0x41, 0x23, 0x48, 0x23, 0x0B, 0xFE, 0x02, 0x25, 0x45, 0xFF, 0x01, 0x21, 0x48, 0x21, 0x0A,
			0x7E, 0x0A, 0x8A, 0x16, 0x8A, 0x5F, 0x56, 0x5F, 0x56, 0x64, 0x52, 0x6C, 0x4E, 0x71, 0x4A, 0x74, 0x44, 0x7D, 0x3C, 0x81, 0x3C, 0x8C, 0x0A, 0x8C,
			0x0A, 0x6D, 0x09, 0x6D, 0x09, 0x51, 0x0D, 0x4B, 0x14, 0x45, 0x15, 0x41, 0x19, 0x3A, 0x1E, 0x37, 0x21, 0x36, 0x21, 0x36, 0x1E, 0x38, 0x1A, 0x3A,
			0x16, 0x41, 0x15, 0x45, 0x0E, 0x4B, 0x0A, 0x51, 0x0A, 0x6C, 0x0B, 0x6D, 0x0B, 0x8B, 0x28, 0x8B, 0x28, 0x76, 0x30, 0x76, 0x34, 0x72, 0x34, 0x5F,
			0x32, 0x5C, 0x32, 0x52, 0x41, 0x45, 0x41, 0x39, 0x3E, 0x37, 0x3B, 0x37, 0x3E, 0x3A, 0x3E, 0x41, 0x3D, 0x42, 0x36, 0x42, 0x33, 0x3F, 0x2A, 0x46,
			0x1E, 0x4C, 0x12, 0x55, 0x12, 0x54, 0x1E, 0x4B, 0x1A, 0x4A, 0x17, 0x47, 0x1A, 0x49, 0x1E, 0x4A, 0x21, 0x48, 0xFF, 0x01, 0x32, 0x3D, 0x34, 0x36,
			0x3C, 0x37, 0x3D, 0x3A, 0x3D, 0x41, 0x36, 0x41, 0x32, 0x3D, 0xFF, 0x01, 0x33, 0x5C, 0x33, 0x52, 0x42, 0x45, 0x42, 0x39, 0x7D, 0x39, 0x7D, 0x5E,
			0x34, 0x5E, 0x33, 0x5A, 0xFF, 0x01, 0x3C, 0x0B, 0x6F, 0x0B, 0x6F, 0x20, 0x3C, 0x20, 0x3C, 0x0B, 0xFF, 0x01, 0x60, 0x0E, 0x6B, 0x0E, 0x6B, 0x1C,
			0x60, 0x1C, 0x60, 0x0E, 0xFE, 0x03, 0x3E, 0x1F, 0xFF, 0x01, 0x62, 0x0F, 0x69, 0x0F, 0x69, 0x1B, 0x62, 0x1B, 0x62, 0x0F, 0xFE, 0x02, 0x63, 0x1A,
			0xFF, 0x01, 0x2F, 0x39, 0x32, 0x39, 0x32, 0x3B, 0x2F, 0x3F, 0x2F, 0x39, 0xFF, 0x01, 0x29, 0x8B, 0x29, 0x77, 0x30, 0x77, 0x35, 0x72, 0x35, 0x69,
			0x39, 0x6B, 0x41, 0x6B, 0x41, 0x6D, 0x45, 0x72, 0x49, 0x72, 0x49, 0x74, 0x43, 0x7D, 0x3B, 0x80, 0x3B, 0x8B, 0x29, 0x8B, 0xFF, 0x01, 0x35, 0x5F,
			0x35, 0x64, 0x3A, 0x61, 0x35, 0x5F, 0xFF, 0x01, 0x39, 0x62, 0x35, 0x64, 0x35, 0x5F, 0x4A, 0x5F, 0x40, 0x69, 0x3F, 0x69, 0x41, 0x67, 0x3C, 0x62,
			0x39, 0x62, 0xFF, 0x01, 0x4E, 0x5F, 0x55, 0x5F, 0x55, 0x64, 0x51, 0x6C, 0x4E, 0x70, 0x49, 0x71, 0x46, 0x71, 0x43, 0x6D, 0x43, 0x6A, 0x4E, 0x5F,
			0xFF, 0x01, 0x44, 0x6A, 0x44, 0x6D, 0x46, 0x70, 0x48, 0x70, 0x4C, 0x6F, 0x4D, 0x6C, 0x49, 0x69, 0x44, 0x6A, 0xFF, 0x01, 0x36, 0x68, 0x3E, 0x6A,
			0x40, 0x67, 0x3C, 0x63, 0x39, 0x63, 0x36, 0x65, 0x36, 0x68, 0xFF, 0x01, 0x7E, 0x0B, 0x89, 0x16, 0x89, 0x5E, 0xFE, 0x01, 0x22, 0x0B, 0xFE, 0x01,
			0x3B, 0x0B, 0xFE, 0x01, 0x61, 0x0F, 0xFE, 0x01, 0x6A, 0x1B, 0xFE, 0x01, 0x70, 0x0F, 0xFE, 0x01, 0x7E, 0x5E, 0xFE, 0x01, 0x4B, 0x60, 0xFE, 0x01,
			0x2E, 0x39, 0xFF, 0xFF
		];
		//https://retrocomputing.stackexchange.com/questions/13897/why-was-the-kickstart-1-x-insert-floppy-graphic-so-bad

		// The code uses the SetAPen, Move, Draw, Flood, and BltTemplate calls (and some others) from graphics.library to do all this. The screen resolution is set to 320x200 (2 bitplanes; 4 colors) and the code centers the vector image by drawing it at an offset.

		//412 bytes (KS 1.2 FE8E1C->FE8FB8, routine @ fe8cfa)

		//Rendering algorithm:

		//Read two bytes at a time.
		//If both bytes are FF, end the program.
		//If the first byte is FF and the second byte is not, start drawing a polyline with the color index given in the second byte.
		//  Treat any subsequent two bytes as x, y coordinates belonging to that polyline except if the first byte is FF (see rules 2 and 3) or FE(see rule 4), which is where you stop drawing the line.
		//If the first byte is FE, flood fill an area using the color index given in the second byte, starting from the point whose coordinates are given in the next two bytes.
		//The palette is:

		//    0: #fff 
		//    1: #000
		//    2: #77c
		//    3: #bbb
		//The offsets used for drawing the image centered are X = 70, Y= 40.
	}
}