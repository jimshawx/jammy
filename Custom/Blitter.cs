using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Win32;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class Blitter : IEmulate
	{
		private readonly Chips custom;
		private readonly IMemoryMappedDevice memory;
		private readonly Interrupt interrupt;

		public Blitter(Chips custom, IMemoryMappedDevice memory, Interrupt interrupt)
		{
			this.custom = custom;
			this.memory = memory;
			this.interrupt = interrupt;
		}

		public void Emulate(ulong ns)
		{

		}

		public void Reset()
		{
			bltapt = 0;
			bltbpt = 0;
			bltcpt = 0;
			bltdpt = 0;

			bltamod = 0;
			bltbmod = 0;
			bltcmod = 0;
			bltdmod = 0;

			bltadat = 0;
			bltbdat = 0;
			bltcdat = 0;
			bltddat = 0;

			bltafwm = 0;
			bltalwm = 0;

			bltsize = 0;
			bltsizv = 0;
			bltsizh = 0;

			bltcon0 = 0;
			bltcon1 = 0;
		}

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			//Logger.WriteLine($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");
			return value;
		}

		private uint bltapt;
		private uint bltbpt;
		private uint bltcpt;
		private uint bltdpt;

		private uint bltamod;
		private uint bltbmod;
		private uint bltcmod;
		private uint bltdmod;

		private uint bltadat;
		private uint bltbdat;
		private uint bltcdat;
		private uint bltddat;

		private uint bltafwm;
		private uint bltalwm;

		private uint bltsize;
		private uint bltsizv;
		private uint bltsizh;

		private uint bltcon0;
		private uint bltcon1;

		public void Write(uint insaddr, uint address, ushort value)
		{
			//Logger.WriteLine($"W {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.BLTCON0:
					bltcon0 = value;
					//uint lf = (uint) value & 0xff;
					//uint ash = (uint) value >> 12;
					//uint use = (uint) (value >> 8) & 0xf;
					//Logger.WriteLine($"minterm:{lf:X2} ash:{ash} use:{Convert.ToString(use, 2).PadLeft(4, '0')}");
					break;

				case ChipRegs.BLTCON1:
					bltcon1 = value;
					//uint bsh = (uint) value >> 12;
					//uint doff = (uint) (value >> 7) & 1;
					//uint efe = (uint) (value >> 4) & 1;
					//uint ife = (uint) (value >> 3) & 1;
					//uint fci = (uint) (value >> 2) & 1;
					//uint desc = (uint) (value >> 1) & 1;
					//uint line = (uint) value & 1;
					//Logger.WriteLine($"bsh:{bsh} doff:{doff} efe:{efe} ife:{ife} fci:{fci} desc:{desc} line:{line}");
					break;

				case ChipRegs.BLTAFWM:
					bltafwm = value;
					break;
				case ChipRegs.BLTALWM:
					bltalwm = value;
					break;

				case ChipRegs.BLTCPTH:
					bltcpt = (bltcpt & 0x0000ffff) | ((uint) value << 16);
					break;
				case ChipRegs.BLTCPTL:
					bltcpt = (bltcpt & 0xffff0000) | value;
					break;
				case ChipRegs.BLTBPTH:
					bltbpt = (bltbpt & 0x0000ffff) | ((uint) value << 16);
					break;
				case ChipRegs.BLTBPTL:
					bltbpt = (bltbpt & 0xffff0000) | value;
					break;
				case ChipRegs.BLTAPTH:
					bltapt = (bltapt & 0x0000ffff) | ((uint) value << 16);
					break;
				case ChipRegs.BLTAPTL:
					bltapt = (bltapt & 0xffff0000) | value;
					break;
				case ChipRegs.BLTDPTH:
					bltdpt = (bltdpt & 0x0000ffff) | ((uint) value << 16);
					break;
				case ChipRegs.BLTDPTL:
					bltdpt = (bltdpt & 0xffff0000) | value;
					break;

				case ChipRegs.BLTSIZE:
					bltsize = value;
					BlitSmall(insaddr);
					break;

				case ChipRegs.BLTCON0L:
					bltcon0 = (bltcon0 & 0x0000ff00) | ((uint) value & 0x000000ff);
					uint minterm = (uint) value & 0xff;
					Logger.WriteLine($"minterm:{minterm:X2}");
					break;

				case ChipRegs.BLTSIZV:
					bltsizv = value;
					break;
				case ChipRegs.BLTSIZH:
					bltsizh = value;
					BlitBig(insaddr);
					break;

				case ChipRegs.BLTCMOD:
					bltcmod = (uint) (short) value;
					break;
				case ChipRegs.BLTBMOD:
					bltbmod = (uint) (short) value;
					break;
				case ChipRegs.BLTAMOD:
					bltamod = (uint) (short) value;
					break;
				case ChipRegs.BLTDMOD:
					bltdmod = (uint) (short) value;
					break;

				case ChipRegs.BLTCDAT:
					bltcdat = value;
					break;
				case ChipRegs.BLTBDAT:
					bltbdat = value;
					break;
				case ChipRegs.BLTADAT:
					bltadat = value;
					break;
				case ChipRegs.BLTDDAT:
					bltddat = value;
					break;
			}

			//hacky alignment fudge

			//if ((bltapt & 1) != 0 && (bltcon0 & (1u << 11)) != 0) Logger.WriteLine($"Channel A is odd {bltapt:X8}");
			//if ((bltbpt & 1) != 0 && (bltcon0 & (1u << 10)) != 0) Logger.WriteLine($"Channel B is odd {bltbpt:X8}");
			//if ((bltcpt & 1) != 0 && (bltcon0 & (1u << 9)) != 0) Logger.WriteLine($"Channel C is odd {bltcpt:X8}");
			//if ((bltdpt & 1) != 0 && (bltcon0 & (1u << 8)) != 0) Logger.WriteLine($"Channel D is odd {bltdpt:X8}");
			//if ((bltamod & 1) != 0 && (bltcon0 & (1u << 11)) != 0) Logger.WriteLine($"Channel Amod is odd {bltamod:X8}");
			//if ((bltbmod & 1) != 0 && (bltcon0 & (1u << 10)) != 0) Logger.WriteLine($"Channel Bmod is odd {bltbmod:X8}");
			//if ((bltcmod & 1) != 0 && (bltcon0 & (1u << 9)) != 0) Logger.WriteLine($"Channel Cmod is odd {bltcmod:X8}");
			//if ((bltdmod & 1) != 0 && (bltcon0 & (1u << 8)) != 0) Logger.WriteLine($"Channel Dmod is odd {bltdmod:X8}");

			//these bits are ignored anyway
			bltapt &= ~1u;
			bltbpt &= ~1u;
			bltcpt &= ~1u;
			bltdpt &= ~1u;

			bltamod &= ~1u;
			bltbmod &= ~1u;
			bltcmod &= ~1u;
			bltdmod &= ~1u;
		}

		private void BlitSmall(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			if ((bltcon1 & (7 << 2)) != 0)
			{
				Fill(insaddr);
				return;
			}

			uint width = bltsize & 0x3f;
			uint height = bltsize >> 6;

			//Logger.WriteLine($"BLIT! size:{width}x{height} @{insaddr:X8}");
			Blit(width, height);
		}

		private void BlitBig(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			if ((bltcon1 & (7 << 2)) != 0)
			{
				Fill(insaddr);
				return;
			}

			//Logger.WriteLine($"BLIT! size:{bltsizh}x{bltsizv} @{insaddr:X8}");
			Blit(bltsizh, bltsizv);
		}

		private void Fill(uint insaddr)
		{
			throw new NotImplementedException();
		}

		private void Blit(uint width, uint height)
		{
			//todo: assumes blitter DMA is enabled

			Logger.WriteLine($"{width}x{height} = {width*16}bits x {height} = {width * 16 * height} bits = {width*height*2} bytes");

			Logger.WriteLine($"->{bltapt:X6} %{(int)bltamod,9} >> {bltcon0 >> 12,2} {(((bltcon0 >> 11) & 1) != 0 ? "on" : "off")}");
			Logger.WriteLine($"->{bltbpt:X6} %{(int)bltbmod,9} >> {bltcon1 >> 12,2} {(((bltcon0 >> 10) & 1) != 0 ? "on" : "off")}");
			Logger.WriteLine($"->{bltcpt:X6} %{(int)bltcmod,9} >> -- {(((bltcon0 >> 9) & 1) != 0 ? "on" : "off")}");
			Logger.WriteLine($"->{bltdpt:X6} %{(int)bltdmod,9} >> -- {(((bltcon0 >> 8) & 1) != 0 ? "on" : "off")}");
			Logger.WriteLine($"cookie: {bltcon0&0xff:X2} {((bltcon1&2)!=0?"descending":"ascending")}");

			int ashift = (int) (bltcon0 >> 12);
			int bshift = (int) (bltcon1 >> 12);

			uint bltzero = 0;

			//set blitter busy in DMACON
			custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);

			for (uint h = 0; h < height; h++)
			{
				uint bltabits = 0;
				uint bltbbits = 0;

				for (uint w = 0; w < width; w++)
				{
					if ((bltcon0 & (1u << 11)) != 0)
					{
						bltadat = memory.Read(0, bltapt, Size.Word);
						
						if (w == 0) bltadat &= bltafwm;
						else if (w == width - 1) bltadat &= bltalwm;

						if ((bltcon1 & (1u << 1)) != 0)
						{
							bltadat <<= ashift;
							bltadat |= bltabits;
							bltabits = bltadat >> 16;
							bltadat &= 0xffff;
						}
						else
						{
							bltadat <<= (16 - ashift);
							bltadat |= bltabits;
							bltabits = bltadat << 16;
							bltadat >>= 16;
						}
					}

					if ((bltcon0 & (1u << 10)) != 0)
					{
						bltbdat = memory.Read(0, bltbpt, Size.Word);

						if ((bltcon1 & (1u << 1)) != 0)
						{
							bltbdat <<= bshift;
							bltbdat |= bltbbits;
							bltbbits = bltbdat >> 16;
							bltbdat &= 0xffff;
						}
						else
						{
							bltbdat <<= (16 - bshift);
							bltbdat |= bltbbits;
							bltbbits = bltbdat << 16;
							bltbdat >>= 16;
						}
					}

					if ((bltcon0 & (1u << 9)) != 0)
					{
						bltcdat = memory.Read(0, bltcpt, Size.Word);
					}

					//bltddat = (bltbdat & bltadat) | (bltcdat & ~bltadat);

					bltddat = 0;
					if ((bltcon0 & 1) != 0) bltddat |= ~bltadat & ~bltbdat & ~bltcdat;
					if ((bltcon0 & 2) != 0) bltddat |= ~bltadat & ~bltbdat & bltcdat;
					if ((bltcon0 & 4) != 0) bltddat |= ~bltadat & bltbdat & ~bltcdat;
					if ((bltcon0 & 8) != 0) bltddat |= ~bltadat & bltbdat & bltcdat;
					if ((bltcon0 & 16) != 0) bltddat |= bltadat & ~bltbdat & ~bltcdat;
					if ((bltcon0 & 32) != 0) bltddat |= bltadat & ~bltbdat & bltcdat;
					if ((bltcon0 & 64) != 0) bltddat |= bltadat & bltbdat & ~bltcdat;
					if ((bltcon0 & 128) != 0) bltddat |= bltadat & bltbdat & bltcdat;

					bltzero |= bltddat;

					if (((bltcon0 & (1u << 8)) != 0) && ((bltcon1 & (1u << 7)) == 0))
						memory.Write(0, bltdpt, (ushort) bltddat, Size.Word);

					//Logger.Write($"{Convert.ToString(bltddat,2).PadLeft(16,'0')}");

					if ((bltcon1 & (1u << 1)) != 0)
					{
						if ((bltcon0 & (1u << 11)) != 0) bltapt -= 2;
						if ((bltcon0 & (1u << 10)) != 0) bltbpt -= 2;
						if ((bltcon0 & (1u << 9)) != 0) bltcpt -= 2;
						if ((bltcon0 & (1u << 8)) != 0) bltdpt -= 2;
					}
					else
					{
						if ((bltcon0 & (1u << 11)) != 0) bltapt += 2;
						if ((bltcon0 & (1u << 10)) != 0) bltbpt += 2;
						if ((bltcon0 & (1u << 9)) != 0) bltcpt += 2;
						if ((bltcon0 & (1u << 8)) != 0) bltdpt += 2;
					}
				}
				//Logger.WriteLine("");

				if ((bltcon1 & (1u << 1)) != 0)
				{
					if ((bltcon0 & (1u << 11)) != 0) bltapt -= bltamod;
					if ((bltcon0 & (1u << 10)) != 0) bltbpt -= bltbmod;
					if ((bltcon0 & (1u << 9)) != 0) bltcpt -= bltcmod;
					if ((bltcon0 & (1u << 8)) != 0) bltdpt -= bltdmod;
				}
				else
				{
					if ((bltcon0 & (1u << 11)) != 0) bltapt += bltamod;
					if ((bltcon0 & (1u << 10)) != 0) bltbpt += bltbmod;
					if ((bltcon0 & (1u << 9)) != 0) bltcpt += bltcmod;
					if ((bltcon0 & (1u << 8)) != 0) bltdpt += bltdmod;
				}
			}

			//write the BZERO bit in DMACON
			if (bltzero == 0)
				custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 13), Size.Word);
			else
				custom.Write(0, ChipRegs.DMACON, (1u << 13), Size.Word);

			//write blitter interrupt bit to INTREQ
			custom.Write(0, ChipRegs.INTREQ, 0x8000 + (1u << (int) Interrupt.BLIT), Size.Word);

			//disable blitter busy in DMACON
			custom.Write(0, ChipRegs.DMACON, (1u << 14), Size.Word);

			//blitter done
			interrupt.TriggerInterrupt(Interrupt.BLIT);
		}

		private void Line(uint insaddr)
		{
			Logger.WriteLine($"BLIT LINE! @{insaddr:X8}");

			uint octant = (bltcon1 >> 2) & 7;
			uint sign = (bltcon1 >> 6) & 1;

			//Logger.WriteLine($"octant:{octant} sign:{sign}");
			//if (bltadat != 0x8000) Logger.WriteLine("BLTADAT is not 0x8000");
			//if (bltafwm != 0xffff) Logger.WriteLine("BLTAFWM is not 0xffff");
			//if (bltalwm != 0xffff) Logger.WriteLine("BLTALWM is not 0xffff");
			//if (bltcpt != bltdpt) Logger.WriteLine("BLTCPT != BLTDPT");
			//if (bltcmod != bltdmod) Logger.WriteLine("BLTCMOD != BLTDMOD");

			//Logger.WriteLine($"{bltamod:X8} {(int)bltamod} 4*(dy-dx)");
			//Logger.WriteLine($"{bltbmod:X8} {bltbmod} 4*dy");
			//Logger.WriteLine($"{bltcmod:X8} cmod");
			//Logger.WriteLine($"{bltdmod:X8} {bltdmod} mod");
			//Logger.WriteLine($"{bltapt:X8} {(short)bltapt} (4*dy)-(2*dx)");
			//Logger.WriteLine($"{bltdpt:X8} dest");
			//Logger.WriteLine($"{bltcon0 >> 12} x1 mod 15");
			//Logger.WriteLine($"{Convert.ToString(bltcon0, 2).PadLeft(16, '0')} bltcon0");
			//Logger.WriteLine($"{Convert.ToString(bltcon1, 2).PadLeft(16, '0')} bltcon1");
			//Logger.WriteLine($"{bltsize >> 6:X8} dx+1");
			//Logger.WriteLine($"{bltsize & 0x3f:X8} 2");

			uint length = bltsize >> 6;
			if (length <= 1) return;

			double ty = bltbmod / 4.0;
			double tx = -(int)bltamod / 4.0 + ty;

			tx *= 2.0;
			ty *= 2.0;

			double dx=0, dy=0;
			switch (octant)
			{
				case 0: dx =  ty; dy = -tx; break;
				case 1: dx =  ty; dy =  tx; break;
				case 2: dx = -ty; dy = -tx; break;
				case 3: dx = -ty; dy =  tx; break;
				case 4: dx =  tx; dy = -ty; break;
				case 5: dx = -tx; dy = -ty; break;
				case 6: dx =  tx; dy =  ty; break;
				case 7: dx = -tx; dy =  ty; break;
			}

			dy = -dy;

			double dydl, dxdl;
			dydl = dy / (length-1);
			dxdl = dx / (length-1);

			Logger.WriteLine($"tx,ty {tx,3},{ty,3} dx,dy {dx,3},{dy,3} {Convert.ToString(octant,2).PadLeft(3,'0')}({octant}) {sign} am:{bltamod&0xffff:X4} cm:{bltcmod:X4} dm:{bltdmod:X4} a:{bltapt,5} d:{bltdpt:X8} dydl:{dydl} dxdl:{dxdl}");

			//todo: these are supposed to be the same, why are they not?
			bltdmod = bltcmod;

			uint bltzero = 0;

			//set blitter busy in DMACON
			custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);

			double x = bltcon0 >> 12;
			double y = 0.0;
			uint p;
			while (length-- > 0)
			{
				int x1 = (int) (x+0.5);

				p = memory.Read(0, bltdpt, Size.Word);
				//todo: apply minterm here
				p |= (1u << (x1^15));
				bltzero |= p;
				memory.Write(0, bltdpt, p, Size.Word);

				x += dxdl;
				if (dxdl < 0 && x < 0)
				{
					bltdpt += (uint)(2 * (-1+(int)(x / 16)));
					x = 16 + (x % 16.0);
				}
				else if (dxdl > 0 && x >= 16)
				{
					bltdpt += (uint)(2 * ((int)(x / 16)));
					x = x % 16.0;
				}

				y += dydl;
				if (dydl < 0 && y <= -1.0)
				{
					bltdpt += (uint)(bltdmod * (int)y);
					y = y % 1.0;
				}
				else if (dydl > 0 && y >= 1.0)
				{
					bltdpt += (uint)(bltdmod * (int)y);
					y = y % 1.0;
				}
			}

			//write the BZERO bit in DMACON
			if (bltzero == 0)
				custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 13), Size.Word);
			else
				custom.Write(0, ChipRegs.DMACON, (1u << 13), Size.Word);

			//write blitter interrupt bit to INTREQ
			custom.Write(0, ChipRegs.INTREQ, 0x8000 + (1u << (int)Interrupt.BLIT), Size.Word);

			//disable blitter busy in DMACON
			custom.Write(0, ChipRegs.DMACON, (1u << 14), Size.Word);

			//blitter done
			interrupt.TriggerInterrupt(Interrupt.BLIT);
		}
	}
}
