using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;
using Jammy.Interface;
using Jammy.Types;
using Jammy.Types.Debugger;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Text;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Debugger
{
	public class Debugger : IDebugger
	{
		private readonly IBreakpointCollection breakpoints;
		private readonly IKickstartROM kickstart;
		private readonly ICopper copper;
		private readonly IChipsetClock clock;
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
		private readonly IAnalysis analysis;
		private readonly ILVOInterceptors interceptors;
		private readonly IReturnValueSnagger returnValueSnagger;
		private readonly ILibraryBases libraryBases;

		public Debugger(IDebugMemoryMapper memory, ICPU cpu, IChips custom,
			IDiskDrives diskDrives, IInterrupt interrupt, ICIAAOdd ciaa, ICIABEven ciab, ILogger<Debugger> logger,
			IBreakpointCollection breakpoints, IKickstartROM kickstart, ICopper copper, IChipsetClock clock,
			IOptions<EmulationSettings> settings, IDisassembly disassembly, ITracer tracer, IAnalyser analyser, IAnalysis analysis,
			ILVOInterceptors interceptors, IReturnValueSnagger returnValueSnagger, ILibraryBases libraryBases)
		{
			this.breakpoints = breakpoints;
			this.kickstart = kickstart;
			this.copper = copper;
			this.clock = clock;
			this.disassembly = disassembly;
			this.tracer = tracer;
			this.analyser = analyser;
			this.analysis = analysis;
			this.memory = memory;
			this.cpu = cpu;
			this.custom = custom;
			this.diskDrives = diskDrives;
			this.interrupt = interrupt;
			this.ciaa = ciaa;
			this.ciab = ciab;
			this.logger = logger;
			this.returnValueSnagger = returnValueSnagger;
			this.libraryBases = libraryBases;
			this.interceptors = interceptors;

			if (settings.Value.Debugger.IsEnabled())
				(memory as IMemoryMapper).AddMemoryIntercept(this);

			if (string.IsNullOrEmpty(settings.Value.KickStartDisassembly)) return;
			
			if (settings.Value.KickStartDisassembly.StartsWith("87BA7A3E"))//3.1 A1200
			{
				//AddBreakpoint(0xFBF3EA);//RAMLIB dev/lib not found, call LoadSeg
				//AddBreakpoint(0xF84514);//strap init
				//AddBreakpoint(0xF847BC);//OpenDevice("trackdisk.device") in strap
				//AddBreakpoint(0xFC03F0);//disk.resource drive detection
			}

			if (settings.Value.KickStartDisassembly.StartsWith("9FDEEEF6"))//3.1
			{
				//AddBreakpoint(0xFA710E);//allocate memory for hunk in loadseg
				return;
			}

			if (settings.Value.KickStartDisassembly.StartsWith("000B927C"))//2.04
			{
				//AddBreakpoint(0xf85804);//KS2.04 battclock.resource init
				return;
			}

			if (settings.Value.KickStartDisassembly.StartsWith("DB27680D"))//2.05
			{
				//AddBreakpoint(0xFC0BE2);//card.resource Gayle detection
				//AddBreakpoint(0xFC120C);//poll Gayle INTREQ
				return;
			}

			if (settings.Value.KickStartDisassembly.StartsWith("15267DB3"))//1.3
			{
				//AddBreakpoint(0xFC509A);//expansion.library
				//AddBreakpoint(0xFC7D84);//OpenFont()
				//AddBreakpoint(0xFC84D8, BreakpointType.Read);//ROM topaz.font read
				//AddBreakpoint(0xFC8D40, BreakpointType.Read);
				//AddBreakpoint(0xFC8D41, BreakpointType.Read);
				//AddBreakpoint(0xFC8D42, BreakpointType.Read);
				//AddBreakpoint(0xFC8D43, BreakpointType.Read);

				//AddBreakpoint(0xFC8500, BreakpointType.Read);
				//AddBreakpoint(0xFC8501, BreakpointType.Read);
				//AddBreakpoint(0xFC8502, BreakpointType.Read);
				//AddBreakpoint(0xFC8503, BreakpointType.Read);

				//AddBreakpoint(0xFC4966);//disk.resource drive detection
				//AddBreakpoint(0xeb4);
				//AddBreakpoint(0xacc8);
				return;
			}

			if (settings.Value.KickStartDisassembly.StartsWith("56F2E2A6"))//1.2
			{
				//AddBreakpoint(0xFC8498, BreakpointType.Read);//ROM topaz.font read

				//AddBreakpoint(0xFC061A);//trapdoor RAM detection

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
				//AddBreakpoint(0xfc0222);
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
				//AddBreakpoint(0xfc0e86);//Schedule()
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
				return;
			}
		}

		//occurs after Read
		public void Read(uint insaddr, uint address, uint value, Size size)
		{
			//interceptors.CheckLVOAccess(address, size);
			//returnValueSnagger.CheckSnaggers();

			//analyser.MarkAsType(address, MemType.Byte, size);

			breakpoints.Read(insaddr, address, value, size);
		}

		//occurs before Write
		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			//update execbase
			if (address == 4 && size == Size.Long)
				libraryBases.SetLibraryBaseaddress("exec.library", value);
			
			breakpoints.Write(insaddr, address, value, size);
		}

		public void Fetch(uint insaddr, uint address, uint value, Size size)
		{
			interceptors.CheckLVOAccess(address, size);
			//cpu PC might not be the actual instruction address at this point
			returnValueSnagger.CheckSnaggers(address, cpu.GetRegs().SP);

			//analyser.MarkAsType(address, MemType.Code, size);
			
			breakpoints.Read(insaddr, address, value, size);
		}

		public void ToggleBreakpoint(uint pc)
		{
			breakpoints.ToggleBreakpoint(pc);
		}

		public void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Execute, int counter = 0, Size size = Size.Long)
		{
			breakpoints.AddBreakpoint(address,type,counter,size);
		}

		public void RemoveBreakpoint(uint address)
		{
			breakpoints.RemoveBreakpoint(address);
		}

		public void BreakAtNextPC()
		{
			uint pc = cpu.GetRegs().PC;
			int line = disassembly.GetAddressLine(pc) + 1;
			AddBreakpoint(disassembly.GetLineAddress(line), BreakpointType.OneShot);
		}

		public string GetCopperDisassembly()
		{
			return copper.GetDisassembly();
		}

		public IMemoryDump GetMemory()
		{
			return new MemoryDump(memory.GetBulkRanges());
		}

		public uint Read32(uint address)
		{
			return memory.UnsafeRead32(address);
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

		public uint FindMemory(byte[] seq)
		{
			return memory.FindSequence(seq);
		}

		public void InsertDisk(int df)
		{
			diskDrives.InsertDisk(df);
		}

		public void RemoveDisk(int df)
		{
			diskDrives.RemoveDisk(df);
		}

		public void ChangeDisk(int df, string fileName)
		{
			diskDrives.ChangeDisk(df, fileName);
		}

		public void ReadyDisk()
		{
			diskDrives.ReadyDisk();
		}

		public void CIAInt(ICRB icr)
		{
			ciaa.DebugSetICR(icr);
			ciab.DebugSetICR(icr);
			interrupt.AssertInterrupt(Core.Types.Interrupt.PORTS);
		}

		public void IRQ(uint irq)
		{
			interrupt.AssertInterrupt(irq);
		}

		public void INTENA(uint irq)
		{
			custom.Write(0, ChipRegs.INTENA, 0x8000 + (uint)(1 << (int)irq), Size.Word);
		}

		public void INTDIS(uint irq)
		{
			custom.Write(0, ChipRegs.INTENA, (uint)(1 << (int)irq), Size.Word);
		}

		public void IDEACK()
		{
			logger.LogTrace("IDEACK is not implemented");
		}

		public void ClearBBUSY()
		{
			custom.Write(0, ChipRegs.DMACON, 1<<14, Size.Word);
		}

		public ChipState GetChipRegs()
		{
			var regs = new ChipState();
			regs.dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			regs.intreq = (ushort)custom.Read(0, ChipRegs.INTREQR, Size.Word);
			regs.intena = (ushort)custom.Read(0, ChipRegs.INTENAR, Size.Word);
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

		public uint KickstartSize()
		{
			return (uint)kickstart.MappedRange().First().Length;
		}

		public uint DebugRead(uint address, Size size)
		{
			return memory.UnsafeRead(address, size);
		}

		public void DebugWrite(uint address, uint value, Size size)
		{
			if (size == Size.Byte) memory.UnsafeWrite8(address, (byte)value);
			if (size == Size.Word) memory.UnsafeWrite16(address, (ushort)value);
			if (size == Size.Long) memory.UnsafeWrite32(address, value);
		}

		public void DumpBreakpoints()
		{
			breakpoints.DumpBreakpoints();
		}

		public ClockInfo GetChipClock()
		{
			var rv = new ClockInfo();
			rv.Tick = clock.Tick;
			rv.HorizontalPos = clock.HorizontalPos;
			rv.VerticalPos = clock.VerticalPos;
			rv.State = clock.ClockState;
			return rv;
		}
	}
}
