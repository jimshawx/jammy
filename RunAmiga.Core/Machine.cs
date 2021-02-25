using System;
using System.Collections.Generic;
using System.Threading;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core
{
	public class Machine : IMachine
	{
		private readonly ICPU cpu;
		private readonly IChips custom;
		private readonly ICIAAOdd ciaa;
		private readonly ICIABEven ciab;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;

		private readonly List<IEmulate> emulations = new List<IEmulate>();

		public Machine(IInterrupt interrupt, IMemory memory, IBattClock battClock, 
			ICIAAOdd ciaa, ICIABEven ciab, IChips custom, 
			ICPU cpu, IKeyboard keyboard, IBlitter blitter, ICopper copper, IAudio audio)
		{
			this.ciaa = ciaa;
			this.ciab = ciab;
			this.custom = custom;
			this.cpu = cpu;

			custom.Init(blitter, copper, audio);

			keyboard.SetCIA(ciaa);

			interrupt.Init(custom);

			emulations.Add(battClock);
			emulations.Add(ciaa);
			emulations.Add(ciab);
			emulations.Add(custom);
			emulations.Add(memory);
			emulations.Add(cpu);
			emulations.Add(interrupt);

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
