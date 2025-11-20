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
using System.Runtime.CompilerServices;

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

		bpldatPix.SetPixelBitMask(15);

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

	private Func<uint, uint> pixelAction = (uint pix) => { return 0; };

	public void Emulate()
	{
		var clockState = clock.ClockState;

		if ((clockState&ChipsetClockState.StartOfFrame)!=0)
			RunVerticalBlankStart();

		if ((clockState & ChipsetClockState.StartOfLine) != 0) 
			StartDeniseLine();

		//for (int p = 0; p < pixelLoop; p++)
		//{ 
		//	ClockBuffer();
		//	RunDeniseTick(p);
		//}
		if (pixelLoop == 2)
		{
			ClockBuffer();
			RunDeniseTick(0,0);
			ClockBuffer();
			RunDeniseTick(1,1);
		}
		else if (pixelLoop == 4)
		{
			ClockBuffer();
			RunDeniseTick(0,0);
			ClockBuffer();
			RunDeniseTick(0,1);
			ClockBuffer();
			RunDeniseTick(1,2);
			ClockBuffer();
			RunDeniseTick(1,3);
		}
		else if (pixelLoop == 8)
		{
			ClockBuffer();
			RunDeniseTick(0,0);
			ClockBuffer();
			RunDeniseTick(0,1);
			ClockBuffer();
			RunDeniseTick(0,2);
			ClockBuffer();
			RunDeniseTick(0,3);
			ClockBuffer();
			RunDeniseTick(1,4);
			ClockBuffer();
			RunDeniseTick(1,5);
			ClockBuffer();
			RunDeniseTick(1,6);
			ClockBuffer();
			RunDeniseTick(1,7);
		}

		if ((clockState & ChipsetClockState.EndOfLine)!=0)
			EndDeniseLine();

		if ((clockState & ChipsetClockState.EndOfFrame)!=0)
			RunVerticalBlankEnd();
	}

	public void Reset()
	{
		bplcon4 = 0x0011;//OSPRM/ESPRM both set to 1 (sprites use bank 1 for colours)
	}

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

	[Persist]
	private int scrollhack = 0;

	public void SetDDFSTRTScrollHack(uint ddfstrt)
	{
		scrollhack = 0;
		return;
		if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
		{
			if ((ddfstrt & 3) != 0) logger.LogTrace("DDFSTRT is unaligned (hi-res)");
		}
		else if ((bplcon0 & (uint)BPLCON0.SuperHiRes) != 0)
		{
			if ((ddfstrt & 1) != 0) logger.LogTrace("DDFSTRT is unaligned (super hi-res)");
		}
		else
		{
			if ((ddfstrt&4)!=0) scrollhack = 8;
			if ((ddfstrt&3)!=0) logger.LogTrace("DDFSTRT is unaligned");
		}
	}

	private void ApplyDDFSTRTScrollHack(ref int even, ref int odd)
	{
		even += scrollhack; even &= 0xf;
		odd += scrollhack; odd &= 0xf;
	}

	public void WriteBitplanes(ulong[] bpldat)
	{
		//scrolling
		int even = bplcon1 & 0xf;
		int odd = bplcon1 >> 4 & 0xf;

		ApplyDDFSTRTScrollHack(ref even, ref odd);

		if (bufferDelayBase + debugger.bplDelayHack == 0)
			bpldatPix.WriteBitplanes(ref bpldat, even, odd);
		else
			Buffer(bpldat);
	}

	private ulong[] buffered = new ulong[8];
	private int bufferDelay = 0;
	private const int bufferDelayBase = 0;

	private void Buffer(ulong[] bpldat)
	{
		Array.Copy(bpldat, buffered, 8);
		bufferDelay = bufferDelayBase + debugger.bplDelayHack;
	}
	private void ClockBuffer()
	{
		if (bufferDelay > 0)
		{
			bufferDelay--;
			if (bufferDelay == 0)
			{
				//scrolling
				int even = bplcon1 & 0xf;
				int odd = bplcon1 >> 4 & 0xf;

				ApplyDDFSTRTScrollHack(ref even, ref odd);

				bpldatPix.WriteBitplanes(ref buffered, even, odd);
			}
		}
	}

	public void WriteSprite(uint s, ulong[] sprdata, ulong[] sprdatb, ushort[] sprctl)
	{
		sprdatapix[s] = sprdata[s];
		sprdatbpix[s] = sprdatb[s];
		this.sprctl[s] = sprctl[s];
		int spriteFetch = (fmode>>2)&3;

		if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS)
		{ 
			spriteMask[s] = 0x8000;
		}
		else
		{ 
			switch (spriteFetch)
			{
				case 0: spriteMask[s] = 0x8000; break;
				case 1: spriteMask[s] = 0x80000000; break;
				case 2: spriteMask[s] = 0x80000000; break;
				case 3: spriteMask[s] = 0x8000000000000000; break;
			}
		}
	}

	private void FirstPixel()
	{
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
	private readonly ulong[] spriteMask = new ulong[8];
	[Persist]
	private readonly int[] clx = new int[8];

	private Func<uint,uint> GetModeConversion()
	{
		int bp = (bplcon0 >> 12) & 7;

		//BPLAM is set
		if ((bplcon4 >> 8) != 0) return CopperBitplaneConvertOther;

		//DPF
		if ((bplcon0 & (uint)BPLCON0.DPF) != 0) return CopperBitplaneConvertDPF;

		//HAM6
		if (bp == 6 && ((bplcon0 & (uint)BPLCON0.HAM) != 0)) return CopperBitplaneConvertHAM6;

		//EHB
		if (bp == 6 && ((bplcon0 & (uint)BPLCON0.HAM) == 0) &&
			(settings.ChipSet != ChipSet.AGA || (bplcon2 & (uint)BPLCON2.NoEHB) == 0)) return CopperBitplaneConvertEHB;

		//HAM8
		if (bp == 8 && ((bplcon0 & (uint)BPLCON0.HAM) != 0)) return CopperBitplaneConvertHAM8;

		//Normal
		return CopperBitplaneConvertNormal;
	}

	private uint CopperBitplaneConvertNormal(uint pix)
	{
		return truecolour[pix];
	}

	private uint CopperBitplaneConvertDPF(uint pix)
	{
		uint col;

		//DPF
		uint pix0 = dpfLookup[pix];
		uint pix1 = dpfLookup[pix >> 1];

		uint col0 = truecolour[pix0];
		uint col1 = truecolour[pix1 == 0 ? 0 : pix1 + 8];//todo - it's not always 8 in AGA

		//which playfield is in front?
		if ((bplcon2 & (1 << 6)) != 0)
			col = pix1 != 0 ? col1 : col0;
		else
			col = pix0 != 0 ? col0 : col1;

		return col;
	}

	private uint CopperBitplaneConvertHAM6(uint pix)
	{
		uint col;

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
			if (ham == 1) col = lastcol & 0xffffff00 | px; //col+B
			else if (ham == 3) col = lastcol & 0xffff00ff | (px << 8); //col+G
			else col = lastcol & 0xff00ffff | (px << 16); //col+R
		}

		return col;
	}

	private uint CopperBitplaneConvertHAM8(uint pix)
	{
		uint col;
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
			if (ham == 1) col = lastcol & 0xffffff00 | px; //col+B
			else if (ham == 3) col = lastcol & 0xffff00ff | (px << 8); //col+G
			else col = lastcol & 0xff00ffff | (px << 16); //col+R
		}
		return col;
	}

	private uint CopperBitplaneConvertEHB(uint pix)
	{
		//EHB
		uint col = truecolour[pix & 0x1f];
		if ((pix & 0b100000) != 0)
			col = (col & 0x00fefefe) >> 1;

		return col;
	}

	private uint CopperBitplaneConvertOther(uint pix)
	{
		uint col;

		//BPLAM
		pix ^= (uint)(bplcon4 >> 8);

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
				if (ham == 1) col = lastcol & 0xffffff00 | px; //col+B
				else if (ham == 3) col = lastcol & 0xffff00ff | (px << 8); //col+G
				else col = lastcol & 0xff00ffff | (px << 16); //col+R
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
				if (ham == 1) col = lastcol & 0xffffff00 | px; //col+B
				else if (ham == 3) col = lastcol & 0xffff00ff | (px << 8); //col+G
				else col = lastcol & 0xff00ffff | (px << 16); //col+R
			}
		}
		else
		{
			col = truecolour[pix];
		}

		return col;
	}

	private readonly uint[] bits = { 0, 0, 0, 0, 0, 0, 0, 0 };
	private void DoSprites(ref uint col, byte pix, int p)
	{
		uint active = 0;
		uint attached = 0;

		int clxm = 0;
		for (int s = 0; s < 8; s++)
		{
			active <<= 1;
			attached <<= 1;

			//colour bits
			bits[s] = 0;
			attached |= (uint)(sprctl[s] >> 7) & 1;
			if (spriteMask[s] != 0)
			{
				active |= 1;
				bits[s] = ((sprdatapix[s] & spriteMask[s]) != 0 ? 1u : 0) + ((sprdatbpix[s] & spriteMask[s]) != 0 ? 2u : 0);
			}

			//collision bits
			clx[s] = (int)bits[s];
			clxm |= clx[s];//keep a track of if ANY sprites have any bits set (optimisation)
		}
		//attached/active bits are now like so:
		//01234567

		if (clxm == 0) goto nospritebits;

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
		//bplcon4 defaults to 0x0011
		uint bankEven = (uint)((bplcon4 & 0xf) * 16);
		uint bankOdd = (uint)(((bplcon4>>4) & 0xf) * 16);

		//bplcon2 informs us the priority of sprites/playfields
		//in single PF mode, only sprpri2 is used
		uint sprpri1 = (uint)(bplcon2 & 7);
		uint sprpri2 = (uint)((bplcon2>>3) & 7);
		if ((bplcon0 & (uint)BPLCON0.DPF) == 0) sprpri1 = sprpri2;
		//000 - pf1 s01 s23 s45 s67
		//001 - s01 pf1 s23 s45 s67
		//010 - s01 s23 pf1 s45 s67
		//011 - s01 s23 s45 pf1 s67
		//100 - s01 s23 s45 s67 pf1
		//other = special, see here https://eab.abime.net/showthread.php?t=119463

		//todo, fix dual-playfield (need to know which playfield 'won' so we can choose between sprpri1/2)
		if ((bplcon0 & (uint)BPLCON0.DPF) != 0)
			sprpri1 = sprpri2 = 4;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool IsVis() 
		{
			//either the spritebank is in front of the playfield, or the playfield is transparent
			int bank = sp>>1;
			return bank < sprpri2 || pix == 0;
		}

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

				if (scol != 0 && IsVis())
				{
					col = truecolour[scol + bankEven];
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

				if (scol != 0 && IsVis())
				{ 
					col = truecolour[4 * (sp >> 1) + scol + bankOdd];
				}

				sp--;
				active >>= 1;
				attached >>= 1;

				//even
				scol = 0;
				if ((active & 1) != 0) scol |= bits[sp];

				if (scol != 0 && IsVis())
				{
					col = truecolour[4 * (sp >> 1) + scol + bankEven];
				}

				sp--;
				active >>= 1;
				attached >>= 1;
			}
		}

		//sprite collision

		if (clxm != 0)
			CheckSpriteCollision(pix);

nospritebits:
		//playfield collision

		CheckPlayfieldCollision(pix);

		/*
		Mask off lower pri sprites against pf1
		Mask off lower pri sprites against pf2
		Compute whether pf1 or pf2 is in front
		Apply sprites in reverse order
		Might not need to compute all sprites if they're behind both playfielda
		The pf2 bits describe the priority for single playfield
		BPLCON2 contains the bits
		2-0 pf1
		3-5 pf2
		Playfield is at priority n (0-4)
		Who knows what 5,6,7 mean? 
		https://eab.abime.net/showthread.php?t=119463
		SWIV hi-score relies on this
		6 pf2>pf1
		Remaining bits 0 on ECS
		*/

		//stuff to try to deal with sprite/playfield priorities that doesn't work
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

		//in lowres, p=0,1, we want to shift every pixel (0,1) 01 &m==00
		//in hires, p=0,1,2,3 we want to shift every 2 pixels (1 and 3) &m=0101
		//in shires, p=0,1,2,3,4,5,6,7 we want to shift every 4 pixels (3 and 7) &m==01230123
		int m = pixelLoop / 2 - 1; //2->0,4->1,8->3
		int shift = ((p & m) == m)?1:0;

		//in AGA, sprites can have different resolutions
		int spriteRes = (bplcon3 >> 6) & 3;
		if (spriteRes != 0)
		{
			if (pixelLoop == 2)//lowres screen
			{ 
				if (spriteRes == 1) shift = 1; //lowres
				else if (spriteRes == 2) shift = 2; //hires
				else if (spriteRes == 3) shift = 4; //shres
			}
			else if (pixelLoop == 4)//hires screen
			{
				if (spriteRes == 1) shift = shift = p & 1; //lowres
				else if (spriteRes == 2) shift = 1; //hires
				else if (spriteRes == 3) shift = 2; //shres
			}
			else if (pixelLoop == 8)//shres screen
			{
				if (spriteRes == 1) shift = shift = ((p & 3)==3)?1:0; //lowres
				else if (spriteRes == 2) shift = p & 1; //hires
				else if (spriteRes == 3) shift = 1; //shres
			}
		}

		if (shift!=0)
		{
			for (int s = 0; s < 8; s++)
				spriteMask[s] >>= shift;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckSpriteCollision(byte pix)
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
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckPlayfieldCollision(byte pix)
	{
		//int clxconMatch = clxcon & 0x3f | (clxcon2 & 0x3) << 6;
		//int clxconEnable = clxcon >> 6 & 0x3f | clxcon2 & 0xc0;

		//odd->even playfield collision

		//v1 - are bits set at the same position in both playfields?
		if (((pix & 0b10101010) >> 1 & pix) != 0)
			clxdat |= 1;

		//v1.5
		//uint match = (uint)((pix ^ ~clxconMatch) & clxconEnable);
		//if (match != 0)
		//	clxdat |= 1;

		//v2 - are enabled / colour bits set at the same position in both playfields?
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

		uint pixelBits;
		if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS || (fmode & 3) == 0)
			pixelBits = 15;
		else if ((fmode & 3) == 3)
			pixelBits = 63;
		else
			pixelBits = 31;

		bpldatPix.SetPixelBitMask(pixelBits);
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

	private void RunDeniseTick(int d, int p)
	{
		//if (clock.DeniseHorizontalPos < FIRST_DMA)
		//	return;
		uint col;

		if (blankingStatus == Blanking.None)
		{
			//is it the visible area horizontally?
			//when h >= diwstrt, bits are read out of the bitplane data, turned into pixels and output
			if (clock.DeniseHorizontalPos+d >= diwstrth + debugger.diwSHack && clock.DeniseHorizontalPos+d <= diwstoph + debugger.diwEHack)
			{
				uint pix = bpldatPix.GetPixel(planes);

				col = pixelAction(pix);

				//remember the last colour for HAM modes
				lastcol = col;

				DoSprites(ref col, (byte)pix, p);
			}
			else
			{
				int m = pixelLoop / 2 - 1; //2->0,4->1,8->3

				bpldatPix.NextPixel();
				if ((p & m) == m)
					for (int s = 0; s < 8; s++)
						spriteMask[s] >>= 1;

				//output colour 0 pixels
				col = lastcol = truecolour[0];
			}
		}
		else
		{
			//outside display window

			//output colour 0 pixels
			col = lastcol = truecolour[0];

			bool stipple = ((clock.HorizontalPos ^ clock.VerticalPos) & 1) != 0;
			if (stipple && (blankingStatus & Blanking.HorizontalBlank) != 0) col |= 0xff0000;
			if (stipple && (blankingStatus & Blanking.VerticalBlank) != 0) col |= 0x0000ff;
		}

		//horizontal pixel double
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

	private void DebugBPLCON1()
	{
		//logger.LogTrace($"BPLCON1 {clock} {bplcon1:X4} {bplcon1&0xf} {(bplcon1>>4)&0xf}");
	}

	public void Write(uint insaddr, uint address, ushort value)
	{
		switch (address)
		{
			case ChipRegs.BPLCON0: bplcon0 = value; UpdateBPLCON0(); break;
			case ChipRegs.BPLCON1: bplcon1 = value; DebugBPLCON1(); break;
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
		{
			for (int p = 0; p < pixelLoop; p++)
				RunDeniseTick(0,p);
		}
		//this should be a no-op
		//System.Diagnostics.Debug.Assert(SCREEN_WIDTH - (dptr - lineStart) == 0);
		dptr += SCREEN_WIDTH - (dptr - lineStart);

		//scan double
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
