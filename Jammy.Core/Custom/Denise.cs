using System;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom;

public class Denise : IDenise
{
	private readonly EmulationSettings settings;
	private readonly IChipsetClock clock;
	private readonly ILogger logger;
	private readonly IEmulationWindow emulationWindow;
	
	[Flags]
	public enum BPLCON0 : uint
	{
		HiRes = 1 << 15,
		SuperHiRes = 1 << 6,
	}

	public const int DMA_WIDTH = 228;// Agnus.DMA_END - Agnus.DMA_START;
	private const int SCREEN_WIDTH = DMA_WIDTH * 4; //227 (E3) * 4;
	private const int SCREEN_HEIGHT = 313 * 2; //x2 for scan double

	private int[] screen;

	public Denise(IChipsetClock clock, IEmulationWindow emulationWindow, IOptions<EmulationSettings> settings, ILogger<Denise> logger)
	{
		this.settings = settings.Value;
		this.clock = clock;
		this.logger = logger;
		this.emulationWindow = emulationWindow;

		ComputeDPFLookup();

		emulationWindow.SetPicture(SCREEN_WIDTH, SCREEN_HEIGHT);

		RunVerticalBlankStart();
	}

	public FastUInt128 pixelMask;
	public int pixelMaskBit;
	public uint pixelBits;
	public FastUInt128[] bpldatpix = new FastUInt128[8];

	public int planes;
	public int diwstrth = 0;
	public int diwstoph = 0;

	public int pixelLoop;
	public uint lastcol = 0;

	private int lineStart;
	public int dptr = 0;

	private int ii = 0;
	public void Emulate(ulong cycles)
	{
		clock.WaitForTick();

		//logger.LogTrace($"{clock.VerticalPos} {clock.HorizontalPos}");

		if (clock.StartOfFrame())
			RunVerticalBlankStart();

		if (clock.StartOfLine())
			StartDeniseLine();

		try
		{
			//ii++;
			if ((ii &1)==0)
				RunDeniseTick();
		}
		catch (IndexOutOfRangeException ex)
		{

		}

		if (clock.EndOfLine())
			EndDeniseLine();

		if (clock.EndOfFrame())
			RunVerticalBlankEnd();

		clock.Ack();
	}

	public void Reset() { }

	private void RunVerticalBlankStart()
	{
		screen = emulationWindow.GetFramebuffer();
		dptr = 0;
		lastcol = 0;
	}

	private void RunVerticalBlankEnd()
	{
		emulationWindow.Blit(screen);
		//DebugLocation();
	}

	private bool blanking;

	public void EnterVisibleArea()
	{
		blanking = false;
	}

	public void ExitVisibleArea()
	{
		blanking = true;
	}

	public void WriteBitplanes(ulong[] bpldat)
	{
		//scrolling
		int even = bplcon1 & 0xf;
		int odd = (bplcon1 >> 4) & 0xf;

		for (int i = 0; i < 8; i++)
		{
			if ((i & 1) != 0)
				bpldatpix[i].Or(bpldat[i], (16 - odd));
			else
				bpldatpix[i].Or(bpldat[i], (16 - even));
		}
	}

	private void FirstPixel()
	{
		if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
			pixelBits = 15;
		else if ((fmode & 3) == 3)
			pixelBits = 63;
		else
			pixelBits = 31;

		pixelMask.SetBit((int)(pixelBits + 16));
		pixelMaskBit = (int)(pixelBits + 16);

		//pixelCounter = 0;

		for (int i = 0; i < 8; i++)
			bpldatpix[i].Zero();
	}

	private void NextPixel()
	{
		for (int i = 0; i < 8; i++)
			bpldatpix[i].Shl1();
	}

	//(x&(1<<0))*1 + x&(1<<2)*2 + x&(1<<4)*4 + x&(1<<6) *8
	//00000 -> 0  01000 -> 0  10000 -> 4  11000 -> 4
	//00001 -> 1  01001 -> 1  10001 -> 5  11001 -> 5
	//00010 -> 0  01010 -> 0  10010 -> 4  11010 -> 4
	//00011 -> 1  01011 -> 1  10011 -> 5  11011 -> 5
	//00100 -> 2  01100 -> 2  10100 -> 6  11100 -> 6
	//00101 -> 3  01101 -> 3  10101 -> 7  11101 -> 7
	//00110 -> 2  01110 -> 2  10110 -> 6  11110 -> 6
	//00111 -> 3  01111 -> 3  10111 -> 7  11111 -> 7

	private readonly byte[] dpfLookup = new byte[256];

	private void ComputeDPFLookup()
	{
		for (int i = 0; i < 256; i++)
		{
			dpfLookup[i] = (byte)(
				((i & 1) != 0 ? 1 : 0) +
				((i & 4) != 0 ? 2 : 0) +
				((i & 16) != 0 ? 4 : 0) +
				((i & 64) != 0 ? 8 : 0)
			);
		}
	}

	private readonly ushort[] sprdatapix = new ushort[8];
	private readonly ushort[] sprdatbpix = new ushort[8];
	private readonly uint[] spriteMask = new uint[8];
	private readonly int[] clx = new int[8];

	private void CopperBitplaneConvert(int h)
	{
		//if (currentLine == 50)
		//	currentLine = 50;

		//if the sprite horiz position matches, clock the sprite data in
		for (int s = 0; s < 8; s++)
		{
			//todo: share this with Agnus somehow
			//if (spriteState[s] == SpriteState.Fetching)
			{
				int hstart = (sprpos[s] & 0xff) << 1;
				hstart |= sprctl[s] & 1; //bit 0 is low bit of hstart

				if (h == hstart >> 1)
				{
					sprdatapix[s] = sprdata[s];
					sprdatbpix[s] = sprdatb[s];
					spriteMask[s] = 0x8000;
				}
			}
		}

		int m = (pixelLoop / 2) - 1; //2->0,4->1,8->3
		for (int p = 0; p < pixelLoop; p++)
		{
			//decode the colour

			uint col;

			byte pix = 0;
			for (int i = 0, b = 1; i < planes; i++, b <<= 1)
				//pix |= (byte)((bpldatpix[i].AnyBitsSet(ref pixelMask)) ? b : 0);
				pix |= (byte)((bpldatpix[i].IsBitSet(pixelMaskBit)) ? b : 0);

			NextPixel();

			//BPLAM
			pix ^= (byte)(bplcon4 >> 8);

			//pix &= cdbg.bitplaneMask;
			//pix |= cdbg.bitplaneMod;

			if ((bplcon0 & (1 << 10)) != 0)
			{
				//DPF
				byte pix0 = dpfLookup[pix];
				byte pix1 = dpfLookup[pix >> 1];

				uint col0 = truecolour[pix0];
				uint col1 = truecolour[pix1 == 0 ? 0 : pix1 + 8];

				//which playfield is in front?
				if ((bplcon2 & (1 << 6)) != 0)
					col = pix1 != 0 ? col1 : col0;
				else
					col = pix0 != 0 ? col0 : col1;
			}
			else if (planes == 6 && ((bplcon0 & (1 << 11)) != 0))
			{
				//HAM6
				byte ham = (byte)(pix & 0b11_0000);
				pix &= 0xf;
				if (ham == 0)
				{
					col = truecolour[pix];
				}
				else
				{
					ham >>= 4;
					uint px = (uint)(pix * 0x11);
					if (ham == 1)
					{
						//col+B
						col = (lastcol & 0xffffff00) | px;
					}
					else if (ham == 3)
					{
						//col+G
						col = (lastcol & 0xffff00ff) | (px << 8);
					}
					else
					{
						//col+R
						col = (lastcol & 0xff00ffff) | (px << (8 + 8));
					}
				}
			}
			else if (planes == 6 && ((bplcon0 & (1 << 11)) == 0 &&
			                         (settings.ChipSet != ChipSet.AGA || (bplcon2 & (1 << 9)) == 0)))
			{
				//EHB
				col = truecolour[pix & 0x1f];
				if ((pix & 0b100000) != 0)
					col = (col & 0x00fefefe) >> 1;
			}
			else if (planes == 8 && ((bplcon0 & (1 << 11)) != 0))
			{
				//HAM8
				byte ham = (byte)(pix & 0b11);
				pix &= 0xfc;
				if (ham == 0)
				{
					col = truecolour[pix];
				}
				else
				{
					uint px = (uint)(pix | (pix >> 6));
					if (ham == 1)
					{
						//col+B
						col = (lastcol & 0xffffff00) | px;
					}
					else if (ham == 3)
					{
						//col+G
						col = (lastcol & 0xffff00ff) | (px << 8);
					}
					else
					{
						//col+R
						col = (lastcol & 0xff00ffff) | (px << (8 + 8));
					}
				}
			}
			else
			{
				col = truecolour[pix];
			}

			//sprites
			int clxm = 0;
			for (int s = 7; s >= 0; s--)
			{
				clx[s] = 0;
				if (spriteMask[s] != 0)
				{
					uint x = spriteMask[s];
					bool attached = (sprctl[s] & 0x80) != 0 && (s & 1) != 0;
					int spix = ((sprdatapix[s] & x) != 0 ? 1 : 0) + ((sprdatbpix[s] & x) != 0 ? 2 : 0);

					//in lowres, p=0,1, we want to shift every pixel (0,1) 01 &m==00
					//in hires, p=0,1,2,3 we want to shift every 2 pixels (1 and 3) &m=0101
					//in shires, p=0,1,2,3,4,5,6,7 we want to shift every 4 pixels (3 and 7) &m==01230123
					//todo: in AGA, sprites can have different resolutions
					if ((p & m) == m)
						spriteMask[s] >>= 1;

					clx[s] = spix;
					clxm |= clx[s];

					if (attached)
					{
						s--;
						spix <<= 2;
						int apix = ((sprdatapix[s] & x) != 0 ? 1 : 0) + ((sprdatbpix[s] & x) != 0 ? 2 : 0);
						clx[s] = apix;
						clxm |= clx[s];
						spix += apix;

						if ((p & m) == m)
							spriteMask[s] >>= 1;
						if (spix != 0)
							col = truecolour[16 + spix];
					}
					else
					{
						if (spix != 0)
							col = truecolour[16 + 4 * (s >> 1) + spix];
					}
				}
			}

			//sprite collision

			if (clxm != 0)
			{
				int clxconMatch = (clxcon & 0x3f) | ((clxcon2 & 0x3) << 6);
				int clxconEnable = ((clxcon >> 6) & 0x3f) | (clxcon2 & 0xc0);

				//combine in the enabled odd-numbered sprites
				for (int s = 0; s < 4; s++)
				{
					if ((clxcon & (0x1000 << s)) != 0)
						clx[s] = (clx[s * 2] | clx[s * 2 + 1]) != 0 ? 0xff : 0;
					else
						clx[s] = clx[s * 2] != 0 ? 0xff : 0;
				}

				ushort sscol = 1 << 9;
				for (int s = 0; s < 4; s++)
				{
					//planes enabled for collision
					int clp = (pix ^ ~clxconMatch) & clxconEnable;

					int mask = clx[s] & clp;
					if (mask != 0)
					{
						//sprite 's'->bitplane collision

						//even plane collision
						if ((mask & 0b01010101) != 0)
							clxdat |= (ushort)(2 << s);
						//odd plane collision
						if ((mask & 0b10101010) != 0)
							clxdat |= (ushort)(32 << s);
					}

					//sprite -> sprite collision
					for (int t = s + 1; t < 4; t++)
					{
						if ((clx[s] & clx[t]) != 0)
							clxdat |= sscol;
						sscol <<= 1;
					}
				}

				//odd->even bitplane collision
				if ((((pix & 0xb10101010) >> 1) & pix) != 0)
					clxdat |= 1;
			}

			//pixel double
			//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
			//since we've only set up a hi-res window, it's 2x, 1x and 0.5x
			if (pixelLoop == 8)
			{
				//hack for the 0.5x above - skip every other horizontal pixel
				if ((p & 1) == 0)
					screen[dptr++] = (int)col;
			}
			else
			{
				for (int k = 0; k < 4 / pixelLoop; k++)
					screen[dptr++] = (int)col;
			}

			//remember the last colour for HAM modes
			lastcol = col;
		}
	}

	private void StartDeniseLine()
	{
		lineStart = dptr;

		FirstPixel();
		lastcol = truecolour[0]; //todo: should be colour 0 at time of diwstrt
		//lineState = CopperLineState.LineStart;

		//if (currentLine == cdbg.dbugLine)
		//	DebugPalette();

		//clear sprites from wrapping from the right
		for (int s = 0; s < 8; s++)
			spriteMask[s] = 0;
	}

	private void UpdateBPLCON0()
	{
		planes = (bplcon0 >> 12) & 7;
		if (settings.ChipSet == ChipSet.AGA)
		{
			if (planes == 0 && (bplcon0 & (1 << 4)) != 0)
				planes = 8;
		}

		//https://eab.abime.net/showthread.php?t=111329

		//how many pixels should be fetched per clock in the current mode?
		if ((bplcon0 & (uint)Denise.BPLCON0.HiRes) != 0)
		{
			//4 colour clocks, fetch 16 pixels
			//1 colour clock, draw 4 pixel
			pixelLoop = 4;
		}
		else if ((bplcon0 & (uint)Denise.BPLCON0.SuperHiRes) != 0)
		{
			//2 colour clocks, fetch 16 pixels
			//1 colour clock, draw 8 pixel
			pixelLoop = 8;
		}
		else
		{
			//8 colour clocks, fetch 16 pixels
			//1 colour clock, draw 2 pixel
			pixelLoop = 2;
		}
	}

	private void UpdateDIWSTRT()
	{
		diwstrth = diwstrt & 0xff;
	}

	private void UpdateDIWSTOP()
	{
		diwstoph = (diwstop & 0xff) | 0x100;
	}

	private void UpdateDIWHIGH()
	{
		//if diwhigh is written, the 'magic' bits are overwritten
		if (diwhigh != 0)
		{
			diwstrth |= (diwhigh & 0b1_00000) << 3;

			diwstoph &= 0xff;
			diwstoph |= (diwhigh & 0b1_00000_00000000) >> 5;

			//todo: there are also an extra two bottom bits for strth/stoph
		}
	}

	private void RunDeniseTick()
	{
		if (!blanking)
		{
			//is it the visible area horizontally?
			//when h >= diwstrt, bits are read out of the bitplane data, turned into pixels and output
			//HACK-the minuses are a hack.  the bitplanes are ready from fetching but they're not supposed to be copied into Denise until 4 cycles later
			if (clock.HorizontalPos >= ((diwstrth /*+ cdbg.diwSHack*/) >> 1) - 1 && clock.HorizontalPos < ((diwstoph /*+ cdbg.diwEHack*/) >> 1) - 1)
			{
				CopperBitplaneConvert((int)clock.HorizontalPos);
			}
			else
			{
				//outside horizontal area
				for (int p = 0; p < pixelLoop; p++)
					NextPixel();

				//output colour 0 pixels
				uint col = truecolour[0];
				for (int k = 0; k < 4; k++)
					screen[dptr++] = (int)col;
			}
		}
		else
		{
			//outside vertical area

			//output colour 0 pixels
			uint col = truecolour[0];
			for (int k = 0; k < 4; k++)
				screen[dptr++] = (int)col;
		}
	}

	private ushort diwstrt;
	private ushort diwstop;

	private ushort bplcon0;
	private ushort bplcon1;
	private ushort bplcon2;
	private ushort bplcon3;
	private ushort bplcon4;
	private readonly ushort[] sprpos = new ushort[8];
	private readonly ushort[] sprctl = new ushort[8];
	private readonly ushort[] sprdata = new ushort[8];
	private readonly ushort[] sprdatb = new ushort[8];
	private ushort clxdat;
	private ushort clxcon;
	private ushort clxcon2;

	////ECS/AGA
	//private ushort vbstrt;
	//private ushort vbstop;
	//private ushort vsstop;
	//private ushort vsstrt;
	private ushort diwhigh;
	//private ushort vtotal;
	private ushort fmode;
	//private ushort beamcon0;

	private readonly ushort[] colour = new ushort[256];
	private readonly ushort[] lowcolour = new ushort[256];
	private readonly uint[] truecolour = new uint[256];

	public ushort Read(uint insaddr, uint address)
	{
		ushort value = 0;
		//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

		switch (address)
		{
			case ChipRegs.BPLCON0: value = bplcon0; break;
			case ChipRegs.BPLCON1: value = bplcon1; break;
			case ChipRegs.BPLCON2: value = bplcon2; break;
			case ChipRegs.BPLCON3: value = bplcon3; break;
			case ChipRegs.BPLCON4: value = bplcon4; break;

			case ChipRegs.DIWSTRT: value = diwstrt; break;
			case ChipRegs.DIWSTOP: value = diwstop; break;
			case ChipRegs.DIWHIGH: value = diwhigh; break;

			case ChipRegs.CLXDAT: value = clxdat; clxdat = 0; break;
		}

		if (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31)
		{
			//uint bank = (custom.Read(0, ChipRegs.BPLCON3, Size.Word) & 0b111_00000_00000000) >> (13 - 5);
			int bank = (bplcon3 & 0b111_00000_00000000) >> (13 - 5);

			int loct = bplcon3 & (1 << 9);

			//Amiga colour
			int index = (int)(bank + ((address - ChipRegs.COLOR00) >> 1));

			if (loct != 0)
				value = lowcolour[index];
			else
				value = colour[index];
		}

		return value;
	}

	public void Write(uint insaddr, uint address, ushort value)
	{
		switch (address)
		{
			case ChipRegs.BPLCON0: bplcon0 = value; UpdateBPLCON0(); break;
			case ChipRegs.BPLCON1: bplcon1 = value; break;
			case ChipRegs.BPLCON2: bplcon2 = value; break;
			case ChipRegs.BPLCON3: bplcon3 = value; break;
			case ChipRegs.BPLCON4: bplcon4 = value; break;

			case ChipRegs.DIWSTRT: diwstrt = value; diwhigh = 0; UpdateDIWSTRT(); break;
			case ChipRegs.DIWSTOP: diwstop = value; diwhigh = 0; UpdateDIWSTOP(); break;
			case ChipRegs.DIWHIGH: diwhigh = value; UpdateDIWHIGH(); break;

			case ChipRegs.FMODE: fmode = value; break;

			case ChipRegs.CLXCON: clxcon = value; clxcon2 = 0; break;
			case ChipRegs.CLXCON2: clxcon2 = value; break;
		}

		if (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31)
		{
			value &= 0x0fff;

			//uint bank = (custom.Read(0, ChipRegs.BPLCON3, Size.Word) & 0b111_00000_00000000) >> (13 - 5);
			int bank = (bplcon3 & 0b111_00000_00000000) >> (13 - 5);

			//Amiga colour
			int index = (int)(bank + ((address - ChipRegs.COLOR00) >> 1));

			int loct = bplcon3 & (1 << 9);
			if (loct != 0)
			{
				lowcolour[index] = value;
			}
			else
			{
				colour[index] = value;
				lowcolour[index] = value;
			}

			//24bit colour
			truecolour[index] = Explode(colour[index]) | (Explode(lowcolour[index]) >> 4);
		}
	}

	private uint Explode(ushort c)
	{
		return (uint)(((c & 0xf) << 4) | ((c & 0xf0) << 8) | ((c & 0xf00) << 12));
	}

	private void EndDeniseLine()
	{
		//this should be a no-op
		System.Diagnostics.Debug.Assert(SCREEN_WIDTH - (dptr - lineStart) == 0);
		dptr += SCREEN_WIDTH - (dptr - lineStart);

		//scan double
		for (int i = lineStart; i < lineStart + SCREEN_WIDTH; i++)
			screen[dptr++] = screen[i];
	}

	//private void DebugPalette()
	//{
	//	int sx = 5;
	//	int sy = 5;

	//	int box = 5;
	//	for (int y = 0; y < 4; y++)
	//	{
	//		for (int x = 0; x < 64; x++)
	//		{
	//			for (int p = 0; p < box; p++)
	//			{
	//				for (int q = 0; q < box; q++)
	//				{
	//					screen[sx + x * box + q + (sy + (y * box) + p) * SCREEN_WIDTH] = (int)truecolour[x + y * 64];
	//				}
	//			}
	//		}
	//	}

	//}


	//private void DebugLocation()
	//{
	//	//if (cdbg.dbugLine < 0) return;
	//	//if (cdbg.dbugLine >= SCREEN_HEIGHT / 2) return;
	//	//for (int x = 0; x < SCREEN_WIDTH; x += 4)
	//	//	screen[x + cdbg.dbugLine * SCREEN_WIDTH * 2] ^= 0xffffff;
	//}


	public void Reset(uint copperPC)
	{
		dptr = 0;
	}
}
