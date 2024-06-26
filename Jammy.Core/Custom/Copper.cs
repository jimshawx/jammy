using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class Copper : ICopper
	{
		private readonly IChipRAM memory;
		private readonly IChips custom;
		private readonly IEmulationWindow emulationWindow;
		private readonly IInterrupt interrupt;
		private readonly EmulationSettings settings;
		private readonly ILogger logger;

		//private const int SCREEN_WIDTH = 1280;
		//private const int SCREEN_HEIGHT = 1024;

		private const int SCREEN_WIDTH = DMA_WIDTH * 4;//227 (E3) * 4;
		private const int SCREEN_HEIGHT = 313 * 2;//x2 for scan double

		//hack: optimisation to avoid processing pixels too far left and right on the screen
		//sprite DMA starts at 0x18, but can be eaten into by bitmap DMA
		//normal bitmap DMA start at 0x38, overscan at 0x30, Menace starts at 0x28
		private const int DMA_START = 0x18;
		//bitmap DMA ends at 0xD8, with 8 slots after that
		private const int DMA_END = 0xF0;

		private const int DMA_WIDTH = DMA_END - DMA_START;

		private int[] screen;

		public Copper(IChipRAM memory, IChips custom, IEmulationWindow emulationWindow, IInterrupt interrupt, IOptions<EmulationSettings> settings, ILogger<Copper> logger)
		{
			this.memory = memory;
			this.custom = custom;
			this.emulationWindow = emulationWindow;
			this.interrupt = interrupt;
			this.settings = settings.Value;
			this.logger = logger;

			emulationWindow.SetPicture(SCREEN_WIDTH, SCREEN_HEIGHT);

			emulationWindow.SetKeyHandlers(dbug_Keydown, dbug_Keyup);

			ComputeDPFLookup();

			//start the first frame
			RunCopperVerticalBlankStart();

			logger.LogTrace("Press F9 to enable Copper debug");
		}

		private void dbug_Keyup(int obj)
		{
		}

		private bool keys = false;
		private void dbug_Keydown(int obj)
		{
			if (obj == (int)VK.VK_F9) {keys ^= true; logger.LogTrace($"KEYS {keys}");}

			if (keys)
			{
				if (obj == (int)VK.VK_F11) cdbg.dbug = true;
				if (obj == (int)VK.VK_F7) cdbg.dbugLine--;
				if (obj == (int)VK.VK_F6) cdbg.dbugLine++;
				if (obj == (int)VK.VK_F8) cdbg.dbugLine = -1;
				if (obj == (int)VK.VK_F5) cdbg.dbugLine = diwstrt >> 8;

				if (obj == (int)'Q') cdbg.ddfSHack++;
				if (obj == (int)'W') cdbg.ddfSHack--;
				if (obj == (int)'E') cdbg.ddfSHack = 0;
				if (obj == (int)'R') cdbg.ddfEHack++;
				if (obj == (int)'T') cdbg.ddfEHack--;
				if (obj == (int)'Y') cdbg.ddfEHack = 0;

				if (obj == (int)'1') cdbg.diwSHack++;
				if (obj == (int)'2') cdbg.diwSHack--;
				if (obj == (int)'3') cdbg.diwSHack = 0;
				if (obj == (int)'4') cdbg.diwEHack++;
				if (obj == (int)'5') cdbg.diwEHack--;
				if (obj == (int)'6') cdbg.diwEHack = 0;

				if (obj == (int)'A') cdbg.bitplaneMask ^= 1;
				if (obj == (int)'S') cdbg.bitplaneMask ^= 2;
				if (obj == (int)'D') cdbg.bitplaneMask ^= 4;
				if (obj == (int)'F') cdbg.bitplaneMask ^= 8;
				if (obj == (int)'G') cdbg.bitplaneMask ^= 16;
				if (obj == (int)'H') cdbg.bitplaneMask ^= 32;
				if (obj == (int)'J') cdbg.bitplaneMask ^= 64;
				if (obj == (int)'K') cdbg.bitplaneMask ^= 128;
				if (obj == (int)'L')
				{
					cdbg.bitplaneMask = 0xff;
					cdbg.bitplaneMod = 0;
				}

				if (obj == (int)'Z') cdbg.bitplaneMod ^= 1;
				if (obj == (int)'X') cdbg.bitplaneMod ^= 2;
				if (obj == (int)'C') cdbg.bitplaneMod ^= 4;
				if (obj == (int)'V') cdbg.bitplaneMod ^= 8;
				if (obj == (int)'B') cdbg.bitplaneMod ^= 16;
				if (obj == (int)'N') cdbg.bitplaneMod ^= 32;
				if (obj == (int)'M') cdbg.bitplaneMod ^= 64;
				if (obj == (int)VK.VK_OEM_COMMA) cdbg.bitplaneMod ^= 128;

				if (obj == (int)VK.VK_F10) cdbg.ws = true;
			}
		}

		private bool copperDumping;
		public void Dumping(bool enabled)
		{
			copperDumping = enabled;
		}

		private ulong copperTime;
		//HRM 3rd Ed, PP24
		private uint copperHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		private uint copperVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313
		private uint copperFrame = 0;

		public void Emulate(ulong cycles)
		{
			copperTime += cycles;

			//new horz count
			copperHorz = (uint)copperTime;

			//end of scanline?
			if (copperTime >= 448)
			{
				copperTime -= 448;

				//new horz count
				copperHorz = (uint)copperTime;
				//next scanline
				copperVert++;

				//last scanline?
				if (copperVert >= 312)
				{
					copperVert = 0;

					//end the current frame
					RunCopperVerticalBlankEnd();

					interrupt.AssertInterrupt(Interrupt.VERTB);

					copperFrame++;

					if (cop.status == CopperStatus.Waiting && cop.data != 0xfffe)
					{
						logger.LogTrace ($"Hit VBL while still waiting for {cop.data:X2}");
					}

					if (cdbg.dbug)
					{
						DebugCopperList(cop1lc);
						cdbg.dbug = false;
					}

					//start the next frame
					RunCopperVerticalBlankStart();
				}

				//run the next scanline
				cop.currentLine = (int)copperVert;
				StartCopperLine();
				RunCopperLine();
				EndCopperLine();
			}
		}

		public void Reset()
		{
			copperTime = 0;
			for (int i = 0; i < 8; i++)
				spriteState[i] = SpriteState.Idle;
		}

		private const int MAX_COPPER_ENTRIES = 1024;

		public void DebugCopperList(uint copPC)
		{
			if (copPC == 0) return;

			var csb = cdbg.GetStringBuilder();
			csb.AppendLine($"Copper List @{copPC:X8}");

			var skipTaken = new HashSet<uint>();

			int counter = MAX_COPPER_ENTRIES;
			while (counter-- > 0)
			{
				ushort ins = (ushort)memory.Read(0, copPC, Size.Word);
				copPC += 2;

				ushort data = (ushort)memory.Read(0, copPC, Size.Word);
				copPC += 2;

				//csb.AppendLine($"{copPC - 4:X8} {ins:X4},{data:X4} ");

				if ((ins & 0x0001) == 0)
				{
					//MOVE
					uint reg = (uint)(ins & 0x1fe);

					csb.AppendLine($"{copPC - 4:X8} MOVE {ins:X4} {data:X4} {ChipRegs.Name(ChipRegs.ChipBase + reg)}({reg:X4}),{data:X4}");

					if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP1)
						copPC = custom.Read(copPC - 4, ChipRegs.COP1LCH, Size.Long);//COP1LC
					else if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP2)
						copPC = custom.Read(copPC - 4, ChipRegs.COP2LCH, Size.Long);//COP2LC
				}
				else if ((ins & 0x0001) == 1)
				{
					//WAIT/SKIP

					uint hp = (uint)(ins & 0xfe);
					uint vp = (uint)((ins >> 8) & 0xff);

					uint he = (uint)(data & 0xfe);
					uint ve = (uint)(((data >> 8) & 0x7f) | 0x80);
					uint blit = (uint)(data >> 15);

					if ((data & 1) == 0)
					{
						//WAIT
						csb.AppendLine($"{copPC - 4:X8} WAIT {ins:X4} {data:X4} vp:{vp:X2} hp:{hp:X2} ve:{ve:X2} he:{he:X2} b:{blit}");
					}
					else
					{
						//SKIP
						csb.AppendLine($"{copPC - 4:X8} SKIP {ins:X4} {data:X4} vp:{vp:X2} hp:{hp:X2} ve:{ve:X2} he:{he:X2} b:{blit}");
						if (skipTaken.Contains(copPC - 4))
						{
							copPC += 4;
							csb.AppendLine("SKIPPED");
						}
						else
						{
							skipTaken.Add(copPC - 4);
						}
					}

					//this is usually how a copper list ends
					if (ins == 0xffff && data == 0xfffe)
						break;
				}
			}
			logger.LogTrace(csb.ToString());
			File.WriteAllText($"../../../../copper{DateTime.Now:yyyyMMdd-HHmmss}.txt", csb.ToString());
		}

		////HRM 3rd Ed, PP24
		//private uint beamHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		//private uint beamVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313
		private enum CopperStatus
		{
			RunningWord1,
			RunningWord2,
			Waiting,
			WakingUp,
			Stopped
		}

		[Flags]
		private enum BPLCON0 : uint
		{
			HiRes = 1 << 15,
			SuperHiRes = 1 << 6,
		}

		//global state of the copper

		private class CopperFrame
		{
			public int dptr = 0;
			public int waitH = 0;
			public int waitV = 0;
			public int waitHMask;
			public int waitVMask;
			public CopperStatus status;
			public uint copPC;
			public int currentLine;
			public uint waitMask;
			public uint waitPos;
			public int waitTimer;

			public int lineStart;
			public ushort ins;
			public ushort data;

			public void Reset(uint copperPC)
			{
				copPC = copperPC;

				dptr = 0;
				waitH = 0;
				waitV = 0;
				waitHMask = 0xff;
				waitVMask = 0xff;
				status = CopperStatus.RunningWord1;
			}
		}

		private class CopperDebug
		{
			public char[] fetch = new char[256];
			public char[] write = new char[256];
			public int dma;
			public int dbugLine = -1;
			public bool dbug = false;
			public int ddfSHack;
			public int ddfEHack;
			public int diwSHack;
			public int diwEHack;
			public byte bitplaneMask=0xff;
			public byte bitplaneMod = 0;
			public bool ws;
			private StringBuilder sb = new StringBuilder();

			public void Reset()
			{
				dma = 0;
				//ddfSHack = ddfEHack = diwEHack = diwSHack = 0;
			}

			public StringBuilder GetStringBuilder()
			{
				sb.Length = 0;
				return sb;
			}
		}

		private void Debug(CopperFrame cf, CopperDebug cd, CopperLine cl, ushort dmacon)
		{
			if (cdbg.dbugLine == -1)
				return;
			if (cdbg.dbugLine != cf.currentLine)
				return;

			logger.LogTrace($"LINE {cdbg.dbugLine}");
			logger.LogTrace($"DDF {ddfstrt:X4} {ddfstop:X4} ({cl.wordCount}) {cl.ddfstrtfix:X4}{cdbg.ddfSHack:+#0;-#0} {cl.ddfstopfix:X4}{cdbg.ddfEHack:+#0;-#0} FMODE {fmode:X4}");
			logger.LogTrace($"DIW {diwstrt:X4} {diwstop:X4} {diwhigh:X4} V:{cl.diwstrtv}->{cl.diwstopv}({cl.diwstopv - cl.diwstrtv}) H:{cl.diwstrth}{cdbg.diwSHack:+#0;-#0}->{cl.diwstoph}{cdbg.diwEHack:+#0;-#0}({cl.diwstoph - cl.diwstrth}/16={(cl.diwstoph - cl.diwstrth) / 16})");
			logger.LogTrace($"MOD {bpl1mod:X4} {bpl2mod:X4} DMA {Dmacon(dmacon)}");
			logger.LogTrace($"BCN 0:{bplcon0:X4} {Bplcon0()} 1:{bplcon1:X4} {Bplcon1()} 2:{bplcon2:X4} {Bplcon2()} 3:{bplcon3:X4} {Bplcon3()} 4:{bplcon4:X4} {Bplcon4()}");
			logger.LogTrace($"BPL {bplpt[0]:X6} {bplpt[1]:X6} {bplpt[2]:X6} {bplpt[3]:X6} {bplpt[4]:X6} {bplpt[5]:X6} {bplpt[6]:X6} {bplpt[7]:X6} {new string(Convert.ToString(cd.bitplaneMask,2).PadLeft(8,'0').Reverse().ToArray())} {new string(Convert.ToString(cd.bitplaneMod, 2).PadLeft(8, '0').Reverse().ToArray())}");
			var sb = cdbg.GetStringBuilder();
			sb.AppendLine();
			for (int i = 0; i < 256; i++)
				sb.Append(cd.fetch[i]);

			logger.LogTrace(sb.ToString());
			sb.Clear();
			sb.AppendLine();
			for (int i = 0; i < 256; i++)
				sb.Append(cd.write[i]);
			sb.Append($"({cd.dma})");
			logger.LogTrace(sb.ToString());
		}

		private string Dmacon(ushort dmacon)
		{
			var sb = cdbg.GetStringBuilder();
			if ((dmacon & 0x200) != 0) sb.Append("DMA ");
			if ((dmacon & 0x100) != 0) sb.Append("BPL ");
			if ((dmacon & 0x80) != 0) sb.Append("COP ");
			if ((dmacon & 0x40) != 0) sb.Append("BLT ");
			if ((dmacon & 0x20) != 0) sb.Append("SPR ");
			return sb.ToString();
		}

		private string Bplcon0()
		{
			var sb = cdbg.GetStringBuilder();
			if ((bplcon0 & 0x8000)!=0) sb.Append("H ");
			else if ((bplcon0 & 0x40) != 0) sb.Append("SH ");
			else if ((bplcon0 & 0x80) != 0) sb.Append("UH ");
			else sb.Append("N ");
			if ((bplcon0 & 0x400)!=0) sb.Append("DPF ");
			if ((bplcon0 & 0x800) != 0) sb.Append("HAM ");
			if ((bplcon0 & 0x10) != 0) sb.Append("8");
			else sb.Append($"{(bplcon0 >> 12) & 7} ");
			if ((bplcon0 & 0x4) != 0) sb.Append("LACE");

			if (((bplcon0 >> 12) & 7) == 6 && ((bplcon0 & (1 << 11)) == 0 && (bplcon0 & (1 << 10)) == 0 && (settings.ChipSet != ChipSet.AGA || (bplcon2 & (1 << 9)) == 0))) sb.Append("EHB ");

			return sb.ToString();
		}

		private string Bplcon1()
		{
			int pf0 = bplcon1 & 0xf;
			int pf1 = (bplcon1 >>4)&0xf;
			return $"SCR{pf0}:{pf1} ";
		}

		private string Bplcon2()
		{
			var sb = cdbg.GetStringBuilder();
			if ((bplcon2 & (1 << 9)) != 0) sb.Append("KILLEHB ");
			if ((bplcon2 & (1 << 6)) != 0) sb.Append("PF2PRI ");
			return sb.ToString();
		}

		private string Bplcon3()
		{
			var sb = cdbg.GetStringBuilder();
			sb.Append($"BNK{bplcon3 >> 13} ");
			sb.Append($"PF2O{(bplcon3 >> 10) & 7} ");
			sb.Append($"SPRRES{(bplcon3 >> 6) & 3} ");
			if ((bplcon3 & (1 << 9)) != 0) sb.Append("LOCT ");
			return sb.ToString();
		}

		private string Bplcon4()
		{
			var sb = cdbg.GetStringBuilder();
			sb.Append($"BPLAM{bplcon4 >> 8:X2} ");
			sb.Append($"ESPRM{(bplcon4 >> 4)&15:X2} ");
			sb.Append($"OSPRM{bplcon4&15:X2} ");
			if ((bplcon3 & (1 << 9)) != 0) sb.Append("LOCT ");
			return sb.ToString();
		}

		private CopperFrame cop = new CopperFrame();
		private CopperDebug cdbg = new CopperDebug();
		private CopperLine cln = new CopperLine();

		private void RunCopperVerticalBlankStart()
		{
			//logger.LogTrace("VBL");

			if (copperDumping)
			{
				var c = memory.ToBmp(1280);
				File.WriteAllBytes($"../../../../chip-{DateTime.Now:yyyy-MM-dd-HHmmss}.bmp", c.ToArray());
			}

			screen = emulationWindow.GetFramebuffer();
			cop.Reset(cop1lc);
			cdbg.Reset();
			for (int i = 0; i < 8; i++)
				spriteState[i] = SpriteState.Idle;
		}

		private void RunCopperVerticalBlankEnd()
		{
			DebugLocation();
			emulationWindow.Blit(screen);
		}

		private void DebugPalette()
		{
			int sx = 5;
			int sy = 5;

			int box = 5;
			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 64; x++)
				{
					for (int p = 0; p < box; p++)
					{
						for (int q = 0; q < box; q++)
						{
							screen[sx + x * box + q + (sy + (y * box) + p) * SCREEN_WIDTH] = (int)truecolour[x + y * 64];
						}
					}
				}
			}

		}

		private void DebugLocation()
		{
			if (cdbg.dbugLine < 0) return;
			if (cdbg.dbugLine >= SCREEN_HEIGHT / 2) return;
			for (int x = 0; x < SCREEN_WIDTH; x += 4)
				screen[x + cdbg.dbugLine * SCREEN_WIDTH * 2] ^= 0xffffff;
		}

		private enum CopperLineState
		{
			LineStart,
			Fetching,
			LineComplete,
			LineTerminated
		}


		private class CopperLine
		{
			public FastUInt128 pixelMask;
			public int pixelMaskBit;
			public uint pixelBits;
			public FastUInt128[] bpldatpix = new FastUInt128[8];

			public int planes;
			public int diwstrth = 0;
			public int diwstrtv = 0;
			public int diwstoph = 0;
			public int diwstopv = 0;

			//public int fetchMode = 0;

			public ushort ddfstrtfix = 0;
			public ushort ddfstopfix = 0;
			public int pixelLoop;
			public int pixmod;
			public uint lastcol = 0;

			public int wordCount = 0;

			public CopperLineState lineState;

			//https://eab.abime.net/showthread.php?t=111329
			private const int OCS=0;
			private const int AGA=1;
			private int FetchWidth(int DDFSTRT, int DDFSTOP, int chipset, int res, int FMODE)
			{
				// validate bits
				FMODE &= 3;
				DDFSTRT &= (chipset!=OCS) ? 0xfe : 0xfc;
				DDFSTOP &= (chipset!=OCS) ? 0xfe : 0xfc;
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

			public void InitLine(ushort bplcon0, ushort diwstrt, ushort diwstop, ushort diwhigh, ushort ddfstrt, ushort ddfstop, ushort fmode, EmulationSettings settings)
			{
				// start - pre-cache some useful per-scanline values

				//don't expect these to change within a scanline, weird undocumented things
				//will happen which this emulator probably will never understand
				//if (settings.ChipSet == ChipSet.AGA)
				//	fetchMode = fmode & 3;

				planes = (bplcon0 >> 12) & 7;
				if (settings.ChipSet == ChipSet.AGA)
				{
					if (planes == 0 && (bplcon0 & (1 << 4)) != 0)
						planes = 8;
				}
				diwstrth = diwstrt & 0xff;
				diwstrtv = diwstrt >> 8;
				diwstoph = (diwstop & 0xff) | 0x100;
				diwstopv = (diwstop >> 8) | (((diwstop & 0x8000) >> 7) ^ 0x100);

				//if diwhigh is written, the 'magic' bits are overwritten
				if (diwhigh != 0)
				{
					diwstrth |= (diwhigh & 0b1_00000) << 3;
					diwstrtv |= (diwhigh & 0b111) << 8;

					diwstoph &= 0xff;
					diwstoph |= (diwhigh & 0b1_00000_00000000) >> 5;
					diwstopv &= 0xff;
					diwstopv |= (diwhigh & 0b111_00000000);

					//todo: there are also an extra two bottom bits for strth/stoph
				}

			//ddfstrt->ddfstop
			//HRM says DDFSTRT = DDFSTOP - (4 * (word count - 2)) for high resolution
			//workbench
			//KS2.04 3C->D4 => 3C = D4 - (4 * (40-2)) = D4-98 = 3C
			//KS1.3  3C->D0 => 3C = D0 - (4 * (40-2)) = D0-98 = 38
			//kickstart
			//KS2.04 40->D0 => 40 = D0 - (4 * (40-2)) = D0-98 = 38

			//https://eab.abime.net/showthread.php?t=111329


				//how many pixels should be fetched per clock in the current mode?
				if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
				{
					//4 colour clocks, fetch 16 pixels
					//1 colour clock, draw 4 pixel
					pixelLoop = 4;
					pixmod = 4;

					//ddfstrtfix = (ushort)(ddfstrt & 0xfffc);

					if (settings.ChipSet == ChipSet.OCS || (fmode&3) == 0)
					{
						ddfstrtfix = ddfstrt;
						ddfstopfix=(ushort)(ddfstrt+((((ddfstop-ddfstrt+7)>>3)+1)<<3));
						//FetchWidth(ddfstrt, ddfstop, OCS, 1, 0);
					}
					else if ((fmode&3) == 3)
					{
						//round to multiple of 4 words
						//int round = 15;
						//ddfstrtfix = (ushort)(ddfstrt & ~round);
						//ddfstopfix = (ushort)((ddfstop + round) & ~round);
						ddfstrtfix = ddfstrt;
						ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 0xf) >> 4) + 1) << 4));
						//FetchWidth(ddfstrt, ddfstop, AGA, 1, 3);
						pixmod = 16;
					}
					else
					{
						//round up to multiple of 2 words
						//int round = 7;
						//ddfstrtfix = (ushort)(ddfstrt & ~round);
						//ddfstopfix = (ushort)((ddfstop + round) & ~round);
						ddfstrtfix = ddfstrt;
						ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
						//FetchWidth(ddfstrt, ddfstop, AGA, 1, 2);
						pixmod = 8;
					}

					//diwstrth &= 0xf8;
					//diwstoph &= 0x1f8;
				}
				else if ((bplcon0 & (uint)BPLCON0.SuperHiRes) != 0)
				{
					//2 colour clocks, fetch 16 pixels
					//1 colour clock, draw 8 pixel
					pixelLoop = 8;
					pixmod = 2;
				}
				else
				{
					//8 colour clocks, fetch 16 pixels
					//1 colour clock, draw 2 pixel
					pixelLoop = 2;
					pixmod = 8;

					//low-res ddfstrt ignores bit 2
					ddfstrtfix = ddfstrt;//(ushort)(ddfstrt & 0xfff8);

					if (settings.ChipSet == ChipSet.OCS || (fmode&3) == 0)
					{
						ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
						//FetchWidth(ddfstrt, ddfstop, OCS, 0, 0);
					}
					else if ((fmode&3)==3)
					{
						//wordCount = (ddfstop - ddfstrt) / 32 + 1;
						//ddfstopfix = (ushort)(ddfstrtfix + wordCount * 32);
						ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
						//FetchWidth(ddfstrt, ddfstop, AGA, 0, 3);
						pixmod =32;
					}
					else
					{
						ddfstopfix = (ushort)(ddfstrt + ((((ddfstop - ddfstrt + 7) >> 3) + 1) << 3));
						//FetchWidth(ddfstrt, ddfstop, AGA, 0, 2);
						pixmod = 16;
					}

					//diwstrth &= 0xf0;
					//diwstoph &= 0x1f0;
				}

				//pixelMask = 0x8000;
			}
		}

		private void StartCopperLine()
		{
			cop.lineStart = cop.dptr;

			FirstPixel();
			cln.lastcol = truecolour[0];//should be colour 0 at time of diwstrt
			cln.lineState = CopperLineState.LineStart;

			if (cop.currentLine == cdbg.dbugLine)
				DebugPalette();
		}

		private enum SpriteState
		{
			Idle=0,
			Waiting,
			Fetching,
		}
		private enum SpriteBits
		{
			Off = 0,
			Active,
		}

		private SpriteState [] spriteState = new SpriteState[8];
		private uint[] spriteMask = new uint[8];

		private void RunSpriteDMA(int slot)
		{
			//is sprite DMA enabled
			ushort dmaconr = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmaconr & (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.SPREN)) != (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.SPREN))
				return;

			int s = slot >> 1;

			if (spriteState[s] == SpriteState.Waiting)
			{
				int vstart = sprpos[s] >> 8;
				vstart += (sprctl[s] & 4) << 6; //bit 2 is high bit of vstart
				if (cop.currentLine == vstart)
				{
					spriteState[s] = SpriteState.Fetching;
				}
			}
			else if (spriteState[s] == SpriteState.Fetching)
			{
				int vstop = sprctl[s] >> 8;
				vstop += (sprctl[s] & 2) << 7; //bit 1 is high bit of vstop
				if (cop.currentLine == vstop)
					spriteState[s] = SpriteState.Idle;
			}

			if ((slot & 1) == 0)
			{
				if (spriteState[s] == SpriteState.Idle)
				{
					sprpos[s] = (ushort)memory.Read(0, sprpt[s], Size.Word);
				}
				else if (spriteState[s] == SpriteState.Fetching)
				{
					sprdata[s] = (ushort)memory.Read(0, sprpt[s], Size.Word);
				}
			}
			else
			{
				if (spriteState[s] == SpriteState.Idle)
				{
					sprctl[s] = (ushort)memory.Read(0, sprpt[s]+2, Size.Word);

					if (sprpos[s]== 0 && sprctl[s] == 0)
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
					sprdatb[s] = (ushort)memory.Read(0, sprpt[s]+2, Size.Word);
					sprpt[s] += 4;
				}
			}
		}

		private void RunCopperLine()
		{
			if (cop.currentLine == 50)
				cop.currentLine = 50;

			cln.InitLine(bplcon0, diwstrt, diwstop, diwhigh, ddfstrt, ddfstop, fmode, settings);

			for (int h = 0; h < 256; h++)
			{
				//debugging
				if (cop.currentLine == cdbg.dbugLine)
				{
					cdbg.fetch[h] = '-';
					cdbg.write[h] = '-';
				}
				//debugging

				ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);

				//Copper DMA is ON
				if ((dmacon & (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.COPEN)) == (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.COPEN))
					CopperInstruction(h);

				switch (h)
				{
					//case int i when i >= 0  && i <=3: continue;//memory refresh
					//case int i when i >= 4 && i <= 6: continue;//disk DMA
					//case int i when i >= 7 && i <= 10: continue;//audio DMA
					case int i when i >= 11 && i <= 26: RunSpriteDMA(i-11); break;//sprite DMA
				}

				//bitplane fetching (optimisation)
				if (h < DMA_START || h >= DMA_END)
					continue;

				//is it the visible area, vertically?
				if (cop.currentLine >= cln.diwstrtv && cop.currentLine < cln.diwstopv)
				{
					if (cop.currentLine == cdbg.dbugLine)
						cdbg.write[h] = cdbg.fetch[h] = ':';

					//is it time to do bitplane DMA?
					//when h >= ddfstrt, bitplanes are fetching. one plane per cycle, until all the planes are fetched
					//bitplane DMA is ON
					if ((dmacon & (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.BPLEN)) == (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.BPLEN))
					{
						if (h >= cln.ddfstrtfix + cdbg.ddfSHack && h < cln.ddfstopfix + cdbg.ddfEHack && (cln.lineState == CopperLineState.Fetching || cln.lineState == CopperLineState.LineStart))
						{
							CopperBitplaneFetch(h);
							cln.lineState = CopperLineState.Fetching;
						}

						if (h >= cln.ddfstopfix + cdbg.ddfEHack && cln.lineState == CopperLineState.Fetching)
						{
							cln.lineState = CopperLineState.LineComplete;
						}
					}

					//is it the visible area horizontally?
					//when h >= diwstrt, bits are read out of the bitplane data, turned into pixels and output
					//HACK-the minuses are a hack.  the bitplanes are ready from fetching but they're not supposed to be copied into Denise until 4 cycles later
					if (h >= ((cln.diwstrth + cdbg.diwSHack) >> 1)-2 && h < ((cln.diwstoph + cdbg.diwEHack) >> 1)-2)
					{
						CopperBitplaneConvert(h);
					}
					else
					{
						//outside horizontal area
						for (int p = 0; p < cln.pixelLoop; p++)
							NextPixel();

						//output colour 0 pixels
						uint col = truecolour[0];
						//col = 0xff0000;

						for (int k = 0; k < 4; k++)
							screen[cop.dptr++] = (int)col;
					}
				}
				else
				{
					//outside vertical area

					//output colour 0 pixels
					uint col = truecolour[0];
					//col = 0xff0000;

					for (int k = 0; k < 4; k++)
						screen[cop.dptr++] = (int)col;
				}
			}
		}

		private void EndCopperLine()
		{
			//next horizontal line, and we did fetching this line
			if (cop.currentLine >= cln.diwstrtv && cop.currentLine < cln.diwstopv && cln.lineState == CopperLineState.LineComplete)
			{
				for (int i = 0; i < cln.planes; i++)
				{ 
					bplpt[i] += ((i & 1) == 0) ? bpl1mod : bpl2mod;
					bplpt[i]&=0xfffffffe;
				}
				cln.lineState = CopperLineState.LineTerminated;
			}

			//this should be a no-op
			System.Diagnostics.Debug.Assert(SCREEN_WIDTH - (cop.dptr - cop.lineStart) == 0);
			cop.dptr += SCREEN_WIDTH - (cop.dptr - cop.lineStart);

			//scan double
			for (int i = cop.lineStart; i < cop.lineStart + SCREEN_WIDTH; i++)
				screen[cop.dptr++] = screen[i];

			Debug(cop, cdbg, cln, (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word));
		}

		private void CopperInstruction(int h)
		{
			//copper instruction every odd clock (and copper DMA is on)
			if ((h & 1) == 1 && h < 227/*E3*/)
			{
				if (cop.status == CopperStatus.Stopped)
				{
					return;
				}
				else if (cop.status == CopperStatus.WakingUp)
				{
					//burn a cycle after waking up
					cop.waitTimer--;
					if (cop.waitTimer <= 0)
						cop.status = CopperStatus.RunningWord1;
				}
				else if (cop.status == CopperStatus.RunningWord1)
				{
					cop.ins = copins = (ushort)memory.Read(0, cop.copPC, Size.Word);
					cop.copPC += 2;
					cop.status = CopperStatus.RunningWord2;
				}
				else if (cop.status == CopperStatus.RunningWord2)
				{
					ushort ins = cop.ins;

					cop.data = (ushort)memory.Read(0, cop.copPC, Size.Word);
					ushort data = cop.data;
					cop.copPC += 2;
					cop.status = CopperStatus.RunningWord1;

					if ((ins & 0x0001) == 0)
					{
						//MOVE
						uint reg = (uint)(ins & 0x1fe);
						uint regAddress = ChipRegs.ChipBase + reg;

						//in OCS mode CDANG in COPCON means can access >= 0x40->0x7E as well as the usual >= 0x80
						//in ECS/AGA mode CDANG in COPCON means can access ALL chip regs, otherwise only >= 080
						if (settings.ChipSet == ChipSet.OCS)
						{
							if (((copcon & 2) != 0 && reg >= 0x40) || reg >= 0x80)
							{
								custom.Write(0, regAddress, data, Size.Word);
							}
							else
							{
								cop.status = CopperStatus.Stopped;
								//logger.LogTrace($"Copper Stopped! W {ChipRegs.Name(regAddress)} {data:X4} CDANG: {((copcon&2)!=0?1:0)}");
							}
						}
						else
						{
							if ((copcon & 2) !=0 || reg >= 0x80)
							{
								custom.Write(0, regAddress, data, Size.Word);
							}
							else
							{
								cop.status = CopperStatus.Stopped;
								//logger.LogTrace($"Copper Stopped! W {ChipRegs.Name(regAddress)} {data:X4} CDANG: {((copcon&2)!=0?1:0)}");
							}
						}
					}
					else if ((ins & 0x0001) == 1)
					{
						//WAIT
						cop.waitH = ins & 0xfe;
						cop.waitV = (ins >> 8) & 0xff;

						cop.waitHMask = data & 0xfe;
						cop.waitVMask = ((data >> 8) & 0x7f) | 0x80;

						cop.waitMask = (uint)(cop.waitHMask | (cop.waitVMask << 8));
						cop.waitPos = (uint)((cop.waitV << 8) | cop.waitH);

						uint blit = (uint)(data >> 15);

						//todo: blitter is immediate, so currently ignored.
						//todo: in reality if blitter-busy bit is set the comparisons will fail.

						if ((data & 1) == 0)
						{
							//WAIT
							//logger.LogTrace($"WAIT until ({cop.waitH},{cop.waitV}) @({h},{cop.currentLine}) hm:{cop.waitHMask:X3} vm:{cop.waitVMask:X3} m:{cop.waitMask:X4} p:{cop.waitPos:X4}");
							cop.status = CopperStatus.Waiting;
						}
						else
						{
							//SKIP
							//logger.LogTrace("SKIP");

							uint coppos = (uint)(((cop.currentLine & 0xff) << 8) | (h & 0xff));
							coppos &= cop.waitMask;
							if (CopperCompare(coppos, (cop.waitPos & cop.waitMask)))
							{
								//logger.LogTrace($"RUN  {h},{cop.currentLine} {coppos:X4} {cop.waitPos:X4}");
								cop.copPC += 4;
							}
						}
					}
				}
				else if (cop.status == CopperStatus.Waiting)
				{
					//if ((cop.currentLine & cop.waitVMask) == cop.waitV)
					//{
					//	if ((h & cop.waitHMask) >= cop.waitH)
					//	{
					//		//logger.LogTrace($"RUN ({h},{cop.currentLine})");
					//		cop.status = CopperStatus.RunningWord1;
					//	}
					//}

					uint coppos = (uint)(((cop.currentLine & 0xff) << 8) | (h & 0xff));
					coppos &= cop.waitMask;
					if (CopperCompare(coppos, (cop.waitPos & cop.waitMask)))
					{
						//logger.LogTrace($"RUN  {h},{cop.currentLine} {coppos:X4} {cop.waitPos:X4}");
						cop.waitTimer = 2;
						cop.status = CopperStatus.WakingUp;

						if (cop.ins == 0xffff && cop.data == 0xfffe)
						{
							logger.LogTrace("Went off the end of the Copper List");
							cop.status = CopperStatus.Waiting;
						}
					}
				}
			}
		}

		private bool CopperCompare(uint coppos, uint waitPos)
		{
			//return coppos >= waitPos;
			//return ((coppos&0xff00)>=(waitPos&0xff00))&&((coppos&0xff)>=(waitPos&0xff));
			return (((coppos & 0xff00) == (waitPos & 0xff00)) && ((coppos & 0xff) >= (waitPos & 0xff))) || ((coppos & 0xff00) > (waitPos & 0xff00));
		}

		private static readonly int[] fetchLo = { 8, 4, 6, 2, 7, 3, 5, 1 };
		private static readonly int[] fetchHi = { 4, 2, 3, 1, 4, 2, 3, 1 };
		private static readonly int[] fetchF3 = {
					 //10,10,10,10,10,10,10,10, 10,10,10,10,10,10,10,10, 10,10,10,10,10,10,10,10, 8, 4, 6, 2, 7, 3, 5, 1,
					 8, 4, 6, 2, 7, 3, 5, 1, 10,10,10,10,10,10,10,10, 10,10,10,10,10,10,10,10, 10,10,10,10,10,10,10,10,
				};
		private static readonly int[] fetchF2 = {
					//10,10,10,10,10,10,10,10, 8, 4, 6, 2, 7, 3, 5, 1,
					 8, 4, 6, 2, 7, 3, 5, 1, 10,10,10,10,10,10,10,10,
				};
		private void CopperBitplaneFetch(int h)
		{
			int planeIdx;
			int plane;

			if (settings.ChipSet == ChipSet.OCS || (fmode&3) == 0)
			{
				planeIdx = (h - cln.ddfstrtfix) % cln.pixmod;

				if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
					plane = fetchHi[planeIdx] - 1;
				else
					plane = fetchLo[planeIdx] - 1;
			}
			else if ((fmode&3) == 3)
			{
				planeIdx = (h - cln.ddfstrtfix) % cln.pixmod;
				plane = fetchF3[planeIdx] - 1;
			}
			else
			{
				planeIdx = (h-cln.ddfstrtfix) % cln.pixmod;
				plane = fetchF2[planeIdx] - 1;
			}

			if (plane < cln.planes)
			{
				if (settings.ChipSet == ChipSet.OCS || (fmode&3) == 0)
				{
					bpldat[plane] = (ushort)memory.Read(0, bplpt[plane], Size.Word);
					bplpt[plane] += 2;
				}
				else if ((fmode&3) == 3)
				{
					bpldat[plane] = ((ulong)memory.Read(0, bplpt[plane], Size.Long) << 32) | memory.Read(0, bplpt[plane] + 4, Size.Long);
					bplpt[plane] += 8;
				}
				else
				{
					bpldat[plane] = memory.Read(0, bplpt[plane], Size.Long);
					bplpt[plane] += 4;
				}

				//we just filled BPL0DAT
				if (plane == 0)
				{
					for (int i = 0; i < 8; i++)
					{
						//scrolling
						int odd = bplcon1&0xf;
						int even = (bplcon1>>4)&0xf;

						if ((i&1)!=0)
							cln.bpldatpix[i].Or(bpldat[i], (16-odd));
						else
							cln.bpldatpix[i].Or(bpldat[i], (16-even));
					}

					if (cop.currentLine == cdbg.dbugLine)
					{
						cdbg.write[h] = 'x';
						cdbg.dma++;
					}
				}
				else
				{
					if (cop.currentLine == cdbg.dbugLine)
						cdbg.write[h] = '.';
				}

				if (cop.currentLine == cdbg.dbugLine)
					cdbg.fetch[h] = Convert.ToChar(plane + 48 + 1);
			}
			else
			{
				if (cop.currentLine == cdbg.dbugLine)
					cdbg.fetch[h] = '+';
			}
		}

		//private uint pixelCounter = 0;

		private void FirstPixel()
		{
			if (settings.ChipSet == ChipSet.OCS || (fmode&3) == 0)
				cln.pixelBits = 15; 
			else if ((fmode&3) == 3)
				cln.pixelBits = 63; 
			else
				cln.pixelBits = 31; 

			cln.pixelMask.SetBit((int)(cln.pixelBits + 16));
			cln.pixelMaskBit = (int)(cln.pixelBits + 16);

			//pixelCounter = 0;

			for (int i = 0; i < 8; i++)
				cln.bpldatpix[i].Zero();
		}

		private void NextPixel()
		{
			for (int i = 0; i < 8; i++)
				cln.bpldatpix[i].Shl1();
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
						((i &  1) != 0 ? 1 : 0) +
						((i &  4) != 0 ? 2 : 0) +
						((i & 16) != 0 ? 4 : 0) +
						((i & 64) != 0 ? 8 : 0)
					);
			}
		}

		private ushort [] sprdatapix = new ushort[8];
		private ushort[] sprdatbpix = new ushort[8];

		private void CopperBitplaneConvert(int h)
		{
			//if (cop.currentLine == 50)
			//	cop.currentLine = 50;

			//if the sprite horiz position matches, clock the sprite data in
			for (int s = 0; s < 8; s++)
			{
				if (spriteState[s] == SpriteState.Fetching)
				{
					int hstart = (sprpos[s] & 0xff) << 1;
					hstart |= sprctl[s] & 1; //bit 0 is low bit of hstart

					if (h == hstart>>1)
					{
						sprdatapix[s] = sprdata[s];
						sprdatbpix[s] = sprdatb[s];
						spriteMask[s] = 0x8000;
					}
				}
			}

			int m = (cln.pixelLoop/2) - 1;//2->0,4->1,8->3
			for (int p = 0; p < cln.pixelLoop; p++)
			{
				//decode the colour

				uint col;

				byte pix = 0;
				for (int i = 0, b = 1; i < cln.planes; i++, b <<= 1)
					//pix |= (byte)((cln.bpldatpix[i].AnyBitsSet(ref cln.pixelMask)) ? b : 0);
					pix |= (byte)((cln.bpldatpix[i].IsBitSet(cln.pixelMaskBit)) ? b : 0);

				NextPixel();

				//BPLAM
				pix ^= (byte)(bplcon4 >> 8);

				pix &= cdbg.bitplaneMask;
				pix |= cdbg.bitplaneMod;

				if ((bplcon0 & (1 << 10)) != 0)
				{
					//DPF
					byte pix0 = dpfLookup[pix];
					byte pix1 = dpfLookup[pix >> 1];

					uint col0 = truecolour[pix0];
					uint col1 = truecolour[pix1 == 0 ? 0: pix1 + 8];

					//which playfield is in front?
					if ((bplcon2 & (1 << 6)) != 0)
						col = pix1 != 0 ? col1 : col0;
					else
						col = pix0 != 0 ? col0 : col1;
				}
				else if (cln.planes == 6 && ((bplcon0 & (1 << 11)) != 0))
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
							col = (cln.lastcol & 0xffffff00) | px;
						}
						else if (ham == 3)
						{
							//col+G
							col = (cln.lastcol & 0xffff00ff) | (px << 8);
						}
						else
						{
							//col+R
							col = (cln.lastcol & 0xff00ffff) | (px << (8 + 8));
						}
					}
				}
				else if (cln.planes == 6 && ((bplcon0 & (1 << 11)) == 0 && (settings.ChipSet != ChipSet.AGA || (bplcon2 & (1 << 9)) == 0)))
				{
					//EHB
					col = truecolour[pix & 0x1f];
					if ((pix & 0b100000) != 0)
						col = (col & 0x00fefefe) >> 1;
				}
				else if (cln.planes == 8 && ((bplcon0 & (1 << 11)) != 0))
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
							col = (cln.lastcol & 0xffffff00) | px;
						}
						else if (ham == 3)
						{
							//col+G
							col = (cln.lastcol & 0xffff00ff) | (px << 8);
						}
						else
						{
							//col+R
							col = (cln.lastcol & 0xff00ffff) | (px << (8 + 8));
						}
					}
				}
				else
				{
					col = truecolour[pix];
				}

				//sprites
				for (int s = 7; s >= 0; s--)
				{
					if (spriteMask[s] != 0)
					{
						uint x = spriteMask[s];
						bool attached = (sprctl[s] & 0x80) != 0 && (s & 1) != 0;
						int spix = ((sprdatapix[s] & x) != 0 ? 1 : 0) + ((sprdatbpix[s] & x) != 0 ? 2 : 0);

						//in lowres, p=0,1, we want to shift every pixel (0,1) 01 &m==00
						//in hires, p=0,1,2,3 we want to shift every 2 pixels (1 and 3) &m=0101
						//in shires, p=0,1,2,3,4,5,6,7 we want to shift every 4 pixels (3 and 7) &m==01230123
						//todo: in AGA, sprites can have different resolutions
						if ((p&m) == m)
							spriteMask[s] >>= 1;
						if (attached)
						{
							s--;
							spix <<= 2;
							spix += ((sprdatapix[s] & x) != 0 ? 1 : 0) + ((sprdatbpix[s] & x) != 0 ? 2 : 0);
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

				//pixel double
				//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
				//since we've set up a hi-res screen, it' s 2x, 1x and 0.5x and shres isn't supported yet
				for (int k = 0; k < 4 / cln.pixelLoop; k++)
					screen[cop.dptr++] = (int)col;

				//remember the last colour for HAM modes
				cln.lastcol = col;
			}
		}

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

		private ushort copcon;
		private uint cop1lc;
		private uint cop2lc;

		private ushort copjmp1;
		private ushort copjmp2;
		private ushort copins;

		private ulong[] bpldat = new ulong[8];
		private uint[] bplpt = new uint[8];
		private ushort diwstrt;
		private ushort diwstop;
		private ushort bplcon0;
		private ushort bplcon1;
		private ushort bplcon2;
		private ushort bplcon3;
		private ushort bplcon4;
		private ushort ddfstrt;
		private ushort ddfstop;
		private uint bpl1mod;
		private uint bpl2mod;
		private uint[] sprpt = new uint[8];
		private ushort[] sprpos = new ushort[8];
		private ushort[] sprctl = new ushort[8];
		private ushort[] sprdata = new ushort[8];
		private ushort[] sprdatb = new ushort[8];
		private ushort clxdat;
		private ushort clxcon;
		private ushort clxcon2;

		//ECS/AGA
		private ushort vbstrt;
		private ushort vbstop;
		private ushort vsstop;
		private ushort vsstrt;
		private ushort diwhigh;
		private ushort vtotal;
		private ushort fmode;
		private ushort beamcon0;

		public ushort[] colour = new ushort[256];
		public ushort[] lowcolour = new ushort[256];
		public uint[] truecolour = new uint[256];

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.VPOSR:
					value = (ushort)((copperVert >> 8) & 1);//todo: different on hires chips
					if (settings.VideoFormat  == VideoFormat.NTSC)
						value |= (ushort)((copperVert & 1) << 7);//toggle LOL each alternate line (NTSC only)

					//if we're in interlace mode
					if ((bplcon0 & (1<<2))!=0)
					{
						value |= (ushort)((copperFrame & 1) << 15);//set LOF=1/0 on alternate frames
					}
					else
					{
						value |= 1<<15;//set LOF=1
					}

					value &= 0x80ff; 
					switch (settings.ChipSet)
					{
						case ChipSet.AGA: value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC?0x3300:0x2300); break; //Alice
						case ChipSet.ECS: value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC?0x3100:0x2100); break; //Fat Agnus
						case ChipSet.OCS: value |= (ushort)(settings.VideoFormat == VideoFormat.NTSC?0x0000:0x1000); break;//OCS
					}
					break;
				case ChipRegs.VHPOSR:
					value = (ushort)((copperVert << 8) | (copperHorz & 0x00ff));
					break;

				case ChipRegs.COPCON:
					value = (ushort)copcon;
					break;
				case ChipRegs.COP1LCH:
					value = (ushort)(cop1lc >> 16);
					break;
				case ChipRegs.COP1LCL:
					value = (ushort)cop1lc;
					break;
				case ChipRegs.COP2LCH:
					value = (ushort)(cop2lc >> 16);
					break;
				case ChipRegs.COP2LCL:
					value = (ushort)cop2lc;
					break;
				case ChipRegs.COPJMP1:
					value = (ushort)copjmp1;
					cop.copPC = cop1lc;
					break;
				case ChipRegs.COPJMP2:
					value = (ushort)copjmp2;
					cop.copPC = cop2lc;
					break;
				case ChipRegs.COPINS:
					value = copins;
					break;

				//bitplane specific

				case ChipRegs.BPL1MOD: value = (ushort)bpl1mod; break;
				case ChipRegs.BPL2MOD: value = (ushort)bpl2mod; break;

				case ChipRegs.BPLCON0: value = bplcon0; break;
				case ChipRegs.BPLCON1: value = bplcon1; break;
				case ChipRegs.BPLCON2: value = bplcon2; break;
				case ChipRegs.BPLCON3: value = bplcon3; break;
				case ChipRegs.BPLCON4: value = bplcon4; break;

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

				case ChipRegs.CLXDAT:
					logger.LogTrace("CLXDAT accessed - no sprite collisions yet");
					value = (ushort)(clxdat|0x8000);
					clxdat = 0;
					break;

				//ECS/AGA
				case ChipRegs.VBSTRT: value = vbstrt; break;
				case ChipRegs.VBSTOP: value = vbstop; break;
				case ChipRegs.VSSTOP: value = vsstop; break;
				case ChipRegs.VSSTRT: value = vsstrt; break;
				case ChipRegs.VTOTAL: value = vtotal; break;
				case ChipRegs.FMODE: value = fmode; break;
				case ChipRegs.BEAMCON0: value = beamcon0; break;
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
				//copper specific

				case ChipRegs.COPCON: copcon = value; break;
				case ChipRegs.COP1LCH: cop1lc = (cop1lc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.COP1LCL: cop1lc = (cop1lc & 0xffff0000) | (uint)(value & 0xfffe); break;
				case ChipRegs.COP2LCH: cop2lc = (cop2lc & 0x0000ffff) | ((uint)value << 16); /*logger.LogTrace($"{cop2lc:X8}"); */break;
				case ChipRegs.COP2LCL: cop2lc = (cop2lc & 0xffff0000) | (uint)(value & 0xfffe); /*logger.LogTrace($"{cop2lc:X8}"); */break;
				case ChipRegs.COPJMP1: copjmp1 = value; cop.copPC = cop1lc; break;
				case ChipRegs.COPJMP2: copjmp2 = value; cop.copPC = cop2lc; break;
				case ChipRegs.COPINS: copins = value; break;

				//bitplane specific

				case ChipRegs.BPL1MOD: bpl1mod = (uint)(short)value & 0xfffffffe; break;
				case ChipRegs.BPL2MOD: bpl2mod = (uint)(short)value & 0xfffffffe; break;

				case ChipRegs.BPLCON0: bplcon0 = value; /*logger.LogTrace($"BPLCON0 {bplcon0:X4} {(bplcon0>>12)&7}");*/ break;
				case ChipRegs.BPLCON1: bplcon1 = value; break;
				case ChipRegs.BPLCON2: bplcon2 = value; break;
				case ChipRegs.BPLCON3: bplcon3 = value; break;
				case ChipRegs.BPLCON4: bplcon4 = value; break;

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

				case ChipRegs.DIWSTRT: diwstrt = value; diwhigh = 0; break;
				case ChipRegs.DIWSTOP: diwstop = value; diwhigh = 0; break;
				case ChipRegs.DIWHIGH: diwhigh = value; break;

				case ChipRegs.DDFSTRT: 
					ddfstrt = (ushort)(value & (settings.ChipSet == ChipSet.OCS ? 0xfc : 0xfe));
					cln.lineState = CopperLineState.LineTerminated;
					break;
				case ChipRegs.DDFSTOP:
					ddfstop = (ushort)(value & (settings.ChipSet == ChipSet.OCS ? 0xfc : 0xfe));
					cln.lineState = CopperLineState.LineTerminated;
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

				case ChipRegs.CLXCON:
					clxcon = value;
					clxcon2 = 0;
					logger.LogTrace("CLXCON accessed - no sprite collisions yet");
					break;
				case ChipRegs.CLXCON2:
					clxcon2 = value;
					logger.LogTrace("CLXCON2 accessed - no sprite collisions yet");
					break;

				//ECS/AGA
				case ChipRegs.VBSTRT: vbstrt = value; break;
				case ChipRegs.VBSTOP: vbstop = value; break;
				case ChipRegs.VSSTOP: vsstop = value; break;
				case ChipRegs.VSSTRT: vsstrt = value; break;
				case ChipRegs.VTOTAL: vtotal = value; break;
				case ChipRegs.FMODE: fmode = value; break;
				case ChipRegs.BEAMCON0: beamcon0 = value; break;
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

				//logger.LogTrace($"{index,3} {colour[index]:X4} {truecolour[index]:X8}");
			}
		}

		private uint Explode(ushort c)
		{
			return (uint)(((c & 0xf) << 4) | ((c & 0xf0) << 8) | ((c & 0xf00) << 12));
		}
	}
}
