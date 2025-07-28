using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.NativeOverlay;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise;

public class Denise : IDenise
{
	private readonly EmulationSettings settings;
	private readonly IChipsetClock clock;
	private readonly IChipsetDebugger debugger;
	private readonly ILogger logger;
	private readonly IEmulationWindow emulationWindow;
	private readonly IBpldatPix bpldatPix;
	
	[Flags]
	public enum BPLCON0 : uint
	{
		HiRes = 1 << 15,
		HAM = 1 << 11,
		DPF = 1<<10,
		SuperHiRes = 1 << 6,
		Interlace = 1 << 2
	}
	[Flags]
	public enum BPLCON2 : uint
	{
		NoEHB = 1 << 9
	}
	private const int FIRST_DMA = 0;//0x18*2;
	private const int RIGHT_BORDER = 0x18;//cosmetic

	public const int DMA_WIDTH = 227;// Agnus.DMA_END - Agnus.DMA_START;
	private const int SCREEN_WIDTH = (DMA_WIDTH-FIRST_DMA+RIGHT_BORDER) * 4; //227 (E3) * 4;
	private const int SCREEN_HEIGHT = 313 * 2; //x2 for scan double
	private int[] screen;

	public Denise(IBpldatPix bpldatPix, IChipsetClock clock, IChipsetDebugger debugger, IEmulationWindow emulationWindow, INativeOverlay nativeOverlay,
		IOptions<EmulationSettings> settings, ILogger<Denise> logger)
	{
		this.bpldatPix = bpldatPix;
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

	[Persist]
	private int planes;
	[Persist]
	private int diwstrth = 0;
	[Persist]
	private int diwstoph = 0;

	[Persist]
	private int pixelLoop;
	[Persist]
	private uint lastcol = 0;

	[Persist]
	private int lineStart;
	[Persist]
	private int dptr = 0;

	private Action pixelAction = () => { };

	public void Emulate()
	{
		//clock.WaitForTick();
		var clockState = clock.ClockState;

		if ((clockState&ChipsetClockState.StartOfFrame)!=0)
			RunVerticalBlankStart();

		if ((clockState & ChipsetClockState.StartOfLine) != 0) 
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

	[Persist]
	private Blanking blankingStatus;

	public void SetBlankingStatus(Blanking blanking)
	{
		blankingStatus = blanking;
	}

	public void WriteBitplanes(ulong[] bpldat)
	{
		//scrolling
		int even = bplcon1 & 0xf;
		int odd = bplcon1 >> 4 & 0xf;

		bpldatPix.WriteBitplanes(ref bpldat, even, odd);
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

		bpldatPix.SetPixelBitMask(pixelBits);

		//clear sprites from wrapping from the right
		for (int s = 0; s < 8; s++)
			spriteMask[s] = 0;

		bpldatPix.Clear();
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

	[Persist]
	private readonly ulong[] sprdatapix = new ulong[8];
	[Persist]
	private readonly ulong[] sprdatbpix = new ulong[8];
	[Persist]
	private readonly uint[] spriteMask = new uint[8];
	[Persist]
	private readonly int[] clx = new int[8];
	[Persist]
	private readonly byte[] sprpix = new byte[8];

	private Action GetModeConversion()
	{
		//return CopperBitplaneConvert;

		int bp = (bplcon0 >> 12) & 7;

		//DBF
		if ((bplcon0 & (uint)BPLCON0.DPF) != 0) return CopperBitplaneConvertDPF;

		//HAM6
		if (bp == 6 && ((bplcon0 & (uint)BPLCON0.HAM) != 0)) return CopperBitplaneConvertOther;

		//EHB
		if (bp == 6 && ((bplcon0 & (uint)BPLCON0.HAM) == 0) &&
			(settings.ChipSet != ChipSet.AGA || (bplcon2 & (uint)BPLCON2.NoEHB) == 0)) return CopperBitplaneConvertOther;

		//HAM8
		if (bp == 8 && ((bplcon0 & (uint)BPLCON0.HAM) != 0)) return CopperBitplaneConvertOther;

		//BPLAM is set
		if ((bplcon4 >> 8) != 0) return CopperBitplaneConvertOther;

		//Normal
		return CopperBitplaneConvertNormal;
	}

	private void CopperBitplaneConvertNormal()
	{
		int m = pixelLoop / 2 - 1; //2->0,4->1,8->3
		for (int p = 0; p < pixelLoop; p++)
		{
			uint pix = bpldatPix.GetPixel(planes);
			uint col = truecolour[pix];
			
			//remember the last colour for HAM modes
			lastcol = col;

			DoSprites(ref col, (byte)pix, (p&m)==m);

			//pixel double
			//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
			//since we've only set up a hi-res window, it's 2x, 1x and 0.5x
			switch (pixelLoop)
			{
				case 8:
					//hack for the 0.5x above - skip every other horizontal pixel
					if ((p & 1) == 0)
						screen[dptr++] = (int)col;
					break;
				case 4:
					screen[dptr++] = (int)col;
					break;
				default:
					screen[dptr++] = (int)col;
					screen[dptr++] = (int)col;
					break;
			}
		}
	}

	private void CopperBitplaneConvertDPF()
	{
		int m = pixelLoop / 2 - 1; //2->0,4->1,8->3
		for (int p = 0; p < pixelLoop; p++)
		{
			uint col;

			uint pix = bpldatPix.GetPixel(planes);

			//DPF
			uint pix0 = dpfLookup[pix];
			uint pix1 = dpfLookup[pix >> 1];

			uint col0 = truecolour[pix0];
			uint col1 = truecolour[pix1 == 0 ? 0 : pix1 + 8];

			//which playfield is in front?
			if ((bplcon2 & 1 << 6) != 0)
				col = pix1 != 0 ? col1 : col0;
			else
				col = pix0 != 0 ? col0 : col1;

			//remember the last colour for HAM modes
			lastcol = col;

			DoSprites(ref col, (byte)pix, (p & m) == m);

			//pixel double
			//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
			//since we've only set up a hi-res window, it's 2x, 1x and 0.5x
			//pixel double
			//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
			//since we've only set up a hi-res window, it's 2x, 1x and 0.5x
			switch (pixelLoop)
			{
				case 8:
					//hack for the 0.5x above - skip every other horizontal pixel
					if ((p & 1) == 0)
						screen[dptr++] = (int)col;
					break;
				case 4:
					screen[dptr++] = (int)col;
					break;
				default:
					screen[dptr++] = (int)col;
					screen[dptr++] = (int)col;
					break;
			}
		}
	}

	private void CopperBitplaneConvertOther()
	{
		int m = pixelLoop / 2 - 1; //2->0,4->1,8->3
		for (int p = 0; p < pixelLoop; p++)
		{
			uint col;

			uint pix = bpldatPix.GetPixel(planes);

			//bpldatPix.NextPixel();

			//BPLAM
			pix ^= (uint)(bplcon4 >> 8);

			//pix &= debugger.bitplaneMask;
			//pix |= debugger.bitplaneMod;

			if ((bplcon0 & (uint)BPLCON0.DPF) != 0)
			{
				//DPF
				uint pix0 = dpfLookup[pix];
				uint pix1 = dpfLookup[pix >> 1];

				uint col0 = truecolour[pix0];
				uint col1 = truecolour[pix1 == 0 ? 0 : pix1 + 8];

				//which playfield is in front?
				if ((bplcon2 & 1 << 6) != 0)
					col = pix1 != 0 ? col1 : col0;
				else
					col = pix0 != 0 ? col0 : col1;

				//pix1 = (pix1 == 0) ? (byte)0 : (byte)(pix1 + 8);
				//if ((bplcon2 & (1 << 6)) != 0)
				//	pix = pix1 != 0 ? pix1 : pix0;
				//else
				//	pix = pix0 != 0 ? pix0 : pix1;
			}
			else if (planes == 6 && (bplcon0 & (uint)BPLCON0.HAM) != 0)
			{
				//HAM6
				uint ham = pix & 0b11_0000;
				pix &= 0xf;
				if (ham == 0)
				{
					col = truecolour[pix];
				}
				else
				{
					ham >>= 4;
					uint px = pix * 0x11;
					if (ham == 1)
					{
						//col+B
						col = lastcol & 0xffffff00 | px;
					}
					else if (ham == 3)
					{
						//col+G
						col = lastcol & 0xffff00ff | px << 8;
					}
					else
					{
						//col+R
						col = lastcol & 0xff00ffff | px << 8 + 8;
					}
				}
			}
			else if (planes == 6 && (bplcon0 & (uint)BPLCON0.HAM) == 0 &&
					 (settings.ChipSet != ChipSet.AGA || (bplcon2 & (uint)BPLCON2.NoEHB) == 0))
			{
				//EHB
				col = truecolour[pix & 0x1f];
				if ((pix & 0b100000) != 0)
					col = (col & 0x00fefefe) >> 1;
			}
			else if (planes == 8 && (bplcon0 & (uint)BPLCON0.HAM) != 0)
			{
				//HAM8
				uint ham = pix & 0b11;
				pix &= 0xfc;
				if (ham == 0)
				{
					col = truecolour[pix];
				}
				else
				{
					uint px = pix | pix >> 6;
					if (ham == 1)
					{
						//col+B
						col = lastcol & 0xffffff00 | px;
					}
					else if (ham == 3)
					{
						//col+G
						col = lastcol & 0xffff00ff | px << 8;
					}
					else
					{
						//col+R
						col = lastcol & 0xff00ffff | px << 8 + 8;
					}
				}
			}
			else
			{
				col = truecolour[pix];
			}

			//remember the last colour for HAM modes
			lastcol = col;

			DoSprites(ref col, (byte)pix, (p & m) == m);

			//pixel double
			//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
			//since we've only set up a hi-res window, it's 2x, 1x and 0.5x
			//pixel double
			//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
			//since we've only set up a hi-res window, it's 2x, 1x and 0.5x
			switch (pixelLoop)
			{
				case 8:
					//hack for the 0.5x above - skip every other horizontal pixel
					if ((p & 1) == 0)
						screen[dptr++] = (int)col;
					break;
				case 4:
					screen[dptr++] = (int)col;
					break;
				default:
					screen[dptr++] = (int)col;
					screen[dptr++] = (int)col;
					break;
			}
		}
	}


	private readonly uint[] bits = { 0, 0, 0, 0, 0, 0, 0, 0 };
	private void DoSprites(ref uint col, byte pix, bool shift)
	{
		DoSprites2(ref col, pix, shift);
		return;

		uint active = 0;
		uint attached = 0;

		for (int s = 0; s < 8; s++)
		{
			active <<= 1;
			attached <<= 1;

			bits[s] = 0;
			attached |= (uint)(sprctl[s] >> 7) & 1;
			if (spriteMask[s] != 0)
			{
				active |= 1;
				bits[s] = ((sprdatapix[s] & spriteMask[s]) != 0 ? 1u : 0) + ((sprdatbpix[s] & spriteMask[s]) != 0 ? 2u : 0);
			}
		}
		//attached/active bits are now like so:
		//01234567

		uint oattached = attached;

		for (int s = 7; s >= 0; s -= 2)
		{
			//if odd sprite is attached, shift its bits
			if ((attached & 1) != 0)
				bits[s] <<= 2;

			attached >>= 2;
		}

		attached = oattached;

		uint scol = 0;
		int sp = 7;
		while (sp >= 0)
		{
			//if first, or both are attached (check the attached bit on the odd sprite)
			//https://eab.abime.net/showthread.php?t=113291&highlight=Attached+sprites
			//only question is if the attach bit on even numbered sprites has any effect
			if ((attached & 1) != 0)
			{
				//attached (even when not on top of each other)
				scol = 0;
				if ((active & 1) != 0) scol |= bits[sp];
				if ((active & 2) != 0) scol |= bits[sp-1];

				if (scol != 0)
				{ 
					col = truecolour[16 + scol];
				}
				sp -= 2;
				active >>= 2;
				attached >>= 2;
			}
			else
			{
				//odd
				scol = 0;
				if ((active & 1) != 0) scol |= bits[sp];

				if (scol != 0)
				{ 
					col = truecolour[16 + 4 * (sp >> 1) + scol];
				}

				sp--;
				active >>= 1;
				attached >>= 1;

				//even
				scol = 0;
				if ((active & 1) != 0) scol |= bits[sp];

				if (scol != 0)
				{
					col = truecolour[16 + 4 * (sp >> 1) + scol];
				}

				sp--;
				active >>= 1;
				attached >>= 1;
			}
		}

		if (shift)
		{
			for (int s = 0; s < 8; s++)
				spriteMask[s] >>= 1;
		}
	}

	private void DoSprites2(ref uint col, byte pix, bool shift)
	{
		//sprites
		int clxm = 0;
		for (int s = 7; s >= 0; s--)
		{
			clx[s] = 0;
			sprpix[s] = 0;
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

				//byte finalpix = 0;
				if (attached)
				{
					s--;
					spix <<= 2;
					int apix = ((sprdatapix[s] & x) != 0 ? 1 : 0) + ((sprdatbpix[s] & x) != 0 ? 2 : 0);
					clx[s] = apix;
					sprpix[s] = 0;
					clxm |= clx[s];
					spix += apix;

					if (shift)
						spriteMask[s] >>= 1;
					if (spix != 0)
					{ 
						//col = truecolour[16 + spix];
						//sprpix[s] = sprpix[s + 1] = (byte)(16+spix);
						sprpix[s] = (byte)(16 + spix);
						col = truecolour[sprpix[s]];
					}
				}
				else
				{
					if (spix != 0)
					{	//col = truecolour[16 + 4 * (s >> 1) + spix];
						sprpix[s] = (byte)(16 + 4 * (s >> 1) + spix);
						col = truecolour[sprpix[s]];
					}
				}
			}
		}

		//uint originalcol = col;
		////0,1,2,3,4 in bplcon2
		//int pri2 = (bplcon2 >> 3) & 7;
		//int pri1;
		//if ((bplcon0 & (1 << 10)) != 0)
		//	pri1 = bplcon2 & 7;
		//else
		//	pri1 = pri2;
		//pri2 <<= 1;
		//pri1 <<= 1;
		//if (pri2 == 8 || pri1 == 8) col = originalcol;
		//for (int s = 7; s >= 0; s--)
		//{
		//	if (sprpix[s] != 0)
		//		col = truecolour[sprpix[s]];
		//	if (pri2 == s && pix != 0)
		//		col = originalcol;
		//	if (pri1 == s && pix != 0)
		//		col = originalcol;
		//}

		//sprite collision

		if (clxm != 0)
		{
			int clxconMatch = clxcon & 0x3f | (clxcon2 & 0x3) << 6;
			int clxconEnable = clxcon >> 6 & 0x3f | clxcon2 & 0xc0;

			//combine in the enabled odd-numbered sprites
			for (int s = 0; s < 4; s++)
			{
				if ((clxcon & 0x1000 << s) != 0)
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

			//TODO. none of the code below passes the tests in Versatile Amiga Test Program ROM
			//fortunately, hardly any game uses this feature, so it's not too important

			//v1 - are bits set at the same position in both playfields?
			//if (((pix & 0b10101010) >> 1 & pix) != 0)
			//	clxdat |= 1;

			//v1.5
			uint match = (uint)((pix ^ ~clxconMatch) & clxconEnable);
			if (match == 0)
				clxdat |= 1;

			//v2 - are enabled/colour bits set at the same position in both playfields?
			//uint match = (uint)((pix ^ ~clxconMatch) & clxconEnable);
			//if (((match & 0b10101010) >> 1 & match) != 0)
			//	clxdat |= 1;

			//v3 - does playfield 1 collide with playfield 2 and vice versa?
			//uint match = (uint)((pix ^ ~clxconMatch) & clxconEnable);
			//if (((match & 0b10101010) >> 1 & pix) != 0)
			//	clxdat |= 1;
			//if (((match & 0b01010101) << 1 & pix) != 0)
			//	clxdat |= 1;

			//v4
			//https://eab.abime.net/showpost.php?p=965074&postcount=2
			/*
	match = true
	loop twice (first select odd planes, next select even planes)
	 if (dualplayfield)
	   match = true
	 check plane collision condition (odd or even planes, enabled plane's bit pattern == "match" value?)
	 if no bitplane collision: match = false
	 if (match == true) set sprite collision bit in CLXDAT if non-zero sprites in same bitplane pixel position
	end loop		 
			*/
			/*
			{
				bool match = true;
			int clp;

			clp = (pix ^ ~clxconMatch) & clxconEnable & 0b01010101;
			clp<<=1;
			if ((clp&pix) == 0) match = false;
			if (match) clxdat |= 1;

			if ((bplcon0 & (uint)BPLCON0.DPF) != 0) match = true;
			clp = (pix ^ ~clxconMatch) & clxconEnable & 0b10101010;
			clp>>=1;
			if ((clp & pix) == 0) match = false;
			if (match) clxdat |= 1;
			}
			*/

			//v5 - vAmiga algorithm
			//	if ((pix & clxconEnable & 0b01010101) != (clxconMatch & clxconEnable & 0b01010101)) goto no;
			//	if ((pix & clxconEnable & 0b10101010) != (clxconMatch & clxconEnable & 0b10101010)) goto no;
			//	clxdat |= 1; 
			//no:;
		}
	}

	private void StartDeniseLine()
	{
		dptr = (int)(clock.VerticalPos * SCREEN_WIDTH * 2);
		lineStart = dptr;

		FirstPixel();
	}

	private void UpdateBPLCON0()
	{
		planes = bplcon0 >> 12 & 7;

		//logger.LogTrace($"D BPLCON0 {bplcon0:X4} {planes} {clock.TimeStamp()}");

		if (settings.ChipSet == ChipSet.AGA)
		{
			if (planes == 0 && (bplcon0 & 1 << 4) != 0)
				planes = 8;
		}

		//https://eab.abime.net/showthread.php?t=111329

		//how many pixels should be fetched per clock in the current mode?
		if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
		{
			//4 colour clocks, fetch 16 pixels
			//1 colour clock, draw 4 pixel
			pixelLoop = 4;
		}
		else if ((bplcon0 & (uint)BPLCON0.SuperHiRes) != 0)
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

	private uint oldbplcon2;
	private void UpdateBPLCON2()
	{
		//if (bplcon2 != oldbplcon2)
		if (false)
		{
			int pf1 = bplcon2 & 7;
			int pf2 = bplcon2 >> 3&7;
			bool dpf = (bplcon0 & 1 << 10) != 0;
			List<string> s = ["SP01", "SP23", "SP45", "SP67"];
			s.Insert(pf1, "PF1");
			if (pf2 >= pf1) pf2++;
			s.Insert(pf2, "PF2");
			logger.LogTrace($"{(dpf?"DPF":" X ")} PF{(bplcon2>>6&1)+1} {string.Join(' ',s)}");
			oldbplcon2 = bplcon2;
		}
		pixelAction = GetModeConversion();
	}

	private void UpdateFMODE()
	{
		pixelAction = GetModeConversion();
	}

	private void UpdateBPLCON4()
	{
		pixelAction = GetModeConversion();
	}

	private void UpdateDIWSTRT()
	{
		diwstrth = diwstrt & 0xff;
	}

	private void UpdateDIWSTOP()
	{
		diwstoph = diwstop & 0xff | 0x100;
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
		if (clock.DeniseHorizontalPos < FIRST_DMA)
			return;

		if (blankingStatus == Blanking.None)
		{
			//is it the visible area horizontally?
			//when h >= diwstrt, bits are read out of the bitplane data, turned into pixels and output
			//HACK-the minuses are a hack.  the bitplanes are ready from fetching but they're not supposed to be copied into Denise until 4 cycles later
			if (clock.DeniseHorizontalPos >= diwstrth + debugger.diwSHack -0   && clock.DeniseHorizontalPos < diwstoph + debugger.diwEHack -0  )
			{
				//CopperBitplaneConvert();
				pixelAction();
			}
			else
			{
				int m = pixelLoop / 2 - 1; //2->0,4->1,8->3
				//outside horizontal area
				for (int p = 0; p < pixelLoop; p++)
				{
					bpldatPix.NextPixel();
					if ((p & m) == m)
						for (int s = 0; s < 8; s++)
							spriteMask[s] >>= 1;
				}

				//output colour 0 pixels
				uint col = lastcol = truecolour[0];
				//for (int k = 0; k < 4; k++)
				//	screen[dptr++] = (int)col;
				//Array.Fill(screen, (int)col, dptr, 4); dptr += 4;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
			}
		}
		else
		{
			//if (blankingStatus != Blanking.OutsideDisplayWindow)
			if (false)
			{
				//horizontal/vertical blanking
				uint c0 = 0xffffff;
				uint c1 = 0x000000;
				if ((blankingStatus & Blanking.HorizontalBlank)!=0) c0 = 0xff0000;
				if ((blankingStatus & Blanking.VerticalBlank)!=0) c1 = 0x0000ff;
				uint col = ((clock.HorizontalPos ^ clock.VerticalPos) & 1) != 0 ? c0 : c1;
				lastcol = truecolour[0];
				//for (int k = 0; k < 4; k++)
				//	screen[dptr++] = (int)col;
				//Array.Fill(screen, (int)col, dptr, 4); dptr+= 4;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
			}
			else
			{ 
				//outside display window vertical area

				//output colour 0 pixels
				uint col = lastcol = truecolour[0];
				//for (int k = 0; k < 4; k++)
				//	screen[dptr++] = (int)col;
				//Array.Fill(screen, (int)col, dptr, 4); dptr += 4;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
				screen[dptr++] = (int)col;
			}
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

	[Persist]
	private readonly ushort[] colour = new ushort[256];
	[Persist]
	private readonly ushort[] lowcolour = new ushort[256];
	[Persist]
	private readonly uint[] truecolour = new uint[256];

	public ushort Read(uint insaddr, uint address)
	{
		ushort value = 0;
		//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

		switch (address)
		{
			case ChipRegs.CLXDAT: value = clxdat; clxdat = 0; break;

			case ChipRegs.STREQU:
			case ChipRegs.STRHOR:
			case ChipRegs.STRLONG:
			case ChipRegs.STRVBL:
				logger.LogTrace($"Strobe R {ChipRegs.Name(address)} @ {insaddr:X8}");
				break;
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
			case ChipRegs.BPLCON4: bplcon4 = value; UpdateBPLCON4(); break;

			case ChipRegs.DIWSTRT: diwstrt = value; diwhigh = 0; UpdateDIWSTRT(); break;
			case ChipRegs.DIWSTOP: diwstop = value; diwhigh = 0; UpdateDIWSTOP(); break;
			case ChipRegs.DIWHIGH: diwhigh = value; UpdateDIWHIGH(); break;

			case ChipRegs.FMODE: fmode = value; UpdateFMODE(); break;

			case ChipRegs.CLXCON: clxcon = value; clxcon2 = 0; break;
			case ChipRegs.CLXCON2: clxcon2 = value; break;

			case >= ChipRegs.COLOR00 and <= ChipRegs.COLOR31:
				{
					value &= 0x0fff;

					int bank = (bplcon3 & 0b111_00000_00000000) >> 13 - 5;

					//Amiga colour
					int index = (int)(bank + (address - ChipRegs.COLOR00 >> 1));

					int loct = bplcon3 & 1 << 9;
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
					var rgb = Explode(colour[index]) | Explode(lowcolour[index]) >> 4;
					truecolour[index] = rgb;
					debugger.SetColor(index, rgb);
					break;
				}

			case ChipRegs.STREQU:
			case ChipRegs.STRHOR:
			case ChipRegs.STRLONG:
			case ChipRegs.STRVBL:
				logger.LogTrace($"Strobe W {ChipRegs.Name(address)} @ {insaddr:X8}");
				break;
		}
	}

	private uint Explode(ushort c)
	{
		return (uint)((c & 0xf) << 4 | (c & 0xf0) << 8 | (c & 0xf00) << 12);
	}

	private void EndDeniseLine()
	{
		//cosmetics, draw some right border
		blankingStatus = Blanking.OutsideDisplayWindow;
		for (int i = 0; i < RIGHT_BORDER; i++)
			RunDeniseTick();

		//this should be a no-op
		//System.Diagnostics.Debug.Assert(SCREEN_WIDTH - (dptr - lineStart) == 0);
		dptr += SCREEN_WIDTH - (dptr - lineStart);

		//scan double
		//for (int i = lineStart; i < lineStart + SCREEN_WIDTH; i++)
		//	screen[dptr++] = screen[i];
		// Replace the for loop with Array.Copy for better performance and clarity
		Array.Copy(screen, lineStart, screen, dptr, SCREEN_WIDTH);
		dptr += SCREEN_WIDTH;
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

			case >= ChipRegs.COLOR00 and <= ChipRegs.COLOR31:
				{
					int bank = (bplcon3 & 0b111_00000_00000000) >> 13 - 5;

					int loct = bplcon3 & 1 << 9;

					//Amiga colour
					int index = (int)(bank + (address - ChipRegs.COLOR00 >> 1));

					if (loct != 0)
						value = lowcolour[index];
					else
						value = colour[index];
					break;
				}
		}

		return value;
	}

	public void Save(JArray obj)
	{
		var jo = PersistenceManager.ToJObject(this, "denise");
		bpldatPix.Save(obj);
		obj.Add(jo);
	}

	public void Load(JObject obj)
	{
		if (!PersistenceManager.Is(obj, "denise")) return;

		PersistenceManager.FromJObject(this, obj);
		bpldatPix.Load(obj);
	}
}
