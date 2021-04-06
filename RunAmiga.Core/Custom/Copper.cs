using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class Copper : ICopper
	{
		private readonly IMemoryMappedDevice memory;
		private readonly IChips custom;
		private readonly IEmulationWindow emulationWindow;
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;

		//private const int SCREEN_WIDTH = 1280;
		//private const int SCREEN_HEIGHT = 1024;

		private const int SCREEN_WIDTH = DMA_WIDTH*4;//227 * 4;
		private const int SCREEN_HEIGHT = 313*2;//x2 for scan double

		//sprite DMA starts at 0x18, but can be eaten into by bitmap DMA
		//normal bitmap DMA start at 0x38, overscan at 0x30
		private const int DMA_START = 0x30;
		//bitmap DMA ends at 0xD8, with 8 slots after that
		private const int DMA_END = 0xF0;

		//private const int DMA_START = 0;
		//private const int DMA_END = 227;

		private const int DMA_WIDTH = DMA_END - DMA_START;

		private readonly int[] screen = new int[SCREEN_WIDTH * SCREEN_HEIGHT];

		public Copper(IChipRAM memory, IChips custom, IEmulationWindow emulationWindow, IInterrupt interrupt, ILogger<Copper> logger)
		{
			this.memory = memory;
			this.custom = custom;
			this.emulationWindow = emulationWindow;
			this.interrupt = interrupt;
			this.logger = logger;

			emulationWindow.SetPicture(SCREEN_WIDTH, SCREEN_HEIGHT);

			emulationWindow.SetKeyHandlers(dbug_Keydown, dbug_Keyup);
		}

		private void dbug_Keyup(int obj)
		{
		}

		private void dbug_Keydown(int obj)
		{
			if (obj == (int)Keyboard.VK.VK_F11) cdbg.dbug = true;
			if (obj == (int)Keyboard.VK.VK_UP) cdbg.dbugLine--;
			if (obj == (int)Keyboard.VK.VK_DOWN) cdbg.dbugLine++;
			if (obj == (int)Keyboard.VK.VK_RIGHT) cdbg.dbugLine = -1;
			if (obj == (int)Keyboard.VK.VK_LEFT) cdbg.dbugLine = diwstrt>>8;
		}

		private ulong copperTime;
		//HRM 3rd Ed, PP24
		private uint copperHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		private uint copperVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313

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

					if (cdbg.dbug)
					{
						DebugCopperList(cop1lc);
						cdbg.dbug = false;
					}

					//start the next frame
					RunCopperVerticalBlankStart(cop1lc);
				}

				//run the next scanline
				cop.currentLine = (int)copperVert;
				RunCopperLine();
			}
		}

		public void Reset()
		{
			copperTime = 0;
		}

		private const int MAX_COPPER_ENTRIES = 512;

		public void DebugCopperList(uint copPC)
		{
			if (copPC == 0) return;

			logger.LogTrace($"Copper List @{copPC:X8}");

			int counter = MAX_COPPER_ENTRIES;
			while (counter-- > 0)
			{
				ushort ins = (ushort)memory.Read(0, copPC, Size.Word);
				copPC += 2;

				ushort data = (ushort)memory.Read(0, copPC, Size.Word);
				copPC += 2;

				logger.LogTrace($"{copPC - 4:X8} {ins:X4},{data:X4} ");

				if ((ins & 0x0001) == 0)
				{
					//MOVE
					uint reg = (uint)(ins & 0x1fe);

					logger.LogTrace($"{copPC:X8} MOVE {ChipRegs.Name(ChipRegs.ChipBase + reg)}({reg:X4}),{data:X4}");

					if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP1)
						copPC = custom.Read(copPC, ChipRegs.COP1LCH, Size.Long);//COP1LC
					else if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP2) 
						copPC = custom.Read(copPC, ChipRegs.COP2LCH, Size.Long);//COP2LC
				}
				else if ((ins & 0x0001) == 1)
				{
					//WAIT/SKIP

					uint hp = (uint)(ins & 0xfe);
					uint vp = (uint)((ins >> 8) & 0xff);

					uint he = (uint)((data & 0xfe) | 0xff00);
					uint ve = (uint)(((data >> 8) & 0x7f) | 0x80);
					uint blit = (uint)(data >> 15);

					if ((data & 1) == 0)
					{
						//WAIT
						logger.LogTrace($"{copPC:X8} WAIT vp:{vp:X4} hp:{hp:X4} ve:{ve:X4} he:{he:X4} b:{blit}");
					}
					else
					{
						//SKIP
						logger.LogTrace($"{copPC:X8} SKIP vp:{vp:X4} hp:{hp:X4} ve:{ve:X4} he:{he:X4} b:{blit}");
					}

					//this is usually how a copper list ends
					if (ins == 0xffff && data == 0xfffe)
						break;
				}
			}
		}

		////HRM 3rd Ed, PP24
		//private uint beamHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		//private uint beamVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313
		private enum CopperStatus
		{
			Running,
			Waiting,
		}

		[Flags]
		private enum BPLCON0 : uint
		{
			HiRes=1<<15,
			SuperHiRes=1<<6,
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

			public void Reset(uint copperPC)
			{
				copPC = copperPC;

				dptr = 0;
				waitH = 0;
				waitV = 0;
				waitHMask = 0xff;
				waitVMask = 0xff;
				status = CopperStatus.Running;
			}
		}

		private class CopperDebug
		{
			public char[] fetch = new char[256];
			public char[] write = new char[256];
			public int dma;
			public int dbugLine = -1;
			public bool dbug = false;

			public void Reset()
			{
				dma = 0;
			}
		}

		private void Debug(CopperFrame cf, CopperDebug cd, CopperLine cl)
		{
			if (cdbg.dbugLine == -1)
				return;

			logger.LogInformation($"LINE {cdbg.dbugLine}");
			logger.LogInformation($"DDF {ddfstrt:X4} {ddfstop:X4} ({cl.wordCount}) {cl.ddfstrtfix:X4} {cl.ddfstopfix:X4} FMOD {fmode:X4}");
			logger.LogInformation($"DIW {diwstrt:X4} {diwstop:X4} {diwhigh:X4} V:{cl.diwstrtv}->{cl.diwstopv}({cl.diwstopv - cl.diwstrtv}) H:{cl.diwstrth}->{cl.diwstoph}({cl.diwstoph - cl.diwstrth}/16={(cl.diwstoph - cl.diwstrth) / 16})");
			logger.LogInformation($"MOD {bpl1mod:X4} {bpl2mod:X4}");
			logger.LogInformation($"BPL {bplcon0:X4} {bplcon1:X4} {bplcon2:X4} {bplcon3:X4} {bplcon4:X4}");
			var sb = new StringBuilder();
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

		private CopperFrame cop = new CopperFrame();
		private CopperDebug cdbg = new CopperDebug();
		private CopperLine cln = new CopperLine();

		private void RunCopperVerticalBlankStart(uint copperPC)
		{
			cop.Reset(copperPC);
			cdbg.Reset();

			if (copperPC == 0) return;

		}

		private void RunCopperVerticalBlankEnd()
		{
			if (cop.copPC == 0) return;

			RunSprites();

			Debug(cop, cdbg, cln);

			emulationWindow.Blit(screen);
		}

		private class CopperLine
		{
			public ushort pixelMask;
			public ushort[] bpldatdma = new ushort[8];
			public bool copperEarlyOut = false;

			public int planes;
			public int diwstrth = 0;
			public int diwstrtv = 0;
			public int diwstoph = 0;
			public int diwstopv = 0;

			public ushort ddfstrtfix = 0;
			public ushort ddfstopfix = 0;
			public int wordCount = 0;

			public int pixelLoop;
			public int pixmod;
			public uint lastcol = 0;

			public void InitLine(ushort bplcon0, ushort diwstrt, ushort diwstop, ushort ddfstrt, ushort ddfstop)
			{
				// start - pre-cache some useful per-scanline values

				//don't expect these to change within a scanline, weird undocumented things
				//will happen which this emulator probably will never understand

				planes = (bplcon0 >> 12) & 7;
				diwstrth = diwstrt & 0xff;
				diwstrtv = diwstrt >> 8;
				diwstoph = (diwstop & 0xff) | 0x100;
				diwstopv = (diwstop >> 8) | (((diwstop & 0x8000) >> 7) ^ 0x100);

				//how many pixels should be fecthed per clock in the current mode?
				if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
				{
					//4 colour clocks, fetch 16 pixels
					//1 colour clock, draw 4 pixel
					pixelLoop = 4;
					pixmod = 4;

					ddfstrtfix = (ushort)(ddfstrt & 0xfffc);
					wordCount = (ddfstop - ddfstrt) / 4 + 2;
					//word count needs to be a multiple of planes
					//wordCount = ((wordCount / planes) + (planes - 1)) * planes;
					ddfstopfix = (ushort)(ddfstrtfix + wordCount * 4);
					diwstrth &= 0xf8;
					diwstoph &= 0x1f8;
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

					ddfstrtfix = (ushort)(ddfstrt & 0xfff8);
					wordCount = (ddfstop - ddfstrt) / 8 + 1;
					ddfstopfix = (ushort)(ddfstrtfix + wordCount * 8);
					diwstrth &= 0xf0;
					diwstoph &= 0x1f0;
				}

				//pixelMask = 0x8000;
			}

		}

		private void RunCopperLine()
		{
			if (cop.copPC == 0) return;

			int lineStart = cop.dptr;

			cln.pixelMask = 0x8000;
			for (int h = 0; h < 256; h++)
			{
				cln.InitLine(bplcon0, diwstrt, diwstop, ddfstrt, ddfstop);

				ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);

				//Copper DMA is ON
				if ((dmacon & (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.COPEN)) == (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.COPEN))
					CopperInstruction(h);

				//bitplane fetching (optimisation)
				if (h < DMA_START || h >= DMA_END)
					continue;

				if (cop.currentLine == cdbg.dbugLine)
				{
					cdbg.fetch[h] = '-';
					cdbg.write[h] = '-';
				}

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
						if (h >= cln.ddfstrtfix && h < cln.ddfstopfix)
							CopperBitplaneFetch(h);
					}

					//is it the visible area horizontally?
					//when h >= diwstrt, bits are read out of the bitplane data, turned into pixels and output
					if (h >= cln.diwstrth >> 1 && h < (cln.diwstoph + 1) >> 1)
					{
						CopperBitplaneConvert(h);
					}
					else
					{
						//outside horizontal area

						//output colour 0 pixels
						uint col = truecolour[0];
						//col = 0xff0000;

						for (int k = 0; k < 4; k++)
							screen[cop.dptr++] = (int)col;
					}

					//is it time to latch the DMAd bitplanes into the DAT registers
					//if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
					//{
					//	//40, 44, 48, 4C
					//	if (((h) & 3) == 0)
					//	{
					//		for (int i = 0; i < 8; i++)
					//			bpldat[i] = bpldatdma[i];
					//		if (cop.currentLine == cop.dbugLine)
					//		{
					//			write[h] = 'x';
					//			dma++;
					//		}
					//	}
					//	else
					//	{
					//		if (cop.currentLine == cop.dbugLine)
					//			write[h] = '.';
					//	}
					//}
					//else
					//{
					//	//40, 48, 50, 58
					//	if (((h) & 7) == 0)
					//	{
					//		for (int i = 0; i < 8; i++)
					//			bpldat[i] = bpldatdma[i];
					//		if (cop.currentLine == cop.dbugLine)
					//		{
					//			write[h] = 'x';
					//			dma++;
					//		}
					//	}
					//	else
					//	{
					//		if (cop.currentLine == cop.dbugLine)
					//			write[h] = '.';
					//	}
					//}
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

			//next horizontal line
			{
				int planesz = (bplcon0 >> 12) & 7;
				for (int i = 0; i < planesz; i++)
				{
					bplpt[i] += ((i & 1) == 0) ? bpl2mod : bpl1mod;
				}
			}

			//this should be a no-op
			cop.dptr += SCREEN_WIDTH - (cop.dptr - lineStart);

			//scan double
			for (int i = lineStart; i < lineStart + SCREEN_WIDTH; i++)
				screen[cop.dptr++] = screen[i];
		}

		private void CopperInstruction(int h)
		{
			bool copperEarlyOut = false;

			//copper instruction every even clock (and copper DMA is on)
			if ((h & 1) == 0 && h < 227)
			{
				if (cop.status == CopperStatus.Running)
				{
					ushort ins = (ushort)memory.Read(0, cop.copPC, Size.Word);
					cop.copPC += 2;

					ushort data = (ushort)memory.Read(0, cop.copPC, Size.Word);
					cop.copPC += 2;

					if ((ins & 0x0001) == 0)
					{
						//MOVE
						uint reg = (uint)(ins & 0x1fe);
						uint regAddress = ChipRegs.ChipBase + reg;

						custom.Write(0, regAddress, data, Size.Word);

						if (regAddress == ChipRegs.COPJMP1)
						{
							cop.copPC = cop1lc;
							//logger.LogTrace($"JMP1 {copPC:X6}");
						}
						else if (regAddress == ChipRegs.COPJMP2)
						{
							cop.copPC = cop2lc;
							//logger.LogTrace($"JMP2 {copPC:X6}");
						}
					}
					else if ((ins & 0x0001) == 1)
					{
						//WAIT
						cop.waitH = ins & 0xfe;
						cop.waitV = (ins >> 8) & 0xff;

						cop.waitHMask = (data & 0xfe) | 0xff00;
						cop.waitVMask = ((data >> 8) & 0x7f) | 0x80;

						uint blit = (uint)(data >> 15);

						//todo: blitter is immediate, so currently ignored.
						//todo: in reality if blitter-busy bit is set the comparisons will fail.

						if ((data & 1) == 0)
						{
							//WAIT
							//logger.LogTrace($"WAIT {waitH},{waitV} {waitHMask:X3} {waitVMask:X3}");
							cop.status = CopperStatus.Waiting;
						}
						else
						{
							//SKIP
							if ((cop.currentLine & cop.waitVMask) >= cop.waitV)
							{
								if ((h & cop.waitHMask) >= cop.waitH)
									cop.copPC += 4;
							}
						}
					}

					//this is usually how a copper list ends
					if (copperEarlyOut)
					{
						if (ins == 0xffff && data == 0xfffe)
							h = cop.currentLine = 1000;
					}
				}
				else if (cop.status == CopperStatus.Waiting)
				{
					if ((cop.currentLine & cop.waitVMask) == cop.waitV)
					{
						if ((h & cop.waitHMask) >= cop.waitH)
						{
							//logger.LogTrace($"RUN  {h},{v}");
							cop.status = CopperStatus.Running;
						}
					}
				}
			}
		}

		private void CopperBitplaneFetch(int h)
		{
			int[] fetchLo = { 8, 4, 6, 2, 7, 3, 5, 1 };
			int[] fetchHi = { 4, 2, 3, 1 };

			int planeIdx = (h - ddfstrt) % cln.pixmod;

			int plane;
			if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
				plane = fetchHi[planeIdx] - 1;
			else
				plane = fetchLo[planeIdx] - 1;

			if (plane < cln.planes)
			{
				cln.bpldatdma[plane] = (ushort)memory.Read(0, bplpt[plane], Size.Word);
				bplpt[plane] += 2;

				if (cop.currentLine == cdbg.dbugLine)
					cdbg.fetch[h] = Convert.ToChar(plane + 48 + 1);
			}
			else
			{
				if (cop.currentLine == cdbg.dbugLine)
					cdbg.fetch[h] = '+';
			}
		}

		[MethodImpl(MethodImplOptions.NoOptimization)]
		private void CopperBitplaneConvert(int h)
		{
			if (cln.pixelMask == 0x8000)
			{
				if (cop.currentLine == cdbg.dbugLine)
				{
					cdbg.write[h] = 'x';
					cdbg.dma++;
				}

				for (int i = 0; i < 8; i++)
					bpldat[i] = cln.bpldatdma[i];
			}
			else
			{
				if (cop.currentLine == cdbg.dbugLine)
					cdbg.write[h] = '.';
			}

			if ((bplcon0 & (1 << 10)) != 0)
			{
				//DPF

				for (int p = 0; p < cln.pixelLoop; p++)
				{
					uint col0;
					uint col1;
					uint col;

					int bank = (bplcon3 & 0b111_00000_00000000) >> (13 - 5);

					//decode the colours
					byte pix0 = 0;

					for (int i = 0, b = 1; i < cln.planes; i+=2, b <<= 1)
						pix0 |= (byte)((bpldat[i] & cln.pixelMask) != 0 ? b : 0);

					//pix is the Amiga colour
					col0 = truecolour[pix0 + bank];

					byte pix1 = 0;

					for (int i = 1, b = 1; i < cln.planes; i += 2, b <<= 1)
						pix1 |= (byte)((bpldat[i] & cln.pixelMask) != 0 ? b : 0);

					//pix is the Amiga colour
					col1 = truecolour[pix1 + 8 + bank];

					//which playfield is in front?
					if ((bplcon2 & (1 << 6)) != 0)
						col = pix1 != 0 ? col1 : col0;
					else
						col = pix0 != 0 ? col0 : col1;

					//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
					//since we've set up a hi-res screen, it' s 2x, 1x and 0.5x and shres isn't supported yet
					for (int k = 0; k < 4 / cln.pixelLoop; k++)
						screen[cop.dptr++] = (int)col;

					cln.pixelMask = (ushort)((cln.pixelMask >> 1) | (cln.pixelMask << 15)); //next bit
				}
			}
			else if (cln.planes == 6)
			{
				if ((bplcon0 & (1 << 11)) != 0)
				{
					//HAM6

					for (int p = 0; p < cln.pixelLoop; p++)
					{
						uint col;

						int bank = (bplcon3 & 0b111_00000_00000000) >> (13 - 5);

						//decode the colour
						byte pix = 0;

						for (int i = 0, b = 1; i < 6; i++, b <<= 1)
							pix |= (byte)((bpldat[i] & cln.pixelMask) != 0 ? b : 0);

						byte ham = (byte)(pix & 0b11_0000);
						pix &= 0xf;
						//pix is the Amiga colour
						if (ham == 0)
						{
							col = truecolour[pix+bank];
						}
						else
						{
							ham >>= 4;
							uint px = (uint)(pix*0x11);
							if (ham == 1)
							{
								//col+B
								col = (cln.lastcol & 0xffffff00) | px;
							}
							else if (ham == 3)
							{
								//col+G
								col = (cln.lastcol & 0xffff00ff) | (px <<  8);
							}
							else
							{
								//col+R
								col = (cln.lastcol & 0xff00ffff) | (px << (8 + 8));
							}
						}

						//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
						//since we've set up a hi-res screen, it' s 2x, 1x and 0.5x and shres isn't supported yet
						for (int k = 0; k < 4 / cln.pixelLoop; k++)
							screen[cop.dptr++] = (int)col;

						cln.pixelMask = (ushort)((cln.pixelMask >> 1) | (cln.pixelMask << 15)); //next bit
						cln.lastcol = col;
					}
				}
				else
				{
					//EHB
					for (int p = 0; p < cln.pixelLoop; p++)
					{
						//decode the colour
						byte pix = 0;

						for (int i = 0, b = 1; i < 6; i++, b <<= 1)
							pix |= (byte)((bpldat[i] & cln.pixelMask) != 0 ? b : 0);

						//pix is the Amiga colour
						var col = truecolour[pix & 0x1f];
						if ((pix&0b100000)!=0)
							col = ((col & 0x00fefefe)>>1)|0xff000000;

						//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
						//since we've set up a hi-res screen, it' s 2x, 1x and 0.5x and shres isn't supported yet
						for (int k = 0; k < 4 / cln.pixelLoop; k++)
							screen[cop.dptr++] = (int)col;

						cln.pixelMask = (ushort)((cln.pixelMask >> 1) | (cln.pixelMask << 15)); //next bit
					}
				}
			}
			else
			{
				for (int p = 0; p < cln.pixelLoop; p++)
				{
					uint col;

					//decode the colour
					byte pix = 0;

					for (int i = 0, b = 1; i < cln.planes; i++, b <<= 1)
						pix |= (byte)((bpldat[i] & cln.pixelMask) != 0 ? b : 0);

					//pix is the Amiga colour
					int bank = (bplcon3 & 0b111_00000_00000000) >> (13 - 5);
					col = truecolour[pix + bank];

					//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
					//since we've set up a hi-res screen, it' s 2x, 1x and 0.5x and shres isn't supported yet
					for (int k = 0; k < 4 / cln.pixelLoop; k++)
						screen[cop.dptr++] = (int)col;

					cln.pixelMask = (ushort)((cln.pixelMask >> 1) | (cln.pixelMask << 15)); //next bit
				}
			}
		}

		private void RunSprites()
		{
			int dptr;
			// sprites
			ushort dmaconr = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			if ((dmaconr & (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.SPREN)) == (ushort)(ChipRegs.DMA.DMAEN | ChipRegs.DMA.SPREN))
			{
				for (int s = 7; s >= 0; s--)
				{
					for (;;)
					{
						sprpos[s] = (ushort)memory.Read(0, sprpt[s], Size.Word);
						sprpt[s] += 2;
						sprctl[s] = (ushort)memory.Read(0, sprpt[s], Size.Word);
						sprpt[s] += 2;

						if (sprpos[s] == 0 && sprctl[s] == 0)
							break;

						int hstart = (sprpos[s] & 0xff) << 1;
						int vstart = sprpos[s] >> 8;
						int vstop = sprctl[s] >> 8;

						vstart += (sprctl[s] & 4) << 6; //bit 2 is high bit of vstart
						vstop += (sprctl[s] & 2) << 7; //bit 1 is high bit of vstop
						hstart |= sprctl[s] & 1; //bit 0 is low bit of hstart

						hstart -= DMA_START << 1;

						if (hstart < 0)
							break;

						//x2 because they are low-res pixels on our high-res bitmap
						//dptr = (hstart * 2) + vstart * SCREEN_WIDTH;
						dptr = (hstart * 2) + vstart * 2 * SCREEN_WIDTH; //scan double
						for (int r = vstart; r < vstop; r++)
						{
							sprdata[s] = (ushort)memory.Read(0, sprpt[s], Size.Word);
							sprpt[s] += 2;
							sprdatb[s] = (ushort)memory.Read(0, sprpt[s], Size.Word);
							sprpt[s] += 2;

							for (int x = 0x8000; x > 0; x >>= 1)
							{
								int pix = (sprdata[s] & x) != 0 ? 1 : 0 + (sprdatb[s] & x) != 0 ? 2 : 0;
								if (pix != 0)
									screen[dptr] = screen[dptr + 1] = screen[dptr + SCREEN_WIDTH] = screen[dptr + SCREEN_WIDTH + 1] = (int)truecolour[16 + 4 * (s >> 1) + pix];
								dptr += 2;
								if (dptr + SCREEN_WIDTH + 1 >= screen.Length) break;
							}

							dptr += (SCREEN_WIDTH - 16) * 2;

							if (dptr >= screen.Length) break;
						}

						if (dptr >= screen.Length) break;
					}
				}
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

		private ushort[] bpldat = new ushort[8];
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

		//ECS/AGA
		private ushort vbstrt;
		private ushort vbstop;
		private ushort diwhigh;
		private ushort vtotal;
		private ushort fmode;
		private ushort beamcon0;

		public ushort[] colour = new ushort[256];
		public uint[] truecolour = new uint[256];

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.VPOSR:
					value = (ushort)((copperVert >> 8) & 1);
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
					break;
				case ChipRegs.COPJMP2:
					value = (ushort)copjmp2;
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

				case ChipRegs.BPL1DAT: value = bpldat[0]; break;
				case ChipRegs.BPL2DAT: value = bpldat[1]; break;
				case ChipRegs.BPL3DAT: value = bpldat[2]; break;
				case ChipRegs.BPL4DAT: value = bpldat[3]; break;
				case ChipRegs.BPL5DAT: value = bpldat[4]; break;
				case ChipRegs.BPL6DAT: value = bpldat[5]; break;
				case ChipRegs.BPL7DAT: value = bpldat[6]; break;
				case ChipRegs.BPL8DAT: value = bpldat[7]; break;

				case ChipRegs.BPL1PTL: value = (ushort)bplpt[0]; break;
				case ChipRegs.BPL1PTH: value = (ushort)(bplpt[0]>>16); break;
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
				case ChipRegs.SPR0PTH: value = (ushort)(sprpt[0]>>16); break;
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
				case ChipRegs.VTOTAL: value = vtotal; break;
				case ChipRegs.FMODE: value = fmode; break;
				case ChipRegs.BEAMCON0: value = beamcon0; break;
			}

			if (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31)
			{
				//uint bank = (custom.Read(0, ChipRegs.BPLCON3, Size.Word) & 0b111_00000_00000000) >> (13 - 5);
				int bank = (bplcon3 & 0b111_00000_00000000) >> (13 - 5);

				//Amiga colour
				int index = (int)(bank + ((address - ChipRegs.COLOR00) >> 1));
				value = colour[index] = value;
			}

			return value;
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				//copper specific

				case ChipRegs.COPCON:
					copcon = value;
					break;
				case ChipRegs.COP1LCH:
					cop1lc = (cop1lc & 0x0000ffff) | ((uint)value << 16);
					cop1lc &= ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.COP1LCL:
					cop1lc = (cop1lc & 0xffff0000) | value;
					cop1lc &= ChipRegs.ChipAddressMask;
					//DebugCopperList(cop1lc);
					break;
				case ChipRegs.COP2LCH:
					cop2lc = (cop2lc & 0x0000ffff) | ((uint)value << 16);
					cop2lc &= ChipRegs.ChipAddressMask;
					break;
				case ChipRegs.COP2LCL:
					cop2lc = (cop2lc & 0xffff0000) | value;
					cop2lc &= ChipRegs.ChipAddressMask;
					//DebugCopperList(cop2lc);
					break;
				case ChipRegs.COPJMP1:
					copjmp1 = value;
					//SetCopperPC(cop1lc);
					break;
				case ChipRegs.COPJMP2:
					copjmp2 = value;
					//SetCopperPC(cop2lc);
					break;
				case ChipRegs.COPINS:
					copins = value;
					break;

				//bitplane specific

				case ChipRegs.BPL1MOD: bpl1mod = (uint)(short)value; break;
				case ChipRegs.BPL2MOD: bpl2mod = (uint)(short)value; break;

				case ChipRegs.BPLCON0: bplcon0 = value; break;
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

				case ChipRegs.BPL1PTL: bplpt[0] = (bplpt[0] & 0xffff0000) | value; break;
				case ChipRegs.BPL1PTH: bplpt[0] = (bplpt[0] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.BPL2PTL: bplpt[1] = (bplpt[1] & 0xffff0000) | value; break;
				case ChipRegs.BPL2PTH: bplpt[1] = (bplpt[1] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.BPL3PTL: bplpt[2] = (bplpt[2] & 0xffff0000) | value; break;
				case ChipRegs.BPL3PTH: bplpt[2] = (bplpt[2] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.BPL4PTL: bplpt[3] = (bplpt[3] & 0xffff0000) | value; break;
				case ChipRegs.BPL4PTH: bplpt[3] = (bplpt[3] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.BPL5PTL: bplpt[4] = (bplpt[4] & 0xffff0000) | value; break;
				case ChipRegs.BPL5PTH: bplpt[4] = (bplpt[4] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.BPL6PTL: bplpt[5] = (bplpt[5] & 0xffff0000) | value; break;
				case ChipRegs.BPL6PTH: bplpt[5] = (bplpt[5] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.BPL7PTL: bplpt[6] = (bplpt[6] & 0xffff0000) | value; break;
				case ChipRegs.BPL7PTH: bplpt[6] = (bplpt[6] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.BPL8PTL: bplpt[7] = (bplpt[7] & 0xffff0000) | value; break;
				case ChipRegs.BPL8PTH: bplpt[7] = (bplpt[7] & 0x0000ffff) | ((uint)value << 16); break;

				case ChipRegs.DIWSTRT: diwstrt = value; diwhigh = 0; break;
				case ChipRegs.DIWSTOP: diwstop = value; diwhigh = 0; break;
				case ChipRegs.DIWHIGH: diwhigh = value; break;

				case ChipRegs.DDFSTRT: ddfstrt = value; break;
				case ChipRegs.DDFSTOP: ddfstop = value; break;

				case ChipRegs.SPR0PTL: sprpt[0] = (sprpt[0] & 0xffff0000) | value; break;
				case ChipRegs.SPR0PTH: sprpt[0] = (sprpt[0] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.SPR0POS: sprpos[0] = value; break;
				case ChipRegs.SPR0CTL: sprctl[0] = value; break;
				case ChipRegs.SPR0DATA: sprdata[0] = value; break;
				case ChipRegs.SPR0DATB: sprdatb[0] = value; break;

				case ChipRegs.SPR1PTL: sprpt[1] = (sprpt[1] & 0xffff0000) | value; break;
				case ChipRegs.SPR1PTH: sprpt[1] = (sprpt[1] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.SPR1POS: sprpos[1] = value; break;
				case ChipRegs.SPR1CTL: sprctl[1] = value; break;
				case ChipRegs.SPR1DATA: sprdata[1] = value; break;
				case ChipRegs.SPR1DATB: sprdatb[1] = value; break;

				case ChipRegs.SPR2PTL: sprpt[2] = (sprpt[2] & 0xffff0000) | value; break;
				case ChipRegs.SPR2PTH: sprpt[2] = (sprpt[2] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.SPR2POS: sprpos[2] = value; break;
				case ChipRegs.SPR2CTL: sprctl[2] = value; break;
				case ChipRegs.SPR2DATA: sprdata[2] = value; break;
				case ChipRegs.SPR2DATB: sprdatb[2] = value; break;

				case ChipRegs.SPR3PTL: sprpt[3] = (sprpt[3] & 0xffff0000) | value; break;
				case ChipRegs.SPR3PTH: sprpt[3] = (sprpt[3] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.SPR3POS: sprpos[3] = value; break;
				case ChipRegs.SPR3CTL: sprctl[3] = value; break;
				case ChipRegs.SPR3DATA: sprdata[3] = value; break;
				case ChipRegs.SPR3DATB: sprdatb[3] = value; break;

				case ChipRegs.SPR4PTL: sprpt[4] = (sprpt[4] & 0xffff0000) | value; break;
				case ChipRegs.SPR4PTH: sprpt[4] = (sprpt[4] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.SPR4POS: sprpos[4] = value; break;
				case ChipRegs.SPR4CTL: sprctl[4] = value; break;
				case ChipRegs.SPR4DATA: sprdata[4] = value; break;
				case ChipRegs.SPR4DATB: sprdatb[4] = value; break;
				
				case ChipRegs.SPR5PTL: sprpt[5] = (sprpt[5] & 0xffff0000) | value; break;
				case ChipRegs.SPR5PTH: sprpt[5] = (sprpt[5] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.SPR5POS: sprpos[5] = value; break;
				case ChipRegs.SPR5CTL: sprctl[5] = value; break;
				case ChipRegs.SPR5DATA: sprdata[5] = value; break;
				case ChipRegs.SPR5DATB: sprdatb[5] = value; break;

				case ChipRegs.SPR6PTL: sprpt[6] = (sprpt[6] & 0xffff0000) | value; break;
				case ChipRegs.SPR6PTH: sprpt[6] = (sprpt[6] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.SPR6POS: sprpos[6] = value; break;
				case ChipRegs.SPR6CTL: sprctl[6] = value; break;
				case ChipRegs.SPR6DATA: sprdata[6] = value; break;
				case ChipRegs.SPR6DATB: sprdatb[6] = value; break;

				case ChipRegs.SPR7PTL: sprpt[7] = (sprpt[7] & 0xffff0000) | value; break;
				case ChipRegs.SPR7PTH: sprpt[7] = (sprpt[7] & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.SPR7POS: sprpos[7] = value; break;
				case ChipRegs.SPR7CTL: sprctl[7] = value; break;
				case ChipRegs.SPR7DATA: sprdata[7] = value; break;
				case ChipRegs.SPR7DATB: sprdatb[7] = value; break;

				//ECS/AGA
				case ChipRegs.VBSTRT: vbstrt = value; break;
				case ChipRegs.VBSTOP: vbstop = value; break;
				case ChipRegs.VTOTAL: vtotal = value; break;
				case ChipRegs.FMODE: fmode = value; break;
				case ChipRegs.BEAMCON0: beamcon0 = value; break;
			}

			if (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31)
			{
				//uint bank = (custom.Read(0, ChipRegs.BPLCON3, Size.Word) & 0b111_00000_00000000) >> (13 - 5);
				int bank = (bplcon3 & 0b111_00000_00000000) >> (13 - 5);

				//Amiga colour
				int index = (int)(bank + ((address - ChipRegs.COLOR00) >> 1));
				colour[index] = value;

				//24bit colour
				uint col = value;
				truecolour[index] = ((col & 0xf) * 0x11) + ((col & 0xf0) * 0x110) + ((col & 0xf00) * 0x1100);
			}
		}

	}
}
