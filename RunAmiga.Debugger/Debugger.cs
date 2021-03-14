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

namespace RunAmiga.Debugger
{
	public class Debugger : IDebugger
	{
		private readonly IBreakpointCollection breakpoints;
		private readonly IMemory memory;
		private readonly ICPU cpu;
		private readonly IChips custom;
		private readonly ICIAAOdd ciaa;
		private readonly ICIABEven ciab;
		private readonly IDiskDrives diskDrives;
		private readonly IInterrupt interrupt;
		private readonly IDisassembly disassembly;
		private readonly ILogger logger;
		private readonly ITracer tracer;

		public Debugger(IMemoryMapper memoryMapper, IMemory memory, ICPU cpu, IChips custom,
			IDiskDrives diskDrives, IInterrupt interrupt, ICIAAOdd ciaa, ICIABEven ciab, ILogger<Debugger> logger,
			IBreakpointCollection breakpoints, IOptions<EmulationSettings> settings, IDisassembly disassembly, ITracer tracer)
		{
			this.breakpoints = breakpoints;
			this.disassembly = disassembly;
			this.tracer = tracer;
			this.memory = memory;
			this.cpu = cpu;
			this.custom = custom;
			this.diskDrives = diskDrives;
			this.interrupt = interrupt;
			this.ciaa = ciaa;
			this.ciab = ciab;
			this.logger = logger;

			memoryMapper.AddMemoryIntercept(this.breakpoints);

			//dump the kickstart ROM details and disassemblies
			disassembly.ShowRomTags();

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

			AddBreakpoint(0xfc0546);//CPU detection
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

		readonly MemoryRange memoryRange = new MemoryRange(0x0, 0x1000000);

		public bool IsMapped(uint address)
		{
			return true;
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		private bool isROM(uint address)
		{
			return address >= 0xf80000 && address <= 0xffffff;
		}

		private bool isOutOfRange(uint address)
		{
			return address >= 0x1000000;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (isOutOfRange(address))
			{
				tracer.DumpTrace();
				logger.LogTrace($"Trying to read a {size} from {address:X8} @{insaddr:X8}");
				Machine.SetEmulationMode(EmulationMode.Stopped, true);
			}

			//if (IsMemoryBreakpoint(address, BreakpointType.Read))
			//	Machine.SetEmulationMode(EmulationMode.Stopped, true);

			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			//if (address >= 0xc004d2 && address < 0xc004d2+48) 
			//{
			//	DumpTrace();
			//	logger.LogTrace($"Wrote to {address:X8}");
			//	Machine.SetEmulationMode(EmulationMode.Stopped, true);
			//}

			if (isROM(address) || isOutOfRange(address))
			{
				tracer.DumpTrace();
				logger.LogTrace($"Trying to write a {size} ({value:X8} {value}) to {address:X8} @{insaddr:X8}");
				Machine.SetEmulationMode(EmulationMode.Stopped, true);
			}

			//if (address == 0xb328 || address == 0xb32a) System.Diagnostics.Debugger.Break();

			//if (IsMemoryBreakpoint(address, BreakpointType.Write))
			//	Machine.SetEmulationMode(EmulationMode.Stopped, true);
		}

		//private string GetString(uint str)
		//{
		//	var sb = new StringBuilder();
		//	for (; ; )
		//	{
		//		byte c = memory.Read8(str);
		//		if (c == 0)
		//			return sb.ToString();

		//		sb.Append(Convert.ToChar(c));
		//		str++;
		//	}
		//	return null;
		//}

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
			return new MemoryDump(memory.GetMemoryArray());
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
	}
}
