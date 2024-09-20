using System;
using System.Runtime.CompilerServices;
using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.NativeOverlay;
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
	private readonly IChipsetDebugger debugger;
	private readonly ILogger logger;
	private readonly IEmulationWindow emulationWindow;
	
	[Flags]
	public enum BPLCON0 : uint
	{
		HiRes = 1 << 15,
		SuperHiRes = 1 << 6,
	}

	private const int FIRST_DMA = 0x18;
	private const int RIGHT_BORDER = 0x18;//cosmetic
	public const int DMA_WIDTH = 227;// Agnus.DMA_END - Agnus.DMA_START;
	private const int SCREEN_WIDTH = (DMA_WIDTH-FIRST_DMA+RIGHT_BORDER) * 4; //227 (E3) * 4;
	private const int SCREEN_HEIGHT = 313 * 2; //x2 for scan double
	private int[] screen;

	public Denise(IChipsetClock clock, IChipsetDebugger debugger, IEmulationWindow emulationWindow, INativeOverlay nativeOverlay,
		IOptions<EmulationSettings> settings, ILogger<Denise> logger)
	{
		this.settings = settings.Value;
		this.clock = clock;
		this.debugger = debugger;
		this.logger = logger;
		this.emulationWindow = emulationWindow;

		ComputeDPFLookup();

		emulationWindow.SetPicture(SCREEN_WIDTH, SCREEN_HEIGHT);
		screen = emulationWindow.GetFramebuffer();
		nativeOverlay.Init(screen, SCREEN_WIDTH, SCREEN_HEIGHT);

		RunVerticalBlankStart();
	}

	//public FastUInt128 pixelMask;
	public int pixelMaskBit;
	//public uint pixelMaskValue;
	

	//public FastUInt128[] bpldatpix = new FastUInt128[8];
	//public ulong[] bpldatpixul = new ulong[8];

	public ValueTuple<ulong,ulong>[] bpldatpix = new ValueTuple<ulong, ulong>[8];

	public int planes;
	public int diwstrth = 0;
	public int diwstoph = 0;

	public int pixelLoop;
	public uint lastcol = 0;

	private int lineStart;
	public int dptr = 0;

	private Action pixelAction = () => { };

	public void Emulate()
	{
		//clock.WaitForTick();
		var clockState = clock.ClockState;

		if ((clockState&ChipsetClockState.StartOfFrame)!=0)
			RunVerticalBlankStart();

		if ((clockState & ChipsetClockState.StartOfLine)!=0)
			StartDeniseLine();

		RunDeniseTick();

		if ((clockState & ChipsetClockState.EndOfLine)!=0)
			EndDeniseLine();

		if ((clockState & ChipsetClockState.EndOfFrame)!=0)
			RunVerticalBlankEnd();

		//clock.Ack();
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
				Or(ref bpldatpix[i], bpldat[i], (16 - odd));
			else
				Or(ref bpldatpix[i], bpldat[i], (16 - even));

			//if ((i & 1) != 0)
			//	bpldatpixul[i] |= bpldat[i] << (16 - odd);
			//else
			//	bpldatpixul[i] |= bpldat[i] << (16 - even);
		}
	}

	public void WriteSprite(int s, ulong[] sprdata, ulong[] sprdatb, ushort[] sprctl)
	{
		sprdatapix[s] = sprdata[s];
		sprdatbpix[s] = sprdatb[s];
		this.sprctl[s] = sprctl[s];
		spriteMask[s] = 0x8000;
	}

	private void FirstPixel()
	{
		uint pixelBits;
		if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
			pixelBits = 15;
		else if ((fmode & 3) == 3)
			pixelBits = 63;
		else
			pixelBits = 31;

		//pixelMask.SetBit((int)(pixelBits + 16));
		pixelMaskBit = (int)(pixelBits + 16);
		//pixelMaskValue = 1u << pixelMaskBit;

		//clear sprites from wrapping from the right
		for (int s = 0; s < 8; s++)
			spriteMask[s] = 0;

		for (int i = 0; i < 8; i++)
		{
			//bpldatpix[i].Zero();
			//bpldatpixul[i] = 0;
			bpldatpix[i].Item1 = bpldatpix[i].Item2 = 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void NextPixel()
	{
		for (int i = 0; i < 8; i++)
		{
			//bpldatpix[i].Shl1();
			//bpldatpixul[i] <<= 1;
			bpldatpix[i].Item1 <<= 1;
			bpldatpix[i].Item1 |= bpldatpix[i].Item2 >> 63;
			bpldatpix[i].Item2 <<= 1;
		}
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

	private readonly ulong[] sprdatapix = new ulong[8];
	private readonly ulong[] sprdatbpix = new ulong[8];
	private readonly uint[] spriteMask = new uint[8];
	private readonly int[] clx = new int[8];

	private Action GetModeConversion()
	{
		return CopperBitplaneConvert;
		
		bool f = (fmode&3)==3;
		int bp = (bplcon0 >> 12) & 7;

		//DBF
		if ((bplcon0 & (1 << 10)) != 0) return f ? CopperBitplaneConvert : CopperBitplaneConvertDPF;

		//HAM6
		if (bp == 6 && ((bplcon0 & (1 << 11)) != 0)) return f ? CopperBitplaneConvert : CopperBitplaneConvert;
		
		//EHB
		if (bp == 6 && ((bplcon0 & (1 << 11)) == 0 &&
		                    (settings.ChipSet != ChipSet.AGA || (bplcon2 & (1 << 9)) == 0))) return f ? CopperBitplaneConvert : CopperBitplaneConvert;
		//HAM8
		if (bp == 8 && ((bplcon0 & (1 << 11)) != 0)) return f ? CopperBitplaneConvert : CopperBitplaneConvert;

		//Normal
		return f ? CopperBitplaneConvert : CopperBitplaneConvertNormal;
	}

	private void CopperBitplaneConvertDPF()
	{
		int m = (pixelLoop / 2) - 1; //2->0,4->1,8->3
		for (int p = 0; p < pixelLoop; p++)
		{
			//decode the colour

			uint col;

			byte pix = 0;
			for (int i = 0, b = 1; i < planes; i++, b <<= 1)
				pix |= (byte)(IsBitSet(ref bpldatpix[i], pixelMaskBit) ? b : 0);

			//for (int i = 0; i < planes; i++)
			//	bpldatpixul[i] <<= 1;
			NextPixel();

			//BPLAM
			pix ^= (byte)(bplcon4 >> 8);

			//pix &= debugger.bitplaneMask;
			//pix |= debugger.bitplaneMod;

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

			DoSprites(ref col, pix, (p & m) == m);

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

	private void CopperBitplaneConvertNormal()
	{
		int m = (pixelLoop / 2) - 1; //2->0,4->1,8->3
		for (int p = 0; p < pixelLoop; p++)
		{
			//decode the colour

			byte pix = 0;
			for (int i = 0, b = 1; i < planes; i++, b <<= 1)
				pix |= (byte)(IsBitSet(ref bpldatpix[i], pixelMaskBit) ? b : 0);

			//for (int i = 0; i < planes; i++)
			//	bpldatpixul[i] <<= 1;
			NextPixel();

			//BPLAM
			pix ^= (byte)(bplcon4 >> 8);

			//pix &= debugger.bitplaneMask;
			//pix |= debugger.bitplaneMod;
			var col = truecolour[pix];
			
			DoSprites(ref col, pix, (p & m) == m);

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

	private void CopperBitplaneConvertEHB()
	{
		int m = (pixelLoop / 2) - 1; //2->0,4->1,8->3
		for (int p = 0; p < pixelLoop; p++)
		{
			//decode the colour

			byte pix = 0;
			for (int i = 0, b = 1; i < planes; i++, b <<= 1)
				pix |= (byte)(IsBitSet(ref bpldatpix[i], pixelMaskBit) ? b : 0);

			//for (int i = 0; i < planes; i++)
			//	bpldatpixul[i] <<= 1;
			NextPixel();

			//BPLAM
			pix ^= (byte)(bplcon4 >> 8);
			//pix &= debugger.bitplaneMask;
			//pix |= debugger.bitplaneMod;
			
			//EHB
			uint col = truecolour[pix & 0x1f];
			if ((pix & 0b100000) != 0)
				col = (col & 0x00fefefe) >> 1;

			DoSprites(ref col, pix, (p & m) == m);

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

	//[MethodImpl(MethodImplOptions.AggressiveInlining|MethodImplOptions.AggressiveOptimization)]
	//[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsBitSet(ref ValueTuple<ulong, ulong> bp, int bit)
	{
		//ulong mask = 1UL << (bit & 63);
		//if (bit >= 64) return (bp.Item1 & mask) != 0;
		//return (bp.Item2 & mask) != 0;
		if (bit >= 64) return (bp.Item1 & (1UL << (bit - 64))) != 0;
		return (bp.Item2 & (1UL << bit)) != 0;
	}

	//[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	private static void Or(ref ValueTuple<ulong, ulong> bp, ulong bits, int shift)
	{
		bp.Item1 |= bits >> (64 - shift);
		bp.Item2 |= bits << shift;
	}

	//[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	private void CopperBitplaneConvert()
	{
		int m = (pixelLoop / 2) - 1; //2->0,4->1,8->3
		for (int p = 0; p < pixelLoop; p++)
		{
			//decode the colour

			uint col;

			byte pix = 0;
			byte b=1;
			for (int i = 0; i < planes; i++, b <<= 1)
				pix |= (IsBitSet(ref bpldatpix[i], pixelMaskBit) ? b : (byte)0);

			//byte pix = 0;
			//if (planes > 0) { pix  = IsBitSet(bpldatpix[0], pixelMaskBit) ? (byte)1   : (byte)0;
			//if (planes > 1) { pix |= IsBitSet(bpldatpix[1], pixelMaskBit) ? (byte)2   : (byte)0;
			//if (planes > 2) { pix |= IsBitSet(bpldatpix[2], pixelMaskBit) ? (byte)4   : (byte)0;
			//if (planes > 3) { pix |= IsBitSet(bpldatpix[3], pixelMaskBit) ? (byte)8   : (byte)0;
			//if (planes > 4) { pix |= IsBitSet(bpldatpix[4], pixelMaskBit) ? (byte)16  : (byte)0;
			//if (planes > 5) { pix |= IsBitSet(bpldatpix[5], pixelMaskBit) ? (byte)32  : (byte)0;
			//if (planes > 6) { pix |= IsBitSet(bpldatpix[6], pixelMaskBit) ? (byte)64  : (byte)0;
			//if (planes > 7) { pix |= IsBitSet(bpldatpix[7], pixelMaskBit) ? (byte)128 : (byte)0;
			//							} } } } } } } }
			NextPixel();

			//BPLAM
			pix ^= (byte)(bplcon4 >> 8);

			pix &= debugger.bitplaneMask;
			pix |= debugger.bitplaneMod;

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
			
			//remember the last colour for HAM modes
			lastcol = col;

			DoSprites(ref col, pix, (p&m)==m);

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
		}
	}

	private void DoSprites(ref uint col, byte pix, bool shift)
	{
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
				if (shift)
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

					if (shift)
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
	}

	private void StartDeniseLine()
	{
		lineStart = dptr;

		FirstPixel();
		lastcol = truecolour[0]; //todo: should be colour 0 at time of diwstrt
		//lineState = CopperLineState.LineStart;
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

		pixelAction = GetModeConversion();
	}

	private void UpdateBPLCON2()
	{
		pixelAction = GetModeConversion();
	}

	private void UpdateFMODE()
	{
		pixelAction = GetModeConversion();
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
		if (clock.HorizontalPos < FIRST_DMA)
			return;

		if (!blanking)
		{
			//is it the visible area horizontally?
			//when h >= diwstrt, bits are read out of the bitplane data, turned into pixels and output
			//HACK-the minuses are a hack.  the bitplanes are ready from fetching but they're not supposed to be copied into Denise until 4 cycles later
			if (clock.HorizontalPos >= ((diwstrth + debugger.diwSHack -0) >> 1)  && clock.HorizontalPos < ((diwstoph + debugger.diwEHack -0) >> 1) )
			{
				//CopperBitplaneConvert();
				pixelAction();
			}
			else
			{
				int m = (pixelLoop / 2) - 1; //2->0,4->1,8->3
				//outside horizontal area
				for (int p = 0; p < pixelLoop; p++)
				{
					NextPixel();
					if ((p & m) == m)
						for (int s = 0; s < 8; s++)
							spriteMask[s] >>= 1;
				}

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
			uint col = lastcol = truecolour[0];
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
	//private readonly ushort[] sprpos = new ushort[8];
	private readonly ushort[] sprctl = new ushort[8];
	//private readonly ushort[] sprdata = new ushort[8];
	//private readonly ushort[] sprdatb = new ushort[8];
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
			case ChipRegs.CLXDAT: value = clxdat; clxdat = 0; break;
		}

		return value;
	}

	public void Write(uint insaddr, uint address, ushort value)
	{
		switch (address)
		{
			case ChipRegs.BPLCON0: bplcon0 = value; UpdateBPLCON0(); break;
			case ChipRegs.BPLCON1: bplcon1 = value; break;
			case ChipRegs.BPLCON2: bplcon2 = value; UpdateBPLCON2(); break;
			case ChipRegs.BPLCON3: bplcon3 = value; break;
			case ChipRegs.BPLCON4: bplcon4 = value; break;

			case ChipRegs.DIWSTRT: diwstrt = value; diwhigh = 0; UpdateDIWSTRT(); break;
			case ChipRegs.DIWSTOP: diwstop = value; diwhigh = 0; UpdateDIWSTOP(); break;
			case ChipRegs.DIWHIGH: diwhigh = value; UpdateDIWHIGH(); break;

			case ChipRegs.FMODE: fmode = value; UpdateFMODE(); break;

			case ChipRegs.CLXCON: clxcon = value; clxcon2 = 0; break;
			case ChipRegs.CLXCON2: clxcon2 = value; break;
		}

		if (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31)
		{
			value &= 0x0fff;

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
		//cosmetics, draw some right border
		blanking = true;
		for (int i = 0; i < RIGHT_BORDER; i++)
			RunDeniseTick();

		//this should be a no-op
		//System.Diagnostics.Debug.Assert(SCREEN_WIDTH - (dptr - lineStart) == 0);
		dptr += SCREEN_WIDTH - (dptr - lineStart);

		//scan double
		for (int i = lineStart; i < lineStart + SCREEN_WIDTH; i++)
			screen[dptr++] = screen[i];
	}

	public void Reset(uint copperPC)
	{
		dptr = 0;
	}

	public uint[] DebugGetPalette()
	{
		return truecolour;
	}

	public uint DebugChipsetRead(uint address, Size size)
	{
		uint value = 0;
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
}
