using System;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class Blitter : IBlitter
	{
		private readonly IChips custom;
		private readonly IMemory memory;
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;

		public Blitter(IChips custom, IMemory memory, IInterrupt interrupt, ILogger<Blitter> logger)
		{
			this.custom = custom;
			this.memory = memory;
			this.interrupt = interrupt;
			this.logger = logger;
		}

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
					bltcpt = ((bltcpt & 0x0000ffff) | ((uint)value << 16)) & ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.BLTCPTL:
					bltcpt = ((bltcpt & 0xffff0000) | value) & ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.BLTBPTH:
					bltbpt = ((bltbpt & 0x0000ffff) | ((uint)value << 16)) & ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.BLTBPTL:
					bltbpt = ((bltbpt & 0xffff0000) | value) & ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.BLTAPTH:
					bltapt = ((bltapt & 0x0000ffff) | ((uint)value << 16)) & ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.BLTAPTL:
					bltapt = ((bltapt & 0xffff0000) | value) & ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.BLTDPTH:
					bltdpt = ((bltdpt & 0x0000ffff) | ((uint)value << 16)) & ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.BLTDPTL:
					bltdpt = ((bltdpt & 0xffff0000) | value) & ChipRegs.ChipAddressMask;
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

		private void BlitSmall(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
			{
				Line2(insaddr);
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
				Line2(insaddr);
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

					//todo: apply fill here

					bltzero |= bltddat;

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

		private void Line(uint insaddr)
		{
			//logger.LogTrace($"BLIT LINE! @{insaddr:X8}");

			uint octant = (bltcon1 >> 2) & 7;
			uint sign = (bltcon1 >> 6) & 1;

			//logger.LogTrace($"octant:{octant} sign:{sign}");
			//if (bltadat != 0x8000) logger.LogTrace("BLTADAT is not 0x8000");
			//if (bltafwm != 0xffff) logger.LogTrace("BLTAFWM is not 0xffff");
			//if (bltalwm != 0xffff) logger.LogTrace("BLTALWM is not 0xffff");
			//if (bltcpt != bltdpt) logger.LogTrace("BLTCPT != BLTDPT");
			//if (bltcmod != bltdmod) logger.LogTrace("BLTCMOD != BLTDMOD");

			//logger.LogTrace($"{bltamod:X8} {(int)bltamod} 4*(dy-dx)");
			//logger.LogTrace($"{bltbmod:X8} {bltbmod} 4*dy");
			//logger.LogTrace($"{bltcmod:X8} cmod");
			//logger.LogTrace($"{bltdmod:X8} {bltdmod} mod");
			//logger.LogTrace($"{bltapt:X8} {(short)bltapt} (4*dy)-(2*dx)");
			//logger.LogTrace($"{bltdpt:X8} dest");
			//logger.LogTrace($"{bltcon0 >> 12} x1 mod 15");
			//logger.LogTrace($"{Convert.ToString(bltcon0, 2).PadLeft(16, '0')} bltcon0");
			//logger.LogTrace($"{Convert.ToString(bltcon1, 2).PadLeft(16, '0')} bltcon1");
			//logger.LogTrace($"{bltsize >> 6:X8} dx+1");
			//logger.LogTrace($"{bltsize & 0x3f:X8} 2");

			uint length = bltsize >> 6;
			if (length <= 1)
			{
				interrupt.AssertInterrupt(Interrupt.BLIT);
				return;
			}

			double ty = bltbmod / 4.0;
			double tx = -(int)bltamod / 4.0 + ty;

			tx *= 2.0;
			ty *= 2.0;

			double dx = 0, dy = 0;
			switch (octant)
			{
				case 0:
					dx = ty;
					dy = tx;
					break;
				case 1:
					dx = ty;
					dy = -tx;
					break;
				case 2:
					dx = -ty;
					dy = tx;
					break;
				case 3:
					dx = -ty;
					dy = -tx;
					break;
				case 4:
					dx = tx;
					dy = ty;
					break;
				case 5:
					dx = -tx;
					dy = ty;
					break;
				case 6:
					dx = tx;
					dy = -ty;
					break;
				case 7:
					dx = -tx;
					dy = -ty;
					break;
			}

			double dydl, dxdl;
			dydl = dy / (length - 1);
			dxdl = dx / (length - 1);

			//logger.LogTrace($"tx,ty {tx,3},{ty,3} dx,dy {dx,3},{dy,3} {Convert.ToString(octant,2).PadLeft(3,'0')}({octant}) {sign} am:{bltamod&0xffff:X4} cm:{bltcmod:X4} dm:{bltdmod:X4} a:{bltapt,5} d:{bltdpt:X8} dydl:{dydl} dxdl:{dxdl}");

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
			//{
			//	logger.LogTrace($"Fill EFE:{(bltcon1 >> 4) & 1} IFE:{(bltcon1 >> 3) & 1} FCI:{(bltcon1 >> 2) & 1}");
			//	return;
			//}

			//todo: these are supposed to be the same, why are they not?
			bltdmod = bltcmod;

			uint bltzero = 0;

			//set blitter busy in DMACON
			custom.Write(insaddr, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);

			double x = bltcon0 >> 12;
			double y = 0.0;
			while (length-- > 0)
			{
				int x1 = (int)(x + 0.5);

				bltcpt = bltdpt;

				if ((bltcon0 & (1u << 9)) != 0)
					bltcdat = memory.Read(insaddr, bltcpt, Size.Word);

				bltadat = (1u << (x1 ^ 15));

				bltddat = 0;
				if ((bltcon0 & 0x01) != 0) bltddat |= ~bltadat & ~bltbdat & ~bltcdat;
				if ((bltcon0 & 0x02) != 0) bltddat |= ~bltadat & ~bltbdat & bltcdat;
				if ((bltcon0 & 0x04) != 0) bltddat |= ~bltadat & bltbdat & ~bltcdat;
				if ((bltcon0 & 0x08) != 0) bltddat |= ~bltadat & bltbdat & bltcdat;
				if ((bltcon0 & 0x10) != 0) bltddat |= bltadat & ~bltbdat & ~bltcdat;
				if ((bltcon0 & 0x20) != 0) bltddat |= bltadat & ~bltbdat & bltcdat;
				if ((bltcon0 & 0x40) != 0) bltddat |= bltadat & bltbdat & ~bltcdat;
				if ((bltcon0 & 0x80) != 0) bltddat |= bltadat & bltbdat & bltcdat;

				if (((bltcon0 & (1u << 8)) != 0) && ((bltcon1 & (1u << 7)) == 0))
					memory.Write(insaddr, bltdpt, bltddat, Size.Word);

				bltzero |= bltddat;

				x += dxdl;
				if (dxdl < 0 && x < 0)
				{
					bltdpt += (uint)(2 * (-1 + (int)(x / 16)));
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

			bltcpt = bltdpt;

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

		private void Line2(uint insaddr)
		{
			var b = new BlitterLine.Blitter();

			b.bltadat = (ushort)bltadat;
			b.bltbdat = (ushort)bltbdat;
			b.bltbdat_original = bltbdat;
			b.bltcdat = (ushort)bltcdat;

			b.bltcon = (bltcon0<<16)|bltcon1;
			b.bltamod = (ushort)bltamod;
			b.bltbmod = (ushort)bltbmod;
			b.bltcmod = (ushort)bltcmod;

			b.bltafwm = (ushort)bltafwm;

			b.bltapt = bltapt;
			b.bltcpt = bltcpt;
			b.bltdpt = bltdpt;

			b.height = bltsize >> 6;

			b.a_shift_asc = bltcon0 >> 12;
			b.a_shift_desc = 16-b.a_shift_asc;

			b.b_shift_asc = bltcon1 >> 12;

			b.bltzero = 0;

			BlitterLine.blitterLineMode(b, memory);

			bltadat = b.bltadat;
			//bltbdat = b.bltbdat_original;
			bltcdat = b.bltcdat;

			bltcon1 = b.bltcon;
			bltcon0 = b.bltcon >> 16;
			bltamod = b.bltamod;
			bltbmod = b.bltbmod;
			bltcmod = b.bltcmod;
			bltafwm = b.bltafwm;

			bltapt = b.bltapt;
			bltcpt = b.bltcpt;
			bltdpt = b.bltdpt;

			//write the BZERO bit in DMACON
			if (b.bltzero == 0)
				custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 13), Size.Word);
			else
				custom.Write(0, ChipRegs.DMACON, (1u << 13), Size.Word);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);
		}
	}

	public static class BlitterLine
	{
		public class Blitter
		{
			public ushort bltadat;
			public uint bltbdat_original;
			public ushort bltbdat; //output
			public ushort bltcdat;

			public uint bltcon; //output
			public ushort bltamod;
			public ushort bltbmod;
			public ushort bltcmod;
			public ushort bltafwm;

			public uint bltapt;
			public uint bltcpt;
			public uint bltdpt;

			public uint height;

			public uint a_shift_asc;
			public uint a_shift_desc;

			public uint b_shift_asc;

			public uint bltzero; //output
		}

		private static uint chipsetMaskPtr(uint c)
		{
			return c & ChipRegs.ChipAddressMask;
		}

		private static void blitterLineIncreaseX(ref uint a_shift, ref uint cpt)
		{
			if (a_shift < 15) a_shift++;
			else
			{
				a_shift = 0;
				cpt = chipsetMaskPtr(cpt + 2);
			}
		}

		private static void blitterLineDecreaseX(ref uint a_shift, ref uint cpt)
		{
			{
				if (a_shift == 0)
				{
					a_shift = 16;
					cpt = chipsetMaskPtr(cpt - 2);
				}

				a_shift--;
			}
		}

		private static void blitterLineIncreaseY(ref uint cpt, ushort cmod)
		{
			cpt = chipsetMaskPtr(cpt + cmod);
		}

		private static void blitterLineDecreaseY(ref uint cpt, ushort cmod)
		{
			cpt = chipsetMaskPtr(cpt - cmod);
		}

		private static ushort chipmemReadWord(uint address)
		{
			return (ushort)memory.Read(0, address, Size.Word);
		}

		private static void chipmemWriteWord(ushort value, uint address)
		{
			memory.Write(0, address, value, Size.Word);
		}

		private static void memoryWriteWord(ushort value, uint address)
		{
			memory.Write(0, address, value, Size.Word);
		}

		private static IMemoryMappedDevice memory;

		public static void blitterLineMode(Blitter blitter, IMemoryMappedDevice memory)
		{
			BlitterLine.memory = memory;

			uint bltadat_local;
			uint bltbdat_local = 0;
			uint bltcdat_local = blitter.bltcdat;
			uint bltddat_local;
			ushort mask = (ushort)((blitter.bltbdat_original >> (int)blitter.b_shift_asc) | (blitter.bltbdat_original << (int)(16 - blitter.b_shift_asc)));
			bool a_enabled = (blitter.bltcon & 0x08000000) != 0;
			bool c_enabled = (blitter.bltcon & 0x02000000) != 0;

			bool decision_is_signed = (((blitter.bltcon >> 6) & 1) == 1);
			int decision_variable = (int)(short)blitter.bltapt;

			// Quirk: Set decision increases to 0 if a is disabled, ensures bltapt remains unchanged
			short decision_inc_signed = (a_enabled) ? ((short)blitter.bltbmod) : (short)0;
			short decision_inc_unsigned = (a_enabled) ? ((short)blitter.bltamod) : (short)0;

			uint bltcpt_local = blitter.bltcpt;
			uint bltdpt_local = blitter.bltdpt;
			uint blit_a_shift_local = blitter.a_shift_asc;
			uint bltzero_local = 0;
			uint i;

			uint sulsudaul = (uint)((blitter.bltcon >> 2) & 0x7);
			bool x_independent = (sulsudaul & 4) != 0;
			bool x_inc = ((!x_independent) && !((sulsudaul & 2) != 0)) || (x_independent && !((sulsudaul & 1) != 0));
			bool y_inc = ((!x_independent) && !((sulsudaul & 1) != 0)) || (x_independent && !((sulsudaul & 2) != 0));
			bool single_dot = false;
			byte minterm = (byte)(blitter.bltcon >> 16);

			for (i = 0; i < blitter.height; ++i)
			{
				// Read C-data from memory if the C-channel is enabled
				if (c_enabled)
				{
					bltcdat_local = chipmemReadWord(bltcpt_local);
				}

				// Calculate data for the A-channel
				bltadat_local = (ushort)((blitter.bltadat & blitter.bltafwm) >> (int)blit_a_shift_local);

				// Check for single dot
				if (x_independent)
				{
					if ((blitter.bltcon & 0x00000002) != 0)
					{
						if (single_dot)
						{
							bltadat_local = 0;
						}
						else
						{
							single_dot = true;
						}
					}
				}

				// Calculate data for the B-channel
				bltbdat_local = ((mask & 1) != 0) ? (ushort)0xffff : (ushort)0;

				// Calculate result
				bltddat_local = 0;
				if ((minterm & 0x01) != 0) bltddat_local |= ~bltadat_local & ~bltbdat_local & ~bltcdat_local;
				if ((minterm & 0x02) != 0) bltddat_local |= ~bltadat_local & ~bltbdat_local &  bltcdat_local;
				if ((minterm & 0x04) != 0) bltddat_local |= ~bltadat_local &  bltbdat_local & ~bltcdat_local;
				if ((minterm & 0x08) != 0) bltddat_local |= ~bltadat_local &  bltbdat_local &  bltcdat_local;
				if ((minterm & 0x10) != 0) bltddat_local |=  bltadat_local & ~bltbdat_local & ~bltcdat_local;
				if ((minterm & 0x20) != 0) bltddat_local |=  bltadat_local & ~bltbdat_local &  bltcdat_local;
				if ((minterm & 0x40) != 0) bltddat_local |=  bltadat_local &  bltbdat_local & ~bltcdat_local;
				if ((minterm & 0x80) != 0) bltddat_local |=  bltadat_local &  bltbdat_local &  bltcdat_local;

				// Save result to D-channel, same as the C ptr after first pixel. 
				if (c_enabled) // C-channel must be enabled
				{
					chipmemWriteWord((ushort)bltddat_local, bltdpt_local);
				}

				// Remember zero result status
				bltzero_local = bltzero_local | bltddat_local;

				// Rotate mask
				mask = (ushort)((mask << 1) | (mask >> 15));

				// Test movement in the X direction
				// When the decision variable gets positive,
				// the line moves one pixel to the right

				// decrease/increase x
				if (decision_is_signed)
				{
					// Do not yet increase, D has sign
					// D = D + (2*sdelta = bltbmod)
					decision_variable += decision_inc_signed;
				}
				else
				{
					// increase, D reached a positive value
					// D = D + (2*sdelta - 2*ldelta = bltamod)
					decision_variable += decision_inc_unsigned;

					if (!x_independent)
					{
						if (x_inc)
						{
							blitterLineIncreaseX(ref blit_a_shift_local, ref bltcpt_local);
						}
						else
						{
							blitterLineDecreaseX(ref blit_a_shift_local, ref bltcpt_local);
						}
					}
					else
					{
						if (y_inc)
						{
							blitterLineIncreaseY(ref bltcpt_local, blitter.bltcmod);
						}
						else
						{
							blitterLineDecreaseY(ref bltcpt_local, blitter.bltcmod);
						}

						single_dot = false;
					}
				}

				decision_is_signed = ((short)decision_variable < 0);

				if (!x_independent)
				{
					// decrease/increase y
					if (y_inc)
					{
						blitterLineIncreaseY(ref bltcpt_local, blitter.bltcmod);
					}
					else
					{
						blitterLineDecreaseY(ref bltcpt_local, blitter.bltcmod);
					}
				}
				else
				{
					if (x_inc)
					{
						blitterLineIncreaseX(ref blit_a_shift_local, ref bltcpt_local);
					}
					else
					{
						blitterLineDecreaseX(ref blit_a_shift_local, ref bltcpt_local);
					}
				}

				bltdpt_local = bltcpt_local;
			}

			blitter.bltcon = (ushort)(blitter.bltcon & 0x0FFFFFFBF);
			if (decision_is_signed) blitter.bltcon |= 0x00000040;

			blitter.a_shift_asc = blit_a_shift_local;
			blitter.a_shift_desc = 16 - blitter.a_shift_asc;
			blitter.bltbdat = (ushort)bltbdat_local;
			blitter.bltapt = (uint)((blitter.bltapt & 0xffff0000) | (decision_variable & 0xffff));
			blitter.bltcpt = bltcpt_local;
			blitter.bltdpt = bltdpt_local;
			blitter.bltzero = bltzero_local;
			//memoryWriteWord(0x8040, 0x00DFF09C);
		}
	}
}