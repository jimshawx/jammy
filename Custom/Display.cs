using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RunAmiga.Custom
{
	public class Playfield
	{
		public uint bpl1mod;
		public uint bpl2mod;

		public uint bplcon0;
		public uint bplcon1;
		public uint bplcon2;
		public uint bplcon3;
		public uint bplcon4;

		public uint bpl1dat;
		public uint bpl2dat;
		public uint bpl3dat;
		public uint bpl4dat;
		public uint bpl5dat;
		public uint bpl6dat;
		public uint bpl7dat;
		public uint bpl8dat;

		public uint bpl1pt;
		public uint bpl2pt;
		public uint bpl3pt;
		public uint bpl4pt;
		public uint bpl5pt;
		public uint bpl6pt;
		public uint bpl7pt;
		public uint bpl8pt;

		public uint diwstrt;
		public uint diwstop;

		public uint ddfstrt;
		public uint ddfstop;

		public uint address;

		public ushort [] colour = new ushort[256];
		public uint [] truecolour = new uint[256];

		public Playfield()
		{
			var r = new Random();
			for (int i = 0; i < 256; i++)
			{
				uint col = (uint)r.Next(0xfff);
				colour[i] = (ushort)col;
				truecolour[i] = ((col & 0xf) * 0x11) + ((col & 0xf0) * 0x110) + ((col & 0xf00) * 0x1100);
			}
		}

		public void UpdatePalette()
		{
			for (int i = 0; i < 256; i++)
			{
				uint col = colour[i];
				colour[i] = (ushort)col;
				truecolour[i] = ((col & 0xf) * 0x11) + ((col & 0xf0) * 0x110) + ((col & 0xf00) * 0x1100);
			}
		}
	}

	public class Display
	{
		public Playfield pf { get; }
		private readonly IMemoryMappedDevice memory;
		private readonly Form form;
		private readonly PictureBox picture;
		private readonly Bitmap bitmap;

		public Display(Playfield pf, IMemoryMappedDevice memory)
		{
			this.pf = pf;
			this.memory = memory;
			uint bitdepth = (pf.bplcon0 >> 12)&7;
			//if (bitdepth == 0) return;

			picture = new PictureBox();
			picture.Size = new Size(320, 200);
			picture.Location = new Point(0, 0);

			form = new Form();
			form.Controls.Add(picture);
			form.Text = $"0x{pf.address:X6}";
			form.ClientSize = new Size(320, 200);

			bitmap = new Bitmap(320, 200, PixelFormat.Format32bppRgb);
			picture.Image = bitmap;

			Refresh();

			form.Show();
			form.Invalidate();

			Application.DoEvents();
		}

		public void Refresh()
		{
			var pixels = new int[320 * 200];

			PlanarToChunky(pixels);

			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, 320, 200), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
			Marshal.Copy(pixels, 0, bitmapData.Scan0, 320 * 200);
			bitmap.UnlockBits(bitmapData);
			picture.Image = bitmap;
			form.Invalidate();

			Application.DoEvents();
		}

		private void PlanarToChunky(int [] dst)
		{
			uint p;
			uint d=0;

			uint s_bpl1pt = pf.bpl1pt;
			uint s_bpl2pt = pf.bpl2pt;

			if (s_bpl1pt == 0)
			{
				//var random = new Random();
				for (int i = 0; i < 320 * 200; i++)
					dst[i] = (i & 0xff) ^ (i >> 8); //random.Next(3);
			}
			else
			{
				for (int i = 0; i < 20 * 200; i++)
				{
					uint p0 = memory.Read(0,s_bpl1pt, Types.Size.Word);
					uint p1 = memory.Read(0,s_bpl2pt, Types.Size.Word);
					for (int j = 0; j < 16; j++)
					{
						p = (p0 >> j) & 1;
						p |= ((p1 >> j) & 1) << 1;
						dst[d++] = (int) p;
					}

					s_bpl1pt += 2;
					s_bpl2pt += 2;
				}
			}

			//    0: #fff 
			//    1: #000
			//    2: #77c
			//    3: #bbb
			//pf.truecolour[0] = 0xffffff;
			//pf.truecolour[1] = 0x000000;
			//pf.truecolour[2] = 0x7777cc;
			//pf.truecolour[3] = 0xbbbbbb;
			pf.UpdatePalette();

			for (int i = 0; i < 320 * 200; i++)
			{
				dst[i] = (int)pf.truecolour[dst[i]];
			}

			for (int j = 0; j < 4; j++)
			for (int i = 320 * (10 + j); i < 320 * (11 + j); i++)
				dst[i] = (int)pf.truecolour[j];
		}
	}
}
