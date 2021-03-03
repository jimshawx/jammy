using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using Size = RunAmiga.Core.Types.Types.Size;

namespace RunAmiga.Core.Custom
{
	public class Copper : ICopper
	{
		private readonly IMemoryMappedDevice memory;
		private readonly IChips custom;
		private readonly ILogger logger;

		private readonly Form form;
		private readonly Bitmap bitmap;
		private readonly PictureBox picture;

		//private const int SCREEN_WIDTH = 1280;
		//private const int SCREEN_HEIGHT = 1024;

		private const int SCREEN_WIDTH = 227 * 4;
		private const int SCREEN_HEIGHT = 313;

		private readonly int[] screen = new int[SCREEN_WIDTH * SCREEN_HEIGHT];

		public Copper(IMemory memory, IChips custom, IEmulationWindow emulationWindow, ILogger<Copper> logger)
		{
			this.memory = memory;
			this.custom = custom;
			this.logger = logger;

			form = emulationWindow.GetForm();
			form.ClientSize = new System.Drawing.Size(SCREEN_WIDTH, SCREEN_HEIGHT);
			bitmap = new Bitmap(SCREEN_WIDTH, SCREEN_HEIGHT, PixelFormat.Format32bppRgb);
			picture = new PictureBox {Image = bitmap, ClientSize = new System.Drawing.Size(SCREEN_WIDTH, SCREEN_HEIGHT), Enabled = false};
			form.Controls.Add(picture);
			form.Show();
		}

		private ulong copperTime;
		//HRM 3rd Ed, PP24
		private uint copperHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		private uint copperVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313

		private bool dbug=false;
		private int cf = 0;
		public void Emulate(ulong cycles)
		{
			copperTime += cycles;

			//every 50Hz, reset the copper list
			if (copperTime > 140_000)
			{
				copperTime -= 140_000;

				//RunCopperList(cop1lc, false);
				if ((cf++ % 20) == 0)
					RunCopperList(cop1lc, false);

				if (dbug)
				{
					DebugCopperList(cop1lc);
					dbug = false;
				}

				//if (((25 + cf++) % 50) == 0)
				//	RunCopperList(cop2lc, false);

				custom.Write(0, ChipRegs.INTREQ, 0x8000 + (1u << (int)Interrupt.VERTB), Size.Word);
			}

			//roughly
			copperVert = (uint)((copperTime * 312) / 140_000);
			copperHorz = (uint)(copperTime % (140_000 / 312));
		}

		public void Reset()
		{
			copperTime = 0;
		}

		private const int MAX_COPPER_ENTRIES = 512;

		public void DebugCopperList(uint copPC)
		{
			if (copPC == 0) return;

			//ParseCopperList(copPC);

			logger.LogTrace($"Copper List @{copPC:X8}");

			uint copStartPC = copPC;

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

					if ((data & 1) == 0)
					{
						//WAIT
						uint hp = (uint)((ins >> 1) & 0x7f);
						uint vp = (uint)((ins >> 8) & 0xff);

						uint he = (uint)((data >> 1) & 0x7f);
						uint ve = (uint)((data >> 8) & 0x7f);
						uint blit = (uint)(data >> 15);

						logger.LogTrace($"{copPC:X8} WAIT vp:{vp:X4} hp:{hp:X4} he:{he:X4} ve:{ve:X4} b:{blit}");
					}
					else
					{
						//SKIP
						uint horz = (uint)((ins >> 1) & 0x7f);
						uint vert = (uint)((ins >> 8) & 0xff);

						uint horzC = (uint)((data >> 1) & 0x7f);
						uint vertC = (uint)((data >> 8) & 0x3f);
						uint blitC = (uint)(data >> 15);

						logger.LogTrace($"{copPC:X8} SKIP v:{vert:X4} h:{horz:X4} vC:{vertC} hC:{horzC} bC:{blitC}");
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
		private enum CopperState
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

		private void RunCopperList(uint copPC, bool isEvenFrame)
		{
			Array.Clear(screen,0,screen.Length);
			
			if (copPC == 0) return;

			//logger.LogTrace($"COP  {copPC:X6} {bplpt[0]:X6} {bplpt[1]:X6}");
			//colour[0] = 0xfff;
			//colour[1] = 0x000;
			//colour[2] = 0x77c;
			//colour[3] = 0xbbb;
			//truecolour[0] = 0xffffff;
			//truecolour[1] = 0x000000;
			//truecolour[2] = 0x7777cc;
			//truecolour[3] = 0xbbbbbb;

			int waitH=0, waitV=0;
			int waitHMask = 0xff, waitVMask = 0xff;

			//bool in_display = true;
			//bool in_fetch = true;
			//bool is_new_pixel = true;

			ushort pixelCountdown;
			uint col=0x000000;

			CopperState state = CopperState.Running;
			int lines = isEvenFrame ? 312 : 313;

			int dptr = 0;
			int[] cnt = new int[8];

			ushort[] bpldatdma = new ushort[8];
			bool copperEarlyOut = false;
			int dbugLine = -1;//diwstrt >> 8;

			char[] fetch = new char[227];
			char[] write = new char[227];

			for (int v = 0; v < lines; v++)
			{
				for (int i = 0; i < 8; i++)
					cnt[i] = 0;

				int lineStart = dptr;
				pixelCountdown = 0x8000;
				for (int h = 0; h < 227; h++)
				{
					ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);

					//copper instruction every even clock
					if ((h & 1) == 0 && (dmacon & 0b101000000) == 0b101000000)
					{
						if (state == CopperState.Running)
						{
							ushort ins = (ushort)memory.Read(0, copPC, Size.Word);
							copPC += 2;

							ushort data = (ushort)memory.Read(0, copPC, Size.Word);
							copPC += 2;

							if ((ins & 0x0001) == 0)
							{
								//MOVE
								uint reg = (uint)(ins & 0x1fe);
								uint regAddress = ChipRegs.ChipBase + reg;

								custom.Write(0, regAddress, data, Size.Word);

								if (regAddress == ChipRegs.COPJMP1)
								{
									copPC = cop1lc;
									//logger.LogTrace($"JMP1 {copPC:X6}");
								}
								else if (regAddress == ChipRegs.COPJMP2)
								{
									copPC = cop2lc;
									//logger.LogTrace($"JMP2 {copPC:X6}");
								}
							}
							else if ((ins & 0x0001) == 1)
							{
								//WAIT
								waitH = ins & 0xfe;
								waitV = (ins >> 8) & 0xff;

								waitHMask = (data & 0xfe) | 0xff00;
								waitVMask = ((data >> 8) & 0x7f) | 0x80;

								uint blit = (uint)(data >> 15);

								//todo: blitter is immediate, so currently ignored.
								//todo: in reality if blitter-busy bit is set the comparisons will fail.

								if ((data & 1) == 0)
								{
									//WAIT
									//logger.LogTrace($"WAIT {waitH},{waitV} {waitHMask:X3} {waitVMask:X3}");
									state = CopperState.Waiting;
								}
								else
								{
									//SKIP
									if ((v & waitVMask) >= waitV)
									{
										if ((h & waitHMask) >= waitH)
											copPC += 4;
									}
								}
							}

							//this is usually how a copper list ends
							if (copperEarlyOut)
							{
								if (ins == 0xffff && data == 0xfffe)
									h = v = 1000;
							}
						}
						else if (state == CopperState.Waiting)
						{
							if ((v & waitVMask) >= waitV)
							{
								if ((h & waitHMask) >= waitH)
								{
									//logger.LogTrace($"RUN  {h},{v}");
									state = CopperState.Running;
								}
							}
						}
					}
					//end copper instruction

					//bitplane fetching

					//how many pixels should be fecthed per clock in the current mode?
					int pixelLoop;
					int pixmod;
					if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
					{
						//4 colour clocks, fetch 16 pixels
						//1 colour clock, draw 4 pixel
						pixelLoop = 4;
						pixmod = 4;
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

					}

					int planes = (bplcon0 >> 12) & 7;
					int diwstrth = diwstrt&0xff;
					int diwstrtv = diwstrt>>8;
					int diwstoph = (diwstop&0xff)|0x100;
					int diwstopv = (diwstop >> 8) | (((diwstop&0x8000) >> 7) ^ 0x100);

					//round up ddfstop to nearest 8 pixels difference from ddfstart, and add one extra cycle for luck
					ushort d = (ushort)(((ddfstop-ddfstrt)+ 7)&~7);
					ushort ddfstopfix = (ushort)(ddfstrt + d + 8);

					int[] fetchLo = { 8, 4, 6, 2, 7, 3, 5, 1};
					int[] fetchHi = { 4, 2, 3, 1};

					if (v == dbugLine)
					{
						fetch[h] = '-';
						write[h] = '-';
					}

					//is it the visible area, vertically?
					if (v >= diwstrtv && v < diwstopv)
					{
						//is it time to do bitplane DMA
						//when h >= ddfstrt, bitplanes are fetching. one plane per cycle, until all the planes are fetched
						if (h >= ddfstrt && h < ddfstopfix && (dmacon & 0b110000000) == 0b110000000)//bitplane DMA is on
						{
							int planeIdx = (h - ddfstrt) % pixmod;

							int plane;
							if ((bplcon0 & (uint)BPLCON0.HiRes) != 0)
								plane = fetchHi[planeIdx] - 1;
							else
								plane = fetchLo[planeIdx] - 1;
							if (plane < planes)
							{
								bpldatdma[plane] = (ushort)memory.Read(0, bplpt[plane], Size.Word);
								bplpt[plane] += 2;

								cnt[plane]++;
								if (v == dbugLine)
									fetch[h] = Convert.ToChar(plane+48);
							}
						}

						//is it the visible area horizontally
						//when h >= diwstrt, bits are read out of the bitplane data, turned into pixels and output
						if (h >= diwstrth >> 1 && h < diwstoph >> 1)
						{
							if (pixelCountdown == 0x8000)
							{
								if (v == dbugLine)
									write[h] = 'x';
								for (int i = 0; i < 8; i++)
									bpldat[i] = bpldatdma[i];
							}
							else
							{
								if (v == dbugLine)
									write[h] = '.';
							}

							for (int p = 0; p < pixelLoop; p++)
							{
								//decode the colour
								byte pix = 0;

								for (int i = 0; i < planes; i++)
									pix |= (byte)((bpldat[i] & pixelCountdown) != 0 ? (1 << i) : 0);

								//pix is the Amiga colour
								int bank = (bplcon3 & 0b111_00000_00000000) >> (13 - 5);
								col = truecolour[pix + bank];

								//duplicate the pixel 4 times in low res, 2x in hires and 1x in shres
								//since we've set up a hi-res screen, it' s 2x, 1x and 0.5x and shres isn't supported yet
								for (int k = 0; k < 4 / pixelLoop; k++)
									screen[dptr++] = (int)col;

								pixelCountdown = (ushort)((pixelCountdown >> 1) | (pixelCountdown << 15)); //next bit
							}
						}
						else
						{
							//outside horizontal area

							//output colour 0 pixels
							col = truecolour[0];
							//col = 0xff0000;

							for (int k = 0; k < 4; k++)
								screen[dptr++] = (int)col;
						}
					}
					else
					{
						//outside vertical area

						//output colour 0 pixels
						col = truecolour[0];
						//col = 0xff0000;

						for (int k = 0; k < 4; k++)
							screen[dptr++] = (int)col;
					}
				}

				if (v == dbugLine)
				{
					logger.LogInformation($"DDF {ddfstrt:X4} {ddfstop:X4} FMOD {fmode:X4}");
					logger.LogInformation($"DIW {diwstrt:X4} {diwstop:X4} {diwhigh:X4}");
					logger.LogInformation($"MOD {bpl1mod:X4} {bpl2mod:X4}");
					logger.LogInformation($"BPL {bplcon0:X4} {bplcon1:X4} {bplcon2:X4} {bplcon3:X4} {bplcon4:X4}");
					logger.LogTrace($"{cnt[0]} {cnt[1]}");
					var sb = new StringBuilder();
					sb.AppendLine();
					for (int i = 0; i < 227; i++)
						sb.Append(fetch[i]);

					logger.LogTrace(sb.ToString());
					sb.Clear();
					sb.AppendLine();
					for (int i = 0; i < 227; i++)
						sb.Append(write[i]);
					logger.LogTrace(sb.ToString());
				}

				//next horizontal line
				{
					int planes = (bplcon0 >> 12) & 7;
					for (int i = 0; i < planes; i++)
					{
						bplpt[i] += ((i & 1) == 0) ? bpl2mod : bpl1mod;
					}
				}

				//this should be a no-op
				dptr += SCREEN_WIDTH - (dptr - lineStart);
			}

			// sprites
			if ((custom.Read(0, ChipRegs.DMACONR, Size.Word) & 0b100100000) == 0b100100000)
			{
				for (int s = 7; s >= 0; s--)
				{
					for (;;)
					{
						sprpos[s] = (ushort)memory.Read(0, sprpt[s], Size.Word); sprpt[s] += 2;
						sprctl[s] = (ushort)memory.Read(0, sprpt[s], Size.Word); sprpt[s] += 2;

						if (sprpos[s] == 0 && sprctl[s] == 0)
							break;

						int hstart = (sprpos[s] & 0xff) << 1;
						int vstart = sprpos[s] >> 8;
						int vstop = sprctl[s] >> 8;

						vstart += (sprctl[s] & 4) << 6; //bit 2 is high bit of vstart
						vstop += (sprctl[s] & 2) << 7; //bit 1 is high bit of vstop
						hstart |= sprctl[s] & 1; //bit 0 is low bit of hstart

						//x2 because they are low-res pixels on our high-res bitmap
						dptr = (hstart * 2) + vstart * SCREEN_WIDTH;
						for (int r = vstart; r < vstop; r++)
						{
							sprdata[s] = (ushort)memory.Read(0, sprpt[s], Size.Word); sprpt[s] += 2;
							sprdatb[s] = (ushort)memory.Read(0, sprpt[s], Size.Word); sprpt[s] += 2;

							for (int x = 0x8000; x > 0; x >>= 1)
							{
								int pix = (sprdata[s] & x)!=0?1:0 + (sprdatb[s] & x)!=0?2:0;
								if (pix != 0)
									screen[dptr] = (int)truecolour[16 + 4*(s>>1) + pix];
								dptr++;
								if (dptr >= screen.Length) break;
							}

							dptr += SCREEN_WIDTH - 16;
							if (dptr >= screen.Length) break;
						}

						if (dptr >= screen.Length) break;
					}
				}
			}

			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, SCREEN_WIDTH, SCREEN_HEIGHT), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
			Marshal.Copy(screen, 0, bitmapData.Scan0, screen.Length);
			bitmap.UnlockBits(bitmapData);
			picture.Image = bitmap;
			form.Invalidate();

			Application.DoEvents();
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
