using RunAmiga.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using RunAmiga.Custom;

namespace RunAmiga
{
	public class Machine
	{
		private readonly IEmulate cpu;
		private readonly Chips custom;
		private readonly CIAAOdd ciaa;
		private readonly CIABEven ciab;
		private readonly Interrupt interrupt;
		private readonly Debugger debugger;
		private readonly DiskDrives diskDrives;
		private readonly Mouse mouse;
		private readonly Keyboard keyboard;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;

		private readonly List<IEmulate> emulations = new List<IEmulate>();


		public Machine()
		{
			var labeller = new Labeller();
			debugger = new Debugger(labeller);
			interrupt = new Interrupt();
			var memory = new Memory(debugger, "M");

			mouse = new Mouse();
			diskDrives = new DiskDrives(memory, interrupt);
			//cia = new CIA(debugger, diskDrives, mouse, interrupt);
			keyboard = new Keyboard(interrupt);

			ciaa = new CIAAOdd(debugger, diskDrives, mouse, keyboard, interrupt);
			ciab = new CIABEven(debugger, diskDrives, interrupt);

			keyboard.SetCIA(ciaa);

			custom = new Chips(debugger, memory, interrupt, diskDrives, mouse, keyboard);
			interrupt.Init(custom);

			var memoryMapper = new MemoryMapper(debugger, memory, ciaa, ciab, custom);

			//cpu = new CPU(debugger, interrupt, memoryMapper);
			cpu = new MusashiCPU(debugger, interrupt, memoryMapper);

			emulations.Add(ciaa);
			emulations.Add(ciab);
			emulations.Add(custom);
			emulations.Add(memory);
			emulations.Add(cpu);
			emulations.Add(interrupt);

			debugger.Initialise(memory, (ICPU)cpu, custom, diskDrives, interrupt, ciaa, ciab);

			Reset();

			emulationSemaphore = new SemaphoreSlim(1);
		}


		public void RunEmulations(ulong ns)
		{
			emulations.ForEach(x => x.Emulate(ns));
			//CheckInterrupts(10);
		}

		////private List<uint> mpc = new List<uint>();
		////private ushort last_sr;
		//public void RunEmulations2(ulong ns)
		//{
		//	Musashi_get_regs(musashiRegs);

		//	var regs = cpu.GetRegs();
		//	uint instructionStartPC = regs.PC;

		//	//if (musashiRegs.pc == 0xfc0ca6 || musashiRegs.pc == 0xfc0caa)
		//	//	Logger.WriteLine($"Musashi L2 Interrupt 1 {musashiRegs.pc:X8} {musashiRegs.sr:X4}");
		//	//if (instructionStartPC == 0xfc0ca6)
		//	//	Logger.WriteLine($"C# L2 Interrupt {regs.SR:X4}");

		//	emulations.ForEach(x => x.Emulate(ns));

		//	var regsAfter = cpu.GetRegs();

		//	int counter = 0;
		//	const int maxPCdrift = 6;
		//	int cycles=0;
		//	uint pc;
		//	do
		//	{
		//		//try
		//		//{
		//			pc = Musashi_execute(ref cycles);
		//		//}
		//		//catch
		//		//{
		//		//	debugger.DumpTrace();
		//		//	mpc = mpc.Skip(mpc.Count - 32).ToList();
		//		//	foreach (var v in mpc)
		//		//		Logger.WriteLine($"{v:X8}");
		//		//	pc = 0;
		//		//}

		//		//if (pc == 0xfc0ca6 || pc == 0xfc0caa)
		//		//	Logger.WriteLine($"Musashi L2 Interrupt 2 {pc:X8} {musashiRegs.sr:X4}");
		//		//mpc.Add(pc);
		//		counter++;
		//	} while (pc != regsAfter.PC && counter < maxPCdrift);

		//	Musashi_get_regs(musashiRegs);

		//	//if ((regsAfter.SR & 0xff00) != last_sr)
		//	//	Logger.WriteLine($"SR {instructionStartPC:X8} {last_sr:X4}->{regsAfter.SR & 0xff00:X4} M:{musashiRegs.sr:X4}");
		//	//last_sr = (ushort)(regsAfter.SR & 0xff00);

		//	if (counter == maxPCdrift)
		//	{
		//		debugger.DumpTrace();
		//		//mpc = mpc.Skip(mpc.Count - 32).ToList();
		//		//foreach (var v in mpc)
		//		//	Logger.WriteLine($"{v:X8}");
		//		Logger.WriteLine($"PC Drift too far at {regsAfter.PC:X8} {pc:X8}");
		//		//Machine.SetEmulationMode(EmulationMode.Stopped, true);
		//	}
		//	else if (counter != 1)
		//	{
		//		Logger.WriteLine($"Counter isn't 1 {counter}");
		//	}

		//	if (regsAfter.PC != pc)
		//	{
		//		Logger.WriteLine($"PC Drift at {regsAfter.PC:X8} {pc:X8}");
		//	}
		//	else
		//	{
		//		bool differs = false;
		//		if (regsAfter.D[0] != musashiRegs.d0) { Logger.WriteLine($"reg D0 differs {regsAfter.D[0]:X8} {musashiRegs.d0:X8}"); differs = true; }
		//		if (regsAfter.D[1] != musashiRegs.d1) { Logger.WriteLine($"reg D1 differs {regsAfter.D[1]:X8} {musashiRegs.d1:X8}"); differs = true; }
		//		if (regsAfter.D[2] != musashiRegs.d2) { Logger.WriteLine($"reg D2 differs {regsAfter.D[2]:X8} {musashiRegs.d2:X8}"); differs = true; }
		//		if (regsAfter.D[3] != musashiRegs.d3) { Logger.WriteLine($"reg D3 differs {regsAfter.D[3]:X8} {musashiRegs.d3:X8}"); differs = true; }
		//		if (regsAfter.D[4] != musashiRegs.d4) { Logger.WriteLine($"reg D4 differs {regsAfter.D[4]:X8} {musashiRegs.d4:X8}"); differs = true; }
		//		if (regsAfter.D[5] != musashiRegs.d5) { Logger.WriteLine($"reg D5 differs {regsAfter.D[5]:X8} {musashiRegs.d5:X8}"); differs = true; }
		//		if (regsAfter.D[6] != musashiRegs.d6) { Logger.WriteLine($"reg D6 differs {regsAfter.D[6]:X8} {musashiRegs.d6:X8}"); differs = true; }
		//		if (regsAfter.D[7] != musashiRegs.d7) { Logger.WriteLine($"reg D7 differs {regsAfter.D[7]:X8} {musashiRegs.d7:X8}"); differs = true; }

		//		if (regsAfter.A[0] != musashiRegs.a0) { Logger.WriteLine($"reg A0 differs {regsAfter.A[0]:X8} {musashiRegs.a0:X8}"); differs = true; }
		//		if (regsAfter.A[1] != musashiRegs.a1) { Logger.WriteLine($"reg A1 differs {regsAfter.A[1]:X8} {musashiRegs.a1:X8}"); differs = true; }
		//		if (regsAfter.A[2] != musashiRegs.a2) { Logger.WriteLine($"reg A2 differs {regsAfter.A[2]:X8} {musashiRegs.a2:X8}"); differs = true; }
		//		if (regsAfter.A[3] != musashiRegs.a3) { Logger.WriteLine($"reg A3 differs {regsAfter.A[3]:X8} {musashiRegs.a3:X8}"); differs = true; }
		//		if (regsAfter.A[4] != musashiRegs.a4) { Logger.WriteLine($"reg A4 differs {regsAfter.A[4]:X8} {musashiRegs.a4:X8}"); differs = true; }
		//		if (regsAfter.A[5] != musashiRegs.a5) { Logger.WriteLine($"reg A5 differs {regsAfter.A[5]:X8} {musashiRegs.a5:X8}"); differs = true; }
		//		if (regsAfter.A[6] != musashiRegs.a6) { Logger.WriteLine($"reg A6 differs {regsAfter.A[6]:X8} {musashiRegs.a6:X8}"); differs = true; }
		//		if (regsAfter.A[7] != musashiRegs.a7) { Logger.WriteLine($"reg A7 differs {regsAfter.A[7]:X8} {musashiRegs.a7:X8}"); differs = true; }

		//		if (regsAfter.SSP != musashiRegs.ssp) { Logger.WriteLine($"reg SSP differs {regsAfter.SSP:X8} {musashiRegs.ssp:X8}"); differs = true; }
		//		if (regsAfter.SP != musashiRegs.usp) { Logger.WriteLine($"reg SSP differs {regsAfter.SP:X8} {musashiRegs.usp:X8}"); differs = true; }

		//		if (regsAfter.SR != musashiRegs.sr)
		//		{
		//			Logger.WriteLine($"reg SR differs {regsAfter.SR:X4} {musashiRegs.sr:X4}");
		//			Logger.WriteLine($"  XNZVC\nJ {Convert.ToString(regsAfter.SR & 0x1f, 2).PadLeft(5,'0')}\nM {Convert.ToString(musashiRegs.sr & 0x1f, 2).PadLeft(5, '0')}");
		//			differs = true;
		//			//regsAfter.SR &= 0b11111111_11101111;
		//		}

		//		if (differs) 
		//			Logger.WriteLine($"cycles {cycles} @{instructionStartPC:X8} {disassembler.Disassemble(instructionStartPC, new ReadOnlySpan<byte>(memory.GetMemoryArray(),(int)instructionStartPC, 12))}");
		//	}

		//	CheckInterrupts(cycles);

		//	//if (MemChk(instructionStartPC))
		//	//{
		//	//	debugger.DumpTrace();
		//	//	//mpc = mpc.Skip(mpc.Count - 32).ToList();
		//	//	//foreach (var v in mpc)
		//	//	//	Logger.WriteLine($"{v:X8}");
		//	//}
		//}

		//private uint memChkCnt = 0;
		//private bool MemChk(uint pc)
		//{
		//	bool differ = false;

		//	memChkCnt++;
		//	if ((memChkCnt % 100000) == 0)
		//	{
		//		var mem1 = memory.GetMemoryArray();
		//		var mem2 = musashiMemory.GetMemoryArray();
		//		for (int i = 0; i < 0xa00000; i++)
		//		{
		//			if (mem1[i] != mem2[i])
		//			{
		//				Logger.WriteLine($"[MEMX] {i:X8} {mem1[i]:X2} {mem2[i]:X2}");
		//				differ = true;
		//			}
		//		}

		//		for (int i = 0xc00000; i < 0xd80000; i++)
		//		{
		//			if (mem1[i] != mem2[i])
		//			{
		//				Logger.WriteLine($"[MEMX] {i:X8} {mem1[i]:X2} {mem2[i]:X2}");
		//				differ = true;
		//			}
		//		}
		//	}

		//	if (differ)
		//		Logger.WriteLine($"@{pc:X8}");

		//	return differ;
		//}


		//private uint clock=0;
		//private uint intType = 0;
		//private void CheckInterrupts(int cycles)
		//{
		//	clock += (uint)cycles;

		//	//7Mhz = 7M/50 = 140,000 cycles in a frame.
		//	//the multitask timeslice is 4frames, 560000 cycles
		//	if (clock > 140_000)
		//	{
		//		clock -= 140_000;

		//		//hack to force enable scheduling
		//		//interrupt.EnableSchedulerAttention();

		//		//hack to force enable this interrupt level
		//		//interrupt.EnableInterrupt(Interrupt.VERTB);
		//		//interrupt.EnableInterrupt(Interrupt.BLIT);
		//		//interrupt.EnableInterrupt(Interrupt.COPPER);
		//		//interrupt.EnableInterrupt(Interrupt.PORTS);
		//		//interrupt.EnableInterrupt(Interrupt.SOFTINT);

		//		////trigger the interrupt
		//		//intType = 0;
		//		//if (intType == 2) interrupt.TriggerInterrupt(Interrupt.VERTB);
		//		//if (intType == 3) interrupt.TriggerInterrupt(Interrupt.BLIT);
		//		//if (intType == 0) interrupt.TriggerInterrupt(Interrupt.COPPER);
		//		//if (intType == 1) interrupt.TriggerInterrupt(Interrupt.PORTS);
		//		//if (intType == 1) interrupt.TriggerInterrupt(Interrupt.DSKBLK);
		//		//if (intType == 4) interrupt.TriggerInterrupt(Interrupt.SOFTINT);
		//		intType++; intType %= 5;
		//	}
		//}

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
			//Logger.WriteLine($"Unlock {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name} {emulationSemaphore.CurrentCount}");
			//Interlocked.Exchange(ref emulationLock, 0);

			//if (emulationSemaphore.Wait(0)) return;
			//if (emulationSemaphore.CurrentCount == 1) return;
			emulationSemaphore.Release();

			//emulationMutex.ReleaseMutex();
		}

		public static void LockEmulation()
		{
			//Logger.WriteLine($"Lock {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name} {emulationSemaphore.CurrentCount}");
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
			ciaa.Reset();
			ciab.Reset();
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
						RunEmulations(8);
						break;
					case EmulationMode.Step:
						RunEmulations(8);
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
