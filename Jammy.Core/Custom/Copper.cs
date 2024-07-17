using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class Copper : ICopper
	{
		private readonly IChipsetClock clock;
		private readonly IDMA memory;
		private readonly IChips custom;

		private readonly IInterrupt interrupt;
		private readonly EmulationSettings settings;
		private readonly ILogger logger;

		public Copper(IChipsetClock clock, IDMA memory, IChips custom, IEmulationWindow emulationWindow,
			IOptions<EmulationSettings> settings, ILogger<Copper> logger)
		{
			this.clock = clock;
			this.memory = memory;
			this.custom = custom;
			this.interrupt = interrupt;
			this.settings = settings.Value;
			this.logger = logger;

			emulationWindow.SetKeyHandlers(dbug_Keydown, dbug_Keyup);

			//start the first frame
			//RunCopperVerticalBlankStart();

			logger.LogTrace("Press F9 to enable Copper debug");
		}


		//global state of the copper

		private CopperStatus status;
		private uint copPC;
		private uint activeCopperAddress;
		private uint waitMask;
		private uint waitPos;
		private int waitTimer;
		private uint waitBlit;
		private int waitH = 0;
		private int waitV = 0;
		private int waitHMask;
		private int waitVMask;

		private ushort ins;
		private ushort data;

		//HRM 3rd Ed, PP24
		//private uint copperFrame = 0;

		public void Reset()
		{
		}

		public void Emulate(ulong cycles)
		{
			//copper instruction every odd clock (and copper DMA is on)
			if ((clock.HorizontalPos&1)!=0)
				CopperInstruction();

			//if (status == CopperStatus.Waiting && data != 0xfffe)
			//	logger.LogTrace($"Hit VBL while still waiting for {data:X2}");
			
			//if (cdbg.dbug)
			//{
			//	DebugCopperList(cop1lc);
			//	cdbg.dbug = false;
			//}

			//start the next frame
			//CopperNextFrame();

			//cdbg.Reset();
		}

		public void CopperNextFrame()
		{
			copPC = activeCopperAddress = cop1lc;

			waitH = 0;
			waitV = 0;
			waitHMask = 0xff;
			waitVMask = 0xff;
			waitBlit = 0;
			status = CopperStatus.RunningWord1;
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
		
		private void CopperInstruction()
		{
			if (status == CopperStatus.Stopped)
			{
				return;
			}
			else if (status == CopperStatus.WakingUp)
			{
				//burn a cycle after waking up
				waitTimer--;
				if (waitTimer <= 0)
					status = CopperStatus.RunningWord1;
			}
			else if (status == CopperStatus.RunningWord1)
			{
				ins = copins = (ushort)memory.Read(copPC, DMAPriority.Copper, Size.Word);
				copPC += 2;
				status = CopperStatus.RunningWord2;
			}
			else if (status == CopperStatus.RunningWord2)
			{
				data = (ushort)memory.Read(copPC, DMAPriority.Copper, Size.Word);
				copPC += 2;
				status = CopperStatus.RunningWord1;

				if ((ins & 0x0001) == 0)
				{
					//MOVE
					uint reg = (uint)(ins & 0x1fe);
					uint regAddress = ChipRegs.ChipBase + reg;

					//in OCS mode CDANG in COPCON means can access >= 0x40->0x7E as well as the usual >= 0x80
					//in ECS/AGA mode CDANG in COPCON means can access ALL chip regs, otherwise only >= 040
					if (settings.ChipSet == ChipSet.OCS)
					{
						if (((copcon & 2) != 0 && reg >= 0x40) || reg >= 0x80)
						{
							custom.Write(0, regAddress, data, Size.Word);
						}
						else
						{
							status = CopperStatus.Stopped;
							//logger.LogTrace($"Copper Stopped! W {ChipRegs.Name(regAddress)} {data:X4} CDANG: {((copcon&2)!=0?1:0)}");
						}
					}
					else
					{
						if ((copcon & 2) != 0 || reg >= 0x40)
						{
							custom.Write(0, regAddress, data, Size.Word);
						}
						else
						{
							status = CopperStatus.Stopped;
							//logger.LogTrace($"Copper Stopped! W {ChipRegs.Name(regAddress)} {data:X4} CDANG: {((copcon&2)!=0?1:0)}");
						}
					}
				}
				else if ((ins & 0x0001) == 1)
				{
					//WAIT
					waitH = ins & 0xfe;
					waitV = (ins >> 8) & 0xff;

					waitHMask = data & 0xfe;
					waitVMask = ((data >> 8) & 0x7f) | 0x80;

					waitMask = (uint)(waitHMask | (waitVMask << 8));
					waitPos = (uint)((waitV << 8) | waitH);

					waitBlit = (uint)(data >> 15);

					if ((data & 1) == 0)
					{
						//WAIT
						status = CopperStatus.Waiting;
					}
					else
					{
						//SKIP
						//logger.LogTrace("SKIP");

						uint coppos = (uint)((clock.VerticalPos & 0xff) << 8) | (clock.HorizontalPos & 0xff);
						coppos &= waitMask;
						if (CopperCompare(coppos, (waitPos & waitMask)))
						{
							//logger.LogTrace($"RUN  {h},{currentLine} {coppos:X4} {waitPos:X4}");
							copPC += 4;
						}
					}
				}
			}
			else if (status == CopperStatus.Waiting)
			{
				//if ((currentLine & waitVMask) == waitV)
				//{
				//	if ((h & waitHMask) >= waitH)
				//	{
				//		//logger.LogTrace($"RUN ({h},{currentLine})");
				//		status = CopperStatus.RunningWord1;
				//	}
				//}

				//If blitter-busy bit is set the comparisons will fail.
				if (waitBlit == 0 && (custom.Read(0, ChipRegs.DMACONR, Size.Word) & (1 << 14)) != 0)
				{
					logger.LogTrace("WAIT delayed due to blitter running");
					return;
				}

				uint coppos = (uint)((clock.VerticalPos & 0xff) << 8) | (clock.HorizontalPos & 0xff);
				coppos &= waitMask;
				if (CopperCompare(coppos, (waitPos & waitMask)))
				{
					waitTimer = 1;
					status = CopperStatus.WakingUp;

					if (ins == 0xffff && data == 0xfffe)
					{
						logger.LogTrace("Went off the end of the Copper List");
						status = CopperStatus.Waiting;
					}
				}
			}
		}

		private bool CopperCompare(uint coppos, uint waitPos)
		{
			return coppos >= waitPos;
		}

		private ushort copcon;
		private uint cop1lc;
		private uint cop2lc;

		private ushort copjmp1;
		private ushort copjmp2;
		private ushort copins;

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.COPCON:  value = (ushort)copcon; break;
				case ChipRegs.COP1LCH: value = (ushort)(cop1lc >> 16); break;
				case ChipRegs.COP1LCL: value = (ushort)cop1lc; break;
				case ChipRegs.COP2LCH: value = (ushort)(cop2lc >> 16); break;
				case ChipRegs.COP2LCL: value = (ushort)cop2lc; break;
				case ChipRegs.COPJMP1: value = (ushort)copjmp1; copPC = cop1lc; break;
				case ChipRegs.COPJMP2: value = (ushort)copjmp2; copPC = cop2lc; break; 
				case ChipRegs.COPINS: value = copins; break;
			}

			return value;
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.COPCON: copcon = value; break;
				case ChipRegs.COP1LCH: cop1lc = (cop1lc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.COP1LCL: cop1lc = (cop1lc & 0xffff0000) | (uint)(value & 0xfffe); break;
				case ChipRegs.COP2LCH: cop2lc = (cop2lc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.COP2LCL: cop2lc = (cop2lc & 0xffff0000) | (uint)(value & 0xfffe); break;
				case ChipRegs.COPJMP1: copjmp1 = value; copPC = cop1lc; break;
				case ChipRegs.COPJMP2: copjmp2 = value; copPC = cop2lc; break;
				case ChipRegs.COPINS: copins = value; break;
			}
		}

		private const int MAX_COPPER_ENTRIES = 2048;

		private string DisassembleCopperList(uint copPC)
		{
			if (copPC == 0) return "";

			var csb = GetStringBuilder();
			csb.AppendLine($"Copper List @{copPC:X6} PC:{copPC:X6}");

			var skipTaken = new HashSet<uint>();

			int counter = MAX_COPPER_ENTRIES;
			while (counter-- > 0)
			{
				ushort ins = (ushort)memory.DebugRead(copPC, Size.Word);
				copPC += 2;

				ushort data = (ushort)memory.DebugRead(copPC, Size.Word);
				copPC += 2;

				//csb.AppendLine($"{copPC - 4:X8} {ins:X4},{data:X4} ");

				if ((ins & 0x0001) == 0)
				{
					//MOVE
					uint reg = (uint)(ins & 0x1fe);

					csb.AppendLine($"{copPC - 4:X6} MOVE {ins:X4} {data:X4} {ChipRegs.Name(ChipRegs.ChipBase + reg)}({reg:X3}),{data:X4}");

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
						if (ins == 0xffff && data == 0xfffe)
							csb.AppendLine($"{copPC - 4:X6} WAIT {ins:X4} {data:X4} End of Copper list");
						else
							csb.AppendLine($"{copPC - 4:X6} WAIT {ins:X4} {data:X4} vp:{vp:X2} hp:{hp:X2} ve:{ve:X2} he:{he:X2} b:{blit}");
					}
					else
					{
						//SKIP
						csb.AppendLine($"{copPC - 4:X6} SKIP {ins:X4} {data:X4} vp:{vp:X2} hp:{hp:X2} ve:{ve:X2} he:{he:X2} b:{blit}");
						if (!skipTaken.Add(copPC - 4))
						{
							copPC += 4;
							csb.AppendLine("SKIPPED");
						}
					}

					//this is usually how a copper list ends
					if (ins == 0xffff && data == 0xfffe)
						break;
				}
			}
			return csb.ToString();
		}

		private readonly StringBuilder sb = new StringBuilder();
		private StringBuilder GetStringBuilder()
		{
			return sb;
		}

		public void DebugCopperList(uint copPC)
		{
			string csb = DisassembleCopperList(copPC);
			logger.LogTrace(csb);
			File.WriteAllText($"../../../../copper{DateTime.Now:yyyyMMdd-HHmmss}.txt", csb);
		}

		public void Dumping(bool enabled)
		{
			throw new NotImplementedException();
		}

		public string GetDisassembly()
		{
			return DisassembleCopperList(activeCopperAddress);
		}

		//private class CopperDebug
		//{
		//	public char[] fetch = new char[256];
		//	public char[] write = new char[256];
		//	public int dma;
		//	public int dbugLine = -1;
		//	public bool dbug = false;
		//	public int ddfSHack;
		//	public int ddfEHack;
		//	public int diwSHack;
		//	public int diwEHack;
		//	public byte bitplaneMask = 0xff;
		//	public byte bitplaneMod = 0;
		//	public bool ws;
		//	private StringBuilder sb = new StringBuilder();

		//	public void Reset()
		//	{
		//		dma = 0;
		//		//ddfSHack = ddfEHack = diwEHack = diwSHack = 0;
		//	}

		//	public StringBuilder GetStringBuilder()
		//	{
		//		sb.Length = 0;
		//		return sb;
		//	}
		//}

		//private void Debug(CopperFrame cf, CopperDebug cd, ushort dmacon)
		//{
		//	if (cdbg.dbugLine == -1)
		//		return;
		//	if (cdbg.dbugLine != cf.currentLine)
		//		return;

		//	logger.LogTrace($"LINE {cdbg.dbugLine}");
		//	//logger.LogTrace($"DDF {ddfstrt:X4} {ddfstop:X4} ({cl.wordCount}) {cl.ddfstrtfix:X4}{cdbg.ddfSHack:+#0;-#0} {cl.ddfstopfix:X4}{cdbg.ddfEHack:+#0;-#0} FMODE {fmode:X4}");
		//	//logger.LogTrace($"DIW {diwstrt:X4} {diwstop:X4} {diwhigh:X4} V:{cl.diwstrtv}->{cl.diwstopv}({cl.diwstopv - cl.diwstrtv}) H:{cl.diwstrth}{cdbg.diwSHack:+#0;-#0}->{cl.diwstoph}{cdbg.diwEHack:+#0;-#0}({cl.diwstoph - cl.diwstrth}/16={(cl.diwstoph - cl.diwstrth) / 16})");
		//	//logger.LogTrace($"MOD {bpl1mod:X4} {bpl2mod:X4} DMA {Dmacon(dmacon)}");
		//	//logger.LogTrace($"BCN 0:{bplcon0:X4} {Bplcon0()} 1:{bplcon1:X4} {Bplcon1()} 2:{bplcon2:X4} {Bplcon2()} 3:{bplcon3:X4} {Bplcon3()} 4:{bplcon4:X4} {Bplcon4()}");
		//	//logger.LogTrace($"BPL {bplpt[0]:X6} {bplpt[1]:X6} {bplpt[2]:X6} {bplpt[3]:X6} {bplpt[4]:X6} {bplpt[5]:X6} {bplpt[6]:X6} {bplpt[7]:X6} {new string(Convert.ToString(cd.bitplaneMask, 2).PadLeft(8, '0').Reverse().ToArray())} {new string(Convert.ToString(cd.bitplaneMod, 2).PadLeft(8, '0').Reverse().ToArray())}");
		//	var sb = cdbg.GetStringBuilder();
		//	sb.AppendLine();
		//	for (int i = 0; i < 256; i++)
		//		sb.Append(cd.fetch[i]);

		//	logger.LogTrace(sb.ToString());
		//	sb.Clear();
		//	sb.AppendLine();
		//	for (int i = 0; i < 256; i++)
		//		sb.Append(cd.write[i]);
		//	sb.Append($"({cd.dma})");
		//	logger.LogTrace(sb.ToString());
		//}

		//private string Dmacon(ushort dmacon)
		//{
		//	var sb = cdbg.GetStringBuilder();
		//	if ((dmacon & 0x200) != 0) sb.Append("DMA ");
		//	if ((dmacon & 0x100) != 0) sb.Append("BPL ");
		//	if ((dmacon & 0x80) != 0) sb.Append("COP ");
		//	if ((dmacon & 0x40) != 0) sb.Append("BLT ");
		//	if ((dmacon & 0x20) != 0) sb.Append("SPR ");
		//	return sb.ToString();
		//}

		//private string Bplcon0()
		//{
		//	var sb = cdbg.GetStringBuilder();
		//	if ((bplcon0 & 0x8000) != 0) sb.Append("H ");
		//	else if ((bplcon0 & 0x40) != 0) sb.Append("SH ");
		//	else if ((bplcon0 & 0x80) != 0) sb.Append("UH ");
		//	else sb.Append("N ");
		//	if ((bplcon0 & 0x400) != 0) sb.Append("DPF ");
		//	if ((bplcon0 & 0x800) != 0) sb.Append("HAM ");
		//	if ((bplcon0 & 0x10) != 0) sb.Append("8");
		//	else sb.Append($"{(bplcon0 >> 12) & 7} ");
		//	if ((bplcon0 & 0x4) != 0) sb.Append("LACE");

		//	if (((bplcon0 >> 12) & 7) == 6 && ((bplcon0 & (1 << 11)) == 0 && (bplcon0 & (1 << 10)) == 0 && (settings.ChipSet != ChipSet.AGA || (bplcon2 & (1 << 9)) == 0))) sb.Append("EHB ");

		//	return sb.ToString();
		//}

		//private string Bplcon1()
		//{
		//	int pf0 = bplcon1 & 0xf;
		//	int pf1 = (bplcon1 >> 4) & 0xf;
		//	return $"SCR{pf0}:{pf1} ";
		//}

		//private string Bplcon2()
		//{
		//	var sb = cdbg.GetStringBuilder();
		//	if ((bplcon2 & (1 << 9)) != 0) sb.Append("KILLEHB ");
		//	if ((bplcon2 & (1 << 6)) != 0) sb.Append("PF2PRI ");
		//	return sb.ToString();
		//}

		//private string Bplcon3()
		//{
		//	var sb = cdbg.GetStringBuilder();
		//	sb.Append($"BNK{bplcon3 >> 13} ");
		//	sb.Append($"PF2O{(bplcon3 >> 10) & 7} ");
		//	sb.Append($"SPRRES{(bplcon3 >> 6) & 3} ");
		//	if ((bplcon3 & (1 << 9)) != 0) sb.Append("LOCT ");
		//	return sb.ToString();
		//}

		//private string Bplcon4()
		//{
		//	var sb = cdbg.GetStringBuilder();
		//	sb.Append($"BPLAM{bplcon4 >> 8:X2} ");
		//	sb.Append($"ESPRM{(bplcon4 >> 4) & 15:X2} ");
		//	sb.Append($"OSPRM{bplcon4 & 15:X2} ");
		//	if ((bplcon3 & (1 << 9)) != 0) sb.Append("LOCT ");
		//	return sb.ToString();
		//}

		//private CopperFrame cop = new CopperFrame();
		//private CopperDebug cdbg = new CopperDebug();


		private void dbug_Keyup(int obj)
		{
		}

		//private bool keys = false;
		private void dbug_Keydown(int obj)
		{
			//if (obj == (int)VK.VK_F9) { keys ^= true; logger.LogTrace($"KEYS {keys}"); }

			//if (keys)
			//{
			//	if (obj == (int)VK.VK_F11) cdbg.dbug = true;
			//	if (obj == (int)VK.VK_F7) cdbg.dbugLine--;
			//	if (obj == (int)VK.VK_F6) cdbg.dbugLine++;
			//	if (obj == (int)VK.VK_F8) cdbg.dbugLine = -1;
			//	//if (obj == (int)VK.VK_F5) cdbg.dbugLine = diwstrt >> 8;

			//	if (obj == (int)'Q') cdbg.ddfSHack++;
			//	if (obj == (int)'W') cdbg.ddfSHack--;
			//	if (obj == (int)'E') cdbg.ddfSHack = 0;
			//	if (obj == (int)'R') cdbg.ddfEHack++;
			//	if (obj == (int)'T') cdbg.ddfEHack--;
			//	if (obj == (int)'Y') cdbg.ddfEHack = 0;

			//	if (obj == (int)'1') cdbg.diwSHack++;
			//	if (obj == (int)'2') cdbg.diwSHack--;
			//	if (obj == (int)'3') cdbg.diwSHack = 0;
			//	if (obj == (int)'4') cdbg.diwEHack++;
			//	if (obj == (int)'5') cdbg.diwEHack--;
			//	if (obj == (int)'6') cdbg.diwEHack = 0;

			//	if (obj == (int)'A') cdbg.bitplaneMask ^= 1;
			//	if (obj == (int)'S') cdbg.bitplaneMask ^= 2;
			//	if (obj == (int)'D') cdbg.bitplaneMask ^= 4;
			//	if (obj == (int)'F') cdbg.bitplaneMask ^= 8;
			//	if (obj == (int)'G') cdbg.bitplaneMask ^= 16;
			//	if (obj == (int)'H') cdbg.bitplaneMask ^= 32;
			//	if (obj == (int)'J') cdbg.bitplaneMask ^= 64;
			//	if (obj == (int)'K') cdbg.bitplaneMask ^= 128;
			//	if (obj == (int)'L')
			//	{
			//		cdbg.bitplaneMask = 0xff;
			//		cdbg.bitplaneMod = 0;
			//	}

			//	if (obj == (int)'Z') cdbg.bitplaneMod ^= 1;
			//	if (obj == (int)'X') cdbg.bitplaneMod ^= 2;
			//	if (obj == (int)'C') cdbg.bitplaneMod ^= 4;
			//	if (obj == (int)'V') cdbg.bitplaneMod ^= 8;
			//	if (obj == (int)'B') cdbg.bitplaneMod ^= 16;
			//	if (obj == (int)'N') cdbg.bitplaneMod ^= 32;
			//	if (obj == (int)'M') cdbg.bitplaneMod ^= 64;
			//	if (obj == (int)VK.VK_OEM_COMMA) cdbg.bitplaneMod ^= 128;

			//	if (obj == (int)VK.VK_F10) cdbg.ws = true;
		}
	}
}

