using System;
using System.Collections.Generic;
using System.IO;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
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
		private readonly IMemoryMappedDevice ram;
		private readonly IDMA dma;
		private readonly IOptions<EmulationSettings> settings;
		private readonly ILogger logger;

		public Blitter(IChips custom, IInterrupt interrupt, IChipsetClock clock, IChipRAM ram, 
			IDMA dma, IOptions<EmulationSettings> settings, ILogger<Blitter> logger)
		{
			this.custom = custom;
			this.interrupt = interrupt;
			this.clock = clock;
			this.ram = ram;
			this.dma = dma;
			this.settings = settings;
			this.logger = logger;
			PreComputeFill();
		}

		public void Init(IDMA dma)
		{
			memory = dma;
		}

		private bool blitterLog = false;
		private bool blitterDump = false;

		public void Logging(bool enabled) { blitterLog = enabled; }
		public void Dumping(bool enabled) { blitterDump = enabled; }

		private enum BlitterState
		{
			Idle,

			BlitStartUp1,
			BlitStartUp2,
			BlitA,
			BlitB,
			BlitC,
			BlitMinterm,
			BlitD,
			BlitEndOfWord,
			BlitDLast,
			BlitEnd,

			LineA,
			LineB,
			LineC,
			LineB2,
			LineMinterm,
			LineD,
			LineEndOfWord,
			LineEnd,
		}

		private BlitterState status = BlitterState.Idle;

		public void Emulate()
		{
			//clock.WaitForTick();
			while (RunStateMachine()) ;

			var currentState = status;
			while (RunStateMachine()) ;
			if (currentState != status)
				logger.LogTrace($"{currentState}->{status}");

			//test the blitter state machine by running it to completion immediately
			//while (status != BlitterState.Idle)
			//{
			//	RunStateMachine();
			//	if (memory.IsWaitingForDMA(DMASource.Blitter))
			//		memory.DebugExecuteDMAActivity(DMASource.Blitter);
			//}

			//clock.Ack();
		}

		private bool RunStateMachine()
		{
			if (status == BlitterState.Idle) return false;

			if (memory.IsWaitingForDMA(DMASource.Blitter)) return false;

			switch (status)
			{
				case BlitterState.BlitStartUp1: return BlitStartUp1();
				case BlitterState.BlitStartUp2: return BlitStartUp2();
				case BlitterState.BlitA: return BlitA();
				case BlitterState.BlitB: return BlitB();
				case BlitterState.BlitC: return BlitC();
				case BlitterState.BlitMinterm: return BlitMinterm();
				case BlitterState.BlitD: return BlitD();
				case BlitterState.BlitEndOfWord: return BlitEndOfWord();

				case BlitterState.BlitDLast: return BlitDLast();
				case BlitterState.BlitEnd: return BlitEnd();

				case BlitterState.LineA: return LineA();
				case BlitterState.LineB: return LineB();
				case BlitterState.LineC: return LineC();
				case BlitterState.LineMinterm: return LineMinterm();
				case BlitterState.LineB2: return LineB2();
				case BlitterState.LineD: return LineD();
				case BlitterState.LineEndOfWord: return LineEndOfWord();
				case BlitterState.LineEnd: return LineEnd();
			}

			throw new ArgumentOutOfRangeException();
		}

		public bool IsIdle()
		{
			return status == BlitterState.Idle;
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
			if (address == ChipRegs.BLTDDAT) value = (ushort)bltddat;
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

				case ChipRegs.BLTCPTH: bltcpt = ((bltcpt & 0x0000ffff) | ((uint)(value & 0x1f) << 16)); break;
				case ChipRegs.BLTCPTL: bltcpt = ((bltcpt & 0xffff0000) | (uint)(value & 0xfffe)); break;
				case ChipRegs.BLTBPTH: bltbpt = ((bltbpt & 0x0000ffff) | ((uint)(value & 0X1f) << 16)); break;
				case ChipRegs.BLTBPTL: bltbpt = ((bltbpt & 0xffff0000) | (uint)(value & 0xfffe)); break;
				case ChipRegs.BLTAPTH: bltapt = ((bltapt & 0x0000ffff) | ((uint)(value & 0x1f) << 16)); break;
				case ChipRegs.BLTAPTL: bltapt = ((bltapt & 0xffff0000) | (uint)(value & 0xfffe)); break;
				case ChipRegs.BLTDPTH: bltdpt = ((bltdpt & 0x0000ffff) | ((uint)(value & 0x1f) << 16)); break;
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
			if (blitterLog)
				WriteBlitterState();

			ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmacon & (1<<14)) != 0 || status != BlitterState.Idle)
				logger.LogTrace($"Previous blit not finished {((dmacon & (1<<14))!=0?"BBUSY":"~BBUSY")} {status}");

			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			uint h = bltsize & 0x3f;
			uint v = bltsize >> 6;

			if (h == 0) h = 64;
			if (v == 0) v = 1024;

			if (blitterDump)
				DumpBlitterState(insaddr,h,v);

			Blit(insaddr, h, v);
		}

		private void BlitBig(uint insaddr)
		{
			if (blitterLog)
				WriteBlitterState();

			ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmacon & (1 << 14)) != 0 || status != BlitterState.Idle)
				logger.LogTrace($"Previous blit not finished {((dmacon & (1 << 14)) != 0 ? "BBUSY" : "~BBUSY")} {status}");

			if ((bltcon1 & 1) != 0)
			{
				Line(insaddr);
				return;
			}

			uint h = bltsizh & 0x07ff;
			uint v = bltsizv & 0x7fff;

			if (h == 0) h = 2048;
			if (v == 0) v = 32768;

			if (blitterDump)
				DumpBlitterState(insaddr,h, v);

			Blit(insaddr, h, v);
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

		private void DelayedWriteImmediate()
		{
			if (writecache.Address == NO_WRITECACHE) return;

			ram.Write(0, writecache.Address, writecache.Value, Size.Word);
		}

		private bool DelayedWrite(uint address, ushort value)
		{
			bool x = DelayedWrite();
			writecache.Address = address;
			writecache.Value = value;
			return x;
		}

		private void DelayedWriteImmediate(uint address, ushort value)
		{
			DelayedWriteImmediate();
			writecache.Address = address;
			writecache.Value = value;
		}

		private void ClearDelayedWrite()
		{
			writecache.Address = NO_WRITECACHE;
		}

		private void Blit(uint insaddr, uint h, uint v)
		{
			blitWidth = h;
			blitHeight = v;

			ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmacon & (1 << 6)) == 0)
				logger.LogTrace($"BLTEN is off! @{insaddr:X8}");
			if ((dmacon & (1 << 9)) == 0)
				logger.LogTrace($"DMAEN is off! @{insaddr:X8}");

			BlitImmediate(blitWidth, blitHeight);
			//BeginBlit();
		}

		private bool BeginBlit()
		{
			memory.ClearWaitingForDMA(DMASource.Blitter);

			//set blitter busy in DMACON
			//custom.Write(0, ChipRegs.DMACON, 0x8000 + (1u << 14), Size.Word);
			//set BBUSY and BZERO
			dma.WriteDMACON(0x8000 + (1 << 14) + (1 << 13));

			ClearDelayedWrite();

			fci = bltcon1 & (1u << 2);

			ashift = (int)(bltcon0 >> 12);
			bshift = (int)(bltcon1 >> 12);

			bltzero = 0;
			w = h = 0;

			bltabits = bltbbits = 0;

			status = BlitterState.BlitStartUp1;
			return false;
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

		//eat a DMA slot
		private bool BlitStartUp1()
		{
			status = BlitterState.BlitStartUp2;
			memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);
			return false;
		}

		//eat a DMA slot
		private bool BlitStartUp2()
		{
			status = BlitterState.BlitA;
			memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);
			return false;
		}

		//Read A if needed
		private bool BlitA()
		{
			status = BlitterState.BlitB;

			if ((bltcon0 & (1u << 11)) == 0)
			{
				memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);
				return false;
			}

			memory.Read(DMASource.Blitter, bltapt, DMA.BLTEN, Size.Word, ChipRegs.BLTADAT);
			return false;
		}

		//Read B if needed, Shift / Mask A
		private bool BlitB()
		{
			status = BlitterState.BlitC;

			if ((bltcon0 & (1u << 10)) != 0)
				memory.Read(DMASource.Blitter, bltbpt, DMA.BLTEN, Size.Word, ChipRegs.BLTBDAT);
			else
				memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);

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

			return false;
		}

		//Read C if needed, Shift B
		private bool BlitC()
		{
			if ((bltcon0 & (1u << 9)) != 0)
				memory.Read(DMASource.Blitter, bltcpt, DMA.BLTEN, Size.Word, ChipRegs.BLTCDAT);
			else
				memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);

			status = BlitterState.BlitMinterm;

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
			return false;
		}

		private bool BlitMinterm()
		{
			memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);

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

			//todo: does this need an extra cycle?
			Fill();

			return false;
		}

		private bool BlitD()
		{
			status = BlitterState.BlitEndOfWord;

			bltzero |= bltddat;

			bool x = false;
			if (((bltcon0 & (1u << 8)) != 0) && ((bltcon1 & (1u << 7)) == 0))
				x = DelayedWrite(bltdpt, (ushort)bltddat);

			return x;
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
			DelayedWrite();
			return false;
		}

		private bool BlitEnd()
		{
			ushort dmacon = 1 << 14;

			//clear BZERO
			if (bltzero != 0)
				dmacon |= 1 << 13;

			//disable blitter busy in DMACON
			dma.WriteDMACON(dmacon);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);

			status = BlitterState.Idle;
			return false;
		}

		//todo: is this faster? it's a bit of a cache smasher
		private readonly uint[][] fills = [new uint[65536], new uint[65536], new uint[65536], new uint[65536]];
		private void PreComputeFill()
		{
			for (uint i = 0; i <= 0xffff; i++)
			{
				
				bltddat = i;
				bltcon1 = (1 << 3) | (1 << 1);
				FillInline();
				fills[0][i] = bltddat | ((bltcon1 & (1 << 2)) << 16);
			}
			for (uint i = 0; i <= 0xffff; i++)
			{
				bltddat = i;
				bltcon1 = (1 << 3) | (1 << 2) | (1 << 1);
				FillInline();
				fills[2][i] = bltddat | ((bltcon1 & (1 << 2)) << 16);
			}
			for (uint i = 0; i <= 0xffff; i++)
			{
				bltddat = i;
				bltcon1 = (2 << 3) | (1 << 1);
				FillInline();
				fills[1][i] = bltddat | ((bltcon1 & (1 << 2)) << 16);
			}
			for (uint i = 0; i <= 0xffff; i++)
			{
				bltddat = i;
				bltcon1 = (2 << 3) | (1 << 2) | (1 << 1);
				FillInline();
				fills[3][i] = bltddat | ((bltcon1 & (1 << 2)) << 16);
			}
		}

		private void Fill()
		{
			uint mode = (bltcon1 >> 3) & 3;
			//descending mode and one of the fill modes must be set
			if (mode == 0 || (bltcon1 & (1 << 1)) == 0) return;

			//hack: what to do if both EFE and IFE set? Let's choose EFE
			if (mode == 3) mode = 2;

			mode--;
			mode |= (bltcon1 & (1 << 2))>>1;

			bltddat = fills[mode][bltddat];
			bltcon1 &= ~(1u << 2);
			bltcon1 |= bltddat >> 16;
			bltddat = (ushort)bltddat;
		}

		private void FillInline()
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

		private void Line(uint insaddr)
		{
			if (blitterDump)
				DumpBlitterState(insaddr, bltsize>>6);

			//Line2(insaddr, logger);
			//LineImmediate(insaddr);
			LineBegin();
		}

		private uint length;
		private bool writeBit;
		private ushort mask;
		private int x0, x1;
		private int y1;
		private int dm;
		private bool sing;
		private int sx, sy;
		private int dx, dy;

		private bool LineBegin()
		{
			memory.ClearWaitingForDMA(DMASource.Blitter);

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
			dma.WriteDMACON(0x8000 + (1 << 14) + (1 << 13));

			writeBit = true;

			x0 = (int)(bltcon0 >> 12);
			int ror = (int)(bltcon1 >> 12);

			mask = (ushort)((bltbdat >> ror) | (bltbdat << (16 - ror)));

			dm = Math.Max(dx, dy);
			x1 = dm / 2;
			y1 = dm / 2;

			status = BlitterState.LineA;
			return false;
		}

		//-C-D
		//or
		//-BC-BD (B is read twice)
		private bool LineA()
		{
			status = BlitterState.LineB;

			bltadat = 0x8000u >> x0;
			memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);
			return false;
		}

		private bool LineB()
		{
			status = BlitterState.LineC;

			//if USEB is off, then it's -C-D, else if -BC-BD
			if ((bltcon0 & (1u << 10)) == 0) return true;//B is off, keep going

			memory.Read(DMASource.Blitter, bltbpt, DMA.BLTEN, Size.Word, ChipRegs.BLTBDAT);
			return false;
		}

		private bool LineC()
		{
			if ((bltcon0 & (1u << 10)) != 0)
			{
				int ror = (int)(bltcon1 >> 12);
				mask = (ushort)((bltbdat >> ror) | (bltbdat << (16 - ror)));
			}

			status = BlitterState.LineMinterm;

			if ((bltcon0 & (1u << 9)) == 0)
			{
				memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);
				return false;
			}

			memory.Read(DMASource.Blitter, bltcpt, DMA.BLTEN, Size.Word, ChipRegs.BLTCDAT);
			return false;
		}

		private bool LineMinterm()
		{
			uint bltbdatror = ((mask & 1) != 0) ? 0xffffu : 0;

			bltddat = 0;
			if ((bltcon0 & 0x01) != 0) bltddat |= ~bltadat & ~bltbdatror & ~bltcdat;
			if ((bltcon0 & 0x02) != 0) bltddat |= ~bltadat & ~bltbdatror & bltcdat;
			if ((bltcon0 & 0x04) != 0) bltddat |= ~bltadat & bltbdatror & ~bltcdat;
			if ((bltcon0 & 0x08) != 0) bltddat |= ~bltadat & bltbdatror & bltcdat;
			if ((bltcon0 & 0x10) != 0) bltddat |= bltadat & ~bltbdatror & ~bltcdat;
			if ((bltcon0 & 0x20) != 0) bltddat |= bltadat & ~bltbdatror & bltcdat;
			if ((bltcon0 & 0x40) != 0) bltddat |= bltadat & bltbdatror & ~bltcdat;
			if ((bltcon0 & 0x80) != 0) bltddat |= bltadat & bltbdatror & bltcdat;
			mask = (ushort)((mask << 1) | (mask >> 15));

			status = BlitterState.LineB2;

			memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);

			return false;
		}

		private bool LineB2()
		{
			status = BlitterState.LineD;

			//if USEB is off, then it's -C-D, else if -BC-BD
			if ((bltcon0 & (1u << 10)) == 0) return true;//B is off, keep going

			memory.Read(DMASource.Blitter, bltbpt, DMA.BLTEN, Size.Word, ChipRegs.BLTBDAT);
			return false;
		}

		private bool LineD()
		{
			if ((bltcon0 & (1u << 10)) != 0)
			{
				int ror = (int)(bltcon1 >> 12);
				mask = (ushort)((bltbdat >> ror) | (bltbdat << (16 - ror)));
			}

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

			memory.NeedsDMA(DMASource.Blitter, DMA.BLTEN);
			return false;
		}

		private bool LineEndOfWord()
		{
			bltzero |= bltddat;

			x1 -= dx;
			if (x1 < 0)
			{
				x1 += dm;
				x0 += sx;
				if (x0 >= 16) {	x0 = 0;	bltcpt += 2; }
				if (x0 < 0)	{ x0 = 15; bltcpt -= 2; }
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
				status = BlitterState.LineA;
			else
				status = BlitterState.LineEnd;

			return true;
		}

		private bool LineEnd()
		{
			ushort dmacon = 1 << 14;

			//clear BZERO
			if (bltzero != 0)
				dmacon |= 1 << 13;

			//disable blitter busy in DMACON
			dma.WriteDMACON(dmacon);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);

			status = BlitterState.Idle;
			return false;
		}

		private void LineImmediate(uint insaddr)
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

			//set BBUSY and BZERO
			dma.WriteDMACON(0x8000 + (1 << 14) + (1 << 13));

			bool writeBit = true;

			int x0 = (int)(bltcon0 >> 12);
			int ror = (int)(bltcon1 >> 12);

			ushort mask = (ushort)((bltbdat >> ror) | (bltbdat << (16 - ror)));

			int dm = Math.Max(dx, dy);
			int x1 = dm / 2;
			int y1 = dm / 2;

			while (length-- > 0)
			{
				if ((bltcon0 & (1u << 10)) != 0)
				{
					bltbdat = ram.Read(insaddr, bltbpt, Size.Word);
					mask = (ushort)((bltbdat >> ror) | (bltbdat << (16 - ror)));
				}

				if ((bltcon0 & (1u << 9)) != 0)
					bltcdat = ram.Read(insaddr, bltcpt, Size.Word);

				bltadat = 0x8000u >> x0;

				uint bltbdatror = ((mask&1)!=0)?0xffffu:0;

				bltddat = 0;
				if ((bltcon0 & 0x01) != 0) bltddat |= ~bltadat & ~bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x02) != 0) bltddat |= ~bltadat & ~bltbdatror & bltcdat;
				if ((bltcon0 & 0x04) != 0) bltddat |= ~bltadat & bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x08) != 0) bltddat |= ~bltadat & bltbdatror & bltcdat;
				if ((bltcon0 & 0x10) != 0) bltddat |= bltadat & ~bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x20) != 0) bltddat |= bltadat & ~bltbdatror & bltcdat;
				if ((bltcon0 & 0x40) != 0) bltddat |= bltadat & bltbdatror & ~bltcdat;
				if ((bltcon0 & 0x80) != 0) bltddat |= bltadat & bltbdatror & bltcdat;

				mask = (ushort)((mask << 1) | (mask >> 15));

				//oddly, USEC must be checked, not USED
				if ((bltcon0 & (1u << 9)) != 0 && (bltcon1 & (1u << 7)) == 0)
				{
					if (writeBit)
					{
						ram.Write(insaddr, bltdpt, bltddat, Size.Word);
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
					if (x0 < 0) { x0 = 15; bltcpt -= 2; }
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

			//clear BZERO if necessary and disable BBUSY in DMACON
			ushort dmacon = 1 << 14;
			if (bltzero != 0)
				dmacon |= 1 << 13;

			dma.WriteDMACON(dmacon);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);
		}

		private void BlitImmediate(uint width, uint height)
		{
			uint mode = (bltcon1 >> 3) & 3;
			int ashift = (int)(bltcon0 >> 12);
			int bshift = (int)(bltcon1 >> 12);

			uint bltzero = 0;
			uint s_bltadat, s_bltbdat;

			//set BBUSY and BZERO
			dma.WriteDMACON(0x8000 + (1 << 14) + (1 << 13));

			uint bltabits = 0;
			uint bltbbits = 0;
			uint fci = bltcon1 & (1u << 2);
			ClearDelayedWrite();

			for (uint h = 0; h < height; h++)
			{
				for (uint w = 0; w < width; w++)
				{
					if ((bltcon0 & (1u << 11)) != 0)
						bltadat = ram.Read(0, bltapt, Size.Word);

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
						bltbdat = ram.Read(0, bltbpt, Size.Word);

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
						bltcdat = ram.Read(0, bltcpt, Size.Word);

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
						DelayedWriteImmediate(bltdpt, (ushort)bltddat);

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
			}
			DelayedWriteImmediate();

			//clear BZERO if necessary and disable BBUSY in DMACON
			ushort dmacon = 1 << 14;
			if (bltzero != 0)
				dmacon |= 1 << 13;

			dma.WriteDMACON(dmacon);

			//write blitter interrupt bit to INTREQ, trigger blitter done
			interrupt.AssertInterrupt(Interrupt.BLIT);
			status = BlitterState.Idle;
		}

		public uint DebugChipsetRead(uint address, Size size)
		{
			uint value = 0;
			switch (address)
			{
				case ChipRegs.BLTCON0: value = bltcon0; break;
				case ChipRegs.BLTCON1: value = bltcon1; break;

				case ChipRegs.BLTAFWM: value = bltafwm; break;
				case ChipRegs.BLTALWM: value = bltalwm; break;

				case ChipRegs.BLTCPTH: value = bltcpt >> 16; break;
				case ChipRegs.BLTCPTL: value = bltcpt & 0xffff; break;
				case ChipRegs.BLTBPTH: value = bltbpt >> 16; break;
				case ChipRegs.BLTBPTL: value = bltbpt & 0xffff; break;
				case ChipRegs.BLTAPTH: value = bltapt >> 16; break;
				case ChipRegs.BLTAPTL: value = bltapt & 0xffff; break;
				case ChipRegs.BLTDPTH: value = bltdpt >> 16; break;
				case ChipRegs.BLTDPTL: value = bltdpt & 0xffff; break;

				case ChipRegs.BLTSIZE: value = bltsize; break;

				case ChipRegs.BLTCON0L: value = bltcon0; break;

				case ChipRegs.BLTSIZV: value = bltsizv; break;

				case ChipRegs.BLTSIZH: value = bltsizh; break;

				case ChipRegs.BLTCMOD: value = bltcmod; break;
				case ChipRegs.BLTBMOD: value = bltbmod; break;
				case ChipRegs.BLTAMOD: value = bltamod; break;
				case ChipRegs.BLTDMOD: value = bltdmod; break;

				case ChipRegs.BLTCDAT: value = bltcdat; break;
				case ChipRegs.BLTBDAT: value = bltbdat; break;
				case ChipRegs.BLTADAT: value = bltadat; break;
				case ChipRegs.BLTDDAT: value = bltddat; break;
			}
			return value;
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

		private void DumpBlitterState(uint insaddr, uint width, uint height)
		{
			logger.LogTrace($"BLIT! @{insaddr:X6} {width}x{height} = {width * 16}bits x {height} = {width * 16 * height} bits = {width * height * 2} bytes");

			logger.LogTrace($"A->{bltapt:X6} %{(int)bltamod,9} >> {bltcon0 >> 12,2} {(((bltcon0 >> 11) & 1) != 0 ? "on " : "off")} A {bltadat:X4}");
			logger.LogTrace($"B->{bltbpt:X6} %{(int)bltbmod,9} >> {bltcon1 >> 12,2} {(((bltcon0 >> 10) & 1) != 0 ? "on " : "off")} B {bltbdat:X4}");
			logger.LogTrace($"C->{bltcpt:X6} %{(int)bltcmod,9} >> -- {(((bltcon0 >> 9) & 1) != 0 ? "on " : "off")} C {bltcdat:X4}");
			logger.LogTrace($"D->{bltdpt:X6} %{(int)bltdmod,9} >> -- {(((bltcon0 >> 8) & 1) != 0 ? "on " : "off")} D {bltddat:X4}");
			logger.LogTrace($"M {bltafwm.ToBin(16)} {bltalwm.ToBin(16)}");
			logger.LogTrace($"cookie: {bltcon0 & 0xff:X2} {((bltcon1 & 2) != 0 ? "descending" : "ascending")}");
			//logger.LogTrace("ABC");
			//if ((bltcon0 & 0x01) != 0) logger.LogTrace("000");
			//if ((bltcon0 & 0x02) != 0) logger.LogTrace("001");
			//if ((bltcon0 & 0x04) != 0) logger.LogTrace("010");
			//if ((bltcon0 & 0x08) != 0) logger.LogTrace("011");
			//if ((bltcon0 & 0x10) != 0) logger.LogTrace("100");
			//if ((bltcon0 & 0x20) != 0) logger.LogTrace("101");
			//if ((bltcon0 & 0x40) != 0) logger.LogTrace("110");
			//if ((bltcon0 & 0x80) != 0) logger.LogTrace("111");
			if ((bltcon1 & (3 << 3)) != 0 && (bltcon1 & (1u << 1)) != 0)
				logger.LogTrace($"Fill EFE:{(bltcon1 >> 4) & 1} IFE:{(bltcon1 >> 3) & 1} FCI:{(bltcon1 >> 2) & 1}");
		}

		private void DumpBlitterState(uint insaddr, uint length)
		{
			logger.LogTrace($"LINE! @{insaddr:X6} Len:{length}");

			logger.LogTrace($"A->{bltapt:X6} %{(int)bltamod,9} >> {bltcon0 >> 12,2} {(((bltcon0 >> 11) & 1) != 0 ? "on " : "off")} A {bltadat:X4}");
			logger.LogTrace($"B->{bltbpt:X6} %{(int)bltbmod,9} >> {bltcon1 >> 12,2} {(((bltcon0 >> 10) & 1) != 0 ? "on " : "off")} B {bltbdat:X4}");
			logger.LogTrace($"C->{bltcpt:X6} %{(int)bltcmod,9} >> -- {(((bltcon0 >> 9) & 1) != 0 ? "on " : "off")} C {bltcdat:X4}");
			logger.LogTrace($"D->{bltdpt:X6} %{(int)bltdmod,9} >> -- {(((bltcon0 >> 8) & 1) != 0 ? "on " : "off")} D {bltddat:X4}");
			logger.LogTrace($"M {bltafwm.ToBin(16)} {bltalwm.ToBin(16)}");
			logger.LogTrace($"X {bltcon0.ToBin(16)} {bltcon1.ToBin(16)}");
			logger.LogTrace($"SIGN {((bltcon1 & 64) != 0 ? "1" : "0")}");
			logger.LogTrace($"OVF  {((bltcon1 & 32) != 0 ? "1" : "0")}");
			logger.LogTrace($"SUD  {((bltcon1 & 16) != 0 ? "1" : "0")}");
			logger.LogTrace($"SUL  {((bltcon1 & 8) != 0 ? "1" : "0")}");
			logger.LogTrace($"AUL  {((bltcon1 & 4) != 0 ? "1" : "0")}");
			logger.LogTrace($"SING {((bltcon1 & 2) != 0 ? "1" : "0")}");
			logger.LogTrace($"cookie: {bltcon0 & 0xff:X2}");
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
		}
	}
}
