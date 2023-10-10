using System;
using System.Collections.Generic;
using System.IO;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class Blitter : IBlitter
	{
		private readonly IChips custom;
		private readonly IMemoryMappedDevice memory;
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;

		public Blitter(IChips custom, IChipRAM memory, IInterrupt interrupt, ILogger<Blitter> logger)
		{
			this.custom = custom;
			this.memory = memory;
			this.interrupt = interrupt;
			this.logger = logger;
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
				filename = Path.Combine("../../../../", $"blitter-{DateTime.Now:yyyy-MM-dd-HHmmss}.txt");
			
			if (counter < 1000)
				File.AppendAllLines(filename, b);
	
			counter++;
		}

		private int mode = 0;
		public void SetLineMode(int mode)
		{
			this.mode = mode;
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

			Blit(width, height);
		}

		private void BlitBig(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			Blit(bltsizh, bltsizv);
		}

		private void Blit(uint width, uint height)
		{
			ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmacon & (1 << 6)) == 0)
				logger.LogTrace("BLTEN is off!");
			if ((dmacon & (1 << 9)) == 0)
				logger.LogTrace("DMAEN is off!");

			//todo: assumes blitter DMA is enabled

			//logger.LogTrace($"BLIT! {width}x{height} = {width * 16}bits x {height} = {width * 16 * height} bits = {width * height * 2} bytes");

			//logger.LogTrace($"->{bltapt:X6} %{(int)bltamod,9} >> {bltcon0 >> 12,2} {(((bltcon0 >> 11) & 1) != 0 ? "on" : "off")}");
			//logger.LogTrace($"->{bltbpt:X6} %{(int)bltbmod,9} >> {bltcon1 >> 12,2} {(((bltcon0 >> 10) & 1) != 0 ? "on" : "off")}");
			//logger.LogTrace($"->{bltcpt:X6} %{(int)bltcmod,9} >> -- {(((bltcon0 >> 9) & 1) != 0 ? "on" : "off")}");
			//logger.LogTrace($"->{bltdpt:X6} %{(int)bltdmod,9} >> -- {(((bltcon0 >> 8) & 1) != 0 ? "on" : "off")}");
			//logger.LogTrace($"M {Convert.ToString(bltafwm, 2).PadLeft(16, '0')} {Convert.ToString(bltalwm, 2).PadLeft(16, '0')}");
			//logger.LogTrace($"cookie: {bltcon0 & 0xff:X2} {((bltcon1 & 2) != 0 ? "descending" : "ascending")}");
			//logger.LogTrace("ABC");
			//if ((bltcon0 & 0x01) != 0) logger.LogTrace("000");
			//if ((bltcon0 & 0x02) != 0) logger.LogTrace("001");
			//if ((bltcon0 & 0x04) != 0) logger.LogTrace("010");
			//if ((bltcon0 & 0x08) != 0) logger.LogTrace("011");
			//if ((bltcon0 & 0x10) != 0) logger.LogTrace("100");
			//if ((bltcon0 & 0x20) != 0) logger.LogTrace("101");
			//if ((bltcon0 & 0x40) != 0) logger.LogTrace("110");
			//if ((bltcon0 & 0x80) != 0) logger.LogTrace("111");
			//if ((bltcon1 & (3 << 3)) != 0 && (bltcon1 & (1u << 1)) != 0)
			//	logger.LogTrace($"Fill EFE:{(bltcon1 >> 4) & 1} IFE:{(bltcon1 >> 3) & 1} FCI:{(bltcon1 >> 2) & 1}");

			bool dont_blit = false;
			uint mode = (bltcon1 >> 3) & 3;
			//if (mode != 0) dont_blit = true;
			//these ones are weird
			//if ((bltcon0 & 0xff) == 0x1a && (bltcon1 & 2) != 0) //00,1a,2a,3a,ca,ea
			//if (width == 20 && height == 200 && (bltcon1 & 2) != 0)
			//if ((bltcon0 >> 12) != 0 || (bltcon1 >> 12) != 0)
			//if (width == 1 && height>1)
			//{
			//	dont_blit = true;
			//	logger.LogTrace("********* NOT DRAWN!");
			//}

			int ashift = (int)(bltcon0 >> 12);
			int bshift = (int)(bltcon1 >> 12);

			uint bltzero = 0;
			uint s_bltadat, s_bltbdat;

			//set blitter busy in DMACON
			custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);

			for (uint h = 0; h < height; h++)
			{
				uint bltabits = 0;
				uint bltbbits = 0;

				for (uint w = 0; w < width; w++)
				{
					if ((bltcon0 & (1u << 11)) != 0)
						bltadat = memory.Read(0, bltapt, Size.Word);

					s_bltadat = bltadat;

					if (w == 0) s_bltadat &= bltafwm;
					if (w == width - 1) s_bltadat &= bltalwm;

					if ((bltcon1 & (1u << 1)) != 0)
					{
						s_bltadat <<= ashift;                 // 0000000000000111:1111111111111000, say ash = 3
						s_bltadat |= bltabits;                // 0000000000000111:1111111111111aaa
						bltabits = s_bltadat >> 16;           // 0000000000000000:0000000000000111
						s_bltadat &= 0xffff;                  // 0000000000000000:1111111111111aaa
					}
					else
					{
						s_bltadat <<= (16 - ashift);          // 0001111111111111:1110000000000000
						s_bltadat |= bltabits;                // aaa1111111111111:1110000000000000
						bltabits = s_bltadat << 16;           // 1110000000000000:0000000000000000
						s_bltadat >>= 16;                     // 0000000000000000:aaa1111111111111
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
					if ((bltcon0 & 0x02) != 0) bltddat |= ~s_bltadat & ~s_bltbdat &  bltcdat;
					if ((bltcon0 & 0x04) != 0) bltddat |= ~s_bltadat &  s_bltbdat & ~bltcdat;
					if ((bltcon0 & 0x08) != 0) bltddat |= ~s_bltadat &  s_bltbdat &  bltcdat;
					if ((bltcon0 & 0x10) != 0) bltddat |=  s_bltadat & ~s_bltbdat & ~bltcdat;
					if ((bltcon0 & 0x20) != 0) bltddat |=  s_bltadat & ~s_bltbdat &  bltcdat;
					if ((bltcon0 & 0x40) != 0) bltddat |=  s_bltadat &  s_bltbdat & ~bltcdat;
					if ((bltcon0 & 0x80) != 0) bltddat |=  s_bltadat &  s_bltbdat &  bltcdat;

					Fill();

					bltzero |= bltddat;

					bltdpt &= 0xfffffffe;

					if (((bltcon0 & (1u << 8)) != 0) && ((bltcon1 & (1u << 7)) == 0) && !dont_blit)
						memory.Write(0, bltdpt, (ushort)bltddat, Size.Word);

					if ((bltcon1 & (1u << 1)) != 0)
					{
						if ((bltcon0 & (1u << 11)) != 0) bltapt -= 2;
						if ((bltcon0 & (1u << 10)) != 0) bltbpt -= 2;
						if ((bltcon0 & (1u <<  9)) != 0) bltcpt -= 2;
						if ((bltcon0 & (1u <<  8)) != 0) bltdpt -= 2;
					}
					else
					{
						if ((bltcon0 & (1u << 11)) != 0) bltapt += 2;
						if ((bltcon0 & (1u << 10)) != 0) bltbpt += 2;
						if ((bltcon0 & (1u <<  9)) != 0) bltcpt += 2;
						if ((bltcon0 & (1u <<  8)) != 0) bltdpt += 2;
					}
				}
				if ((bltcon1 & (1u << 1)) != 0)
				{
					if ((bltcon0 & (1u << 11)) != 0) bltapt -= bltamod;
					if ((bltcon0 & (1u << 10)) != 0) bltbpt -= bltbmod;
					if ((bltcon0 & (1u <<  9)) != 0) bltcpt -= bltcmod;
					if ((bltcon0 & (1u <<  8)) != 0) bltdpt -= bltdmod;
				}
				else
				{
					if ((bltcon0 & (1u << 11)) != 0) bltapt += bltamod;
					if ((bltcon0 & (1u << 10)) != 0) bltbpt += bltbmod;
					if ((bltcon0 & (1u <<  9)) != 0) bltcpt += bltcmod;
					if ((bltcon0 & (1u <<  8)) != 0) bltdpt += bltdmod;
				}
			}

			//write the BZERO bit in DMACON
			if (bltzero == 0)
				custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 13), Size.Word);
			else
				custom.Write(0, ChipRegs.DMACON, (1u << 13), Size.Word);

			//disable blitter busy in DMACON
			custom.Write(0, ChipRegs.DMACON, (1u << 14), Size.Word);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);
		}

		private void Fill()
		{
			uint mode = (bltcon1 >> 3) & 3;
			//descending mode and one of the fill modes must be set
			if (mode ==0 || (bltcon1&(1<<1))==0) return;

			ushort dbg_bltddat = (ushort)bltddat;
			ushort dbg_bltcon1 = (ushort)bltcon1;

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

		private void Line(uint insaddr)
		{
			uint octant = (bltcon1 >> 2) & 7;
			bool sing = (bltcon1 & (1 << 1)) != 0;

			uint length = bltsize >> 6;
			if (length == 0)
			{
				interrupt.AssertInterrupt(Interrupt.BLIT);
				return;
			}

			int dy = (int)(bltbmod / 2);
			int dx = -(int)bltamod / 2 + dy;

			if (octant < 4) (dx, dy) = (dy, dx);

			int sx = 1;
			if (octant == 2 || octant == 3 || octant == 5 || octant == 7) sx = -1;
			int sy = 1;
			if (octant == 1 || octant == 3 || octant == 6 || octant == 7) sy = -1;

			uint bltzero = 0;

			//set blitter busy in DMACON
			custom.Write(insaddr, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);

			bool writeBit = true;

			int x0 = (int)(bltcon0 >> 12);
			int ror = (int)(bltcon1 >> 12);

			uint bltbdatror = (bltbdat << ror) | (bltbdat>>(16-ror));

			int dm = (int)Math.Max(dx, dy);
			int x1 = dm / 2; 
			int y1 = dm / 2;
			
			while (length-- > 0)
			{
				if ((bltcon0 & (1u << 9)) != 0)
					bltcdat = memory.Read(insaddr, bltcpt, Size.Word);

				bltadat = 0x8000u >> x0;

				bltddat = 0;
				if ((bltcon0 & 0x01) != 0) bltddat |= ~bltadat & ~bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x02) != 0) bltddat |= ~bltadat & ~bltbdatror &  bltcdat;
				if ((bltcon0 & 0x04) != 0) bltddat |= ~bltadat &  bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x08) != 0) bltddat |= ~bltadat &  bltbdatror &  bltcdat;
				if ((bltcon0 & 0x10) != 0) bltddat |=  bltadat & ~bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x20) != 0) bltddat |=  bltadat & ~bltbdatror &  bltcdat;
				if ((bltcon0 & 0x40) != 0) bltddat |=  bltadat &  bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x80) != 0) bltddat |=  bltadat &  bltbdatror &  bltcdat;

				//oddly, USEC must be checked, not USED
				if ((bltcon0 & (1u << 9)) != 0 && (bltcon1 & (1u << 7)) == 0)
				{
					if (writeBit)
					{
						memory.Write(insaddr, bltdpt, bltddat, Size.Word);
						if (sing) writeBit = false;
					}
				}

				bltzero |= bltddat;

				x1 -= dx;
				if (x1 < 0)
				{
					x1 += dm;
					x0 += sx;
					if (x0 >= 16) { x0 = 0; bltcpt += 2; }
					if (x0 < 0)   { x0 =15; bltcpt -= 2; }
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

			//write the BZERO bit in DMACON
			if (bltzero == 0)
				custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 13), Size.Word);
			else
				custom.Write(0, ChipRegs.DMACON, (1u << 13), Size.Word);

			//disable blitter busy in DMACON
			custom.Write(0, ChipRegs.DMACON, (1u << 14), Size.Word);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);
		}
	}
}