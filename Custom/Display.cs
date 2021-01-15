using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

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
	}

	public class Display
	{
		private readonly Form form;
		private readonly PictureBox picture;
		private readonly Bitmap bitmap;

		public Display(Playfield pf, Memory memory)
		{
			uint bitdepth = (pf.bplcon0 >> 12)&7;
			//if (bitdepth == 0) return;

			picture = new PictureBox();
			picture.Size = new Size(320, 200);
			picture.Location = new Point(0, 0);

			form = new Form();
			form.Controls.Add(picture);
			form.Text = $"0x{pf.address:X6}";
			form.Size = new Size(320, 200);

			var random = new Random();
			var pixels = new int[320 * 200];
			for (int i = 0; i < 320 * 200; i++)
				pixels[i] = (int)(0xff0000ff|random.Next());

			PlanarToChunky(pf, memory, pixels);

			bitmap = new Bitmap(320, 200, PixelFormat.Format32bppRgb);
			var bitmapData = bitmap.LockBits(new Rectangle(0,0,320,200), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
			Marshal.Copy(pixels, 0, bitmapData.Scan0, 320*200);
			bitmap.UnlockBits(bitmapData);

			picture.Image = bitmap;

			form.Show();
			form.Invalidate();

			Application.DoEvents();
		}

		public void Refresh()
		{

		}

		private void PlanarToChunky(Playfield pf, Memory memory, int [] dst)
		{
			uint p;
			uint d=0;
			for (int i = 0; i < 20 * 200; i++)
			{
				uint p0 = memory.read16(pf.bpl1pt);
				uint p1 = memory.read16(pf.bpl2pt);
				for (int j = 0; j < 16; j++)
				{
					p = (p0 >> j) & 1;
					p |= ((p1 >> j) & 1)<<1;
					dst[d++] = (int)p;
				}
				pf.bpl1pt += 2;
				pf.bpl2pt += 2;
			}

			int[] cols = new int[4] {0x0f00ffff, 0xff00ff, 0xff, 0x00ffff};
			for (int i = 0; i < 320 * 200; i++)
			{
				dst[i] = cols[dst[i]];
			}
		}
	}
}
