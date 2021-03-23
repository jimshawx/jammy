using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core;
using RunAmiga.Core.Custom;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Enums;
using RunAmiga.Core.Types.Types;
using RunAmiga.Core.Types.Types.Breakpoints;
using RunAmiga.Core.Types.Types.Debugger;
using RunAmiga.Disassembler.AmigaTypes;

namespace RunAmiga.Debugger
{
	public class Debugger : IDebugger
	{
		private readonly IBreakpointCollection breakpoints;
		private readonly IDebugMemoryMapper memory;
		private readonly ICPU cpu;
		private readonly IChips custom;
		private readonly ICIAAOdd ciaa;
		private readonly ICIABEven ciab;
		private readonly IDiskDrives diskDrives;
		private readonly IInterrupt interrupt;
		private readonly IDisassembly disassembly;
		private readonly ILogger logger;
		private readonly ITracer tracer;
		private readonly IAnalyser analyser;

		public Debugger(IMemoryMapper memoryMapper, IDebugMemoryMapper memory, ICPU cpu, IChips custom,
			IDiskDrives diskDrives, IInterrupt interrupt, ICIAAOdd ciaa, ICIABEven ciab, ILogger<Debugger> logger,
			IBreakpointCollection breakpoints,
			IOptions<EmulationSettings> settings, IDisassembly disassembly, ITracer tracer, IAnalyser analyser)
		{
			this.breakpoints = breakpoints;
			this.disassembly = disassembly;
			this.tracer = tracer;
			this.analyser = analyser;
			this.memory = memory;
			this.cpu = cpu;
			this.custom = custom;
			this.diskDrives = diskDrives;
			this.interrupt = interrupt;
			this.ciaa = ciaa;
			this.ciab = ciab;
			this.logger = logger;

			memoryMapper.AddMemoryIntercept(this);

			//dump the kickstart ROM details and disassemblies
			disassembly.ShowRomTags();

			libraryBaseAddresses["exec.library"] = memory.UnsafeRead32(4);
			//AddLVOIntercept("exec.library", "OpenLibrary", OpenLibraryLogger);
			//AddLVOIntercept("exec.library", "OpenResource", OpenResourceLogger);
			//AddLVOIntercept("exec.library", "MakeLibrary", MakeLibraryLogger);

			if (settings.Value.KickStart == "1.3")
			{
				//AddBreakpoint(0xFC509A);//expansion.library
				return;
			}

			if (settings.Value.KickStart != "1.2")
				return;

			//AddBreakpoint(0xfc0af0);//InitCode
			//AddBreakpoint(0xfc0afe);
			//AddBreakpoint(0xfc0af0);
			//AddBreakpoint(0xfc14ec);//MakeLibrary
			//AddBreakpoint(0xfc0900);
			//AddBreakpoint(0xfc096c);
			//AddBreakpoint(0xfc0bc8);//InitStruct
			//AddBreakpoint(0xfc1c34);//OpenResource
			//AddBreakpoint(0xfc1438);//OpenLibrary
			//AddBreakpoint(0xfe9180);
			//AddBreakpoint(0xfc30e4);//setup LastAlert
			//AddBreakpoint(0xfc19ea);//AddMemList
			//AddBreakpoint(0xfc165a);//FindName

			//AddBreakpoint(0xfc02b0);//initialize exec lists
			//AddBreakpoint(0xFC125C);//initialize exec interrupts

			//AddBreakpoint(0xfc01ee);//relocate ExecBase to $C00276
			//AddBreakpoint(0xfc0240);
			//AddBreakpoint(0xfc033e);
			//AddBreakpoint(0xfcac92);

			//AddBreakpoint(0xfc1798);

			//AddBreakpoint(0xfcac92);

			//AddBreakpoint(0x00fcac82);//copper list
			//AddBreakpoint(0x00fc0e60);//ExitIntr
			//AddBreakpoint(0x00fc0c4c);//Interrupt Bail Out
			//AddBreakpoint(0x00fc0ca6);//L2 Autovector IO/Timer
			//AddBreakpoint(0xFC465E);//Timer A
			//AddBreakpoint(0xFC4668);//Timer B
			//AddBreakpoint(0xFC4672);//TOD
			//AddBreakpoint(0xFC467C);//Serial
			//AddBreakpoint(0xFC4686);//Flag

			//AddBreakpoint(0x00fc0cdc);//L3 Autovector
			//AddBreakpoint(0xfc0e8a);//Schedule()+4

			//AddBreakpoint(0xfc0b28);//InitResident
			//AddBreakpoint(0xFC1C28);//AddResource
			//AddBreakpoint(0xFC0ca2);//sw interrupt

			//AddBreakpoint(0xfc13ec);

			//AddBreakpoint(0xfcabe4);//Init Graphics Library

			//AddBreakpoint(0xFE930E);//
			//AddBreakpoint(0xFC0F2A);//
			//AddBreakpoint(0xFC6d1a);
			//AddBreakpoint(0xFc666a);
			//AddBreakpoint(0xFC050C);
			//AddBreakpoint(0xFC559C);
			//AddBreakpoint(0xFC7C28);

			//AddBreakpoint(0xfc0546);//CPU detection
			//AddBreakpoint(0xfc04be);//start exec
			//AddBreakpoint(0xfc1208);
			//AddBreakpoint(0xfc0e86);//Schedule().
			//AddBreakpoint(0xfc0ee0);//Correct version of Switch() routine.

			AddBreakpoint(0xfc108A);//Incorrect version of Switch() routine. Shouldn't be here, this one handles 68881.
			AddBreakpoint(0xfc2fb4);//Task Crash Routine
			AddBreakpoint(0xfc2fd6);//Alert()
			AddBreakpoint(0xfc305e);//Irrecoverable Crash


			//diskDrives debugging
			//AddBreakpoint(0xFe89cc);//diskDrives changes
			////AddBreakpoint(0xFe89e4);//read boot block
			//AddBreakpoint(0xFe8a84);//after logo, wait for diskDrives change
			//AddBreakpoint(0xFe8a9c);//after logo, check for diskDrives inserted
			//AddBreakpoint(0xFe8a0a);//track read, is it a DOS diskDrives?

			//AddBreakpoint(0xFe800e);//dispatch trackdisk.device message
			//AddBreakpoint(0xFea734);//CMD_READ
			//AddBreakpoint(0xFea99e);//step to track and read
			//AddBreakpoint(0xFea5b2);//just after diskDrives DMA
			//AddBreakpoint(0xFea9ce);//after track-read message before fixing track gap
			//AddBreakpoint(0xFeab76);//blitter decode start
			//AddBreakpoint(0xFeb2a4);//blitter decode start

			//for (uint i = 0; i < 12; i++)
			//	AddBreakpoint(0xc004d2 + 4 * i, BreakpointType.Write);

			//AddBreakpoint(0xb328, BreakpointType.Write);
			//AddBreakpoint(0xb32a, BreakpointType.Write);
			//AddBreakpoint(0xfd18dc);

			//AddBreakpoint(0xFE571C);//Keyboard ISR

			//AddBreakpoint(0xfe5efa);//Mouse
			//AddBreakpoint(0xfe572a);//Keyboard
			//AddBreakpoint(0xfe544e);//Install Keyboard ISR
			//AddBreakpoint(0xfc6d00);//wrong copper address 0xc00276

			//AddBreakpoint(0xf85804);//KS2.04 battclock.resource init
			//AddBreakpoint(0xfe9232);

			//AddBreakpoint(0xfe9550);//TR_ADDREQUEST
			//AddBreakpoint(0xFE958A);//something
			//AddBreakpoint(0xFE9458);
			//AddBreakpoint(0xFE9440);
			//AddBreakpoint(0xFE91d6);
			//AddBreakpoint(0xFE9622);

			//AddBreakpoint(0xFE974C);//where do these jumps go?
			//AddBreakpoint(0xFE9778);

			//AddBreakpoint(0xfe9550);//TR_ADDREQUEST
			//C037C8

		}

		private void OpenLibraryLogger(LVO lvo)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"{lvo.Name}() libname {regs.A[1]:X8} {GetString(regs.A[1])} version: {regs.D[0]:X8}");
		}
		private void OpenResourceLogger(LVO lvo)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"{lvo.Name}() resName: {regs.A[1]:X8} {GetString(regs.A[1])}");
		}
		private void MakeLibraryLogger(LVO lvo)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"{lvo.Name}() vectors: {regs.A[0]:X8} structure: {regs.A[1]:X8} init: {regs.A[2]:X8} dataSize: {regs.D[0]:X8} segList: {regs.D[1]:X8}");
		}

		private class LVOInterceptor
		{
			public string Library { get; set; }
			public LVO LVO { get; set; }
			public Action<LVO> Action { get; set; }
		}

		private Dictionary<string, uint> libraryBaseAddresses = new Dictionary<string, uint>();

		private List<LVOInterceptor> LVOInterceptors { get; } = new List<LVOInterceptor>();

		private void AddLVOIntercept(string library, string vectorName, Action<LVO> action)
		{
			var lvos = analyser.GetLVOs();
			if (lvos.TryGetValue(library, out var lib))
			{
				var vector = lib.LVOs.SingleOrDefault(x => x.Name == vectorName);
				if (vector != null)
				{
					uint baseAddress = lib.BaseAddress;
					if (library == "exec.library")
						baseAddress = memory.UnsafeRead32(4);

					LVOInterceptors.Add(new LVOInterceptor{ 
						Library = library,
						LVO = vector,
						Action = action});
				}
			}
		}

		//occurs after Read
		public void Read(uint insaddr, uint address, uint value, Size size)
		{
			if (size == Size.Word)
			{
				var lvo = LVOInterceptors.SingleOrDefault(x => memory.UnsafeRead32((uint)(libraryBaseAddresses[x.Library] + x.LVO.Offset + 2)) == address);
				if (lvo != null)
				{
					//breakpoints.SignalBreakpoint(address);
					lvo.Action(lvo.LVO);
				}
			}

			breakpoints.Read(insaddr, address, value, size);
		}

		//occurs before Write
		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			//update execbase
			if (address == 4 && size == Size.Long)
				libraryBaseAddresses["exec.library"] = value;
			
			breakpoints.Write(insaddr, address, value, size);
		}

		private string GetString(uint str)
		{
			var sb = new StringBuilder();
			for (; ; )
			{
				byte c = memory.UnsafeRead8(str);
				if (c == 0)
					return sb.ToString();

				sb.Append(Convert.ToChar(c));
				str++;
			}
		}

		public void ToggleBreakpoint(uint pc)
		{
			breakpoints.ToggleBreakpoint(pc);
		}

		public void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Permanent, int counter = 0, Size size = Size.Long)
		{
			breakpoints.AddBreakpoint(address,type,counter,size);
		}

		public void BreakAtNextPC()
		{
			uint pc = cpu.GetRegs().PC;
			int line = disassembly.GetAddressLine(pc) + 1;
			AddBreakpoint(disassembly.GetLineAddress(line), BreakpointType.OneShot);
		}

		public MemoryDump GetMemory()
		{
			return new MemoryDump(memory.GetEnumerable(0));
		}

		public Regs GetRegs()
		{
			return cpu.GetRegs();
		}

		public void SetPC(uint pc)
		{
			cpu.SetPC(pc);
		}

		public uint FindMemoryText(string txt)
		{
			return memory.FindSequence(Encoding.ASCII.GetBytes(txt));
		}

		public void InsertDisk()
		{
			diskDrives.InsertDisk();
		}

		public void RemoveDisk()
		{
			diskDrives.RemoveDisk();
		}

		public void CIAInt(ICRB icr)
		{
			ciaa.DebugSetICR(icr);
			ciab.DebugSetICR(icr);
			interrupt.AssertInterrupt(Interrupt.PORTS);
		}

		public void IRQ(uint irq)
		{
			interrupt.AssertInterrupt(irq);
		}

		public void INTENA(uint irq)
		{
			custom.Write(0, ChipRegs.INTENA, 0x8000 + (uint)(1 << (int)irq), Size.Word);
		}

		public ChipState GetChipRegs()
		{
			var regs = new ChipState();
			regs.dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Core.Types.Types.Size.Word);
			regs.intreq = (ushort)custom.Read(0, ChipRegs.INTREQR, Core.Types.Types.Size.Word);
			regs.intena = (ushort)custom.Read(0, ChipRegs.INTENAR, Core.Types.Types.Size.Word);
			return regs;
		}

		public ushort GetInterruptLevel()
		{
			return interrupt.GetInterruptLevel();
		}

		public void WriteTrace()
		{
			tracer.WriteTrace();
		}
	}
}
