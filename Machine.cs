using RunAmiga.Types;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RunAmiga
{
	public class Machine : IMachine
	{
		private readonly ICPU cpu;
		private readonly IChips custom;
		private readonly ICIAAOdd ciaa;
		private readonly ICIABEven ciab;
		private readonly IDebugger debugger;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;

		private readonly List<IEmulate> emulations = new List<IEmulate>();

		public Machine(IInterrupt interrupt, IMemory memory, IBattClock battClock, 
			IDiskDrives diskDrives,
			ICIAAOdd ciaa, ICIABEven ciab, IChips custom, IDebugger debugger,
			ICPU cpu, IKeyboard keyboard, IBlitter blitter, ICopper copper, IAudio audio)
		{
			this.ciaa = ciaa;
			this.ciab = ciab;
			this.custom = custom;
			this.debugger = debugger;
			this.cpu = cpu;

			var kickstart = new Kickstart("../../../../kick12.rom", "Kickstart 1.2");
			//var kickstart = new Kickstart("../../../../kick13.rom", "Kickstart 1.3");
			//var kickstart = new Kickstart("../../../../kick204.rom", "Kickstart 2.04");
			//var kickstart = new Kickstart("../../../../kick31.rom", "Kickstart 3.1");

			custom.Init(blitter, copper, audio);

			memory.SetKickstart(kickstart);

			var disassembly = new Disassembly(memory.GetMemoryArray(), debugger.GetBreakpoints());
			var labeller = new Labeller();
			var tracer = new Tracer(disassembly, labeller);

			keyboard.SetCIA(ciaa);

			interrupt.Init(custom);

			emulations.Add(battClock);
			emulations.Add(ciaa);
			emulations.Add(ciab);
			emulations.Add(custom);
			emulations.Add(memory);
			emulations.Add(cpu);
			emulations.Add(interrupt);

			debugger.Initialise(memory, cpu, custom, diskDrives, interrupt, ciaa, ciab, tracer);

			Reset();

			emulationSemaphore = new SemaphoreSlim(1);
		}


		public void RunEmulations(ulong ns)
		{
			emulations.ForEach(x => x.Emulate(ns));
		}

		private Thread emuThread;

		public void Start()
		{
			emuThread = new Thread(Emulate);
			emuThread.Name = "Emulation";
			emuThread.Start();
		}

		public IDebugger GetDebugger()
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
			//logger.LogTrace($"Unlock {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name} {emulationSemaphore.CurrentCount}");
			//Interlocked.Exchange(ref emulationLock, 0);

			//if (emulationSemaphore.Wait(0)) return;
			//if (emulationSemaphore.CurrentCount == 1) return;
			emulationSemaphore.Release();

			//emulationMutex.ReleaseMutex();
		}

		public static void LockEmulation()
		{
			//logger.LogTrace($"Lock {Thread.CurrentThread.ManagedThreadId} {Thread.CurrentThread.Name} {emulationSemaphore.CurrentCount}");
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
