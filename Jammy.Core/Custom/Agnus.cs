using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

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
	private IDMA dma;
	private readonly IDenise denise;
	private readonly IInterrupt interrupt;
	private readonly IDiskDrives diskDrives;

	//private readonly IChipRAM chipRam;
	//private readonly IChips custom;
	private readonly IChipsetDebugger debugger;
	private readonly EmulationSettings settings;
	private readonly ILogger<Agnus> logger;

	//sprite DMA starts at 0x18, but can be eaten into by bitmap DMA
	//normal bitmap DMA start at 0x38, overscan at 0x30, Menace starts at 0x28
	//public const int DMA_START = 0x18;

	//bitmap DMA ends at 0xD8, with 8 slots after that
	//public const int DMA_END = 0xF0;

	public Agnus(IChipsetClock clock, IDenise denise, IInterrupt interrupt,
		IDiskDrives diskDrives,
		/*IChips custom,*/ IChipsetDebugger debugger,
		IOptions<EmulationSettings> settings, ILogger<Agnus> logger)
	{
		this.clock = clock;
		this.denise = denise;
		this.interrupt = interrupt;
		this.diskDrives = diskDrives;
		//chipRam = chipRAM;
		//trapdoorRam = trapdoorRAM;
		//this.kickstartROM = kickstartROM;
		//this.custom = custom;
		this.debugger = debugger;
		this.settings = settings.Value;
		this.logger = logger;

		SPRITE_DMA_START_LINE = settings.Value.VideoFormat == VideoFormat.NTSC ? 20u : 25;
	}

	public void Reset()
	{
		for (int i = 0; i < 8; i++)
			spriteState[i] = SpriteState.Idle;

		lineState = DMALineState.LineStart;
	}


	public void Emulate()
	{
		//clock.WaitForTick();
		var clockState = clock.ClockState;

		if ((clockState & ChipsetClockState.StartOfFrame)!=0)
		{
			for (int i = 0; i < 8; i++)
				spriteState[i] = SpriteState.Idle;
		}

		if ((clockState & ChipsetClockState.StartOfLine)!=0)
			lineState = DMALineState.LineStart;

		RunAgnusTick();
		UpdateSprites();

		//if ((clockState & ChipsetClockState.EndOfLine)!=0)
		//{
		//	EndAgnusLine();
		//}

		if ((clockState & ChipsetClockState.EndOfFrame)!=0)
			interrupt.AssertInterrupt(Types.Interrupt.VERTB);

		//clock.Ack();
	}

	public void Init(IDMA dma)
	{
		this.dma = dma;
	}

	private enum DMALineState
	{
		LineStart,
		Fetching,
		LineComplete,
		LineTerminated
	}

	[Persist]
	private int planes;
	[Persist]
	private int diwstrtv = 0;
	[Persist]
	private int diwstopv = 0;
	[Persist]
	private ushort ddfstrtfix = 0;
	[Persist]
	private ushort ddfstopfix = 0;
	[Persist]
	private int pixmod;
	[Persist]
	private DMALineState lineState;
	[Persist]
	private uint plane;

	private void RunAgnusTick()
	{
		//debugging
		if (clock.VerticalPos == debugger.dbugLine)
		{
			debugger.fetch[clock.HorizontalPos] = '-';
			debugger.write[clock.HorizontalPos] = '-';
		}
		//debugging

		//start by saying there's no DMA required, later code will overwrite it
		dma.NoDMA(DMASource.Agnus);

		if (clock.HorizontalPos < 0x18)
		{
			if ((clock.HorizontalPos & 1) == 0)
				return;

			switch (clock.HorizontalPos)
			{
				case 1: dma.NeedsDMA(DMASource.Agnus, DMA.DMAEN); break;
				case 3: dma.NeedsDMA(DMASource.Agnus, DMA.DMAEN); break;
				case 5: dma.NeedsDMA(DMASource.Agnus, DMA.DMAEN); break;
				case 7: if (dma.IsDMAEnabled(DMA.DSKEN)) diskDrives.DoDMA(); break;
				case 9: if (dma.IsDMAEnabled(DMA.DSKEN)) diskDrives.DoDMA(); break;
				case 0xB: if (dma.IsDMAEnabled(DMA.DSKEN)) diskDrives.DoDMA(); break;
				case 0xD: if (dma.IsDMAEnabled(DMA.AUD0EN)) dma.NeedsDMA(DMASource.Agnus, DMA.AUD0EN); break;//actually Audio 0 DMA
				case 0xF: if (dma.IsDMAEnabled(DMA.AUD1EN)) dma.NeedsDMA(DMASource.Agnus, DMA.AUD1EN); break;//actually Audio 1 DMA
				case 0x11: if (dma.IsDMAEnabled(DMA.AUD2EN)) dma.NeedsDMA(DMASource.Agnus, DMA.AUD2EN); break;//actually Audio 2 DMA
				case 0x13: if (dma.IsDMAEnabled(DMA.AUD3EN)) dma.NeedsDMA(DMASource.Agnus, DMA.AUD3EN); break;//actually Audio 3 DMA
				case 0x15: RunSpriteDMA(0); break;
				case 0x17: RunSpriteDMA(1); break;
			}
			return;
		}

		bool fetched = false;

		var blanking = Blanking.None;

		//is it in the vertical blanking zone (should swap to using some of the ECS registers)
		if (clock.VerticalPos >= 0 && clock.VerticalPos <= 0x19)
			blanking |= Blanking.VerticalBlank;

		//is it the visible area, vertically?
		if (clock.VerticalPos < diwstrtv || clock.VerticalPos >= diwstopv)
			blanking |= Blanking.OutsideDisplayWindow;

		//what are the correct values? Agnus can fetch at 0x18, how does that correspond to Denise clock?
		if (clock.DeniseHorizontalPos >= 0x10 && clock.DeniseHorizontalPos <= 51)//0x5e)
		{
			//if (clock.HorizontalPos >= ddfstrtfix)
			//	logger.LogTrace($"Fetch in HPOS {clock.DeniseHorizontalPos:X2} {clock.HorizontalPos:X2}");
			blanking |= Blanking.HorizontalBlank;
		}

		//tell Denise the blaking status and whether to start processing pixel data
		denise.SetBlankingStatus(blanking);

		if (blanking != Blanking.None) goto noBitplaneDMA;

		//debugging
		if (clock.VerticalPos == debugger.dbugLine)
			debugger.write[clock.HorizontalPos] = debugger.fetch[clock.HorizontalPos] = ':';
		//debugging

		//is it time to do bitplane DMA?
		//when h >= ddfstrt, bitplanes are fetching. one plane per cycle, until all the planes are fetched
		//bitplane DMA is ON
		if (clock.HorizontalPos >= ddfstrtfix + debugger.ddfSHack && clock.HorizontalPos < ddfstopfix + debugger.ddfEHack &&
			(lineState == DMALineState.Fetching || lineState == DMALineState.LineStart))
		{
			if (dma.IsDMAEnabled(DMA.BPLEN))
				fetched = CopperBitplaneFetch((int)clock.HorizontalPos);
			if (fetched)
				lineState = DMALineState.Fetching;
		}

		if (clock.HorizontalPos >= ddfstopfix + debugger.ddfEHack && lineState == DMALineState.Fetching)
		{
			lineState = DMALineState.LineComplete;
			EndAgnusLine();
		}

		if (fetched)
			return;

noBitplaneDMA:

		//can we use the non-bitplane DMA for something else?

		if (clock.HorizontalPos < 0x34)
		{
			if ((clock.HorizontalPos & 1) == 0)
				return;

			//switch (clock.HorizontalPos)
			//{
			//	case 0x19: RunSpriteDMA(2); break;
			//	case 0x1B: RunSpriteDMA(3); break;
			//	case 0x1D: RunSpriteDMA(4); break;
			//	case 0x1F: RunSpriteDMA(5); break;
			//	case 0x21: RunSpriteDMA(6); break;
			//	case 0x23: RunSpriteDMA(7); break;
			//	case 0x25: RunSpriteDMA(8); break;
			//	case 0x27: RunSpriteDMA(9); break;
			//	case 0x29: RunSpriteDMA(10); break;
			//	case 0x2B: RunSpriteDMA(11); break;
			//	case 0x2D: RunSpriteDMA(12); break;
			//	case 0x2F: RunSpriteDMA(13); break;
			//	case 0x31: RunSpriteDMA(14); break;
			//	case 0x33: RunSpriteDMA(15); break;
			//}
			uint spriteSlot = (clock.HorizontalPos - 0x15) / 2;
			RunSpriteDMA(spriteSlot);
		}
		if (clock.HorizontalPos == 0xE1)
			dma.NeedsDMA(DMASource.Agnus, DMA.DMAEN);
	}

	private readonly uint SPRITE_DMA_START_LINE;

	private static readonly uint[] fetchLo = [8, 4, 6, 2, 7, 3, 5, 1];
	private static readonly uint[] fetchHi = [4, 2, 3, 1, 4, 2, 3, 1];
	private static readonly uint[] fetchSh = [2, 1, 2, 1, 2, 1, 2, 1];
	private static readonly uint[] fetchF3 = [8, 4, 6, 2, 7, 3, 5, 1, 10,10,10,10,10,10,10,10, 10,10,10,10,10,10,10,10, 10,10,10,10,10,10,10,10];
	private static readonly uint[] fetchF2 = [8, 4, 6, 2, 7, 3, 5, 1, 10,10,10,10,10,10,10,10];

	private bool CopperBitplaneFetch(int h)
	{
		//int planeIdx = h % pixmod;
		int planeIdx = (h - ddfstrtfix) % pixmod;
		while (planeIdx < 0) planeIdx += pixmod;

		if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
		{
			if ((bplcon0 & (uint)Denise.Denise.BPLCON0.HiRes) != 0)
				plane = fetchHi[planeIdx] - 1;
			else if ((bplcon0 & (uint)Denise.Denise.BPLCON0.SuperHiRes) != 0)
				plane = fetchSh[planeIdx] - 1;
			else
				plane = fetchLo[planeIdx] - 1;
		}
		else if ((fmode & 3) == 3)
		{
			plane = fetchF3[planeIdx] - 1;
		}
		else
		{
			plane = fetchF2[planeIdx] - 1;
		}

		if (plane < planes)
		{
			if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
			{
				dma.ReadReg(DMASource.Agnus, bplpt[plane], DMA.BPLEN, Size.Word, ChipRegs.BPL1DAT + plane * 2);
				bplpt[plane] += 2;
			}
			else if ((fmode & 3) == 3)
			{
				dma.ReadReg(DMASource.Agnus, bplpt[plane], DMA.BPLEN, Size.QWord, ChipRegs.BPL1DAT + plane * 2);
				bplpt[plane] += 8;
			}
			else
			{
				dma.ReadReg(DMASource.Agnus, bplpt[plane], DMA.BPLEN, Size.Long, ChipRegs.BPL1DAT + plane * 2);
				bplpt[plane] += 4;
			}

			//debugging
			if (clock.VerticalPos == debugger.dbugLine)
			{ 
				//we just filled BPL1DAT
				if (plane == 0)
				{
					debugger.write[h] = 'x';
					debugger.dma++;
				}
				else
				{
					debugger.write[h] = '.';
				}

				debugger.fetch[h] = Convert.ToChar(plane + 48 + 1);
			}
			//debugging

			return true;
		}
		else
		{
			//debugging
			if (clock.VerticalPos == debugger.dbugLine)
				debugger.fetch[h] = '+';
			//debugging
		}
		return false;
	}

	private string MergeBP(ulong sprdata, ulong sprdatb)
	{
		ulong a = ulong.Parse(sprdata.ToBin());
		ulong b = ulong.Parse(sprdata.ToBin());
		return (a+b*2).ToString().PadLeft(16,'0');
	}

	public void UpdateSprites()
	{
		//if (plane == 0 && lineState == DMALineState.Fetching)
		//	denise.WriteBitplanes(bpldat);

		//if the sprite horiz position matches, clock the sprite data in
		for (uint s = 0; s < 8; s++)
		{
			//if (spriteState[s] == SpriteState.Fetching)
			if (clock.VerticalPos >= VStart(s) && clock.VerticalPos < VStop(s) && spriteState[s] != SpriteState.Idle)
			{
				int hstart = HStart(s);

				if (clock.DeniseHorizontalPos == (uint)(hstart & 0xfffe))
				{
					//logger.LogTrace($"SPR{s} {clock} {MergeBP(sprdata[s], sprdatb[s])} h:{sprpos[s]&0xff,3} v:{sprpos[s]>>8,3} {sprctl[s].ToBin()}");
					denise.WriteSprite(s, sprdata, sprdatb, sprctl);
				}
			}
		}
	}

	private void EndAgnusLine()
	{
		//next horizontal line, and we did some fetching this line, add on the modulos
		if (/*clock.VerticalPos >= diwstrtv && clock.VerticalPos < diwstopv &&*/ lineState == DMALineState.LineComplete)
		{
			//logger.LogTrace($"MOD {clock} {planes} {bpl1mod:X4} {bpl2mod:X4}");
			for (int i = 0; i < planes; i++)
			{
				bplpt[i] += ((i & 1) == 0) ? bpl1mod : bpl2mod;
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

	private enum SpriteState
	{
		Idle = 0,
		Waiting,
		Fetching,
	}

	[Persist]
	private readonly SpriteState[] spriteState = new SpriteState[8];

	private bool SpritesEnabledForThisFrame()
	{
		//todo: this might not categorically be the full answer
		//some notes say that perhaps if bitplane DMA is enabled earlier, or BPLCON0 is written earlier
		//then sprites are enabled then.
		return clock.VerticalPos >= SPRITE_DMA_START_LINE;
	}

	private int VStart(uint s)
	{
		int vstart = sprpos[s] >> 8;
		vstart += (sprctl[s] & 4) << 6; //bit 2 is high bit of vstart
		return vstart;
	}

	private int VStop(uint s)
	{
		int vstop = sprctl[s] >> 8;
		vstop += (sprctl[s] & 2) << 7; //bit 1 is high bit of vstop
		return vstop;
	}

	private int HStart(uint s)
	{
		int hstart = (sprpos[s] & 0xff) << 1;
		hstart |= sprctl[s] & 1; //bit 0 is low bit of hstart
		return hstart;
	}

	private void RunSpriteDMA(uint slot)
	{
		uint s = slot >> 1;

		if (spriteState[s] == SpriteState.Waiting)
		{
			int vstart = VStart(s);
			if (clock.VerticalPos == vstart)
			{
				spriteState[s] = SpriteState.Fetching;
			}
		}
		else if (spriteState[s] == SpriteState.Fetching)
		{
			int vstop = VStop(s);
			if (clock.VerticalPos == vstop)
				spriteState[s] = SpriteState.Idle;
		}

		//if DMA is off, or not possible, then don't do any
		if (!dma.IsDMAEnabled(DMA.SPREN) || !SpritesEnabledForThisFrame())
			return;

		if ((slot & 1) == 0)
		{
			if (spriteState[s] == SpriteState.Idle)
			{
				dma.ReadReg(DMASource.Agnus, sprpt[s], DMA.SPREN, Size.Word, ChipRegs.SPR0POS+s*8);
				sprpt[s] += 2;
			}
			else if (spriteState[s] == SpriteState.Fetching)
			{
				dma.ReadReg(DMASource.Agnus, sprpt[s], DMA.SPREN, Size.Word, ChipRegs.SPR0DATA+s*8);
				sprpt[s] += 2;
			}
		}
		else
		{
			if (spriteState[s] == SpriteState.Idle)
			{
				dma.ReadReg(DMASource.Agnus, sprpt[s], DMA.SPREN, Size.Word, ChipRegs.SPR0CTL+s*8);
				sprpt[s] += 2;
			}
			else if (spriteState[s] == SpriteState.Fetching)
			{
				dma.ReadReg(DMASource.Agnus, sprpt[s], DMA.SPREN, Size.Word, ChipRegs.SPR0DATB+s*8);
				sprpt[s] += 2;
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
		if ((bplcon0 & (uint)Denise.Denise.BPLCON0.HiRes) != 0)
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
				//FetchWidth(ddfstrt, ddfstop, OCS, HIRES, 0);
			}
			else if ((fmode & 3) == 3)
			{
				//ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 0xf) >> 4) + 1) << 4));
				ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
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
		else if ((bplcon0 & (uint)Denise.Denise.BPLCON0.SuperHiRes) != 0)
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
		debugger.ddfStrtFix = ddfstrtfix;
		debugger.ddfStopFix = ddfstopfix;
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
	private ulong[] sprdata = new ulong[8];
	private ulong[] sprdatb = new ulong[8];
	private ushort vpos;
	private ushort vhpos;

	//ECS/AGA
	private ushort vbstrt;
	private ushort vbstop;
	private ushort vsstop;
	private ushort vsstrt;
	private ushort diwhigh;
	private ushort vtotal;
	private ushort htotal;
	private ushort hbstrt;
	private ushort hbstop;
	private ushort hsstrt;
	private ushort hsstop;
	private ushort hcentre;
	private ushort fmode;
	private ushort beamcon0;

	public ushort Read(uint insaddr, uint address)
	{
		ushort value = 0;
		//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

		switch (address)
		{
			case ChipRegs.VPOSR:
				value = (ushort)((clock.VerticalPos >> 8) & 1); //todo: different on hires chips
				if (settings.VideoFormat == VideoFormat.NTSC)
					value |= (ushort)((clock.VerticalPos & 1) << 7); //toggle LOL each alternate line (NTSC only)

				//if we're in interlace mode
				if ((bplcon0 & (uint)Denise.Denise.BPLCON0.Interlace) != 0)
				{
					value |= (ushort)(clock.LongFrame() << 15); //set LOF=1/0 on alternate frames
				}
				else
				{
					value |= 1 << 15; //set LOF=1
				}

				value &= 0x80ff;
				switch (settings.ChipSet)
				{
					case ChipSet.AGA:
						value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC ? 0x3300 : 0x2300);
						break; //Alice
					case ChipSet.ECS:
						value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC ? 0x3100 : 0x2100);
						break; //Fat Agnus
					case ChipSet.OCS:
						value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC ? 0x0000 : 0x1000);
						break; //OCS
				}
				//logger.LogTrace($"VPOSR {clock} {value:X4} @ {insaddr:X6}");
				vpos = value;
				break;

			case ChipRegs.VHPOSR:
				int h = (int)clock.HorizontalPos;
				h -= 2;
				if (h < 0) h += 227;
				value = (ushort)((clock.VerticalPos << 8) | ((uint)h & 0x00ff));
				//logger.LogTrace($"VHPOSR {clock} {value:X4} @ {insaddr:X6}");
				vhpos = value;
				break;
		}

		return value;
	}

	private void UpdateSpriteState(int s)
	{
		spriteState[s] = SpriteState.Waiting;
		//sprpt[s] += 4;
	}

	private void DebugBPL(int plane, ushort value)
	{
		//logger.LogTrace($"BPL{plane + 1}DAT {clock} {value:X4}");
	}
	private void DebugBPLPT(int plane, ushort value)
	{
		//logger.LogTrace($"BPL{plane + 1}PT {clock} {value:X4}");
	}
	private void DebugSPRPOS(uint s)
	{
		//logger.LogTrace($"SPR{s}POS {clock} v:{VStart(s)} h:{HStart(s)} pos:{sprpos[s]:X4} ctl:{sprctl[s]:X4}");
	}

	public void Write(uint insaddr, uint address, ushort value)
	{
		switch (address)
		{
			case ChipRegs.BPL1MOD: bpl1mod = (uint)(short)value & 0xfffffffe; break;
			case ChipRegs.BPL2MOD: bpl2mod = (uint)(short)value & 0xfffffffe; break;

			case ChipRegs.BPLCON0: bplcon0 = value; UpdateBPLCON0(); UpdateDDF(); break;

			case ChipRegs.BPL1DAT: bpldat[0] = value; DebugBPL(0,value); denise.WriteBitplanes(bpldat); break;
			case ChipRegs.BPL2DAT: bpldat[1] = value; DebugBPL(1, value); break;
			case ChipRegs.BPL3DAT: bpldat[2] = value; DebugBPL(2, value); break;
			case ChipRegs.BPL4DAT: bpldat[3] = value; DebugBPL(3, value); break;
			case ChipRegs.BPL5DAT: bpldat[4] = value; DebugBPL(4, value); break;
			case ChipRegs.BPL6DAT: bpldat[5] = value; DebugBPL(5, value); break;
			case ChipRegs.BPL7DAT: bpldat[6] = value; DebugBPL(6, value); break;
			case ChipRegs.BPL8DAT: bpldat[7] = value; DebugBPL(7, value); break;

			case ChipRegs.BPL1PTL: bplpt[0] = (bplpt[0] & 0xffff0000) | (uint)(value & 0xfffe); DebugBPLPT(0,value); break;
			case ChipRegs.BPL1PTH: bplpt[0] = (bplpt[0] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugBPLPT(0, value); break;
			case ChipRegs.BPL2PTL: bplpt[1] = (bplpt[1] & 0xffff0000) | (uint)(value & 0xfffe); DebugBPLPT(1, value); break;
			case ChipRegs.BPL2PTH: bplpt[1] = (bplpt[1] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugBPLPT(1, value); break;
			case ChipRegs.BPL3PTL: bplpt[2] = (bplpt[2] & 0xffff0000) | (uint)(value & 0xfffe); DebugBPLPT(2, value); break;
			case ChipRegs.BPL3PTH: bplpt[2] = (bplpt[2] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugBPLPT(2, value); break;
			case ChipRegs.BPL4PTL: bplpt[3] = (bplpt[3] & 0xffff0000) | (uint)(value & 0xfffe); DebugBPLPT(3, value); break;
			case ChipRegs.BPL4PTH: bplpt[3] = (bplpt[3] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugBPLPT(3, value); break;
			case ChipRegs.BPL5PTL: bplpt[4] = (bplpt[4] & 0xffff0000) | (uint)(value & 0xfffe); DebugBPLPT(4, value); break;
			case ChipRegs.BPL5PTH: bplpt[4] = (bplpt[4] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugBPLPT(4, value); break;
			case ChipRegs.BPL6PTL: bplpt[5] = (bplpt[5] & 0xffff0000) | (uint)(value & 0xfffe); DebugBPLPT(5, value); break;
			case ChipRegs.BPL6PTH: bplpt[5] = (bplpt[5] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugBPLPT(5, value); break;
			case ChipRegs.BPL7PTL: bplpt[6] = (bplpt[6] & 0xffff0000) | (uint)(value & 0xfffe); DebugBPLPT(6, value); break;
			case ChipRegs.BPL7PTH: bplpt[6] = (bplpt[6] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugBPLPT(6, value); break;
			case ChipRegs.BPL8PTL: bplpt[7] = (bplpt[7] & 0xffff0000) | (uint)(value & 0xfffe); DebugBPLPT(7, value); break;
			case ChipRegs.BPL8PTH: bplpt[7] = (bplpt[7] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugBPLPT(7, value); break;

			case ChipRegs.DIWSTRT: diwstrt = value; diwhigh = 0; UpdateDIWSTRT(); break;
			case ChipRegs.DIWSTOP: diwstop = value; diwhigh = 0; UpdateDIWSTOP(); break;
			case ChipRegs.DIWHIGH: diwhigh = value; UpdateDIWHIGH(); break;

			case ChipRegs.DDFSTRT:
				ddfstrt = (ushort)(value & (settings.ChipSet == ChipSet.OCS ? 0xfc : 0xfe));
				//causes modulo not to be added, even if there was fetching on the line before this is written
				//lineState = DMALineState.LineTerminated;
				UpdateDDF();
				break;
			case ChipRegs.DDFSTOP:
				ddfstop = (ushort)(value & (settings.ChipSet == ChipSet.OCS ? 0xfc : 0xfe));
				//lineState = DMALineState.LineTerminated;
				UpdateDDF();
				break;

			case ChipRegs.SPR0PTL: sprpt[0] = (sprpt[0] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR0PTH: sprpt[0] = (sprpt[0] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
			case ChipRegs.SPR0POS: sprpos[0] = value; DebugSPRPOS(0); break;
			case ChipRegs.SPR0CTL: sprctl[0] = value; UpdateSpriteState(0); break;
			case ChipRegs.SPR0DATA: sprdata[0] = value; break;
			case ChipRegs.SPR0DATB: sprdatb[0] = value; break;

			case ChipRegs.SPR1PTL: sprpt[1] = (sprpt[1] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR1PTH: sprpt[1] = (sprpt[1] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
			case ChipRegs.SPR1POS: sprpos[1] = value; break;
			case ChipRegs.SPR1CTL: sprctl[1] = value; UpdateSpriteState(1); break;
			case ChipRegs.SPR1DATA: sprdata[1] = value; break;
			case ChipRegs.SPR1DATB: sprdatb[1] = value; break;

			case ChipRegs.SPR2PTL: sprpt[2] = (sprpt[2] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR2PTH: sprpt[2] = (sprpt[2] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
			case ChipRegs.SPR2POS: sprpos[2] = value; break;
			case ChipRegs.SPR2CTL: sprctl[2] = value; UpdateSpriteState(2); break;
			case ChipRegs.SPR2DATA: sprdata[2] = value; break;
			case ChipRegs.SPR2DATB: sprdatb[2] = value; break;

			case ChipRegs.SPR3PTL: sprpt[3] = (sprpt[3] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR3PTH: sprpt[3] = (sprpt[3] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
			case ChipRegs.SPR3POS: sprpos[3] = value; break;
			case ChipRegs.SPR3CTL: sprctl[3] = value; UpdateSpriteState(3); break;
			case ChipRegs.SPR3DATA: sprdata[3] = value; break;
			case ChipRegs.SPR3DATB: sprdatb[3] = value; break;

			case ChipRegs.SPR4PTL: sprpt[4] = (sprpt[4] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR4PTH: sprpt[4] = (sprpt[4] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
			case ChipRegs.SPR4POS: sprpos[4] = value; break;
			case ChipRegs.SPR4CTL: sprctl[4] = value; UpdateSpriteState(4); break;
			case ChipRegs.SPR4DATA: sprdata[4] = value; break;
			case ChipRegs.SPR4DATB: sprdatb[4] = value; break;

			case ChipRegs.SPR5PTL: sprpt[5] = (sprpt[5] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR5PTH: sprpt[5] = (sprpt[5] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
			case ChipRegs.SPR5POS: sprpos[5] = value; break;
			case ChipRegs.SPR5CTL: sprctl[5] = value; UpdateSpriteState(5); break;
			case ChipRegs.SPR5DATA: sprdata[5] = value; break;
			case ChipRegs.SPR5DATB: sprdatb[5] = value; break;

			case ChipRegs.SPR6PTL: sprpt[6] = (sprpt[6] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR6PTH: sprpt[6] = (sprpt[6] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
			case ChipRegs.SPR6POS: sprpos[6] = value; break;
			case ChipRegs.SPR6CTL: sprctl[6] = value; UpdateSpriteState(6); break;
			case ChipRegs.SPR6DATA: sprdata[6] = value; break;
			case ChipRegs.SPR6DATB: sprdatb[6] = value; break;

			case ChipRegs.SPR7PTL: sprpt[7] = (sprpt[7] & 0xffff0000) | (uint)(value & 0xfffe); break;
			case ChipRegs.SPR7PTH: sprpt[7] = (sprpt[7] & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
			case ChipRegs.SPR7POS: sprpos[7] = value; break;
			case ChipRegs.SPR7CTL: sprctl[7] = value; UpdateSpriteState(7); break;
			case ChipRegs.SPR7DATA: sprdata[7] = value; break;
			case ChipRegs.SPR7DATB: sprdatb[7] = value; break;

			//ECS/AGA
			case ChipRegs.VBSTRT: vbstrt = value; /*logger.LogTrace($"VBSTRT {value:X4} @{insaddr:X8}");*/ break;
			case ChipRegs.VBSTOP: vbstop = value; /*logger.LogTrace($"VBSTOP {value:X4} @{insaddr:X8}");*/ break;
			case ChipRegs.VSSTOP: vsstop = value; logger.LogTrace($"VSSTOP {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.VSSTRT: vsstrt = value; logger.LogTrace($"VSSTRT {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.VTOTAL: vtotal = value; /*logger.LogTrace($"VTOTAL {value:X4} @{insaddr:X8}");*/ break;
			case ChipRegs.VPOSW: logger.LogTrace($"VPOSW {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.VHPOSW: logger.LogTrace($"VHPOSW {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.HTOTAL: htotal = value; logger.LogTrace($"VHPOSW {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.HBSTRT: hbstrt = value; logger.LogTrace($"HBSTRT {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.HBSTOP: hbstop = value; logger.LogTrace($"HBSTOP {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.HSSTRT: hsstrt = value; logger.LogTrace($"HSSTRT {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.HSSTOP: hsstop = value; logger.LogTrace($"HSSTOP {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.HCENTER: hcentre = value; logger.LogTrace($"HCENTER {value:X4} @{insaddr:X8}"); break;
			case ChipRegs.BEAMCON0: beamcon0 = value; logger.LogTrace($"BEAMCON0 {value:X4} @{insaddr:X8}"); break;

			case ChipRegs.FMODE: fmode = value; UpdateDDF(); break;
		}
	}

	public void WriteWide(uint address, ulong value)
	{
		switch (address)
		{
			case ChipRegs.BPL1DAT: bpldat[0] = value; denise.WriteBitplanes(bpldat); break;
			case ChipRegs.BPL2DAT: bpldat[1] = value; break;
			case ChipRegs.BPL3DAT: bpldat[2] = value; break;
			case ChipRegs.BPL4DAT: bpldat[3] = value; break;
			case ChipRegs.BPL5DAT: bpldat[4] = value; break;
			case ChipRegs.BPL6DAT: bpldat[5] = value; break;
			case ChipRegs.BPL7DAT: bpldat[6] = value; break;
			case ChipRegs.BPL8DAT: bpldat[7] = value; break;

			case ChipRegs.SPR0DATA: sprdata[0] = value; break;
			case ChipRegs.SPR0DATB: sprdatb[0] = value; break;
			case ChipRegs.SPR1DATA: sprdata[1] = value; break;
			case ChipRegs.SPR1DATB: sprdatb[1] = value; break;
			case ChipRegs.SPR2DATA: sprdata[2] = value; break;
			case ChipRegs.SPR2DATB: sprdatb[2] = value; break;
			case ChipRegs.SPR3DATA: sprdata[3] = value; break;
			case ChipRegs.SPR3DATB: sprdatb[3] = value; break;
			case ChipRegs.SPR4DATA: sprdata[4] = value; break;
			case ChipRegs.SPR4DATB: sprdatb[4] = value; break;
			case ChipRegs.SPR5DATA: sprdata[5] = value; break;
			case ChipRegs.SPR5DATB: sprdatb[5] = value; break;
			case ChipRegs.SPR6DATA: sprdata[6] = value; break;
			case ChipRegs.SPR6DATB: sprdatb[6] = value; break;
			case ChipRegs.SPR7DATA: sprdata[7] = value; break;
			case ChipRegs.SPR7DATB: sprdatb[7] = value; break;
		}
	}

	//public bool IsMapped(uint address)
	//{
	//	return custom.IsMapped(address);

	//}

	//public List<MemoryRange> MappedRange()
	//{
	//	return custom.MappedRange();
	//}

	private ulong chipRAMReads = 0;
	private ulong chipRAMWrites = 0;
	private ulong trapdoorReads = 0;
	private ulong trapdoorWrites = 0;
	private ulong chipsetReads = 0;
	private ulong chipsetWrites = 0;
	private ulong kickROMReads = 0;

	public void GetRGAReadWriteStats(out ulong chipReads, out ulong chipWrites,
				out ulong trapReads, out ulong trapWrites,
				out ulong customReads, out ulong customWrites,
				out ulong kickReads)
	{
		chipReads = chipRAMReads;
		chipWrites = chipRAMWrites;
		trapReads = trapdoorReads;
		trapWrites = trapdoorWrites;
		customReads = chipsetReads;
		customWrites = chipsetWrites;
		kickReads = kickROMReads;
	}

	private ulong bmchipRAMReads = 0;
	private ulong bmchipRAMWrites = 0;
	private ulong bmtrapdoorReads = 0;
	private ulong bmtrapdoorWrites = 0;
	private ulong bmchipsetReads = 0;
	private ulong bmchipsetWrites = 0;
	private ulong bmkickROMReads = 0;

	public void Bookmark()
	{
		bmchipRAMReads = chipRAMReads;
		bmchipRAMWrites = chipRAMWrites;
		bmtrapdoorReads = trapdoorReads;
		bmtrapdoorWrites = trapdoorWrites;
		bmchipsetReads = chipsetReads;
		bmchipsetWrites = chipsetWrites;
		bmkickROMReads = kickROMReads;
	}

	public uint Read(uint insaddr, uint address, Size size)
	{
		uint v = 0;

		ulong reads = (size == Size.Long)?2U:1U;

		chipsetReads += reads;

		if (size == Size.Long)
		{
			dma.ReadCPU(CPUTarget.ChipReg, address, Size.Word);
			v = dma.ChipsetSync()<<16;
			size = Size.Word;
			address += 2;
		}

		dma.ReadCPU(CPUTarget.ChipReg, address, size);
		v |= dma.ChipsetSync();
	
		return v;
	}

	//public uint ReadX(uint insaddr, uint address, Size size)
	//{
	//		chipsetReads++;
	//		if (size == Size.Long) chipsetReads++;
	//		return custom.Read(insaddr, address, size);
	//}

	public void Write(uint insaddr, uint address, uint value, Size size)
	{
		ulong writes = (size == Size.Long) ? 2U : 1U;

		chipsetWrites += writes;

		if (size == Size.Long)
		{
			dma.WriteCPU(CPUTarget.ChipReg, address, (ushort)(value>>16), Size.Word);
			dma.ChipsetSync();
			size = Size.Word;
			address += 2;
		}

		dma.WriteCPU(CPUTarget.ChipReg, address, (ushort)value, size);
		dma.ChipsetSync();
	}

	//public void WriteX(uint insaddr, uint address, uint value, Size size)
	//{
	//		chipsetWrites++;
	//		if (size == Size.Long) chipsetWrites++;
	//		custom.Write(insaddr, address, value, size);
	//		return;
	//}

	public List<BulkMemoryRange> ReadBulk()
	{
		return new List<BulkMemoryRange>();
	}

	public uint DebugRead(uint address, Size size)
	{
		return 0;
	}

	public void DebugWrite(uint address, uint value, Size size)
	{
	}

	public uint DebugChipsetRead(uint address, Size size)
	{
		uint value=0;
		switch (address)
		{
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
			case ChipRegs.SPR0DATA: value = (ushort)sprdata[0]; break;
			case ChipRegs.SPR0DATB: value = (ushort)sprdatb[0]; break;

			case ChipRegs.SPR1PTL: value = (ushort)sprpt[1]; break;
			case ChipRegs.SPR1PTH: value = (ushort)(sprpt[1] >> 16); break;
			case ChipRegs.SPR1POS: value = sprpos[1]; break;
			case ChipRegs.SPR1CTL: value = sprctl[1]; break;
			case ChipRegs.SPR1DATA: value = (ushort)sprdata[1]; break;
			case ChipRegs.SPR1DATB: value = (ushort)sprdatb[1]; break;

			case ChipRegs.SPR2PTL: value = (ushort)sprpt[2]; break;
			case ChipRegs.SPR2PTH: value = (ushort)(sprpt[2] >> 16); break;
			case ChipRegs.SPR2POS: value = sprpos[2]; break;
			case ChipRegs.SPR2CTL: value = sprctl[2]; break;
			case ChipRegs.SPR2DATA: value = (ushort)sprdata[2]; break;
			case ChipRegs.SPR2DATB: value = (ushort)sprdatb[2]; break;

			case ChipRegs.SPR3PTL: value = (ushort)sprpt[3]; break;
			case ChipRegs.SPR3PTH: value = (ushort)(sprpt[3] >> 16); break;
			case ChipRegs.SPR3POS: value = sprpos[3]; break;
			case ChipRegs.SPR3CTL: value = sprctl[3]; break;
			case ChipRegs.SPR3DATA: value = (ushort)sprdata[3]; break;
			case ChipRegs.SPR3DATB: value = (ushort)sprdatb[3]; break;

			case ChipRegs.SPR4PTL: value = (ushort)sprpt[4]; break;
			case ChipRegs.SPR4PTH: value = (ushort)(sprpt[4] >> 16); break;
			case ChipRegs.SPR4POS: value = sprpos[4]; break;
			case ChipRegs.SPR4CTL: value = sprctl[4]; break;
			case ChipRegs.SPR4DATA: value = (ushort)sprdata[4]; break;
			case ChipRegs.SPR4DATB: value = (ushort)sprdatb[4]; break;

			case ChipRegs.SPR5PTL: value = (ushort)sprpt[5]; break;
			case ChipRegs.SPR5PTH: value = (ushort)(sprpt[5] >> 16); break;
			case ChipRegs.SPR5POS: value = sprpos[5]; break;
			case ChipRegs.SPR5CTL: value = sprctl[5]; break;
			case ChipRegs.SPR5DATA: value = (ushort)sprdata[5]; break;
			case ChipRegs.SPR5DATB: value = (ushort)sprdatb[5]; break;

			case ChipRegs.SPR6PTL: value = (ushort)sprpt[6]; break;
			case ChipRegs.SPR6PTH: value = (ushort)(sprpt[6] >> 16); break;
			case ChipRegs.SPR6POS: value = sprpos[6]; break;
			case ChipRegs.SPR6CTL: value = sprctl[6]; break;
			case ChipRegs.SPR6DATA: value = (ushort)sprdata[6]; break;
			case ChipRegs.SPR6DATB: value = (ushort)sprdatb[6]; break;

			case ChipRegs.SPR7PTL: value = (ushort)sprpt[7]; break;
			case ChipRegs.SPR7PTH: value = (ushort)(sprpt[7] >> 16); break;
			case ChipRegs.SPR7POS: value = sprpos[7]; break;
			case ChipRegs.SPR7CTL: value = sprctl[7]; break;
			case ChipRegs.SPR7DATA: value = (ushort)sprdata[7]; break;
			case ChipRegs.SPR7DATB: value = (ushort)sprdatb[7]; break;

			case ChipRegs.VPOSR: value = vpos; break;
			case ChipRegs.VHPOSR: value = vhpos; break;

			//ECS/AGA
			case ChipRegs.VBSTRT: value = vbstrt; break;
			case ChipRegs.VBSTOP: value = vbstop; break;
			case ChipRegs.VSSTOP: value = vsstop; break;
			case ChipRegs.VSSTRT: value = vsstrt; break;
			case ChipRegs.VTOTAL: value = vtotal; break;

			case ChipRegs.HTOTAL: value = htotal; break;
			case ChipRegs.HBSTRT: value = hbstrt; break;
			case ChipRegs.HBSTOP: value = hbstop; break;
			case ChipRegs.HSSTRT: value = hsstrt; break;
			case ChipRegs.HSSTOP: value = hsstop; break;
			case ChipRegs.HCENTER: value = hcentre; break;

			case ChipRegs.FMODE: value = fmode; break;
			case ChipRegs.BEAMCON0: value = beamcon0; break;
		}

		return value;
	}

	public void Save(JArray obj)
	{
		PersistenceManager.ToJObject(this, "agnus");
	}

	public void Load(JObject obj)
	{
		if (!PersistenceManager.Is(obj, "agnus")) return;

		PersistenceManager.FromJObject(this, obj);
	}
}
