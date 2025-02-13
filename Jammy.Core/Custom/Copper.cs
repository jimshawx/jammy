using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Jammy.Core.Debug;
using Newtonsoft.Json.Linq;
using Jammy.Core.Persistence;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class Copper : ICopper
	{
		private readonly IChipsetClock clock;
		private IDMA memory;
		private readonly IChips custom;
		private readonly IChipsetDebugger debugger;

		private readonly EmulationSettings settings;
		private readonly ILogger logger;

		private bool copperPtrDebug = false;

		public Copper(IChipsetClock clock, IChips custom, IChipsetDebugger debugger,
			IOptions<EmulationSettings> settings, ILogger<Copper> logger)
		{
			this.clock = clock;
			this.custom = custom;
			this.debugger = debugger;
			this.settings = settings.Value;
			this.logger = logger;
		}


		//global state of the copper
		[Persist]
		private CopperStatus status;
		[Persist]
		private uint copPC;
		[Persist]
		private uint activeCopperAddress;
		[Persist]
		private uint waitMask;
		[Persist]
		private uint waitPos;
		[Persist]
		private int waitTimer;
		[Persist]
		private uint waitBlit;
		[Persist]
		private int waitH = 0;
		[Persist]
		private int waitV = 0;
		[Persist]
		private int waitHMask;
		[Persist]
		private int waitVMask;

		[Persist]
		private ushort ins;
		[Persist]
		private ushort data;

		//HRM 3rd Ed, PP24
		public void Init(IDMA dma)
		{
			memory = dma;
		}

		public void Reset()
		{
			status = CopperStatus.Stopped;
			copcon = 0;
		}

		public void Emulate()
		{
			if ((clock.ClockState & ChipsetClockState.EndOfFrame)!=0)
				copjmp1 = 1;

			if (copjmp1 != 0 || copjmp2 != 0)
			{
				status = CopperStatus.Retrace;
				CopperInstruction();
				return;
			}

			if (memory.IsWaitingForDMA(DMASource.Copper))
				return;

			//copper instruction every odd clock (and copper DMA is on)
			//if ((clock.CopperHorizontalPos & 1) != 0)
			{ 
				CopperInstruction();
			}
			//if (status == CopperStatus.Waiting && data != 0xfffe)
			//	logger.LogTrace($"Hit VBL while still waiting for {data:X2}");

			if (debugger.dbug)
			{
				DebugCopperList(cop1lc);
				debugger.dbug = false;
			}
		}

		private void CopperNextFrame()
		{
			memory.ClearWaitingForDMA(DMASource.Copper);

			memory.NeedsDMA(DMASource.Copper, DMA.COPEN);

			//todo: there's some weirdness when both are set, the registers get ORd together, can't see how that could be useful
			if (copjmp1 != 0)
				copPC = cop1lc;
			if (copjmp2 != 0)
				copPC = cop2lc;
			DebugCOPPC(copPC);
			copjmp1 = copjmp2 = 0;

			if ((clock.ClockState & ChipsetClockState.EndOfFrame) != 0)
				activeCopperAddress = copPC;

			waitH = 0;
			waitV = 0;
			waitHMask = 0xff;
			waitVMask = 0xff;
			waitBlit = 0;

			if (copperDumping)
				CopperDump();
		}

		////HRM 3rd Ed, PP24
		//private uint beamHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		//private uint beamVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313
		private enum CopperStatus
		{
			Retrace,
			RunningWord1,
			RunningWord2,
			FetchWait,
			Waiting,
			WakingUp,
			Stopped
		}

		[Persist]
		private bool nextMOVEisNOOP = false;

		private bool IllegalCopperInstruction(uint reg)
		{
			//in OCS mode CDANG in COPCON means can access >= 0x40->0x7E as well as the usual >= 0x80
			//in ECS/AGA mode CDANG in COPCON means can access ALL chip regs, otherwise only >= 040
			if (settings.ChipSet == ChipSet.OCS)
			{
				if (((copcon & 2) != 0 && reg >= 0x40) || reg >= 0x80)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
			else
			{
				if ((copcon & 2) != 0 || reg >= 0x40)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		private void CopperInstruction()
		{
			if (status == CopperStatus.RunningWord1)
			{
				if (!memory.IsDMAEnabled(DMA.COPEN)) return;

				status = CopperStatus.RunningWord2;
				memory.ReadReg(DMASource.Copper, copPC, DMA.COPEN, Size.Word, ChipRegs.COPINS);
				copPC += 2;
			}
			else if (status == CopperStatus.RunningWord2)
			{
				if (!memory.IsDMAEnabled(DMA.COPEN)) return;

				ins = copins;

				if ((ins & 0x0001) == 0)
				{ 
					//MOVE
					uint reg = (uint)(ins & 0x1fe);

					if (IllegalCopperInstruction(reg))
					{ 
						status = CopperStatus.Stopped;
						DebugCOPStopped(reg);
					}
					else
					{
						status = CopperStatus.RunningWord1;
						//if this is being skipped, write to COPINS instead of the specified register
						uint regAddress = nextMOVEisNOOP ? ChipRegs.COPINS : ChipRegs.ChipBase + reg;
						nextMOVEisNOOP = false;
						memory.ReadReg(DMASource.Copper, copPC, DMA.COPEN, Size.Word, regAddress);
						//logger.LogTrace($"{regAddress:X6} {ChipRegs.Name(regAddress)}");
						copPC += 2;
					}
				}
				else
				{
					//todo: should we do the NOOP thing here too?
					status = CopperStatus.FetchWait;
					memory.ReadReg(DMASource.Copper, copPC, DMA.COPEN, Size.Word, ChipRegs.COPINS);
					copPC += 2;
				}

			}
			else if (status == CopperStatus.FetchWait)
			{
				nextMOVEisNOOP = false;

				data = copins;

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
					status = CopperStatus.RunningWord1;

					//SKIP
					uint coppos = (clock.VerticalPos & 0xff) << 8 | (clock.CopperHorizontalPos & 0xff);
					coppos &= waitMask;
					if (CopperCompare(coppos, (waitPos & waitMask)))
					{
						//logger.LogTrace($"RUN  {h},{currentLine} {coppos:X4} {waitPos:X4}");
						//todo: this isn't what happens, the next instruction is fetched, but ignored
						//https://eab.abime.net/showpost.php?p=206242&postcount=1

						//copPC += 4;
						nextMOVEisNOOP = true;
					}
				}
			}
			else if (status == CopperStatus.Waiting)
			{
				memory.NeedsDMA(DMASource.Copper, DMA.COPEN);

				//If blitter-busy bit is set the comparisons will fail.
				if (waitBlit == 0 && (memory.ReadDMACON() & (1 << 14)) != 0)
				{
					logger.LogTrace("WAIT delayed due to blitter running");
					return;
				}

				uint coppos = (clock.VerticalPos & 0xff) << 8 | (clock.CopperHorizontalPos & 0xff);
				coppos &= waitMask;
				if (CopperCompare(coppos, (waitPos & waitMask)))
				{
					//logger.LogTrace($"AWOKE @{clock}");

					//n ticks later
					waitTimer = 1;
					status = CopperStatus.WakingUp;

					//0 ticks delay
					//status = CopperStatus.RunningWord1;

					//one tick sooner
					//memory.Read(DMASource.Copper, copPC, DMA.COPEN, Size.Word, ChipRegs.COPINS);
					//copPC += 2;
					//status = CopperStatus.RunningWord2;

					if (ins == 0xffff && data == 0xfffe)
					{
						logger.LogTrace("Went off the end of the Copper List");
						status = CopperStatus.Stopped;
					}
				}
			}
			else if (status == CopperStatus.WakingUp)
			{
				//burn a cycle after waking up
				waitTimer--;
				if (waitTimer <= 0)
					status = CopperStatus.RunningWord1;
				memory.NeedsDMA(DMASource.Copper, DMA.COPEN);
			}
			else if (status == CopperStatus.Retrace)
			{
				CopperNextFrame();

				//status = CopperStatus.RunningWord1;

				//vAmigaTS\Agnus\Copper\Skip\copstrt1
				//vAmigaTS\Agnus\Copper\Skip\copstrt2
				status = CopperStatus.WakingUp;
				waitTimer = 7;
			}
			else if (status == CopperStatus.Stopped)
			{
				return;
			}
		}

		private bool CopperCompare(uint coppos, uint waitPos)
		{
			//if ((clock.CopperHorizontalPos&1)==0) return false;
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
			switch (address)
			{
				case ChipRegs.COPJMP1: copjmp1 = 1; DebugCOPJmp('R', 1, "COPJMP1", insaddr, cop1lc); break;
				case ChipRegs.COPJMP2: copjmp2 = 1; DebugCOPJmp('R', 2, "COPJMP2", insaddr, cop2lc); break;
			}
			return 0;
		}

		public uint DebugChipsetRead(uint address, Size size)
		{
			ushort value = 0;

			switch (address)
			{
				case ChipRegs.COPCON: value = (ushort)copcon; break;
				case ChipRegs.COP1LCH: value = (ushort)(cop1lc >> 16); break;
				case ChipRegs.COP1LCL: value = (ushort)cop1lc; break;
				case ChipRegs.COP2LCH: value = (ushort)(cop2lc >> 16); break;
				case ChipRegs.COP2LCL: value = (ushort)cop2lc; break;
				case ChipRegs.COPJMP1: value = (ushort)copjmp1; break;
				case ChipRegs.COPJMP2: value = (ushort)copjmp2; break;
				case ChipRegs.COPINS: value = copins; break;
			}

			return value;
		}

		private uint [] lastcopptr = new uint[3];
		private void DebugCOPPtr(int idx, string reg, uint insaddr, uint copptr)
		{
			if (!copperPtrDebug) return;

			if (copptr != lastcopptr[idx])
			{
				logger.LogTrace($"{reg} {insaddr:X8} {copptr:X6} {clock.TimeStamp()}");
				if (reg.EndsWith('L'))
					logger.LogTrace(DisassembleCopperList(copptr));
				lastcopptr[idx] = copptr;
			}
		}

		private uint[] lastjmp = new uint[3];
		private void DebugCOPJmp(char rw, int idx, string reg, uint insaddr, uint copptr)
		{
			if (!copperPtrDebug) return;

			if (copptr != lastjmp[idx])
			{
				logger.LogTrace($"{rw} {reg} {insaddr:X8} {copptr:X6} {clock.TimeStamp()}");
				lastjmp[idx] = copptr;
				logger.LogTrace(DisassembleCopperList(copptr));
			}
			//copPC = copptr;
			//status = CopperStatus.RunningWord1;
			//copjmp1 = copjmp2 = 0;
		}

		private uint [] lastcoppc =new uint[4];
		private void DebugCOPPC(uint pc)
		{
			if (!copperPtrDebug) return;

			int idx = (copjmp1<<1)+copjmp2;
			if (pc != lastcoppc[idx])
			{
				logger.LogTrace($"N {copjmp1}{copjmp2} {pc:X6} {cop1lc:X6} {cop2lc:X6} {clock.TimeStamp()}");
				logger.LogTrace(DisassembleCopperList(pc));
				lastcoppc[idx] = pc;
			}
		}

		private void DebugCOPStopped(uint reg)
		{
			logger.LogTrace($"Copper Stopped dff{reg:x4} {ChipRegs.Name(0xdff000+reg)}");
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.COPCON: copcon = value; break;
				case ChipRegs.COP1LCH: cop1lc = (cop1lc & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugCOPPtr(1, "COP1LCH", insaddr, cop1lc); break;
				case ChipRegs.COP1LCL: cop1lc = (cop1lc & 0xffff0000) | (uint)(value & 0xfffe); DebugCOPPtr(1, "COP1LCL", insaddr, cop1lc); break;
				case ChipRegs.COP2LCH: cop2lc = (cop2lc & 0x0000ffff) | ((uint)(value & 0x1f) << 16); DebugCOPPtr(2, "COP2LCH", insaddr, cop2lc); break;
				case ChipRegs.COP2LCL: cop2lc = (cop2lc & 0xffff0000) | (uint)(value & 0xfffe); DebugCOPPtr(2, "COP2LCL", insaddr, cop2lc); break;
				case ChipRegs.COPJMP1: copjmp1 = 1; DebugCOPJmp('W', 1, "COPJMP1", insaddr, cop1lc); break;
				case ChipRegs.COPJMP2: copjmp2 = 1; DebugCOPJmp('W', 2, "COPJMP2", insaddr, cop2lc); break;
				case ChipRegs.COPINS: copins = value; break;
			}
		}

		private const int MAX_COPPER_ENTRIES = 2048;

		private string DisassembleCopperList(uint copPC)
		{
			if (copPC == 0) return "";

			var csb = GetStringBuilder();
			csb.Clear();
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
						copPC = custom.DebugChipsetRead(ChipRegs.COP1LCH, Size.Long);//COP1LC
					else if (ChipRegs.ChipBase + reg == ChipRegs.COPJMP2)
						copPC = custom.DebugChipsetRead(ChipRegs.COP2LCH, Size.Long);//COP2LC
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

		private bool copperDumping;
		public void Dumping(bool enabled)
		{
			copperDumping = enabled;
		}

		private void CopperDump()
		{
			//var c = memory.ToBmp(1280);
			//File.WriteAllBytes($"../../../../blits/chip-{DateTime.Now:yyyy-MM-dd-HHmmss-fff}.bmp", c.ToArray());
		}

		public string GetDisassembly()
		{
			return DisassembleCopperList(activeCopperAddress);
		}

		public void Save(JArray obj)
		{
			var jo = PersistenceManager.ToJObject(this, "copper");
			obj.Add(jo);
		}

		public void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "copper")) return;

			PersistenceManager.FromJObject(this, obj);
		}
	}
}


