using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RunAmiga.Custom
{
	public class Playfield
	{
		public readonly Copper copper;
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

		public uint spr0pt;
		public uint spr0pos;
		public uint spr0ctl;
		public uint spr0data;
		public uint spr0datb;

		public uint address;

		public ushort [] colour = new ushort[256];
		public uint [] truecolour = new uint[256];

		public Playfield(Copper copper)
		{
			this.copper = copper;
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
		private Bitmap bitmap;

		public Display(Playfield pf, IMemoryMappedDevice memory)
		{
			this.pf = pf;
			//pf.bplcon0 |= 0x8000;

			this.memory = memory;
			uint bitdepth = (pf.bplcon0 >> 12)&7;

			picture = new PictureBox();

			int w = 320;
			if ((pf.bplcon0 & 0x8000) != 0) w *= 2;

			picture.Size = new Size(w, 256);
			picture.Location = new Point(0, 0);

			form = new Form();
			form.Controls.Add(picture);
			form.Text = $"0x{pf.address:X6}";
			form.ClientSize = new Size(w, 256);
			form.KeyPress += DumpCopperList;
			form.Closing += FormClosing;

			bitmap = new Bitmap(w, 256, PixelFormat.Format32bppRgb);
			picture.Image = bitmap;

			Refresh();

			form.Show();
			form.Invalidate();

			Application.DoEvents();
		}

		private void FormClosing(object sender, CancelEventArgs e)
		{
			//remove from list of playfields
			pf.copper.RemoveDisplay(pf);
		}

		private void DumpCopperList(object sender, KeyPressEventArgs e)
		{
			//re-dump the copper list details
			pf.copper.DebugCopperList(pf.address);
		}

		public void Refresh()
		{
			int w = 320;
			if ((pf.bplcon0 & 0x8000) != 0) w *= 2;

			if (bitmap.Width != w)
				bitmap = new Bitmap(w, 256, PixelFormat.Format32bppRgb);

			var pixels = new int[w * 256];

			PlanarToChunky(pixels);

			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, w, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
			Marshal.Copy(pixels, 0, bitmapData.Scan0, w * 256);
			bitmap.UnlockBits(bitmapData);
			picture.Image = bitmap;
			form.Invalidate();

			Application.DoEvents();
		}

		private void PlanarToChunky(int [] dst)
		{
			int w = 320;
			

			uint p;
			uint d=0;

			uint s_bpl1pt = pf.bpl1pt & ~1u;
			uint s_bpl2pt = pf.bpl2pt & ~1u;

			if (s_bpl1pt == 0xfffffffe)
			{

			}
			else if (s_bpl1pt == 0)
			{
				//var random = new Random();
				for (int i = 0; i < w * 256; i++)
					dst[i] = (i & 0xff) ^ (i >> 8); //random.Next(3);
				pf.bpl1pt = 0xfffffffe;
			}
			else
			{
				for (int y = 0; y < 256; y++)
				{
					for (int i = 0; i < w/16; i++)
					{
						uint p0 = memory.Read(0, s_bpl1pt, Types.Size.Word);
						uint p1 = memory.Read(0, s_bpl2pt, Types.Size.Word);
						for (int j = 15; j >= 0; j--)
						{
							p = (p0 >> j) & 1;
							p |= ((p1 >> j) & 1) << 1;
							dst[d++] = (int) p;
						}

						s_bpl1pt += 2;
						s_bpl2pt += 2;
					}

					s_bpl1pt += pf.bpl1mod & ~1u;
					s_bpl2pt += pf.bpl2mod & ~1u;
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

			for (int i = 0; i < w * 256; i++)
			{
				dst[i] = (int)pf.truecolour[dst[i]&0xff];
			}

			int sprx = (int)(pf.spr0pos & 0xff);
			if ((pf.bplcon0 & 0x8000) != 0) sprx *= 2;
			int spry = (int)(pf.spr0pos >> 8);
			if (sprx < 1) sprx = 1;
			else if (sprx >= 638) sprx = 638;
			if (spry < 1) spry = 1;
			if (spry >= 254) spry = 254;
			
			dst[sprx + w * spry] ^= 0xffffff;
			dst[sprx+1 + w * spry] ^= 0xffffff;
			dst[sprx-1 + w * spry] ^= 0xffffff;
			dst[sprx + w * (spry-1)] ^= 0xffffff;
			dst[sprx + w * (spry+1)] ^= 0xffffff;

			//for (int j = 0; j < 4; j++){
			//for (int i = w * (10 + j); i < w * (11 + j); i++)
			//	dst[i] = (int)pf.truecolour[j];}
		}
	}
}
