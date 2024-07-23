using System;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class Blitter : IBlitter
	{
		private readonly IChips custom;
		private IDMA memory;
		private readonly IInterrupt interrupt;
		private readonly IChipsetClock clock;
		private readonly IOptions<EmulationSettings> settings;
		private readonly ILogger logger;

		public Blitter(IChips custom, IInterrupt interrupt, IChipsetClock clock,
			IOptions<EmulationSettings> settings, ILogger<Blitter> logger)
		{
			this.custom = custom;
			this.interrupt = interrupt;
			this.clock = clock;
			this.settings = settings;
			this.logger = logger;
		}

		public void Init(IDMA dma)
		{
			memory = dma;
		}

		public void Logging(bool enabled) { }
		public void Dumping(bool enabled) { }

		private enum BlitterState
		{
			Idle,

			BlitA,
			BlitAShift,
			BlitB,
			BlitBShift,
			BlitC,
			BlitMinterm,
			BlitD,
			BlitEndOfWord,
			BlitDLast,
			BlitEnd,

			LineB,
			LineC,
			LineMinterm,
			LineD,
			LineEndOfWord,
			LineEnd,
		}

		private BlitterState status = BlitterState.Idle;

		public void Emulate(ulong cycles)
		{
			//clock.WaitForTick();
			while (RunStateMachine()) ;
			//clock.Ack();
		}

		private bool RunStateMachine()
		{
			switch (status)
			{
				case BlitterState.Idle: return false;

				case BlitterState.BlitA: return BlitA();
				case BlitterState.BlitAShift: return BlitAShift();
				case BlitterState.BlitB: return BlitB();
				case BlitterState.BlitBShift: return BlitBShift();
				case BlitterState.BlitC: return BlitC();
				case BlitterState.BlitMinterm: return BlitMinterm();
				case BlitterState.BlitD: return BlitD();
				case BlitterState.BlitEndOfWord: return BlitEndOfWord();
				case BlitterState.BlitDLast: return BlitDLast();
				case BlitterState.BlitEnd: return BlitEnd();

				case BlitterState.LineB: return LineB();
				case BlitterState.LineC: return LineC();
				case BlitterState.LineMinterm: return LineMinterm();
				case BlitterState.LineD: return LineD();
				case BlitterState.LineEndOfWord: return LineEndOfWord();
				case BlitterState.LineEnd: return LineEnd();
			}

			throw new ArgumentOutOfRangeException();
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

			status = BlitterState.Idle;
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
				case ChipRegs.BLTCON0: bltcon0 = value; break;
				case ChipRegs.BLTCON1: bltcon1 = value; break;

				case ChipRegs.BLTAFWM: bltafwm = value; break;
				case ChipRegs.BLTALWM: bltalwm = value; break;

				case ChipRegs.BLTCPTH: bltcpt = ((bltcpt & 0x0000ffff) | ((uint)value << 16)); break;
				case ChipRegs.BLTCPTL: bltcpt = ((bltcpt & 0xffff0000) | (uint)(value & 0xfffe)); break;
				case ChipRegs.BLTBPTH: bltbpt = ((bltbpt & 0x0000ffff) | ((uint)value << 16)); break;
				case ChipRegs.BLTBPTL: bltbpt = ((bltbpt & 0xffff0000) | (uint)(value & 0xfffe)); break;
				case ChipRegs.BLTAPTH: bltapt = ((bltapt & 0x0000ffff) | ((uint)value << 16)); break;
				case ChipRegs.BLTAPTL: bltapt = ((bltapt & 0xffff0000) | (uint)(value & 0xfffe)); break;
				case ChipRegs.BLTDPTH: bltdpt = ((bltdpt & 0x0000ffff) | ((uint)value << 16)); break;
				case ChipRegs.BLTDPTL: bltdpt = ((bltdpt & 0xffff0000) | (uint)(value & 0xfffe)); break;

				case ChipRegs.BLTSIZE:
					bltsize = value;
					BlitSmall(insaddr);
					break;

				case ChipRegs.BLTCON0L: bltcon0 = (bltcon0 & 0x0000ff00) | ((uint)value & 0x000000ff); break;

				case ChipRegs.BLTSIZV: bltsizv = value; break;

				case ChipRegs.BLTSIZH:
					bltsizh = value;
					BlitBig(insaddr);
					break;

				case ChipRegs.BLTCMOD: bltcmod = (uint)(short)value & 0xfffffffe; break;
				case ChipRegs.BLTBMOD: bltbmod = (uint)(short)value & 0xfffffffe; break;
				case ChipRegs.BLTAMOD: bltamod = (uint)(short)value & 0xfffffffe; break;
				case ChipRegs.BLTDMOD: bltdmod = (uint)(short)value & 0xfffffffe; break;

				case ChipRegs.BLTCDAT: bltcdat = value; break;
				case ChipRegs.BLTBDAT: bltbdat = value; break;
				case ChipRegs.BLTADAT: bltadat = value; break;
				case ChipRegs.BLTDDAT: bltddat = value; break;
			}
		}

		private void BlitSmall(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			uint h = bltsize & 0x3f;
			uint v = bltsize >> 6;

			if (h == 0) h = 64;
			if (v == 0) v = 1024;

			Blit(h, v);
		}

		private void BlitBig(uint insaddr)
		{
			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			uint h = bltsizh & 0x07ff;
			uint v = bltsizv & 0x7fff;

			if (h == 0) h = 2048;
			if (v == 0) v = 32768;

			Blit(h, v);
		}

		private struct Writecache
		{
			public uint Address;
			public ushort Value;
		}
		private Writecache writecache;
		private const uint NO_WRITECACHE = 0xffffffff;

		private bool DelayedWrite()
		{
			if (writecache.Address == NO_WRITECACHE) return true;

			memory.Write(DMASource.Blitter, writecache.Address, DMA.BLTEN, writecache.Value, Size.Word);
			
			return false;
		}

		private bool DelayedWrite(uint address, ushort value)
		{
			bool x = DelayedWrite();
			writecache.Address = address;
			writecache.Value = value;
			return x;
		}

		private void ClearDelayedWrite()
		{
			writecache.Address = NO_WRITECACHE;
		}

		private void Blit(uint h, uint v)
		{
			blitWidth = h;
			blitHeight = v;

			BeginBlit();
		}

		private bool BeginBlit()
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

			bltabits = bltbbits = 0;

			status = BlitterState.BlitA;
			return true;
		}

		private uint bltabits;
		private uint bltbbits;
		private uint fci;
		private int ashift;
		private int bshift;
		private uint bltzero;

		private uint w, h;
		private uint blitWidth, blitHeight;
		uint s_bltadat, s_bltbdat;

		private bool BlitA()
		{
			status = BlitterState.BlitAShift;

			if ((bltcon0 & (1u << 11)) == 0) return true;

			memory.Read(DMASource.Blitter, bltapt, DMA.BLTEN, Size.Word, ChipRegs.BLTADAT);
			return false;
		}

		private bool BlitAShift()
		{
			status = BlitterState.BlitB;

			s_bltadat = bltadat;

			if (w == 0) s_bltadat &= bltafwm;
			if (w == blitWidth - 1) s_bltadat &= bltalwm;

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
			return true;
		}

		private bool BlitB()
		{
			status = BlitterState.BlitBShift;

			if ((bltcon0 & (1u << 10)) == 0) return true;

			memory.Read(DMASource.Blitter, bltbpt, DMA.BLTEN, Size.Word, ChipRegs.BLTBDAT);
			return false;
		}

		private bool BlitBShift()
		{
			status = BlitterState.BlitC;

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
			return true;
		}

		private bool BlitC()
		{
			status = BlitterState.BlitMinterm;

			if ((bltcon0 & (1u << 9)) == 0) return true;

			memory.Read(DMASource.Blitter, bltcpt, DMA.BLTEN, Size.Word, ChipRegs.BLTCDAT);
			return false;
		}

		private bool BlitMinterm()
		{
			status = BlitterState.BlitD;

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

			return true;
		}

		private bool BlitD()
		{
			status = BlitterState.BlitEndOfWord;

			bltzero |= bltddat;

			if (((bltcon0 & (1u << 8)) != 0) && ((bltcon1 & (1u << 7)) == 0))
				return DelayedWrite(bltdpt, (ushort)bltddat);
			return true;
		}

		private bool BlitEndOfWord()
		{
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
			if (w != blitWidth)
			{
				status = BlitterState.BlitA;
				return true;
			}

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
			if (h != blitHeight)
			{
				status = BlitterState.BlitA;
				return true;
			}

			status = BlitterState.BlitDLast;
			return true;
		}

		private bool BlitDLast()
		{
			status = BlitterState.BlitEnd;
			return DelayedWrite();
		}

		private bool BlitEnd()
		{
			ushort dmacon = 1 << 14;

			//clear BZERO
			if (bltzero != 0)
				dmacon |= 1 << 13;

			//disable blitter busy in DMACON
			custom.WriteDMACON(dmacon);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);

			status = BlitterState.Idle;
			return false;
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
			LineBegin();
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

		private bool LineBegin()
		{
			uint octant = (bltcon1 >> 2) & 7;
			sing = (bltcon1 & (1 << 1)) != 0;

			//todo: fix this, it should be maximum length line
			length = bltsize >> 6;
			if (length == 0)
			{
				interrupt.AssertInterrupt(Interrupt.BLIT);
				status = BlitterState.Idle;
				return false;
			}

			dy = (int)(bltbmod / 2);
			dx = -(int)bltamod / 2 + dy;

			if (octant < 4) (dx, dy) = (dy, dx);

			sx = 1;
			if (octant == 2 || octant == 3 || octant == 5 || octant == 7) sx = -1;
			sy = 1;
			if (octant == 1 || octant == 3 || octant == 6 || octant == 7) sy = -1;

			bltzero = 0;

			//set BBUSY and BZERO
			custom.WriteDMACON(0x8000 + (1 << 14) + (1 << 13));

			writeBit = true;

			x0 = (int)(bltcon0 >> 12);
			int ror = (int)(bltcon1 >> 12);

			bltbdatror = (bltbdat << ror) | (bltbdat >> (16 - ror));

			dm = Math.Max(dx, dy);
			x1 = dm / 2;
			y1 = dm / 2;

			status = BlitterState.LineB;
			return true;
		}

		private bool LineB()
		{
			status = BlitterState.LineC;
			return true;
		}

		private bool LineC()
		{
			status = BlitterState.LineMinterm;

			if ((bltcon0 & (1u << 9)) == 0) return true;

			memory.Read(DMASource.Blitter, bltcpt, DMA.BLTEN, Size.Word, ChipRegs.BLTCDAT);
			return false;

		}

		private bool LineMinterm()
		{
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

			status = BlitterState.LineD;
			return true;
		}

		private bool LineD()
		{
			status = BlitterState.LineEndOfWord;

			//oddly, USEC must be checked, not USED
			if ((bltcon0 & (1u << 9)) != 0 && (bltcon1 & (1u << 7)) == 0)
			{
				if (writeBit)
				{
					if (sing) writeBit = false;
					memory.Write(DMASource.Blitter, bltdpt, DMA.BLTEN, (ushort)bltddat, Size.Word);
					return false;
				}
			}

			return true;
		}

		private bool LineEndOfWord()
		{
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
			
			length--;
			if (length != 0)
				status = BlitterState.LineB;
			else
				status = BlitterState.LineEnd;

			return true;
		}

		private bool LineEnd()
		{
			ushort dmacon = 1 << 14;

			//clear BZERO
			if (bltzero != 0)
				dmacon |= 1 << 1;

			//disable blitter busy in DMACON
			custom.WriteDMACON(dmacon);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);

			status = BlitterState.Idle;
			return false;
		}
	}
}
