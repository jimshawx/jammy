using RunAmiga.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using RunAmiga.Custom;

namespace RunAmiga
{
	[StructLayout(LayoutKind.Sequential)]
	public class Musashi_regs
	{
		public uint d0, d1, d2, d3, d4, d5, d6, d7;
		public uint a0, a1, a2, a3, a4, a5, a6, a7;
		public uint pc, sp, usp, ssp;
		public ushort sr;
	}

	public class Machine
	{
		[DllImport("Musashi.dll")]
		static extern void Musashi_init(IntPtr r32, IntPtr r16, IntPtr r8, IntPtr w32, IntPtr w16, IntPtr w8);

		[DllImport("Musashi.dll")]
		static extern uint Musashi_execute(ref int cycles);

		[DllImport("Musashi.dll")]
		static extern void Musashi_get_regs(Musashi_regs regs);

		[DllImport("Musashi.dll")]
		static extern void Musashi_set_pc(uint pc);

		[DllImport("Musashi.dll")]
		static extern void Musashi_set_irq(uint levels);

		private CPU cpu;
		private Chips custom;
		private CIA cia;
		private Memory memory;
		private Debugger debugger;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;

		private List<IEmulate> emulations = new List<IEmulate>();

		private Musashi_Reader r32;
		private Musashi_Reader r16;
		private Musashi_Reader r8;
		private Musashi_Writer w32;
		private Musashi_Writer w16;
		private Musashi_Writer w8;
		private Memory musashiMemory;
		private Disassembler disassembler;
		private Musashi_regs musashiRegs;
		public Machine()
		{
			var labeller = new Labeller();
			debugger = new Debugger(labeller);
			cia = new CIA(debugger);
			memory = new Memory(debugger, "J");
			custom = new Custom.Chips(debugger, memory);
			cpu = new CPU(cia, custom, memory, debugger);

			emulations.Add(cia);
			emulations.Add(custom);
			emulations.Add(memory);
			emulations.Add(cpu);

			debugger.Initialise(memory, cpu, custom, cia);

			Reset();

			musashiMemory = new Memory(debugger, "M");
			musashiMemory.Reset();
			disassembler = new Disassembler();

			r32 = new Musashi_Reader(Musashi_read32);
			r16 = new Musashi_Reader(Musashi_read16);
			r8 = new Musashi_Reader(Musashi_read8);
			w32 = new Musashi_Writer(Musashi_write32);
			w16 = new Musashi_Writer(Musashi_write16);
			w8 = new Musashi_Writer(Musashi_write8);

			var memoryArray = musashiMemory.GetMemoryArray();
			Musashi_init(
			Marshal.GetFunctionPointerForDelegate(r32),
			Marshal.GetFunctionPointerForDelegate(r16),
			Marshal.GetFunctionPointerForDelegate(r8),
			Marshal.GetFunctionPointerForDelegate(w32),
			Marshal.GetFunctionPointerForDelegate(w16),
			Marshal.GetFunctionPointerForDelegate(w8)
				);
			int cycles=0;
			Musashi_execute(ref cycles);//run RESET
			musashiRegs = new Musashi_regs();

			emulationSemaphore = new SemaphoreSlim(1);
		}

		private delegate uint Musashi_Reader(uint address);
		private delegate void Musashi_Writer(uint address, uint value);

		private uint Musashi_read32(uint address)
		{
			if (address > 0x1000000) { Trace.WriteLine($"[MUSH] read oob @{address:X8}"); return 0; }
			uint value = musashiMemory.read32(address);
			return value;
		}
		private uint Musashi_read16(uint address)
		{
			if (address > 0x1000000) { Trace.WriteLine($"[MUSH] read oob @{address:X8}"); return 0; }

			if (address == ChipRegs.INTENAR) return custom.Read(0, ChipRegs.INTENAR, Size.Word);

			uint value = musashiMemory.read16(address);
			return value;
		}
		private uint Musashi_read8(uint address)
		{
			if (address > 0x1000000) { Trace.WriteLine($"[MUSH] read oob @{address:X8}"); return 0; }
			uint value = musashiMemory.read8(address);
			return value;
		}
		private void Musashi_write32(uint address, uint value)
		{
			if (address > 0x1000000) { Trace.WriteLine($"[MUSH] write oob @{address:X8}"); return; }
			//musashiMemory.write32(address, value);
			musashiMemory.Write(0, address, value, Size.Long);
		}
		private void Musashi_write16(uint address, uint value)
		{
			if (address > 0x1000000) { Trace.WriteLine($"[MUSH] write oob @{address:X8}"); return; }
			//musashiMemory.write16(address, (ushort)value);
			musashiMemory.Write(0, address, value, Size.Word);
		}

		private void Musashi_write8(uint address, uint value)
		{
			if (address > 0x1000000) { Trace.WriteLine($"[MUSH] write oob @{address:X8}"); return; }
			//musashiMemory.write8(address, (byte)value);
			musashiMemory.Write(0, address, value, Size.Byte);
		}

		private List<uint> mpc = new List<uint>();
		//private ushort last_sr;
		public void RunEmulations(ulong ns)
		{
			Musashi_get_regs(musashiRegs);

			var regs = cpu.GetRegs();
			uint instructionStartPC = regs.PC;

			//if (musashiRegs.pc == 0xfc0ca6 || musashiRegs.pc == 0xfc0caa)
			//	Trace.WriteLine($"Musashi L2 Interrupt 1 {musashiRegs.pc:X8} {musashiRegs.sr:X4}");
			//if (instructionStartPC == 0xfc0ca6)
			//	Trace.WriteLine($"C# L2 Interrupt {regs.SR:X4}");

			emulations.ForEach(x => x.Emulate(ns));

			var regsAfter = cpu.GetRegs();

			int counter = 0;
			const int maxPCdrift = 6;
			int cycles=0;
			uint pc;
			do
			{
				pc = Musashi_execute(ref cycles);
				//if (pc == 0xfc0ca6 || pc == 0xfc0caa)
				//	Trace.WriteLine($"Musashi L2 Interrupt 2 {pc:X8} {musashiRegs.sr:X4}");
				mpc.Add(pc);
				counter++;
			} while (pc != regsAfter.PC && counter < maxPCdrift);

			Musashi_get_regs(musashiRegs);

			//if ((regsAfter.SR & 0xff00) != last_sr)
			//	Trace.WriteLine($"SR {instructionStartPC:X8} {last_sr:X4}->{regsAfter.SR & 0xff00:X4} M:{musashiRegs.sr:X4}");
			//last_sr = (ushort)(regsAfter.SR & 0xff00);

			if (counter == maxPCdrift)
			{
				//debugger.DumpTrace();
				//mpc = mpc.Skip(mpc.Count - 32).ToList();
				//foreach (var v in mpc)
				//	Trace.WriteLine($"{v:X8}");
				Trace.WriteLine($"PC Drift too far at {regsAfter.PC:X8} {pc:X8}");
				//Machine.SetEmulationMode(EmulationMode.Stopped, true);
			}
			else if (counter != 1)
			{
				Trace.WriteLine($"Counter isn't 1 {counter}");
			}

			if (regsAfter.PC != pc)
			{
				//debugger.DumpTrace();
				Trace.WriteLine($"PC Drift at {regsAfter.PC:X8} {pc:X8}");
			}
			else
			{
				bool differs = false;
				if (regsAfter.D[0] != musashiRegs.d0) { Trace.WriteLine($"reg D0 differs {regsAfter.D[0]:X8} {musashiRegs.d0:X8}"); differs = true; }
				if (regsAfter.D[1] != musashiRegs.d1) { Trace.WriteLine($"reg D1 differs {regsAfter.D[1]:X8} {musashiRegs.d1:X8}"); differs = true; }
				if (regsAfter.D[2] != musashiRegs.d2) { Trace.WriteLine($"reg D2 differs {regsAfter.D[2]:X8} {musashiRegs.d2:X8}"); differs = true; }
				if (regsAfter.D[3] != musashiRegs.d3) { Trace.WriteLine($"reg D3 differs {regsAfter.D[3]:X8} {musashiRegs.d3:X8}"); differs = true; }
				if (regsAfter.D[4] != musashiRegs.d4) { Trace.WriteLine($"reg D4 differs {regsAfter.D[4]:X8} {musashiRegs.d4:X8}"); differs = true; }
				if (regsAfter.D[5] != musashiRegs.d5) { Trace.WriteLine($"reg D5 differs {regsAfter.D[5]:X8} {musashiRegs.d5:X8}"); differs = true; }
				if (regsAfter.D[6] != musashiRegs.d6) { Trace.WriteLine($"reg D6 differs {regsAfter.D[6]:X8} {musashiRegs.d6:X8}"); differs = true; }
				if (regsAfter.D[7] != musashiRegs.d7) { Trace.WriteLine($"reg D7 differs {regsAfter.D[7]:X8} {musashiRegs.d7:X8}"); differs = true; }

				if (regsAfter.A[0] != musashiRegs.a0) { Trace.WriteLine($"reg A0 differs {regsAfter.A[0]:X8} {musashiRegs.a0:X8}"); differs = true; }
				if (regsAfter.A[1] != musashiRegs.a1) { Trace.WriteLine($"reg A1 differs {regsAfter.A[1]:X8} {musashiRegs.a1:X8}"); differs = true; }
				if (regsAfter.A[2] != musashiRegs.a2) { Trace.WriteLine($"reg A2 differs {regsAfter.A[2]:X8} {musashiRegs.a2:X8}"); differs = true; }
				if (regsAfter.A[3] != musashiRegs.a3) { Trace.WriteLine($"reg A3 differs {regsAfter.A[3]:X8} {musashiRegs.a3:X8}"); differs = true; }
				if (regsAfter.A[4] != musashiRegs.a4) { Trace.WriteLine($"reg A4 differs {regsAfter.A[4]:X8} {musashiRegs.a4:X8}"); differs = true; }
				if (regsAfter.A[5] != musashiRegs.a5) { Trace.WriteLine($"reg A5 differs {regsAfter.A[5]:X8} {musashiRegs.a5:X8}"); differs = true; }
				if (regsAfter.A[6] != musashiRegs.a6) { Trace.WriteLine($"reg A6 differs {regsAfter.A[6]:X8} {musashiRegs.a6:X8}"); differs = true; }
				if (regsAfter.A[7] != musashiRegs.a7) { Trace.WriteLine($"reg A7 differs {regsAfter.A[7]:X8} {musashiRegs.a7:X8}"); differs = true; }

				if (regsAfter.SSP != musashiRegs.ssp) { Trace.WriteLine($"reg SSP differs {regsAfter.SSP:X8} {musashiRegs.ssp:X8}"); differs = true; }
				if (regsAfter.SP != musashiRegs.usp) { Trace.WriteLine($"reg SSP differs {regsAfter.SP:X8} {musashiRegs.usp:X8}"); differs = true; }

				if (regsAfter.SR != musashiRegs.sr)
				{
					Trace.WriteLine($"reg SR differs {regsAfter.SR:X4} {musashiRegs.sr:X4}");
					Trace.WriteLine($"  XNZVC\nJ {Convert.ToString(regsAfter.SR & 0x1f, 2).PadLeft(5,'0')}\nM {Convert.ToString(musashiRegs.sr & 0x1f, 2).PadLeft(5, '0')}");
					differs = true;
					regsAfter.SR &= 0b11111111_11101111;
				}

				if (differs) Trace.WriteLine($"cycles {cycles} @{instructionStartPC:X8} {disassembler.Disassemble(instructionStartPC, new ReadOnlySpan<byte>(memory.GetMemoryArray(),(int)instructionStartPC, 12))}");
			}

			//SWInterrupt(cycles);
			//VBInterrupt(cycles);
			//PortsInterrupt(cycles);

			if (MemChk(instructionStartPC))
			{
				debugger.DumpTrace();
				mpc = mpc.Skip(mpc.Count - 32).ToList();
				foreach (var v in mpc)
					Trace.WriteLine($"{v:X8}");
			}
		}

		private uint memChkCnt = 0;
		private bool MemChk(uint pc)
		{
			bool differ = false;

			memChkCnt++;
			if ((memChkCnt % 1000000) == 0)
			{
				var mem1 = memory.GetMemoryArray();
				var mem2 = musashiMemory.GetMemoryArray();
				for (int i = 0; i < 0xa00000; i++)
				{
					if (mem1[i] != mem2[i])
					{
						Trace.WriteLine($"[MEMX] {i:X8} {mem1[i]:X2} {mem2[i]:X2}");
						differ = true;
					}
				}

				for (int i = 0xc00000; i < 0xd80000; i++)
				{
					if (mem1[i] != mem2[i])
					{
						Trace.WriteLine($"[MEMX] {i:X8} {mem1[i]:X2} {mem2[i]:X2}");
						differ = true;
					}
				}
			}

			//if (memChkCnt != 0)
			//{
			//	var mem1 = memory.GetMemoryArray();
			//	var mem2 = musashiMemory.GetMemoryArray();
			//	for (int i = 0xc014cd; i <= 0xC014ff; i++)
			//	{
			//		if (mem1[i] != mem2[i])
			//		{
			//			Trace.WriteLine($"[MEMX] {i:X8} {mem1[i]:X2} {mem2[i]:X2}");
			//			differ = true;
			//		}
			//	}

			//}
			if (differ)
				Trace.WriteLine($"@{pc:X8}");

			return differ;
		}

		private uint clock=0;
		private void VBInterrupt(int cycles)
		{
			clock += (uint)cycles;

			//7Mhz = 7M/50 = 140,000 cycles in a frame.
			//the multitask timeslice is 4frames, 560000 cycles
			if (clock > 560000)
			{
				clock -= 560000;

				//vblank interrupt enabled
				//uint intenar;
				//intenar = custom.Read(0, ChipRegs.INTENAR, Size.Word);
				//if ((intenar & 32) == 0)
				//	custom.Write(0, ChipRegs.INTENA, 0x8000 + 32, Size.Word);
				uint intenar = custom.Read(0, ChipRegs.INTENAR, Size.Word);
				musashiMemory.Write(0, ChipRegs.INTENAR, intenar, Size.Word);

				//raise the interrupt
				custom.Write(0, ChipRegs.INTREQ, 0x8000 + 32, Size.Word);
				musashiMemory.Write(0, ChipRegs.INTREQ, 0x8000 + 32, Size.Word);

				uint intreqr = custom.Read(0, ChipRegs.INTREQR, Size.Word);
				musashiMemory.Write(0, ChipRegs.INTREQR, intreqr, Size.Word);

				//enable scheduler attention
				//uint execBase = memory.Read(0, 4, Size.Long);
				//uint sysflags = memory.Read(0, execBase + 0x124, Size.Byte);
				//sysflags |= 0x80;
				//memory.Write(0, execBase + 0x124, sysflags, Size.Byte);
				//musashiMemory.Write(0, execBase + 0x124, sysflags, Size.Byte);

				//trigger vblank interrupt level 3
				cpu.Interrupt(3);
				Musashi_set_irq(3);
			}
		}

		private void SWInterrupt(int cycles)
		{
			clock += (uint)cycles;

			//7Mhz = 7M/50 = 140,000 cycles in a frame.
			//the multitask timeslice is 4frames, 560000 cycles
			if (clock > 560000)
			{
				clock -= 560000;

				//sw interrupt enabled (it's mostly always enabled)
				//custom.Write(0,ChipRegs.INTENA, 0x8000+4, Size.Word);
				//uint intenar = custom.Read(0, ChipRegs.INTENAR, Size.Word);
				//musashiMemory.Write(0, ChipRegs.INTENAR, intenar, Size.Word);

				//raise the interrupt
				custom.Write(0, ChipRegs.INTREQ, 0x8000 + 4, Size.Word);
				uint intreqr = custom.Read(0, ChipRegs.INTREQR, Size.Word);
				musashiMemory.Write(0, ChipRegs.INTREQR, intreqr, Size.Word);

				//enable scheduler attention
				uint execBase = memory.Read(0, 4, Size.Long);
				uint sysflags = memory.Read(0, execBase + 0x124, Size.Byte);
				sysflags |= 0x80;
				memory.Write(0, execBase + 0x124, sysflags, Size.Byte);
				musashiMemory.Write(0, execBase + 0x124, sysflags, Size.Byte);

				//trigger sw interrupt level 1
				cpu.Interrupt(1);
				Musashi_set_irq(1);
			}
		}

		private void PortsInterrupt(int cycles)
		{
			clock += (uint)cycles;

			//7Mhz = 7M/50 = 140,000 cycles in a frame.
			//the multitask timeslice is 4frames, 560000 cycles
			if (clock > 560000)
			{
				clock -= 560000;

				uint intenar = custom.Read(0, ChipRegs.INTENAR, Size.Word);
				musashiMemory.Write(0, ChipRegs.INTENAR, intenar, Size.Word);

				//raise the interrupt
				custom.Write(0, ChipRegs.INTREQ, 0x8000 + 8, Size.Word);
				uint intreqr = custom.Read(0, ChipRegs.INTREQR, Size.Word);
				musashiMemory.Write(0, ChipRegs.INTREQR, intreqr, Size.Word);

				//enable scheduler attention
				//uint execBase = memory.Read(0, 4, Size.Long);
				//uint sysflags = memory.Read(0, execBase + 0x124, Size.Byte);
				//sysflags |= 0x80;
				//memory.Write(0, execBase + 0x124, sysflags, Size.Byte);
				//musashiMemory.Write(0, execBase + 0x124, sysflags, Size.Byte);

				//trigger ports interrupt level 2
				cpu.Interrupt(2);
				Musashi_set_irq(2);
			}
		}

		private Thread emuThread;

		public void Start()
		{
			emuThread = new Thread(Emulate);
			emuThread.Name = "Emulation";
			emuThread.Start();
		}

		public Debugger GetDebugger()
		{
			return debugger;
		}

		//private EmulationMode targetEmulationMode;
		public static void SetEmulationMode(EmulationMode mode, bool omitLock = false)
		{
			//if (mode == EmulationMode.Stopped)
			//	LockEmulation();
			//else
			//	UnlockEmulation();

			if (omitLock)
			{
				emulationMode = mode;
				return;
			}

			LockEmulation();
			emulationMode = mode;
			UnlockEmulation();
		}

		public static void WaitEmulationMode(EmulationMode mode)
		{
			for (; ; )
			{
				LockEmulation();
				if (emulationMode == mode)
				{
					UnlockEmulation();
					break;
				}
				UnlockEmulation();
				Thread.Sleep(100);
			}
		}

		public static void UnlockEmulation()
		{
			//Trace.WriteLine($"Unlock {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name} {emulationSemaphore.CurrentCount}");
			//Interlocked.Exchange(ref emulationLock, 0);

			//if (emulationSemaphore.Wait(0)) return;
			//if (emulationSemaphore.CurrentCount == 1) return;
			emulationSemaphore.Release();

			//emulationMutex.ReleaseMutex();
		}

		public static void LockEmulation()
		{
			//Trace.WriteLine($"Lock {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name} {emulationSemaphore.CurrentCount}");
			//for (; ; )
			//{
			//	if (Interlocked.Exchange(ref emulationLock, 1) == 0)
			//		return;

			//	Thread.Yield();
			//}

			emulationSemaphore.Wait();

			//emulationMutex.WaitOne();
		}

		public void Emulate()
		{
			cia.Reset();
			custom.Reset();
			cpu.Reset();

			while (emulationMode != EmulationMode.Exit)
			{
				LockEmulation();

				switch (emulationMode)
				{
					case EmulationMode.Running:
						//int counter = 1000;
						//long time = Stopwatch.GetTimestamp();
						//while (counter-- > 0 && emulationMode == EmulationMode.Running)
						//{
						//	long t = Stopwatch.GetTimestamp();
						//	ulong ns = (ulong) (((t - time) * 1000_000_000L) / Stopwatch.Frequency) ;
						//	time = t;
						//	RunEmulations(ns);
						//}
						RunEmulations(4*1000/7);//4cycles per instruction, 7MHz, in nanoseconds.
						break;
					case EmulationMode.Step:
						RunEmulations(4*1000/7);
						emulationMode = EmulationMode.Stopped;
						break;
					case EmulationMode.Exit: break;
					case EmulationMode.Stopped: break;
					default:
						throw new ApplicationException("unknown emulation mode");
				}

				UnlockEmulation();
			}
		}

		public void Reset()
		{
			emulations.ForEach(x => x.Reset());
		}
	}
}
