using System;
using System.Diagnostics;
using System.Threading;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class Blitter : IEmulate
	{
		private readonly Chips custom;
		private readonly Memory memory;
		private readonly Memory musashiMemory;
		private readonly Interrupt interrupt;

		public Blitter(Chips custom, Memory memory, Memory musashiMemory, Interrupt interrupt)
		{
			this.custom = custom;
			this.memory = memory;
			this.musashiMemory = musashiMemory;
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
			Logger.WriteLine($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");
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
			Logger.WriteLine($"W {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.BLTCON0:
					bltcon0 = value;
					uint lf = (uint) value & 0xff;
					uint ash = (uint) value >> 12;
					uint use = (uint) (value >> 8) & 0xf;
					Logger.WriteLine($"minterm:{lf:X2} ash:{ash} use:{Convert.ToString(use, 2).PadLeft(4, '0')}");
					break;

				case ChipRegs.BLTCON1:
					bltcon1 = value;
					uint bsh = (uint) value >> 12;
					uint doff = (uint) (value >> 7) & 1;
					uint efe = (uint) (value >> 4) & 1;
					uint ife = (uint) (value >> 3) & 1;
					uint fci = (uint) (value >> 2) & 1;
					uint desc = (uint) (value >> 1) & 1;
					uint line = (uint) value & 1;
					Logger.WriteLine($"bsh:{bsh} doff:{doff} efe:{efe} ife:{ife} fci:{fci} desc:{desc} line:{line}");
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
					BlitSmall();
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
					BlitBig();
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
		}

		private void BlitSmall()
		{
			if ((bltcon1 & 1) != 0)
			{
				Line();
				return;
			}

			uint width = bltsize & 0x3f;
			uint height = bltsize >> 6;
			Logger.WriteLine($"BLIT! size:{width}x{height}");
			Blit(width, height);
		}

		private void BlitBig()
		{
			if ((bltcon1 & 1) != 0)
			{
				Line();
				return;
			}

			Logger.WriteLine($"BLIT! size:{bltsizh}x{bltsizv}");
			Blit(bltsizh, bltsizv);
		}

		private void Blit(uint width, uint height)
		{
			//todo: assume blitter DMA is enabled

			//hacky alignment fudge

			if ((bltapt & 1) != 0) Logger.WriteLine($"Channel A is odd {bltapt:X8}");
			if ((bltbpt & 1) != 0) Logger.WriteLine($"Channel B is odd {bltbpt:X8}");
			if ((bltcpt & 1) != 0) Logger.WriteLine($"Channel C is odd {bltcpt:X8}");
			if ((bltdpt & 1) != 0) Logger.WriteLine($"Channel D is odd {bltdpt:X8}");

			uint s_bltapt = bltapt & ~1u;
			uint s_bltbpt = bltbpt & ~1u;
			uint s_bltcpt = bltcpt & ~1u;
			uint s_bltdpt = bltdpt & ~1u;

			int ashift = (int) (bltcon0 >> 12);
			int bshift = (int) (bltcon1 >> 12);

			uint bltzero = 0;

			bltadat = bltbdat = bltcdat = 0;

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
						bltadat = memory.read16(s_bltapt);
						if (w == 0) bltadat &= bltafwm;
						else if (w == width - 1) bltadat &= bltalwm;
						bltadat <<= (16 - ashift);
						bltadat |= bltabits;
						bltabits = bltadat << 16;
						bltadat >>= 16;
					}

					if ((bltcon0 & (1u << 10)) != 0)
					{
						bltbdat = memory.read16(s_bltbpt);
						bltbdat <<= (16 - bshift);
						bltbdat |= bltbbits;
						bltbbits = bltbdat << 16;
						bltbdat >>= 16;
					}

					if ((bltcon0 & (1u << 9)) != 0)
					{
						bltcdat = memory.read16(s_bltcpt);
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
					{
						memory.write16(s_bltdpt, (ushort) bltddat);
						musashiMemory.write16(s_bltdpt, (ushort) bltddat);
					}

					//Logger.Write($"{Convert.ToString(bltddat,2).PadLeft(16,'0')}");

					s_bltapt += 2;
					s_bltbpt += 2;
					s_bltcpt += 2;
					s_bltdpt += 2;
				}
				//Logger.WriteLine("");

				s_bltapt += bltamod;
				s_bltbpt += bltbmod;
				s_bltcpt += bltcmod;
				s_bltdpt += bltdmod;
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

		private void Line()
		{
			Logger.WriteLine($"BLIT LINE!");

			uint octant = (bltcon1 >> 2) & 3;
			uint sign = (bltcon1 >> 1) & 1;
			Logger.WriteLine($"octant:{octant} sign:{sign}");
			if (bltadat != 0x8000) Logger.WriteLine("BLTADAT is not 0x8000");
			if (bltafwm != 0xffff) Logger.WriteLine("BLTAFWM is not 0xffff");
			if (bltalwm != 0xffff) Logger.WriteLine("BLTALWM is not 0xffff");
			if (bltcpt != bltdpt) Logger.WriteLine("BLTCPT != BLTDPT");
			if (bltcmod != bltdmod) Logger.WriteLine("BLTCMOD != BLTDMOD");

			Logger.WriteLine($"{bltamod:X8} {(int)bltamod} 4*(dy-dx)");
			Logger.WriteLine($"{bltbmod:X8} {bltbmod} 4*dy");
			Logger.WriteLine($"{bltcmod:X8} cmod");
			Logger.WriteLine($"{bltdmod:X8} {bltdmod} mod");
			Logger.WriteLine($"{bltapt:X8} {(short)bltapt} (4*dy)-(2*dx)");
			Logger.WriteLine($"{bltdpt:X8} dest");
			Logger.WriteLine($"{bltcon0 >> 12} x1 mod 15");
			Logger.WriteLine($"{Convert.ToString(bltcon0, 2).PadLeft(16, '0')} bltcon0");
			Logger.WriteLine($"{Convert.ToString(bltcon1, 2).PadLeft(16, '0')} bltcon1");
			Logger.WriteLine($"{bltsize >> 6:X8} dx+1");
			Logger.WriteLine($"{bltsize & 0x3f:X8} 2");

			if ((bltapt & 1) != 0) Logger.WriteLine($"Channel A is odd {bltapt:X8}");
			if ((bltcpt & 1) != 0) Logger.WriteLine($"Channel C is odd {bltcpt:X8}");
			if ((bltdpt & 1) != 0) Logger.WriteLine($"Channel D is odd {bltdpt:X8}");

			uint s_bltapt = bltapt & ~1u;
			uint s_bltcpt = bltcpt & ~1u;
			uint s_bltdpt = bltdpt & ~1u;

			uint length = bltsize >> 6;
			//while (length-- >= 0)
			//{
			//	Thread.Sleep(3000);
			//	memory.Write(0, s_bltdpt, (ushort) (1u << (int) (bltcon0 >> 12)), Size.Word);
			//}
			//Thread.Sleep(3000);

			bltdmod = bltcmod;

			double dy = bltbmod / 4.0;
			double dx = -(int)bltamod / 4.0 + dy;
			double dydx;
			if (dx != 0.0)
				dydx = dy / dx;
			else
				dydx = 1.0;

			int xinc=1;

			//if (octant == 3 || octant == 7 || octant == 5 || octant == 2)
			//	xinc = -xinc;
			//if (octant == 3 || octant == 7 || octant == 6 || octant == 1)
			//	dydx = -dydx;

			if (octant == 2 || octant == 3 || octant == 4 || octant == 5)
				xinc = -xinc;
			if (octant == 0 || octant == 1 || octant == 2 || octant == 3)
				dydx = -dydx;

			Logger.WriteLine($"dx,dy {dx},{dy} dydx {dydx}");

			int x1 = (int)(bltcon0 >> 12);
			double y1 = 0.0;
			uint p;
			while (length-- > 0)
			{
				p = memory.Read(0, s_bltdpt, Size.Word);
				memory.Write(0, s_bltdpt, (ushort)(p|(1<<(x1^15))), Size.Word);
				x1+=xinc;
				if (x1 == 16)
				{
					x1 = 0;
					s_bltdpt += 2;
				}
				if (x1 == -1)
				{
					x1 = 15;
					s_bltdpt -= 2;
				}
				y1 += dydx;
				while (y1 > 1.0)
				{
					s_bltdpt += bltdmod;

					p = memory.Read(0, s_bltdpt, Size.Word);
					memory.Write(0, s_bltdpt, (ushort)(p | (1 << (x1 ^ 15))), Size.Word);

					y1 -= 1.0;
				}
				while (y1 < -1.0)
				{
					s_bltdpt -= bltdmod;

					p = memory.Read(0, s_bltdpt, Size.Word);
					memory.Write(0, s_bltdpt, (ushort)(p | (1 << (x1 ^ 15))), Size.Word);

					y1 += 1.0;
				}
			}
		}
	}
}
