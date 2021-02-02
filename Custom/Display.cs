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
		public uint diwhigh;

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

		public int width;
		public int height;
		public int width2;

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

			pf.width = 320;
			pf.height = 256;

			this.memory = memory;
			//uint bitdepth = (pf.bplcon0 >> 12)&7;

			picture = new PictureBox();

			if ((pf.bplcon0 & 0x8000) != 0) pf.width *= 2;

			picture.Size = new Size(pf.width, pf.height);
			picture.Location = new Point(0, 0);

			form = new Form();
			form.Controls.Add(picture);
			form.Text = $"0x{pf.address:X6}";
			form.ClientSize = new Size(pf.width, pf.height);
			form.KeyPress += DumpCopperList;
			form.Closing += FormClosing;

			bitmap = new Bitmap(pf.width, pf.height, PixelFormat.Format32bppRgb);
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

			//DDFSTRT 0040 64
			//DDFSTOP 00D0 208
			//64 = 208-4(N-1))
			//4(N-1)=208-64
			//(N-1)=144/4
			//N-1=36
			//N=37

			//START = STOP - 4 (N-1)
			//((STOP-START)/4)+1
			int div = 8;
			if ((pf.bplcon0 & 0x8000) != 0)
				div = 4;

			pf.width2 = (int)((pf.ddfstop - pf.ddfstrt) / div + 1) * 16;

			int startx = (int)pf.diwstrt & 0xff;
			int stopx = (int)(pf.diwstop & 0xff)+0x100;
			pf.width = stopx - startx;

			int starty = (int)(pf.diwstrt >>8);
			int stopy = (int)(pf.diwstop >>8);
			if ((stopy & 0x80) == 0) stopy += 0x100;
			pf.height = stopy - starty;

			//pf.bplcon0 |= 0x8000;
			//pf.width = 320;
			if ((pf.bplcon0 & 0x8000)!=0)
				pf.width *= 2;
			//pf.height = 256;

			if (bitmap.Width != pf.width || bitmap.Height != pf.height)
			{
				Trace.WriteLine($"DDFSTRT {pf.ddfstrt:X4} {pf.ddfstrt >> 8} {pf.ddfstrt & 0xff}");
				Trace.WriteLine($"DDFSTOP {pf.ddfstop:X4} {pf.ddfstop >> 8} {pf.ddfstop & 0xff}");
				Trace.WriteLine($"DIWSTRT {pf.diwstrt:X4} {pf.diwstrt >> 8} {pf.diwstrt & 0xff}");
				Trace.WriteLine($"DIWSTOP {pf.diwstop:X4} {pf.diwstop >> 8} {pf.diwstop & 0xff}");
				Trace.WriteLine($"BPL1MOD {pf.bpl1mod:X4} {(int)pf.bpl1mod}");
				Trace.WriteLine($"BPL2MOD {pf.bpl1mod:X4} {(int)pf.bpl2mod}");

				Trace.WriteLine($"{pf.width}x{pf.height}-{pf.width2} mod {pf.width2-pf.width}");

				form.ClientSize = picture.Size = new Size(pf.width, pf.height);
				bitmap = new Bitmap(pf.width, pf.height, PixelFormat.Format32bppRgb);
				picture.Image = bitmap;
			}

			var pixels = new int[pf.width *pf.height];

			PlanarToChunky(pixels);

			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pf.width, pf.height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
			Marshal.Copy(pixels, 0, bitmapData.Scan0, pf.width * pf.height);
			bitmap.UnlockBits(bitmapData);
			picture.Image = bitmap;
			form.Invalidate();

			Application.DoEvents();
		}

		private void PlanarToChunky(int [] dst)
		{
			uint d=0;

			uint[] s_bplpt = new uint [8]
			{
				pf.bpl1pt & ~1u,
				pf.bpl2pt & ~1u,
				pf.bpl3pt & ~1u,
				pf.bpl4pt & ~1u,
				pf.bpl5pt & ~1u,
				pf.bpl6pt & ~1u,
				pf.bpl7pt & ~1u,
				pf.bpl8pt & ~1u
			};

			if (s_bplpt[0] == 0xfffffffe)
			{

			}
			else if (s_bplpt[0] == 0)
			{
				//var random = new Random();
				for (int i = 0; i < pf.width*pf.height; i++)
					dst[i] = (i & 0xff) ^ (i >> 8); //random.Next(3);
				pf.bpl1pt = 0xfffffffe;
			}
			else
			{
				try
				{
					uint[] p = new uint[8];
					uint planes = (pf.bplcon0 >> 12) & 7;
					//if (planes == 0) planes = 2;
					uint pix;
					for (int y = 0; y < pf.height; y++)
					{
						for (int x = 0; x < pf.width / 16; x++)
						{
							for (int i = 0; i < planes; i++)
								p[i] = memory.Read(0, s_bplpt[i], Types.Size.Word);

							for (int j = 15; j >= 0; j--)
							{
								pix = 0;
								for (int i = 0; i < planes; i++)
									pix |= ((p[i] >> j) & 1) << i;
								dst[d++] = (int)pix;
							}

							for (int i = 0; i < planes; i++)
								s_bplpt[i] += 2;
						}

						/*
						for (int i = 0; i < planes; i++)
						{
							s_bplpt[i] += (uint)(pf.width2 - pf.width) / 16;
							s_bplpt[i] &= ~1u;
						}

						for (int i = 0; i < planes; i += 2)
						{
							s_bplpt[i] += pf.bpl1mod & ~1u;
							s_bplpt[i + 1] += pf.bpl2mod & ~1u;
						}
						*/
					}
				}
				catch { }
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

			for (int i = 0; i < pf.width * pf.height; i++)
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
			
			dst[sprx + pf.width * spry] ^= 0xffffff;
			dst[sprx+1 + pf.width * spry] ^= 0xffffff;
			dst[sprx-1 + pf.width * spry] ^= 0xffffff;
			dst[sprx + pf.width * (spry-1)] ^= 0xffffff;
			dst[sprx + pf.width * (spry+1)] ^= 0xffffff;

			//for (int j = 0; j < 4; j++){
			//for (int i = w * (10 + j); i < w * (11 + j); i++)
			//	dst[i] = (int)pf.truecolour[j];}
		}
		//DDFSTRT = DDFSTOP - (8 * (word count - 1)) for low resolution
		//DDFSTRT = DDFSTOP - (4 * (word count - 2)) for high resolution
		
		//DIWSTRT 6395 start at (99,149)
		//DIWSTOP F4AD stop at F4,1_AD (244,0b11010_1101) (244,429)
		//145,280
		//DDFSTRT 0040 64
		//DDFSTOP 00D0 208
		//64 = 208-4(N-1))
		//4(N-1)=208-64
		//(N-1)=144/4
		//N-1=36
		//N=37
		//DIWHIGH 2000
		//BPLMOD  FFFA -6

		//PAL
		//DIWSTRT 2C81 (44,129)
		//DIWSTOP 2CC1 (300,449)
		//256,320
	}
}
