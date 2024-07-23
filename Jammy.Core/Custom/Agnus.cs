using System.Collections.Generic;
using System.Linq;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/


/*
	//227 (E3) hclocks @ 3.5MHz,
	// in lowres, 1 pixel is 8 clocks, hires pixel is 4 clocks, shres pixel is 2 clocks
	//                      DIW  1 pixel resolution
	//DIW normally               0x81 to 0x1C1 = 129->449 0x140 = 320 "clipping window" always in lowres pixels
	//                                     /2   64.5->224.5
	//                                     /4  32.25->112.25

	// 0x81/2-8.5 = 0x38   DIW/2-x = DDF
	// 0x81/2-4.5 = 0x3C
	//                      DDF  4 pixel resolution    x4 224->832
	// looks like these values tie in with the horizontal colour clocks
	//                      DDF  lowres  0x38 to 0xD0 =   56->208  0x98 = 152 - in low res mode, 152/8+1 = 19+1 = 20 fetches
	//                      DDF  hires   0x3c to 0xD4 =   60->212  0x98 = 152 - in hi res mode,  152/4+2 = 38+2 = 40 fetches
	//                           shres   0x3c to 0xD4 =   60->212  0x98 = 152 - in shres  mode,  152/2+4 = 76+4 = 80 fetches
	//low res means we'll fetch 20 words per scanline per plane, and 40 in hi-res

	//question: how do DIW and DDF values tie into colour clock?

	//every 8 colour clocks, fetch 16 pixels
	//so in 227 colour clocks, there'd be a max

	During a horizontal scan line (about 63 microseconds), there are 227.5
	"color clocks", or memory access cycles.  A memory cycle is approximately
	280 ns in duration.  The total of 227.5 cycles per horizontal line
	includes both display time and non-display time.  Of this total time, 226
	cycles are available to be allocated to the various devices that need
	memory access.

	The time-slot allocation per horizontal line is:

		  4 cycles for memory refresh
		  3 cycles for disk DMA
		  4 cycles for audio DMA (2 bytes per channel)
		 16 cycles for sprite DMA (2 words per channel)
		 80 cycles for bitplane DMA (even- or odd-numbered slots
			  according to the display size used)
	   =107 total
*/

namespace Jammy.Core.Custom;

public class Agnus : IAgnus
{
	private readonly IChipsetClock clock;
	private IDMA memory;
	private readonly ICopper copper;
	private readonly IDenise denise;
	private readonly IBlitter blitter;
	private readonly IInterrupt interrupt;
	private readonly IChipRAM chipRam;
	private readonly ITrapdoorRAM trapdoorRam;
	private readonly EmulationSettings settings;
	private readonly ILogger<Agnus> logger;

	//sprite DMA starts at 0x18, but can be eaten into by bitmap DMA
	//normal bitmap DMA start at 0x38, overscan at 0x30, Menace starts at 0x28
	public const int DMA_START = 0x18;

	//bitmap DMA ends at 0xD8, with 8 slots after that
	public const int DMA_END = 0xF0;

	public Agnus(IChipsetClock clock, ICopper copper, IDenise denise, IBlitter blitter, IInterrupt interrupt,
		IChipRAM chipRAM, ITrapdoorRAM trapdoorRAM,
		IOptions<EmulationSettings> settings, ILogger<Agnus> logger)
	{
		this.clock = clock;
		this.copper = copper;
		this.denise = denise;
		this.blitter = blitter;
		this.interrupt = interrupt;
		chipRam = chipRAM;
		trapdoorRam = trapdoorRAM;
		this.settings = settings.Value;
		this.logger = logger;
	}

	public void Reset()
	{
		for (int i = 0; i < 8; i++)
			spriteState[i] = SpriteState.Idle;

		lineState = DMALineState.LineStart;
	}

	public void Emulate(ulong cycles)
	{
		//clock.WaitForTick();

		RunAgnusTick();

		if (clock.EndOfLine())
		{
			EndAgnusLine();
			return;
		}

		if (clock.EndOfFrame())
		{
			interrupt.AssertInterrupt(Interrupt.VERTB);

			for (int i = 0; i < 8; i++)
				spriteState[i] = SpriteState.Idle;
		}
		//clock.Ack();
	}

	public void Init(IDMA dma)
	{
		memory = dma;
	}

	private enum DMALineState
	{
		LineStart,
		Fetching,
		LineComplete,
		LineTerminated
	}

	private int planes;
	private int diwstrtv = 0;
	private int diwstopv = 0;
	private ushort ddfstrtfix = 0;
	private ushort ddfstopfix = 0;
	private int pixmod;
	private DMALineState lineState;
	
	private void RunAgnusTick()
	{
		////debugging
		//if (currentLine == cdbg.dbugLine)
		//{
		//	cdbg.fetch[h] = '-';
		//	cdbg.write[h] = '-';
		//}
		////debugging

		if (clock.HorizontalPos < 0x18)
		{
			if ((clock.HorizontalPos & 1) == 0)
			{
				memory.NoDMA(DMASource.Agnus);
				return;
			}

			switch (clock.HorizontalPos)
			{
				case 1: memory.NeedsDMA(DMASource.Agnus); break;
				case 3: memory.NeedsDMA(DMASource.Agnus); break;
				case 5: memory.NeedsDMA(DMASource.Agnus); break;
				case 7: if (memory.IsDMAEnabled(DMA.DSKEN)) memory.NeedsDMA(DMASource.Agnus); break;//actually Disk DMA
				case 9: if (memory.IsDMAEnabled(DMA.DSKEN)) memory.NeedsDMA(DMASource.Agnus); break;//actually Disk DMA
				case 0xB: if (memory.IsDMAEnabled(DMA.DSKEN)) memory.NeedsDMA(DMASource.Agnus); break;//actually Disk DMA
				case 0xD: if (memory.IsDMAEnabled(DMA.AUD0EN)) memory.NeedsDMA(DMASource.Agnus); break;//actually Audio 0 DMA
				case 0xF: if (memory.IsDMAEnabled(DMA.AUD1EN)) memory.NeedsDMA(DMASource.Agnus); break;//actually Audio 1 DMA
				case 0x11: if (memory.IsDMAEnabled(DMA.AUD2EN)) memory.NeedsDMA(DMASource.Agnus); break;//actually Audio 2 DMA
				case 0x13: if (memory.IsDMAEnabled(DMA.AUD3EN)) memory.NeedsDMA(DMASource.Agnus); break;//actually Audio 3 DMA
				case 0x15: if (memory.IsDMAEnabled(DMA.SPREN)) RunSpriteDMA(0); break;
				case 0x17: if (memory.IsDMAEnabled(DMA.SPREN)) RunSpriteDMA(1); break;
			}
			return;
		}

		bool fetched = false;

		//is it the visible area, vertically?
		if (clock.VerticalPos < diwstrtv || clock.VerticalPos >= diwstopv)
		{
			//tell Denise to stop processing pixels and start blanking
			denise.ExitVisibleArea();
			goto noBitplaneDMA;
		}

		//tell Denise to stop blanking and start processing pixel data
		denise.EnterVisibleArea();

		//if (currentLine == cdbg.dbugLine)
		//	cdbg.write[h] = cdbg.fetch[h] = ':';

		//is it time to do bitplane DMA?
		//when h >= ddfstrt, bitplanes are fetching. one plane per cycle, until all the planes are fetched
		//bitplane DMA is ON
		if (clock.HorizontalPos >= ddfstrtfix /*+ cdbg.ddfSHack*/ && clock.HorizontalPos < ddfstopfix /*+ cdbg.ddfEHack*/ &&
			(lineState == DMALineState.Fetching || lineState == DMALineState.LineStart))
		{
			if (memory.IsDMAEnabled(DMA.BPLEN))
				fetched = CopperBitplaneFetch((int)clock.HorizontalPos);
			lineState = DMALineState.Fetching;
		}

		if (clock.HorizontalPos >= ddfstopfix /*+ cdbg.ddfEHack*/ && lineState == DMALineState.Fetching)
		{
			lineState = DMALineState.LineComplete;
		}

		if (fetched)
			return;

noBitplaneDMA:

		//can we use the non-bitplane DMA for something else?

		if ((clock.HorizontalPos < 0x34) && (clock.HorizontalPos & 1) != 0)
		{
			if (memory.IsDMAEnabled(DMA.SPREN))
			{
				switch (clock.HorizontalPos)
				{
					case 0x19: RunSpriteDMA(2); break;
					case 0x1B: RunSpriteDMA(3); break;
					case 0x1D: RunSpriteDMA(4); break;
					case 0x1F: RunSpriteDMA(5); break;
					case 0x21: RunSpriteDMA(6); break;
					case 0x23: RunSpriteDMA(7); break;
					case 0x25: RunSpriteDMA(8); break;
					case 0x27: RunSpriteDMA(9); break;
					case 0x29: RunSpriteDMA(10); break;
					case 0x2B: RunSpriteDMA(11); break;
					case 0x2D: RunSpriteDMA(12); break;
					case 0x2F: RunSpriteDMA(13); break;
					case 0x31: RunSpriteDMA(14); break;
					case 0x33: RunSpriteDMA(15); break;
				}
			}
			return;
		}
		memory.NoDMA(DMASource.Agnus);
	}

	private static readonly uint[] fetchLo = [8, 4, 6, 2, 7, 3, 5, 1];
	private static readonly uint[] fetchHi = [4, 2, 3, 1, 4, 2, 3, 1];
	private static readonly uint[] fetchSh = [2, 1, 2, 1, 2, 1, 2, 1];
	private static readonly uint[] fetchF3 = [8, 4, 6, 2, 7, 3, 5, 1, 10,10,10,10,10,10,10,10, 10,10,10,10,10,10,10,10, 10,10,10,10,10,10,10,10];
	private static readonly uint[] fetchF2 = [8, 4, 6, 2, 7, 3, 5, 1, 10,10,10,10,10,10,10,10];

	private bool CopperBitplaneFetch(int h)
	{
		int planeIdx;
		uint plane;

		if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
		{
			planeIdx = (h - ddfstrtfix) % pixmod;

			if ((bplcon0 & (uint)Denise.BPLCON0.HiRes) != 0)
				plane = fetchHi[planeIdx] - 1;
			else if ((bplcon0 & (uint)Denise.BPLCON0.SuperHiRes) != 0)
				plane = fetchSh[planeIdx] - 1;
			else
				plane = fetchLo[planeIdx] - 1;
		}
		else if ((fmode & 3) == 3)
		{
			planeIdx = (h - ddfstrtfix) % pixmod;
			plane = fetchF3[planeIdx] - 1;
		}
		else
		{
			planeIdx = (h - ddfstrtfix) % pixmod;
			plane = fetchF2[planeIdx] - 1;
		}

		if (plane < planes)
		{
			if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
			{
				memory.Read(DMASource.Agnus, bplpt[plane], DMA.BPLEN, Size.Word, ChipRegs.BPL1DAT+plane*2);
				bplpt[plane] += 2;
			}
			else if ((fmode & 3) == 3)
			{
				memory.Read(DMASource.Agnus, bplpt[plane], DMA.BPLEN, Size.QWord, ChipRegs.BPL1DAT + plane * 2);
				bplpt[plane] += 8;
			}
			else
			{
				memory.Read(DMASource.Agnus, bplpt[plane], DMA.BPLEN, Size.Long, ChipRegs.BPL1DAT + plane * 2);
				bplpt[plane] += 4;
			}

			//we just filled BPL0DAT
			if (plane == 0)
			{
				denise.WriteBitplanes(bpldat);

				//if (currentLine == cdbg.dbugLine)
				//{
				//	cdbg.write[h] = 'x';
				//	cdbg.dma++;
				//}
			}
			else
			{
				//if (currentLine == cdbg.dbugLine)
				//	cdbg.write[h] = '.';
			}

			//if (currentLine == cdbg.dbugLine)
			//	cdbg.fetch[h] = Convert.ToChar(plane + 48 + 1);
			return true;
		}
		else
		{
			//if (currentLine == cdbg.dbugLine)
			//	cdbg.fetch[h] = '+';
		}
		return false;
	}

	private enum SpriteState
	{
		Idle = 0,
		Waiting,
		Fetching,
	}

	private SpriteState[] spriteState = new SpriteState[8];

	private void RunSpriteDMA(uint slot)
	{
		uint s = slot >> 1;

		if (spriteState[s] == SpriteState.Waiting)
		{
			int vstart = sprpos[s] >> 8;
			vstart += (sprctl[s] & 4) << 6; //bit 2 is high bit of vstart
			if (clock.VerticalPos == vstart)
			{
				spriteState[s] = SpriteState.Fetching;
			}
		}
		else if (spriteState[s] == SpriteState.Fetching)
		{
			int vstop = sprctl[s] >> 8;
			vstop += (sprctl[s] & 2) << 7; //bit 1 is high bit of vstop
			if (clock.VerticalPos == vstop)
				spriteState[s] = SpriteState.Idle;
		}

		if ((slot & 1) == 0)
		{
			if (spriteState[s] == SpriteState.Idle)
			{
				memory.Read(DMASource.Agnus, sprpt[s], DMA.SPREN, Size.Word, ChipRegs.SPR0POS+s*8);
			}
			else if (spriteState[s] == SpriteState.Fetching)
			{
				memory.Read(DMASource.Agnus, sprpt[s], DMA.SPREN, Size.Word, ChipRegs.SPR0DATA+s*8);
			}
		}
		else
		{
			if (spriteState[s] == SpriteState.Idle)
			{
				memory.Read(DMASource.Agnus, sprpt[s] + 2, DMA.SPREN, Size.Word, ChipRegs.SPR0CTL+s*8);

				if (sprpos[s] == 0 && sprctl[s] == 0)
				{
					spriteState[s] = SpriteState.Idle;
				}
				else
				{
					spriteState[s] = SpriteState.Waiting;
					sprpt[s] += 4;
				}
			}
			else if (spriteState[s] == SpriteState.Fetching)
			{
				memory.Read(DMASource.Agnus, sprpt[s] + 2, DMA.SPREN, Size.Word, ChipRegs.SPR0DATB+s*8);
				sprpt[s] += 4;
			}
		}
	}

	private void UpdateBPLCON0()
	{
		planes = (bplcon0 >> 12) & 7;
		if (settings.ChipSet == ChipSet.AGA)
		{
			if (planes == 0 && (bplcon0 & (1 << 4)) != 0)
				planes = 8;
		}
	}

	private void UpdateDIWSTRT()
	{
		//diwstrth = diwstrt & 0xff;
		diwstrtv = diwstrt >> 8;
	}

	private void UpdateDIWSTOP()
	{
		//diwstoph = (diwstop & 0xff) | 0x100;
		diwstopv = (diwstop >> 8) | (((diwstop & 0x8000) >> 7) ^ 0x100);
	}

	private void UpdateDIWHIGH()
	{
		//if diwhigh is written, the 'magic' bits are overwritten
		if (diwhigh != 0)
		{
			//diwstrth |= (diwhigh & 0b1_00000) << 3;
			diwstrtv |= (diwhigh & 0b111) << 8;

			//diwstoph &= 0xff;
			//diwstoph |= (diwhigh & 0b1_00000_00000000) >> 5;
			diwstopv &= 0xff;
			diwstopv |= (diwhigh & 0b111_00000000);

			//todo: there are also an extra two bottom bits for strth/stoph
		}
	}

	private void UpdateDDF()
	{
		//ddfstrt->ddfstop
		//HRM says DDFSTRT = DDFSTOP - (4 * (word count - 2)) for high resolution
		//workbench
		//KS2.04 3C->D4 => 3C = D4 - (4 * (40-2)) = D4-98 = 3C
		//KS1.3  3C->D0 => 3C = D0 - (4 * (40-2)) = D0-98 = 38
		//kickstart
		//KS2.04 40->D0 => 40 = D0 - (4 * (40-2)) = D0-98 = 38

		//https://eab.abime.net/showthread.php?t=111329

		//how many pixels should be fetched per clock in the current mode?
		if ((bplcon0 & (uint)Denise.BPLCON0.HiRes) != 0)
		{
			//4 colour clocks, fetch 16 pixels
			//1 colour clock, draw 4 pixel
			//pixelLoop = 4;
			pixmod = 4;

			//ddfstrtfix = (ushort)(ddfstrt & 0xfffc);
			ddfstrtfix = ddfstrt;

			if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
				FetchWidth(ddfstrt, ddfstop, OCS, HIRES, 0);
			}
			else if ((fmode & 3) == 3)
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 0xf) >> 4) + 1) << 4));
				//FetchWidth(ddfstrt, ddfstop, AGA, HIRES, 3);
				pixmod = 16;
			}
			else
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
				//FetchWidth(ddfstrt, ddfstop, AGA, HIRES, 2);
				pixmod = 8;
			}
		}
		else if ((bplcon0 & (uint)Denise.BPLCON0.SuperHiRes) != 0)
		{
			//2 colour clocks, fetch 16 pixels
			//1 colour clock, draw 8 pixel
			ddfstrtfix = ddfstrt;
			//pixelLoop = 8;
			pixmod = 2;

			if (settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
				//FetchWidth(ddfstrt, ddfstop, ECS, SHRES, 0)>>3;
			}
			else if ((fmode & 3) == 3)
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
				//FetchWidth(ddfstrt, ddfstop, AGA, SHRES, 3)>>3;
				pixmod = 8;
			}
			else
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
				//FetchWidth(ddfstrt, ddfstop, AGA, SHRES, 2)>>3;
				pixmod = 4;
			}
		}
		else
		{
			//8 colour clocks, fetch 16 pixels
			//1 colour clock, draw 2 pixel
			//pixelLoop = 2;
			pixmod = 8;

			//low-res ddfstrt ignores bit 2
			ddfstrtfix = ddfstrt;//(ushort)(ddfstrt & 0xfff8);

			if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
				//FetchWidth(ddfstrt, ddfstop, OCS, LORES, 0);
			}
			else if ((fmode & 3) == 3)
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
				//FetchWidth(ddfstrt, ddfstop, AGA, LORES, 3);
				pixmod = 32;
			}
			else
			{
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
				//FetchWidth(ddfstrt, ddfstop, AGA, LORES, 2);
				pixmod = 16;
			}
		}
	}


	private ulong[] bpldat = new ulong[8];
	private uint[] bplpt = new uint[8];
	private ushort diwstrt;
	private ushort diwstop;
	private ushort bplcon0;
	private ushort ddfstrt;
	private ushort ddfstop;
	private uint bpl1mod;
	private uint bpl2mod;
	private uint[] sprpt = new uint[8];
	private ushort[] sprpos = new ushort[8];
	private ushort[] sprctl = new ushort[8];
	private ushort[] sprdata = new ushort[8];
	private ushort[] sprdatb = new ushort[8];

	//ECS/AGA
	private ushort vbstrt;
	private ushort vbstop;
	private ushort vsstop;
	private ushort vsstrt;
	private ushort diwhigh;
	private ushort vtotal;
	private ushort fmode;
	private ushort beamcon0;

	public ushort Read(uint insaddr, uint address)
	{
		ushort value = 0;
		//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

		//copper.Read(insaddr, address);//all copper registers are only writable
		if (address == ChipRegs.CLXDAT)
			return denise.Read(insaddr, address);
		//blitter.Read(insaddr, address);//all blitter registers are only writable

		switch (address)
		{
			case ChipRegs.VPOSR:
				value = (ushort)((clock.VerticalPos >> 8) & 1);//todo: different on hires chips
				if (settings.VideoFormat == VideoFormat.NTSC)
					value |= (ushort)((clock.VerticalPos & 1) << 7);//toggle LOL each alternate line (NTSC only)

				//if we're in interlace mode
				if ((bplcon0 & (1 << 2)) != 0)
				{
					value |= (ushort)((clock.VerticalPos & 1) << 15);//set LOF=1/0 on alternate frames
				}
				else
				{
					value |= 1 << 15;//set LOF=1
				}

				value &= 0x80ff;
				switch (settings.ChipSet)
				{
					case ChipSet.AGA: value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC ? 0x3300 : 0x2300); break; //Alice
					case ChipSet.ECS: value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC ? 0x3100 : 0x2100); break; //Fat Agnus
					case ChipSet.OCS: value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC ? 0x0000 : 0x1000); break;//OCS
				}
				break;

			case ChipRegs.VHPOSR: value = (ushort)((clock.VerticalPos << 8) | (clock.HorizontalPos & 0x00ff)); break;

			//bitplane specific

			case ChipRegs.BPL1MOD: value = (ushort)bpl1mod; break;
			case ChipRegs.BPL2MOD: value = (ushort)bpl2mod; break;

			case ChipRegs.BPLCON0: value = bplcon0; break;

			case ChipRegs.BPL1DAT: value = (ushort)bpldat[0]; break;
			case ChipRegs.BPL2DAT: value = (ushort)bpldat[1]; break;
			case ChipRegs.BPL3DAT: value = (ushort)bpldat[2]; break;
			case ChipRegs.BPL4DAT: value = (ushort)bpldat[3]; break;
			case ChipRegs.BPL5DAT: value = (ushort)bpldat[4]; break;
			case ChipRegs.BPL6DAT: value = (ushort)bpldat[5]; break;
			case ChipRegs.BPL7DAT: value = (ushort)bpldat[6]; break;
			case ChipRegs.BPL8DAT: value = (ushort)bpldat[7]; break;

			case ChipRegs.BPL1PTL: value = (ushort)bplpt[0]; break;
			case ChipRegs.BPL1PTH: value = (ushort)(bplpt[0] >> 16); break;
			case ChipRegs.BPL2PTL: value = (ushort)bplpt[1]; break;
			case ChipRegs.BPL2PTH: value = (ushort)(bplpt[1] >> 16); break;
			case ChipRegs.BPL3PTL: value = (ushort)bplpt[2]; break;
			case ChipRegs.BPL3PTH: value = (ushort)(bplpt[2] >> 16); break;
			case ChipRegs.BPL4PTL: value = (ushort)bplpt[3]; break;
			case ChipRegs.BPL4PTH: value = (ushort)(bplpt[3] >> 16); break;
			case ChipRegs.BPL5PTL: value = (ushort)bplpt[4]; break;
			case ChipRegs.BPL5PTH: value = (ushort)(bplpt[4] >> 16); break;
			case ChipRegs.BPL6PTL: value = (ushort)bplpt[5]; break;
			case ChipRegs.BPL6PTH: value = (ushort)(bplpt[5] >> 16); break;
			case ChipRegs.BPL7PTL: value = (ushort)bplpt[6]; break;
			case ChipRegs.BPL7PTH: value = (ushort)(bplpt[6] >> 16); break;
			case ChipRegs.BPL8PTL: value = (ushort)bplpt[7]; break;
			case ChipRegs.BPL8PTH: value = (ushort)(bplpt[7] >> 16); break;

			case ChipRegs.DIWSTRT: value = diwstrt; break;
			case ChipRegs.DIWSTOP: value = diwstop; break;
			case ChipRegs.DIWHIGH: value = diwhigh; break;

			case ChipRegs.DDFSTRT: value = ddfstrt; break;
			case ChipRegs.DDFSTOP: value = ddfstop; break;

			case ChipRegs.SPR0PTL: value = (ushort)sprpt[0]; break;
			case ChipRegs.SPR0PTH: value = (ushort)(sprpt[0] >> 16); break;
			case ChipRegs.SPR0POS: value = sprpos[0]; break;
			case ChipRegs.SPR0CTL: value = sprctl[0]; break;
			case ChipRegs.SPR0DATA: value = sprdata[0]; break;
			case ChipRegs.SPR0DATB: value = sprdatb[0]; break;

			case ChipRegs.SPR1PTL: value = (ushort)sprpt[1]; break;
			case ChipRegs.SPR1PTH: value = (ushort)(sprpt[1] >> 16); break;
			case ChipRegs.SPR1POS: value = sprpos[1]; break;
			case ChipRegs.SPR1CTL: value = sprctl[1]; break;
			case ChipRegs.SPR1DATA: value = sprdata[1]; break;
			case ChipRegs.SPR1DATB: value = sprdatb[1]; break;

			case ChipRegs.SPR2PTL: value = (ushort)sprpt[2]; break;
			case ChipRegs.SPR2PTH: value = (ushort)(sprpt[2] >> 16); break;
			case ChipRegs.SPR2POS: value = sprpos[2]; break;
			case ChipRegs.SPR2CTL: value = sprctl[2]; break;
			case ChipRegs.SPR2DATA: value = sprdata[2]; break;
			case ChipRegs.SPR2DATB: value = sprdatb[2]; break;

			case ChipRegs.SPR3PTL: value = (ushort)sprpt[3]; break;
			case ChipRegs.SPR3PTH: value = (ushort)(sprpt[3] >> 16); break;
			case ChipRegs.SPR3POS: value = sprpos[3]; break;
			case ChipRegs.SPR3CTL: value = sprctl[3]; break;
			case ChipRegs.SPR3DATA: value = sprdata[3]; break;
			case ChipRegs.SPR3DATB: value = sprdatb[3]; break;

			case ChipRegs.SPR4PTL: value = (ushort)sprpt[4]; break;
			case ChipRegs.SPR4PTH: value = (ushort)(sprpt[4] >> 16); break;
			case ChipRegs.SPR4POS: value = sprpos[4]; break;
			case ChipRegs.SPR4CTL: value = sprctl[4]; break;
			case ChipRegs.SPR4DATA: value = sprdata[4]; break;
			case ChipRegs.SPR4DATB: value = sprdatb[4]; break;

			case ChipRegs.SPR5PTL: value = (ushort)sprpt[5]; break;
			case ChipRegs.SPR5PTH: value = (ushort)(sprpt[5] >> 16); break;
			case ChipRegs.SPR5POS: value = sprpos[5]; break;
			case ChipRegs.SPR5CTL: value = sprctl[5]; break;
			case ChipRegs.SPR5DATA: value = sprdata[5]; break;
			case ChipRegs.SPR5DATB: value = sprdatb[5]; break;

			case ChipRegs.SPR6PTL: value = (ushort)sprpt[6]; break;
			case ChipRegs.SPR6PTH: value = (ushort)(sprpt[6] >> 16); break;
			case ChipRegs.SPR6POS: value = sprpos[6]; break;
			case ChipRegs.SPR6CTL: value = sprctl[6]; break;
			case ChipRegs.SPR6DATA: value = sprdata[6]; break;
			case ChipRegs.SPR6DATB: value = sprdatb[6]; break;

			case ChipRegs.SPR7PTL: value = (ushort)sprpt[7]; break;
			case ChipRegs.SPR7PTH: value = (ushort)(sprpt[7] >> 16); break;
			case ChipRegs.SPR7POS: value = sprpos[7]; break;
			case ChipRegs.SPR7CTL: value = sprctl[7]; break;
			case ChipRegs.SPR7DATA: value = sprdata[7]; break;
			case ChipRegs.SPR7DATB: value = sprdatb[7]; break;

			//ECS/AGA
			case ChipRegs.VBSTRT: value = vbstrt; break;
			case ChipRegs.VBSTOP: value = vbstop; break;
			case ChipRegs.VSSTOP: value = vsstop; break;
			case ChipRegs.VSSTRT: value = vsstrt; break;
			case ChipRegs.VTOTAL: value = vtotal; break;
			case ChipRegs.FMODE: value = fmode; break;
			case ChipRegs.BEAMCON0: value = beamcon0; break;
		}

		return value;
	}

	public void Write(uint insaddr, uint address, ushort value)
	{
		copper.Write(insaddr, address, value);
		denise.Write(insaddr, address, value);
		blitter.Write(insaddr, address, value);

		switch (address)
		{
			//bitplane specific

			case ChipRegs.BPL1MOD: bpl1mod = (uint)(short)value & 0xfffffffe; break;
			case ChipRegs.BPL2MOD: bpl2mod = (uint)(short)value & 0xfffffffe; break;

			case ChipRegs.BPLCON0: bplcon0 = value; UpdateBPLCON0(); UpdateDDF(); break;

			case ChipRegs.BPL1DAT: bpldat[0] = value; break;
			case ChipRegs.BPL2DAT: bpldat[1] = value; break;
			case ChipRegs.BPL3DAT: bpldat[2] = value; break;
			case ChipRegs.BPL4DAT: bpldat[3] = value; break;
			case ChipRegs.BPL5DAT: bpldat[4] = value; break;
			case ChipRegs.BPL6DAT: bpldat[5] = value; break;
			case ChipRegs.BPL7DAT: bpldat[6] = value; break;
			case ChipRegs.BPL8DAT: bpldat[7] = value; break;

			case ChipRegs.BPL1PTL: bplpt[0] = (bplpt[0] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.BPL1PTH: bplpt[0] = (bplpt[0] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.BPL2PTL: bplpt[1] = (bplpt[1] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.BPL2PTH: bplpt[1] = (bplpt[1] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.BPL3PTL: bplpt[2] = (bplpt[2] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.BPL3PTH: bplpt[2] = (bplpt[2] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.BPL4PTL: bplpt[3] = (bplpt[3] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.BPL4PTH: bplpt[3] = (bplpt[3] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.BPL5PTL: bplpt[4] = (bplpt[4] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.BPL5PTH: bplpt[4] = (bplpt[4] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.BPL6PTL: bplpt[5] = (bplpt[5] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.BPL6PTH: bplpt[5] = (bplpt[5] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.BPL7PTL: bplpt[6] = (bplpt[6] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.BPL7PTH: bplpt[6] = (bplpt[6] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.BPL8PTL: bplpt[7] = (bplpt[7] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.BPL8PTH: bplpt[7] = (bplpt[7] & 0x0000ffff) | ((uint)value << 16); break;

			case ChipRegs.DIWSTRT: diwstrt = value; diwhigh = 0; UpdateDIWSTRT(); break;
			case ChipRegs.DIWSTOP: diwstop = value; diwhigh = 0; UpdateDIWSTOP(); break;
			case ChipRegs.DIWHIGH: diwhigh = value; UpdateDIWHIGH(); break;

			case ChipRegs.DDFSTRT:
				ddfstrt = (ushort)(value & (settings.ChipSet == ChipSet.OCS ? 0xfc : 0xfe));
				lineState = DMALineState.LineTerminated;
				UpdateDDF();
				break;
			case ChipRegs.DDFSTOP:
				ddfstop = (ushort)(value & (settings.ChipSet == ChipSet.OCS ? 0xfc : 0xfe));
				lineState = DMALineState.LineTerminated;
				UpdateDDF();
				break;

			case ChipRegs.SPR0PTL: sprpt[0] = (sprpt[0] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR0PTH: sprpt[0] = (sprpt[0] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.SPR0POS: sprpos[0] = value; break;
			case ChipRegs.SPR0CTL: sprctl[0] = value; break;
			case ChipRegs.SPR0DATA: sprdata[0] = value; break;
			case ChipRegs.SPR0DATB: sprdatb[0] = value; break;

			case ChipRegs.SPR1PTL: sprpt[1] = (sprpt[1] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR1PTH: sprpt[1] = (sprpt[1] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.SPR1POS: sprpos[1] = value; break;
			case ChipRegs.SPR1CTL: sprctl[1] = value; break;
			case ChipRegs.SPR1DATA: sprdata[1] = value; break;
			case ChipRegs.SPR1DATB: sprdatb[1] = value; break;

			case ChipRegs.SPR2PTL: sprpt[2] = (sprpt[2] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR2PTH: sprpt[2] = (sprpt[2] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.SPR2POS: sprpos[2] = value; break;
			case ChipRegs.SPR2CTL: sprctl[2] = value; break;
			case ChipRegs.SPR2DATA: sprdata[2] = value; break;
			case ChipRegs.SPR2DATB: sprdatb[2] = value; break;

			case ChipRegs.SPR3PTL: sprpt[3] = (sprpt[3] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR3PTH: sprpt[3] = (sprpt[3] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.SPR3POS: sprpos[3] = value; break;
			case ChipRegs.SPR3CTL: sprctl[3] = value; break;
			case ChipRegs.SPR3DATA: sprdata[3] = value; break;
			case ChipRegs.SPR3DATB: sprdatb[3] = value; break;

			case ChipRegs.SPR4PTL: sprpt[4] = (sprpt[4] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR4PTH: sprpt[4] = (sprpt[4] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.SPR4POS: sprpos[4] = value; break;
			case ChipRegs.SPR4CTL: sprctl[4] = value; break;
			case ChipRegs.SPR4DATA: sprdata[4] = value; break;
			case ChipRegs.SPR4DATB: sprdatb[4] = value; break;

			case ChipRegs.SPR5PTL: sprpt[5] = (sprpt[5] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR5PTH: sprpt[5] = (sprpt[5] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.SPR5POS: sprpos[5] = value; break;
			case ChipRegs.SPR5CTL: sprctl[5] = value; break;
			case ChipRegs.SPR5DATA: sprdata[5] = value; break;
			case ChipRegs.SPR5DATB: sprdatb[5] = value; break;

			case ChipRegs.SPR6PTL: sprpt[6] = (sprpt[6] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR6PTH: sprpt[6] = (sprpt[6] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.SPR6POS: sprpos[6] = value; break;
			case ChipRegs.SPR6CTL: sprctl[6] = value; break;
			case ChipRegs.SPR6DATA: sprdata[6] = value; break;
			case ChipRegs.SPR6DATB: sprdatb[6] = value; break;

			case ChipRegs.SPR7PTL: sprpt[7] = (sprpt[7] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR7PTH: sprpt[7] = (sprpt[7] & 0x0000ffff) | ((uint)value << 16); break;
			case ChipRegs.SPR7POS: sprpos[7] = value; break;
			case ChipRegs.SPR7CTL: sprctl[7] = value; break;
			case ChipRegs.SPR7DATA: sprdata[7] = value; break;
			case ChipRegs.SPR7DATB: sprdatb[7] = value; break;

			//ECS/AGA
			case ChipRegs.VBSTRT: vbstrt = value; break;
			case ChipRegs.VBSTOP: vbstop = value; break;
			case ChipRegs.VSSTOP: vsstop = value; break;
			case ChipRegs.VSSTRT: vsstrt = value; break;
			case ChipRegs.VTOTAL: vtotal = value; break;
			case ChipRegs.FMODE: fmode = value; break;
			case ChipRegs.BEAMCON0: beamcon0 = value; break;
		}
	}

	public void WriteWide(uint address, ulong value)
	{
		switch (address)
		{
			case ChipRegs.BPL1DAT: bpldat[0] = value; break;
			case ChipRegs.BPL2DAT: bpldat[1] = value; break;
			case ChipRegs.BPL3DAT: bpldat[2] = value; break;
			case ChipRegs.BPL4DAT: bpldat[3] = value; break;
			case ChipRegs.BPL5DAT: bpldat[4] = value; break;
			case ChipRegs.BPL6DAT: bpldat[5] = value; break;
			case ChipRegs.BPL7DAT: bpldat[6] = value; break;
			case ChipRegs.BPL8DAT: bpldat[7] = value; break;
		}
	}

	private void EndAgnusLine()
	{
		//next horizontal line, and we did some fetching this line, add on the modulos
		if (clock.VerticalPos >= diwstrtv && clock.VerticalPos < diwstopv && lineState == DMALineState.LineComplete)
		{
			for (int i = 0; i < planes; i++)
			{
				bplpt[i] += ((i & 1) == 0) ? bpl1mod : bpl2mod;
				bplpt[i] &= 0xfffffffe;
			}
			lineState = DMALineState.LineTerminated;
		}
	}

	//https://eab.abime.net/showthread.php?t=111329
	private const int OCS = 0;
	private const int ECS = 1;
	private const int AGA = 2;

	private const int LORES = 0;
	private const int HIRES = 1;
	private const int SHRES = 2;

	private int FetchWidth(int DDFSTRT, int DDFSTOP, int chipset, int res, int FMODE)
	{
		// validate bits
		FMODE &= 3;
		DDFSTRT &= (chipset != OCS) ? 0xfe : 0xfc;
		DDFSTOP &= (chipset != OCS) ? 0xfe : 0xfc;
		res = (chipset == OCS) ? res & 1 : res;

		// fetch=log2(fetch_width)-4; fetch_width=16,32,64
		int fetch = (chipset == AGA) ? ((FMODE <= 1) ? FMODE : FMODE - 1) : 0;

		// sub-block (OCS/ECS) and large-block (AGA) stop pad
		int pad = (fetch > res) ? (8 << (fetch - res)) - 1 : 8 - 1;

		// OCS/ECS/(AGA) sub-block
		int sub = (res > fetch) ? res - fetch : 0;

		// AGA large-block
		int large = (fetch > res) ? fetch - res : 0;

		// DMA fetched blocks
		int blocks = ((DDFSTOP - DDFSTRT + pad) >> (3 + large)) + 1;

		// 16 pixels per fetch_width per sub-block per block
		return blocks << (4 + fetch + sub);
	}

	public bool IsMapped(uint address)
	{
		return chipRam.IsMapped(address)
		       || trapdoorRam.IsMapped(address);
	}

	public List<MemoryRange> MappedRange()
	{
		return chipRam.MappedRange().Concat(trapdoorRam.MappedRange()).ToList();
	}

	public uint Read(uint insaddr, uint address, Size size)
	{
		memory.WaitForChipRamDMASlot();
		
		if (chipRam.IsMapped(address))
			return chipRam.Read(insaddr, address, size);

		return trapdoorRam.Read(insaddr, address, size);
	}

	public void Write(uint insaddr, uint address, uint value, Size size)
	{
		memory.WaitForChipRamDMASlot();

		if (chipRam.IsMapped(address))
		{
			chipRam.Write(insaddr, address, value, size);
			return;
		}

		trapdoorRam.Write(insaddr, address, value, size);

	}
}