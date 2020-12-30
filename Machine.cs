using RunAmiga.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace RunAmiga
{
	public class Machine
	{
		private CPU cpu;
		private Custom custom;
		private CIA cia;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;

		public Machine()
		{
			cia = new CIA();
			custom = new Custom();
			cpu = new CPU(cia, custom);
			emulationSemaphore = new SemaphoreSlim(1);
		}

		public void RunEmulations()
		{
			cia.Emulate();
			custom.Emulate();
			cpu.Emulate();
		}

		Thread emuThread;

		public void Init()
		{
			//cpu.Disassemble(0xfc0000);
		}

		public void Start()
		{
			emuThread = new Thread(Emulate);
			emuThread.Name = "Emulation";
			emuThread.Start();
		}

		public CPU GetCPU()
		{
			return cpu;
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
			for(;;)
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

		private void Emulate(object o)
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
						RunEmulations();
						//UnlockEmulation();
						break;
					case EmulationMode.Step:
						RunEmulations();
						//LockEmulation();
						emulationMode = EmulationMode.Stopped;
						//UnlockEmulation();
						break;
					case EmulationMode.Exit: break;
					case EmulationMode.Stopped:
						//throw new ApplicationException("should not happen");
						//UnlockEmulation();
						break;
					default:
						throw new ApplicationException("unknown emulation mode");
				}

				UnlockEmulation();
				//if (emulationMode == EmulationMode.Stopped)
				//	Thread.Sleep(1000);
			}
		}
	}
}
