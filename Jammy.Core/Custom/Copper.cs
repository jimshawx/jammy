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
		public void Init(IDMA dma)
		{
			memory = dma;
		}

		public void Reset()
		{
			status = CopperStatus.Stopped;
		}

		public void Emulate()
		{
			if (clock.EndOfFrame())
			{
				status = CopperStatus.Retrace;
				CopperInstruction();
				return;
			}

			if (memory.IsWaitingForDMA(DMASource.Copper))
				return;

			//copper instruction every odd clock (and copper DMA is on)
			if ((clock.HorizontalPos&1)!=0)
				CopperInstruction();

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
			Retrace,
			RunningWord1,
			RunningWord1DMA,
			RunningWord2,
			RunningWord2DMA,
			Waiting,
			WakingUp,
			Stopped
		}
		
		private void CopperInstruction()
		{
			if (status == CopperStatus.Retrace)
			{
				CopperNextFrame();
			}
			else if (status == CopperStatus.Stopped)
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
				if (!memory.IsDMAEnabled(DMA.COPEN)) return;

				status = CopperStatus.RunningWord1DMA;
				memory.Read(DMASource.Copper, copPC, DMA.COPEN, Size.Word, ChipRegs.COPINS);
			}
			else if (status == CopperStatus.RunningWord1DMA)
			{
				ins = copins;
				copPC += 2;
				status = CopperStatus.RunningWord2;
			}
			else if (status == CopperStatus.RunningWord2)
			{
				if (!memory.IsDMAEnabled(DMA.COPEN)) return;

				status = CopperStatus.RunningWord2DMA;
				memory.Read(DMASource.Copper, copPC, DMA.COPEN, Size.Word, ChipRegs.COPINS);
			}
			else if (status == CopperStatus.RunningWord2DMA)
			{
				data = copins;
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
						uint coppos = (clock.VerticalPos & 0xff) << 8 | (clock.HorizontalPos & 0xff);
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
				//If blitter-busy bit is set the comparisons will fail.
				if (waitBlit == 0 && (custom.Read(0, ChipRegs.DMACONR, Size.Word) & (1 << 14)) != 0)
				{
					logger.LogTrace("WAIT delayed due to blitter running");
					return;
				}

				uint coppos = (clock.VerticalPos & 0xff) << 8 | (clock.HorizontalPos & 0xff);
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

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.COPCON: copcon = value; break;
				case ChipRegs.COP1LCH: cop1lc = (cop1lc & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
				case ChipRegs.COP1LCL: cop1lc = (cop1lc & 0xffff0000) | (uint)(value & 0xfffe); break;
				case ChipRegs.COP2LCH: cop2lc = (cop2lc & 0x0000ffff) | ((uint)(value & 0x1f) << 16); break;
				case ChipRegs.COP2LCL: cop2lc = (cop2lc & 0xffff0000) | (uint)(value & 0xfffe); break;
				case ChipRegs.COPJMP1: copjmp1 = value; copPC = cop1lc; status = CopperStatus.RunningWord1; memory.ClearWaitingForDMA(DMASource.Copper);
					break;
				case ChipRegs.COPJMP2: copjmp2 = value; copPC = cop2lc; status = CopperStatus.RunningWord1; memory.ClearWaitingForDMA(DMASource.Copper);
					break;
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
	}
}

