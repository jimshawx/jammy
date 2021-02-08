using System.Collections.Generic;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class Copper : IEmulate
	{
		private readonly IMemoryMappedDevice memory;
		private readonly Chips custom;
		private readonly Interrupt interrupt;

		public Copper(IMemoryMappedDevice memory, Chips custom, Interrupt interrupt)
		{
			this.memory = memory;
			this.custom = custom;
			this.interrupt = interrupt;
		}

		private ulong copperTime;
		//HRM 3rd Ed, PP24
		private uint copperHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		private uint copperVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313

		public void Emulate(ulong cycles)
		{
			copperTime += cycles;

			//every 50Hz, reset the copper list
			if (copperTime > 140_000)
			{
				copperTime -= 140_000;

				copperPC = cop1lc;

				ParseCopperList(cop1lc);
				ParseCopperList(cop2lc);

				foreach (var p in displays.Values)
					p.Refresh();

				interrupt.TriggerInterrupt(Interrupt.VERTB);
			}

			//roughly
			copperVert = (uint)((copperTime * 312) / 140_000);
			copperHorz = (uint)(copperTime % (140_000 / 312));
		}

		public void Reset()
		{
			copperTime = 0;
		}

		private uint copperPC;

		public void SetCopperPC(uint address)
		{
			copperPC = address;
			//DebugCopperList(copperPC);
		}

		private Dictionary<uint, Display> displays = new Dictionary<uint, Display>();

		public void RemoveDisplay(Playfield pf)
		{
			displays.Remove(pf.address);
		}

		private const int MAX_COPPER_ENTRIES = 512;

		public void DebugCopperList(uint copPC)
		{
			if (copPC == 0) return;

			//ParseCopperList(copPC);

			Logger.WriteLine($"Copper List @{copPC:X8}");

			uint copStartPC = copPC;

			int counter = MAX_COPPER_ENTRIES;
			while (counter-- > 0)
			{
				ushort ins = (ushort)memory.Read(0, copPC, Size.Word);
				copPC += 2;

				ushort data = (ushort)memory.Read(0, copPC, Size.Word);
				copPC += 2;

				Logger.Write($"{copPC - 4:X8} {ins:X4},{data:X4} ");

				if ((ins & 0x0001) == 0)
				{
					//MOVE
					uint reg = (uint)(ins & 0x1fe);

					Logger.WriteLine($"{copPC:X8} MOVE {ChipRegs.Name(ChipRegs.ChipBase + reg)}({reg:X4}),{data:X4}");

					//if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP1)
					//	copPC = custom.Read(copPC, ChipRegs.COP1LCH, Size.Long);//COP1LC
					//else if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP2) 
					//	copPC = custom.Read(copPC, ChipRegs.COP2LCH, Size.Long);//COP2LC
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

						Logger.WriteLine($"{copPC:X8} WAIT vp:{vp:X4} hp:{hp:X4} he:{he:X4} ve:{ve:X4} b:{blit}");
					}
					else
					{
						//SKIP
						uint horz = (uint)((ins >> 1) & 0x7f);
						uint vert = (uint)((ins >> 8) & 0xff);

						uint horzC = (uint)((data >> 1) & 0x7f);
						uint vertC = (uint)((data >> 8) & 0x3f);
						uint blitC = (uint)(data >> 15);

						Logger.WriteLine($"{copPC:X8} SKIP v:{vert:X4} h:{horz:X4} vC:{vertC} hC:{horzC} bC:{blitC}");
					}

					//this is usually how a copper list ends
					if (ins == 0xffff && data == 0xfffe)
						break;
				}
			}
		}

		public void ParseCopperList(uint copPC)
		{
			if (copPC == 0) return;

			Playfield pf;
			if (displays.ContainsKey(copPC))
			{
				pf = displays[copPC].pf;
			}
			else
			{
				pf = new Playfield(this);
				//uint tmp = copPC;
				//DebugCopperList(copPC);
				//copPC = tmp;
			}

			pf.address = copPC;

			uint copStartPC = copPC;

			int counter = MAX_COPPER_ENTRIES;
			while (counter-- > 0)
			{
				ushort ins = (ushort)memory.Read(0, copPC, Size.Word);
				copPC += 2;

				ushort data = (ushort)memory.Read(0, copPC, Size.Word);
				copPC += 2;

				//Logger.Write($"{copPC - 4:X8} {ins:X4},{data:X4} ");

				if ((ins & 0x0001) == 0)
				{
					//MOVE
					uint reg = (uint)(ins & 0x1fe);

					//Logger.WriteLine($"MOVE {ChipRegs.Name(customBase + reg)}({reg:X4}),{data:X4}");
					uint address = ChipRegs.ChipBase + reg;
					switch (address)
					{
						case ChipRegs.BPL1MOD: pf.bpl1mod = (uint)(short)data; break;
						case ChipRegs.BPL2MOD: pf.bpl2mod = (uint)(short)data; break;

						case ChipRegs.BPLCON0:
							//this is a hack to get the highest colour/resolution
							if (data > pf.bplcon0) pf.bplcon0 = data;
							break;
						case ChipRegs.BPLCON1: pf.bplcon1 = data; break;
						case ChipRegs.BPLCON2: pf.bplcon2 = data; break;
						case ChipRegs.BPLCON3: pf.bplcon3 = data; break;
						case ChipRegs.BPLCON4: pf.bplcon4 = data; break;

						case ChipRegs.BPL1DAT: pf.bpl1dat = data; break;
						case ChipRegs.BPL2DAT: pf.bpl2dat = data; break;
						case ChipRegs.BPL3DAT: pf.bpl3dat = data; break;
						case ChipRegs.BPL4DAT: pf.bpl4dat = data; break;
						case ChipRegs.BPL5DAT: pf.bpl5dat = data; break;
						case ChipRegs.BPL6DAT: pf.bpl6dat = data; break;
						case ChipRegs.BPL7DAT: pf.bpl7dat = data; break;
						case ChipRegs.BPL8DAT: pf.bpl8dat = data; break;

						case ChipRegs.BPL1PTL: pf.bpl1pt = (pf.bpl1pt & 0xffff0000) | data; break;
						case ChipRegs.BPL1PTH: pf.bpl1pt = (pf.bpl1pt & 0x0000ffff) | ((uint)data << 16); break;
						case ChipRegs.BPL2PTL: pf.bpl2pt = (pf.bpl2pt & 0xffff0000) | data; break;
						case ChipRegs.BPL2PTH: pf.bpl2pt = (pf.bpl2pt & 0x0000ffff) | ((uint)data << 16); break;
						case ChipRegs.BPL3PTL: pf.bpl3pt = (pf.bpl3pt & 0xffff0000) | data; break;
						case ChipRegs.BPL3PTH: pf.bpl3pt = (pf.bpl3pt & 0x0000ffff) | ((uint)data << 16); break;
						case ChipRegs.BPL4PTL: pf.bpl4pt = (pf.bpl4pt & 0xffff0000) | data; break;
						case ChipRegs.BPL4PTH: pf.bpl4pt = (pf.bpl4pt & 0x0000ffff) | ((uint)data << 16); break;
						case ChipRegs.BPL5PTL: pf.bpl5pt = (pf.bpl5pt & 0xffff0000) | data; break;
						case ChipRegs.BPL5PTH: pf.bpl5pt = (pf.bpl5pt & 0x0000ffff) | ((uint)data << 16); break;
						case ChipRegs.BPL6PTL: pf.bpl6pt = (pf.bpl6pt & 0xffff0000) | data; break;
						case ChipRegs.BPL6PTH: pf.bpl6pt = (pf.bpl6pt & 0x0000ffff) | ((uint)data << 16); break;
						case ChipRegs.BPL7PTL: pf.bpl7pt = (pf.bpl7pt & 0xffff0000) | data; break;
						case ChipRegs.BPL7PTH: pf.bpl7pt = (pf.bpl7pt & 0x0000ffff) | ((uint)data << 16); break;
						case ChipRegs.BPL8PTL: pf.bpl8pt = (pf.bpl8pt & 0xffff0000) | data; break;
						case ChipRegs.BPL8PTH: pf.bpl8pt = (pf.bpl8pt & 0x0000ffff) | ((uint)data << 16); break;

						case ChipRegs.DIWSTRT: pf.diwstrt = data; break;
						case ChipRegs.DIWSTOP: pf.diwstop = data; break;
						case ChipRegs.DIWHIGH: pf.diwhigh = data; break;

						case ChipRegs.DDFSTRT: pf.ddfstrt = data; break;
						case ChipRegs.DDFSTOP: pf.ddfstop = data; break;

						case ChipRegs.SPR0PTL: pf.spr0pt = (pf.spr0pt & 0xffff0000) | data; break;
						case ChipRegs.SPR0PTH: pf.spr0pt = (pf.spr0pt & 0x0000ffff) | ((uint)data << 16); break;
						case ChipRegs.SPR0POS: pf.spr0pos = data; break;
						case ChipRegs.SPR0CTL: pf.spr0ctl = data; break;
						case ChipRegs.SPR0DATA: pf.spr0data = data; break;
						case ChipRegs.SPR0DATB: pf.spr0datb = data; break;
					}

					if (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31)
					{
						uint bank = (custom.Read(0, ChipRegs.BPLCON3, Size.Word) & 0b111_00000_00000000) >> (13 - 5);

						//Amiga colour
						int index = (int)(bank + ((address - ChipRegs.COLOR00) >> 1));
						pf.colour[index] = data;

						//24bit colour
						uint colour = data;
						//pf.truecolour[index] = ((colour & 0xf) * 0x11) + ((colour & 0xf0) * 0x110) + ((colour & 0xf00) * 0x1100);

						//UI colour
						UI.SetColour(index, data);
					}

					//if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP1)
					//	copPC = custom.Read(copPC, ChipRegs.COP1LCH, Size.Long);//COP1LC
					//else if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP2) 
					//	copPC = custom.Read(copPC, ChipRegs.COP2LCH, Size.Long);//COP2LC
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

						//Logger.WriteLine($"WAIT vp:{vp:X4} hp:{hp:X4} he:{he:X4} ve:{ve:X4} b:{blit}");
					}
					else
					{
						//SKIP
						uint horz = (uint)((ins >> 1) & 0x7f);
						uint vert = (uint)((ins >> 8) & 0xff);

						uint horzC = (uint)((data >> 1) & 0x7f);
						uint vertC = (uint)((data >> 8) & 0x3f);
						uint blitC = (uint)(data >> 15);

						//Logger.WriteLine($"SKIP v:{vert:X4} h:{horz:X4} vC:{vertC} hC:{horzC} bC:{blitC}");
					}

					//this is usually how a copper list ends
					if (ins == 0xffff && data == 0xfffe)
						break;
				}
			}

			if (!displays.ContainsKey(copStartPC))
				displays[copStartPC] = new Display(pf, memory);
		}

		////HRM 3rd Ed, PP24
		//private uint beamHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		//private uint beamVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313
		private enum CopperState
		{
			Running,
			Waiting,
		}

		private enum DisplayMode
		{
			Normal,
			HiRes,
			SuperHiRes,
		}

		private void RunCopperList(uint address, bool isEvenFrame)
		{
			//grap the initial version of these regs.
			uint diwstrt = memory.Read(0, ChipRegs.DIWSTRT, Size.Word);
			uint diwstop = memory.Read(0, ChipRegs.DIWSTOP, Size.Word);
			uint bplcon0 = memory.Read(0, ChipRegs.BPLCON0, Size.Word);
			uint ddfstrt = memory.Read(0, ChipRegs.DDFSTRT, Size.Word);
			uint ddfstop = memory.Read(0, ChipRegs.DDFSTOP, Size.Word);
			uint bpl1mod = (uint)(int)(short)memory.Read(0, ChipRegs.BPL1MOD, Size.Word);
			uint bpl2mod = (uint)(int)(short)memory.Read(0, ChipRegs.BPL2MOD, Size.Word);

			DisplayMode displayMode = DisplayMode.Normal;
			if ((bplcon0 & (1 << 15)) != 0)
				displayMode = DisplayMode.HiRes;
			if ((bplcon0 & (1 << 6)) != 0)
				displayMode = DisplayMode.SuperHiRes;

			ushort [] bpldat = new ushort[8];
			uint[] bplpt = new uint[8];

			bpldat[0] = (ushort)memory.Read(0, ChipRegs.BPL1DAT, Size.Word);
			bpldat[1] = (ushort)memory.Read(0, ChipRegs.BPL2DAT, Size.Word);
			bpldat[2] = (ushort)memory.Read(0, ChipRegs.BPL3DAT, Size.Word);
			bpldat[3] = (ushort)memory.Read(0, ChipRegs.BPL4DAT, Size.Word);
			bpldat[4] = (ushort)memory.Read(0, ChipRegs.BPL5DAT, Size.Word);
			bpldat[5] = (ushort)memory.Read(0, ChipRegs.BPL6DAT, Size.Word);
			bpldat[6] = (ushort)memory.Read(0, ChipRegs.BPL7DAT, Size.Word);
			bpldat[7] = (ushort)memory.Read(0, ChipRegs.BPL8DAT, Size.Word);

			bplpt[0] = memory.Read(0, ChipRegs.BPL1PTH, Size.Long);
			bplpt[1] = memory.Read(0, ChipRegs.BPL2PTH, Size.Long);
			bplpt[2] = memory.Read(0, ChipRegs.BPL3PTH, Size.Long);
			bplpt[3] = memory.Read(0, ChipRegs.BPL4PTH, Size.Long);
			bplpt[4] = memory.Read(0, ChipRegs.BPL5PTH, Size.Long);
			bplpt[5] = memory.Read(0, ChipRegs.BPL6PTH, Size.Long);
			bplpt[6] = memory.Read(0, ChipRegs.BPL7PTH, Size.Long);
			bplpt[7] = memory.Read(0, ChipRegs.BPL8PTH, Size.Long);

			uint planes = (bplcon0>>12)&7;

			uint[] colours = new uint[256];
			colours[0] = 0xffffff;
			colours[1] = 0x000000;
			colours[2] = 0x7777cc;
			colours[3] = 0xbbbbbb;

			int waitH=0, waitV=0;
			int waitHMask = 0xff, waitVMask = 0xff;

			bool in_display = true;
			bool in_fetch = true;
			bool is_new_pixel = true;

			int pixelCountdown = 0;
			uint col=0x000000;

			uint copPC = address;
			CopperState state = CopperState.Running;
			int lines = isEvenFrame ? 312 : 313;
			for (int v = 0; v < lines; v++)
			{
				for (int h = 0; h < 227; h++)
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

							memory.Write(0, regAddress, data, Size.Word);
							
							//todo: if regAddress is one of the cached values, need to update them, and deal with writes to copjmp
						}
						else if ((ins & 0x0001) == 1)
						{
							//WAIT
							waitH = ins & 0xfe;
							waitV = (ins >> 8) & 0xff;

							waitHMask = (data & 0xfe)|0xff00;
							waitVMask = ((data >> 8) & 0x7f)|0xff80;
							uint blit = (uint)(data >> 15);//totod: blitter is immediate, so currently ignored. in reality, if blitter-busy bit is set the comparisons will fail.

							if ((data & 1) == 0)
							{
								//WAIT
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
					}
					else if (state == CopperState.Waiting)
					{
						if ((v&waitVMask) >= waitV)
						{
							if ((h&waitHMask) >= waitH)
								state = CopperState.Running;
						}
					}

					//are we within the display area defined by DIW regs?
					if (in_display)
					{
						//are we within the bitplane fetch area defined by DDF?
						if (in_fetch)
						{
							//is it time to fetch a new pixel colour?
							if (is_new_pixel)
							{
								//is it time to fetch some more bitblane data?
								pixelCountdown--;
								if (pixelCountdown < 0)
								{
									for (int i = 0; i < planes; i++)
									{
										if ((planes & (1 << i)) != 0)
										{
											bpldat[i] = (ushort)custom.Read(0, bplpt[i], Size.Word);
											bplpt[i] += 2;
										}
									}

									pixelCountdown = 15;
								}

								//decode the colour
								byte pix = 0;
								for (int i = 0; i < planes; i++)
									if ((planes & (1 << i)) != 0)
										pix |= (byte)((bpldat[i] & (1 << (pixelCountdown /*^15*/))) != 0 ? (1 << i) : 0);
								//pix is the colour
								col = colours[pix];
							}
						}
						else
						{
							//output colour 0 pixels
							col = colours[0];
						}
					}
					else
					{
						// output black pixels
						col = 0x000000;
					}

					//plot something

					//227 hclocks @ 3.5MHz, DIW normally 0x81 to 0x1C1 = 0x140 = 296
					//                      DDF          0x38 to 0x1D0 = 0x198 = 408

					/*
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
					*/
				}
				//next horizontal line
				for (int i = 0; i < planes; i++)
				{
					if ((planes & (1 << i)) != 0)
					{
						bplpt[i] += ((i&1)==0)? bpl1mod:bpl2mod;
					}
				}
			}
		}

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			//Logger.WriteLine($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");
			return value;
		}

		private uint copcon;
		private uint cop1lc;
		private uint cop2lc;

		private uint copjmp1;
		private uint copjmp2;
		private uint copins;

		public void Write(uint insaddr, uint address, ushort value)
		{


			switch (address)
			{
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
					SetCopperPC(cop1lc);
					break;
				case ChipRegs.COPJMP2:
					copjmp2 = value;
					SetCopperPC(cop2lc);
					break;
				case ChipRegs.COPINS:
					copins = value;
					break;
			}

			if (cop1lc == 0xc00276 || cop2lc == 0xc00276)
				Logger.WriteLine($"W {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");
		}

	}
}
