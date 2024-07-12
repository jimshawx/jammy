using System;
using System.Collections.Generic;
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
	public class SyncBlitter : IBlitter
	{
		private readonly IChips custom;
		private readonly IMemoryMappedDevice memory;
		private readonly IInterrupt interrupt;
		private readonly IOptions<EmulationSettings> settings;
		private readonly ILogger logger;

		public SyncBlitter(IChips custom, IChipRAM memory, IInterrupt interrupt,
			IOptions<EmulationSettings> settings, ILogger<Blitter> logger)
		{
			this.custom = custom;
			this.memory = memory;
			this.interrupt = interrupt;
			this.settings = settings;
			this.logger = logger;
		}
		public void Logging(bool enabled){}
		public void Dumping(bool enabled){}

		private IEnumerator<int> currentBlit = null;

		private void SetNewBlit(IEnumerable<int> blit)
		{
			if (currentBlit != null)
			{
				if (currentBlit.MoveNext())
				{
					logger.LogTrace("Blitter not finished before new blit started!");
					//while (currentBlit.MoveNext()) /* finish the blit */ ;
					//it's too late to do this, all the blitter registers will already have been set for the next blit
					//when they should have been based on the values after this blit finished
				}

				currentBlit.Dispose();
			}
			currentBlit = blit.GetEnumerator();
		}

		public void Emulate(ulong cycles)
		{
			currentBlit.MoveNext();
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

			SetNewBlit(new List<int>());
		}

		public ushort Read(uint insaddr, uint address)
		{
			return 0;
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
					SetNewBlit(BlitSmall(insaddr));
					break;

				case ChipRegs.BLTCON0L:
					bltcon0 = (bltcon0 & 0x0000ff00) | ((uint)value & 0x000000ff);
					break;

				case ChipRegs.BLTSIZV:
					bltsizv = value;
					break;
				case ChipRegs.BLTSIZH:
					bltsizh = value;
					SetNewBlit(BlitBig(insaddr));
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

		private IEnumerable<int> BlitSmall(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
				return Line(insaddr);

			uint width = bltsize & 0x3f;
			uint height = bltsize >> 6;

			if (width == 0) width = 64;
			if (height == 0) height = 1024;

			return Blit(width, height);
		}

		private IEnumerable<int> BlitBig(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
				return Line(insaddr);

			uint width = bltsizh & 0x07ff;
			uint height = bltsizv & 0x7fff;

			if (width == 0) width = 2048;
			if (height == 0) height = 32768;

			return Blit(width, height);
		}

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
				memory.Write(0, writecache.Address, writecache.Value, Size.Word);
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

		private IEnumerable<int> Blit(uint width, uint height)
		{
			ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmacon & (1 << 6)) == 0)
				logger.LogTrace("BLTEN is off!");
			if ((dmacon & (1 << 9)) == 0)
				logger.LogTrace("DMAEN is off!");

			//set BBUSY
			custom.WriteDMACON(0x8000 + (1 << 14));
			//for (;;)
			//{
			//	yield return 0;
			//	if ((custom.Read(0, ChipRegs.DMACONR, Size.Word) & ((1<<6)|(1<<9))) == 0) break;
			//}

			//todo: assumes blitter DMA is enabled

			int ashift = (int)(bltcon0 >> 12);
			int bshift = (int)(bltcon1 >> 12);

			uint bltzero = 0;
			uint s_bltadat, s_bltbdat;

			//set blitter busy in DMACON
			//custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);

			//set BZERO
			custom.WriteDMACON(0x8000 + (1 << 13));

			//yield return 1;

			uint bltabits = 0;
			uint bltbbits = 0;
			uint fci = bltcon1&(1u<<2);
			ClearDelayedWrite();

			//yield return 1;

			for (uint h = 0; h < height; h++)
			{
				for (uint w = 0; w < width; w++)
				{
					if ((bltcon0 & (1u << 11)) != 0)
					{
						bltadat = memory.Read(0, bltapt, Size.Word);
						//yield return 1;
					}

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
					{
						bltbdat = memory.Read(0, bltbpt, Size.Word);
						//yield return 1;
					}

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
					{
						bltcdat = memory.Read(0, bltcpt, Size.Word);
						//yield return 1;
					}

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

					if (((bltcon0 & (1u << 8)) != 0) && ((bltcon1 & (1u << 7)) == 0))
					{
						DelayedWrite(bltdpt, (ushort)bltddat);
						//yield return 1;
					}

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

				//reset carry
				bltcon1 &= ~(1u << 2);
				bltcon1 |= fci;
				//yield return 1;
			}

			DelayedWrite();
			//yield return 1;

			//write the BZERO bit in DMACON
			//if (bltzero == 0)
			//	custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 13), Size.Word);
			//else
			//	custom.Write(0, ChipRegs.DMACON, (1u << 13), Size.Word);
			//blit wasn't all zeros, clear BZERO
			if (bltzero != 0)
				custom.WriteDMACON(1 << 13);

			//disable blitter busy in DMACON
			//custom.Write(0, ChipRegs.DMACON, (1u << 14), Size.Word);
			//clear BBUSY
			custom.WriteDMACON(1 << 14);

			yield return 1;

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

		private IEnumerable<int> Line(uint insaddr)
		{
			uint octant = (bltcon1 >> 2) & 7;
			bool sing = (bltcon1 & (1 << 1)) != 0;

			uint length = bltsize >> 6;
			if (length == 0)
			{
				logger.LogTrace("Zero length line");
				interrupt.AssertInterrupt(Interrupt.BLIT);
				yield break;
			}

			int dy = (int)(bltbmod / 2);
			int dx = -(int)bltamod / 2 + dy;

			if (octant < 4) (dx, dy) = (dy, dx);

			int sx = 1;
			if (octant == 2 || octant == 3 || octant == 5 || octant == 7) sx = -1;
			int sy = 1;
			if (octant == 1 || octant == 3 || octant == 6 || octant == 7) sy = -1;

			uint bltzero = 0;

			ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmacon & (1 << 6)) == 0)
				logger.LogTrace("BLTEN is off!");
			if ((dmacon & (1 << 9)) == 0)
				logger.LogTrace("DMAEN is off!");

			//set BBUSY
			custom.WriteDMACON(0x8000 + (1 << 14));
			//for (;;)
			//{
			//	yield return 0;
			//	if ((custom.Read(0, ChipRegs.DMACONR, Size.Word) & ((1 << 6) | (1 << 9))) == 0) break;
			//}

			//set BZERO
			custom.WriteDMACON(0x8000 | (1 << 13));

			//yield return 1;

			bool writeBit = true;

			int x0 = (int)(bltcon0 >> 12);
			int ror = (int)(bltcon1 >> 12);

			uint bltbdatror = (bltbdat << ror) | (bltbdat>>(16-ror));

			int dm = Math.Max(dx, dy);
			int x1 = dm / 2; 
			int y1 = dm / 2;
			
			while (length-- > 0)
			{
				if ((bltcon0 & (1u << 9)) != 0)
				{
					bltcdat = memory.Read(insaddr, bltcpt, Size.Word);
					//yield return 1;
				}

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
						//yield return 1;
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

			//yield return 1;

			//write the BZERO bit in DMACON
			//if (bltzero == 0)
			//	custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 13), Size.Word);
			//else
			//	custom.Write(0, ChipRegs.DMACON, (1u << 13), Size.Word);
			//blit wasn't all zeros, clear BZERO
			if (bltzero != 0)
				custom.WriteDMACON(1 << 13);

			//disable blitter busy in DMACON
			//custom.Write(0, ChipRegs.DMACON, (1u << 14), Size.Word);
			//clear BBUSY
			custom.WriteDMACON(1 << 14);

			yield return 1;

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);
		}
	}
}
