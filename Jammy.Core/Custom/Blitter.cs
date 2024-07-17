using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class Blitter : IBlitter
	{
		private readonly IChips custom;
		private readonly IDMA memory;
		private readonly IInterrupt interrupt;
		private readonly IOptions<EmulationSettings> settings;
		private readonly ILogger logger;

		public Blitter(IChips custom, IDMA memory, IInterrupt interrupt,
			IOptions<EmulationSettings> settings, ILogger<Blitter> logger)
		{
			this.custom = custom;
			this.memory = memory;
			this.interrupt = interrupt;
			this.settings = settings;
			this.logger = logger;
		}

		public void Logging(bool enabled) { }
		public void Dumping(bool enabled) { }

		public void Emulate(ulong cycles)
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
			//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");
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
			//logger.LogTrace($"W {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.BLTCON0:
					bltcon0 = value;
					break;
				case ChipRegs.BLTCON1:
					bltcon1 = value;
					break;

				case ChipRegs.BLTAFWM:
					bltafwm = value;
					break;
				case ChipRegs.BLTALWM:
					bltalwm = value;
					break;

				case ChipRegs.BLTCPTH:
					bltcpt = ((bltcpt & 0x0000ffff) | ((uint)value << 16));
					break;
				case ChipRegs.BLTCPTL:
					bltcpt = ((bltcpt & 0xffff0000) | (uint)(value & 0xfffe));
					break;
				case ChipRegs.BLTBPTH:
					bltbpt = ((bltbpt & 0x0000ffff) | ((uint)value << 16));
					break;
				case ChipRegs.BLTBPTL:
					bltbpt = ((bltbpt & 0xffff0000) | (uint)(value & 0xfffe));
					break;
				case ChipRegs.BLTAPTH:
					bltapt = ((bltapt & 0x0000ffff) | ((uint)value << 16));
					break;
				case ChipRegs.BLTAPTL:
					bltapt = ((bltapt & 0xffff0000) | (uint)(value & 0xfffe));
					break;
				case ChipRegs.BLTDPTH:
					bltdpt = ((bltdpt & 0x0000ffff) | ((uint)value << 16));
					break;
				case ChipRegs.BLTDPTL:
					bltdpt = ((bltdpt & 0xffff0000) | (uint)(value & 0xfffe));
					break;

				case ChipRegs.BLTSIZE:
					bltsize = value;
					BlitSmall(insaddr);
					break;

				case ChipRegs.BLTCON0L:
					bltcon0 = (bltcon0 & 0x0000ff00) | ((uint)value & 0x000000ff);
					break;

				case ChipRegs.BLTSIZV:
					bltsizv = value;
					break;
				case ChipRegs.BLTSIZH:
					bltsizh = value;
					BlitBig(insaddr);
					break;

				case ChipRegs.BLTCMOD:
					bltcmod = (uint)(short)value & 0xfffffffe;
					break;
				case ChipRegs.BLTBMOD:
					bltbmod = (uint)(short)value & 0xfffffffe;
					break;
				case ChipRegs.BLTAMOD:
					bltamod = (uint)(short)value & 0xfffffffe;
					break;
				case ChipRegs.BLTDMOD:
					bltdmod = (uint)(short)value & 0xfffffffe;
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

		private int counter = 0;
		private string filename;
		
		private void WriteBlitterState()
		{
			var b = new List<string>
			{
				"{",
				$"bltcon0 : {bltcon0},",
				$"bltcon1 : {bltcon1},",
				$"bltapt : {bltapt},",
				$"bltbpt : {bltbpt},",
				$"bltcpt : {bltcpt},",
				$"bltdpt : {bltdpt},",
				$"bltamod : {bltamod},",
				$"bltbmod : {bltbmod},",
				$"bltcmod : {bltcmod},",
				$"bltdmod : {bltdmod},",
				$"bltadat : {bltadat},",
				$"bltbdat : {bltbdat},",
				$"bltcdat : {bltcdat},",
				$"bltddat : {bltddat},",
				$"bltafwm : {bltafwm},",
				$"bltalwm : {bltalwm},",
				$"bltsize : {bltsize},",
				$"bltsizh : {bltsizh},",
				$"bltsizv : {bltsizv},",
				"},"
			};

			if (counter == 0)
				filename = Path.Combine("../../../../blits/", $"blitter-{DateTime.Now:yyyy-MM-dd-HHmmss-fff}.txt");
			
			if (counter < 1000)
				File.AppendAllLines(filename, b);
	
			counter++;
		}

		private void BlitSmall(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			uint width = bltsize & 0x3f;
			uint height = bltsize >> 6;

			if (width == 0) width = 64;
			if (height == 0) height = 1024;

			Blit(width, height);
		}

		private void BlitBig(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			uint width = bltsizh & 0x07ff;
			uint height = bltsizv & 0x7fff;

			if (width == 0) width = 2048;
			if (height == 0) height = 32768;

			Blit(width, height);
		}

		StringBuilder fsb = new StringBuilder();
		private ushort[] bin = new ushort[128];
		private ushort[] bout = new ushort[128];
		private int bcount;
		private struct Writecache
		{
			public uint Address;
			public ushort Value;
		}
		private Writecache writecache;
		private const uint NO_WRITECACHE = 0xffffffff;

		private void DelayedWrite()
		{
			if (writecache.Address != NO_WRITECACHE)
				memory.Write(writecache.Address, DMAPriority.Blitter, writecache.Value, Size.Word);
		}
		private void DelayedWrite(uint address, ushort value)
		{
			DelayedWrite();
			writecache.Address = address;
			writecache.Value = value;
		}
		private void ClearDelayedWrite()
		{
			writecache.Address = NO_WRITECACHE;
		}

		private void Blit(uint width, uint height)
		{
			this.width = width;
			this.height = height;

			Blit1();

			while (Blit2())
				/* spin */
				;

			Blit3();
		}

		private void Blit1()
		{
			ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmacon & (1 << 6)) == 0)
				logger.LogTrace("BLTEN is off!");
			if ((dmacon & (1 << 9)) == 0)
				logger.LogTrace("DMAEN is off!");

			//todo: assumes blitter DMA is enabled

			//set blitter busy in DMACON
			//custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);
			//set BBUSY and BZERO
			custom.WriteDMACON(0x8000 + (1 << 14) + (1 << 13));


			ClearDelayedWrite();

			fci = bltcon1 & (1u << 2);

			ashift = (int)(bltcon0 >> 12);
			bshift = (int)(bltcon1 >> 12);

			bltzero = 0;
			w = h = 0;
		}

		private uint bltabits = 0;
		private uint bltbbits = 0;
		private uint fci;
		private int ashift;
		private int bshift;
		private uint bltzero;

		private uint w, h;
		private uint width, height;

		private bool Blit2()
		{
			uint s_bltadat, s_bltbdat;

			//for (uint h = 0; h < height; h++)

			//for (uint w = 0; w < width; w++)

			if ((bltcon0 & (1u << 11)) != 0)
				bltadat = memory.Read(0, bltapt, Size.Word);

			s_bltadat = bltadat;

			if (w == 0) s_bltadat &= bltafwm;
			if (w == width - 1) s_bltadat &= bltalwm;

			if ((bltcon1 & (1u << 1)) != 0)
			{
				s_bltadat <<= ashift; // 0000000000000111:1111111111111000, say ash = 3
				s_bltadat |= bltabits; // 0000000000000111:1111111111111aaa
				bltabits = s_bltadat >> 16; // 0000000000000000:0000000000000111
				s_bltadat &= 0xffff; // 0000000000000000:1111111111111aaa
			}
			else
			{
				s_bltadat <<= (16 - ashift); // 0001111111111111:1110000000000000
				s_bltadat |= bltabits; // aaa1111111111111:1110000000000000
				bltabits = s_bltadat << 16; // 1110000000000000:0000000000000000
				s_bltadat >>= 16; // 0000000000000000:aaa1111111111111
			}

			if ((bltcon0 & (1u << 10)) != 0)
				bltbdat = memory.Read(0, bltbpt, Size.Word);

			s_bltbdat = bltbdat;

			if ((bltcon1 & (1u << 1)) != 0)
			{
				s_bltbdat <<= bshift;
				s_bltbdat |= bltbbits;
				bltbbits = s_bltbdat >> 16;
				s_bltbdat &= 0xffff;
			}
			else
			{
				s_bltbdat <<= (16 - bshift);
				s_bltbdat |= bltbbits;
				bltbbits = s_bltbdat << 16;
				s_bltbdat >>= 16;
			}

			if ((bltcon0 & (1u << 9)) != 0)
				bltcdat = memory.Read(0, bltcpt, Size.Word);

			bltddat = 0;
			if ((bltcon0 & 0x01) != 0) bltddat |= ~s_bltadat & ~s_bltbdat & ~bltcdat;
			if ((bltcon0 & 0x02) != 0) bltddat |= ~s_bltadat & ~s_bltbdat & bltcdat;
			if ((bltcon0 & 0x04) != 0) bltddat |= ~s_bltadat & s_bltbdat & ~bltcdat;
			if ((bltcon0 & 0x08) != 0) bltddat |= ~s_bltadat & s_bltbdat & bltcdat;
			if ((bltcon0 & 0x10) != 0) bltddat |= s_bltadat & ~s_bltbdat & ~bltcdat;
			if ((bltcon0 & 0x20) != 0) bltddat |= s_bltadat & ~s_bltbdat & bltcdat;
			if ((bltcon0 & 0x40) != 0) bltddat |= s_bltadat & s_bltbdat & ~bltcdat;
			if ((bltcon0 & 0x80) != 0) bltddat |= s_bltadat & s_bltbdat & bltcdat;

			Fill();

			bltzero |= bltddat;

			if (((bltcon0 & (1u << 8)) != 0) && ((bltcon1 & (1u << 7)) == 0))
				DelayedWrite(bltdpt, (ushort)bltddat);

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


			w++;
			if (w != width)
				return true;

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

			//reset carry
			bltcon1 &= ~(1u << 2);
			bltcon1 |= fci;

			w = 0;
			h++;
			return h != height;
		}

		private void Blit3()
		{
			DelayedWrite();

			//write the BZERO bit in DMACON
			//if (bltzero == 0)
			//	custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 13), Size.Word);
			//else
			//	custom.Write(0, ChipRegs.DMACON, (1u << 13), Size.Word);
			//clear BZERO
			if (bltzero != 0)
				custom.WriteDMACON(1 << 13);

			//disable blitter busy in DMACON
			//custom.Write(0, ChipRegs.DMACON, (1u << 14), Size.Word);
			custom.WriteDMACON(1 << 14);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);
		}

		private void Fill()
		{
			uint mode = (bltcon1 >> 3) & 3;
			//descending mode and one of the fill modes must be set
			if (mode ==0 || (bltcon1&(1<<1))==0) return;

			//hack: what to do if both EFE and IFE set? Let's choose EFE
			if (mode == 3) mode = 2;

			//carry in
			bool inside = (bltcon1&(1<<2))!=0;
			if (mode == 1)
			{
				//inclusive fill
				uint obltddat = bltddat;
				for (uint b = 1; b <= 0x8000; b <<= 1)
				{
					bool bit = (b & obltddat) != 0;
					if (!inside && bit)
						inside = true;
					else if (inside && bit)
						inside = false;
					if (inside)
						bltddat |= b;
				}
				//update carry
				bltcon1 &= ~(1u << 2);
				if (inside) bltcon1 |= 1 << 2;
			}
			else if (mode == 2)
			{
				//exclusive fill
				uint obltddat = bltddat;
				for (uint b = 1; b <= 0x8000; b <<= 1)
				{
					bool bit = (b & obltddat) != 0;
					if (!inside && bit)
					{
						inside = true;
					}
					else if (inside && bit)
					{
						inside = false;
						bltddat &= ~b;
						continue;
					}

					if (inside)
						bltddat |= b;
				}
				//update carry
				bltcon1 &= ~(1u << 2);
				if (inside) bltcon1 |= 1 << 2;

			} 
		}

		private void Line(uint _)
		{
			Line1();

			while (Line2()) 
				/* spin */;

			Line3();
		}

		private uint length;
		private bool writeBit;
		private uint bltbdatror;
		private int x0, x1;
		private int y1;
		private int dm;
		private bool sing;
		private int sx, sy;
		private int dx, dy;
		private void Line1()
		{
			uint octant = (bltcon1 >> 2) & 7;
			sing = (bltcon1 & (1 << 1)) != 0;

			length = bltsize >> 6;
			if (length == 0)
			{
				interrupt.AssertInterrupt(Interrupt.BLIT);
				return;
			}

			dy = (int)(bltbmod / 2);
			dx = -(int)bltamod / 2 + dy;

			if (octant < 4) (dx, dy) = (dy, dx);

			sx = 1;
			if (octant == 2 || octant == 3 || octant == 5 || octant == 7) sx = -1;
			sy = 1;
			if (octant == 1 || octant == 3 || octant == 6 || octant == 7) sy = -1;

			bltzero = 0;

			//set blitter busy in DMACON
			//custom.Write(insaddr, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);
			//set BBUSY and BZERO
			custom.WriteDMACON(0x8000 + (1 << 14) + (1 << 13));

			writeBit = true;

			x0 = (int)(bltcon0 >> 12);
			int ror = (int)(bltcon1 >> 12);

			bltbdatror = (bltbdat << ror) | (bltbdat >> (16 - ror));

			dm = Math.Max(dx, dy);
			x1 = dm / 2;
			y1 = dm / 2;
		}

		private bool Line2()
		{
			{
				if ((bltcon0 & (1u << 9)) != 0)
					bltcdat = memory.Read(0, bltcpt, Size.Word);

				bltadat = 0x8000u >> x0;

				bltddat = 0;
				if ((bltcon0 & 0x01) != 0) bltddat |= ~bltadat & ~bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x02) != 0) bltddat |= ~bltadat & ~bltbdatror & bltcdat;
				if ((bltcon0 & 0x04) != 0) bltddat |= ~bltadat & bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x08) != 0) bltddat |= ~bltadat & bltbdatror & bltcdat;
				if ((bltcon0 & 0x10) != 0) bltddat |= bltadat & ~bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x20) != 0) bltddat |= bltadat & ~bltbdatror & bltcdat;
				if ((bltcon0 & 0x40) != 0) bltddat |= bltadat & bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x80) != 0) bltddat |= bltadat & bltbdatror & bltcdat;

				//oddly, USEC must be checked, not USED
				if ((bltcon0 & (1u << 9)) != 0 && (bltcon1 & (1u << 7)) == 0)
				{
					if (writeBit)
					{
						memory.Write(0, bltdpt, bltddat, Size.Word);
						if (sing) writeBit = false;
					}
				}

				bltzero |= bltddat;

				x1 -= dx;
				if (x1 < 0)
				{
					x1 += dm;
					x0 += sx;
					if (x0 >= 16)
					{
						x0 = 0;
						bltcpt += 2;
					}

					if (x0 < 0)
					{
						x0 = 15;
						bltcpt -= 2;
					}
				}

				y1 -= dy;
				if (y1 < 0)
				{
					bltcpt += (uint)(bltcmod * sy);
					y1 += dm;
					writeBit = true;
				}

				bltcpt &= 0xfffffffe;

				//first write goes to bltdpt, thereafter bltdpt = bltcpt
				bltdpt = bltcpt;
			}
			length--;
			return length != 0;
		}

		private void Line3()
		{
			//clear BZERO
			if (bltzero != 0)
				custom.WriteDMACON(1 << 13);

			//disable blitter busy in DMACON
			//custom.Write(0, ChipRegs.DMACON, (1u << 14), Size.Word);
			custom.WriteDMACON(1 << 14);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);
		}
	}
}