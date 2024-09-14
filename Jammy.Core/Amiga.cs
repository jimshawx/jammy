using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Jammy.Core.Custom;
using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core
{
	public class Amiga : IAmiga
	{
		private readonly ICPU cpu;
		private readonly IBlitter blitter;
		private readonly ICopper copper;
		private readonly IAgnus agnus;
		private readonly IDenise denise;
		private readonly IChipsetClock clock;
		private readonly IDMA dma;
		private readonly ICPUClock cpuClock;
		private readonly IBreakpointCollection breakpointCollection;

		private readonly IDebugMemoryMapper memoryMapper;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;
		private static AutoResetEvent nonEmulationCompleteEvent;

		private readonly List<IEmulate> emulations = new List<IEmulate>();
		private readonly List<IEmulate> threadedEmulations = new List<IEmulate>();
		private readonly List<IReset> resetters = new List<IReset>();

		public Amiga(IInterrupt interrupt, IDebugMemoryMapper memoryMapper, IBattClock battClock, 
			ICIAAOdd ciaa, ICIABEven ciab, IChips custom, IMemoryMapper memory,
			ICPU cpu, IKeyboard keyboard, IBlitter blitter, ICopper copper, IAudio audio,
			IDiskDrives diskDrives, IMouse mouse, IDiskController diskController,
			ISerial serial, IMotherboard motherboard, IAgnus agnus, IDenise denise, IChipsetClock clock, IDMA dma,
			IPSUClock psuClock, ICPUClock cpuClock, IChipsetDebugger debugger,
			IBreakpointCollection breakpointCollection, ILogger<Amiga> logger)
		{
			this.memoryMapper = memoryMapper;
			this.cpu = cpu;
			this.blitter = blitter;
			this.copper = copper;
			this.agnus = agnus;
			this.denise = denise;
			this.clock = clock;
			this.dma = dma;
			this.cpuClock = cpuClock;
			this.breakpointCollection = breakpointCollection;

			//fulfil the circular dependencies
			custom.Init(blitter, copper, audio, agnus, denise, dma);
			keyboard.SetCIA(ciaa);
			interrupt.Init(custom);

			//all the emulators and resetters
			emulations.Add(diskDrives);
			emulations.Add(mouse);
			emulations.Add(keyboard);
			emulations.Add(agnus);
			emulations.Add(copper);
			emulations.Add(blitter);
			emulations.Add(audio);
			//emulations.Add(ciaa);
			//emulations.Add(ciab);
			emulations.Add(serial);
			//emulations.Add(cpu);
			//emulations.Add(clock);
			//emulations.Add(psuClock);
			//emulations.Add(dma);
			//emulations.Add(denise);
			emulations.Add(debugger);
			emulations.Add(interrupt);

			//managed by the DMA controller
			//threadedEmulations.Add(copper);
			//threadedEmulations.Add(blitter);
			//threadedEmulations.Add(agnus);
			//threadedEmulations.Add(clock);
			threadedEmulations.Add(ciaa);
			threadedEmulations.Add(ciab);
			threadedEmulations.Add(psuClock);
			threadedEmulations.Add(cpuClock);
			threadedEmulations.Add(denise);

			emulations.Insert(0, clock);
			emulations.AddRange(threadedEmulations);

			resetters.Add(diskController);
			//resetters.Add(interrupt);
			resetters.Add(memory);
			resetters.Add(battClock);
			resetters.Add(motherboard);
			resetters.Add(custom);

			if (resetters.Any(x => x is IEmulate))
				throw new AmbiguousImplementationException();

			resetters.AddRange(emulations);
			resetters.AddRange(threadedEmulations);
			resetters.Add(cpu);
			resetters.Add(clock);

			Reset();

			emulationMode = EmulationMode.Running;
			requestExitEmulationMode = false;

			emulationSemaphore = new SemaphoreSlim(0,1);
			nonEmulationCompleteEvent = new AutoResetEvent(false);

			emulationThreads = new List<Thread>();
			//threadedEmulations.ForEach(
			//	x =>
			//	{
			//		var t = new Thread(() =>
			//		{
			//			clock.RegisterThread();
			//			Thread.CurrentThread.Priority = ThreadPriority.Highest;
			//			for (;;)
			//			{
			//				//clock.WaitForTick();
			//				x.Emulate(0);
			//			}
			//		});
			//		t.Name = x.GetType().Name;
			//		emulationThreads.Add(t);
			//	});
			Thread t;
			//cpu needs special treatment
			//t = new Thread(() =>
			//{
			//	Thread.CurrentThread.Priority = ThreadPriority.Highest;
			//	for (;;)
			//	{
			//		cpuClock.WaitForTick();
			//		cpu.Emulate();
			//	}
			//});
			//t.Name = "CPU";
			//emulationThreads.Add(t);
			//clock needs special treatment
			//t = new Thread(() =>
			//{
			//	Thread.CurrentThread.Priority = ThreadPriority.Highest;
			//	for (; ; )
			//	{
			//		clock.Emulate(0);
			//	}
			//});
			//t.Name = "Clock";
			//emulationThreads.Add(t);
		}

		public void Reset()
		{
			resetters.ForEach(x => x.Reset());
		}

		private ulong chipRAMReads = 0;
		private ulong chipRAMWrites = 0;
		private ulong trapdoorReads = 0;
		private ulong trapdoorWrites = 0;
		private ulong chipsetReads = 0; 
		private ulong chipsetWrites = 0;

		private ulong totalWaits = 0;
		private uint totalCycles = 0;

		public void RunEmulations(ulong ns)
		{
			emulations.ForEach(x => x.Emulate());

			if (totalWaits == 0 && totalCycles == 0)
			{
				ulong nchipRAMReads;
				ulong nchipRAMWrites;
				ulong ntrapdoorReads;
				ulong ntrapdoorWrites;
				ulong nchipsetReads;
				ulong nchipsetWrites;

				ulong dchipRAMReads;
				ulong dchipRAMWrites;
				ulong dtrapdoorReads;
				ulong dtrapdoorWrites;
				ulong dchipsetReads;
				ulong dchipsetWrites;

				//in PAL, there are 312 x 227 ticks of the ChipsetClock per frame = 321 x 227 x 50 = 3,541,200 ticks per second
				//and every time round this RunEmulations loop is on of these ticks.
				//the CPU is running at twice that rate ~7.08MHz

				totalCycles = 0;
				for (int i = 0; i < 2; i++)
				{ 
					cpu.Emulate();
					totalCycles += cpu.GetCycles();
				}
				//the last instruction took this many CPU cycles, we need to make sure we don't try to execute any more
				//CPU instructions until that many cycles have gone past.
				//since chipset cycles are two CPU cycles, then
				totalCycles /= 2;
				//we need to eat this many cycles before we try to execute any more CPU

				agnus.GetRGAReadWriteStats(out nchipRAMReads, out nchipRAMWrites, out ntrapdoorReads, out ntrapdoorWrites, out nchipsetReads, out nchipsetWrites);

				dchipRAMReads = nchipRAMReads - chipRAMReads; chipRAMReads = nchipRAMReads;
				dchipRAMWrites = nchipRAMWrites - chipRAMWrites; chipRAMWrites = nchipRAMWrites;
				dtrapdoorReads = ntrapdoorReads - trapdoorReads; trapdoorReads = ntrapdoorReads;
				dtrapdoorWrites = ntrapdoorWrites - trapdoorWrites; trapdoorWrites = ntrapdoorWrites;
				dchipsetReads = nchipsetReads - chipsetReads; chipsetReads = nchipsetReads;
				dchipsetWrites = nchipsetWrites - chipsetWrites; chipsetWrites = nchipsetWrites;

				//how many chip bus slots did that use?
				totalWaits = dchipRAMReads + dchipRAMWrites + dtrapdoorReads + dtrapdoorWrites + dchipsetReads + dtrapdoorWrites;
			}
			else if (totalWaits > 0)
			{
				//set waiting for a DMA slot
				dma.SetCPUWaitingForDMA();
			}

			//use up a CPU cycle
			if (totalCycles > 0) totalCycles--;

			//this will allocate the DMA slot
			clock.AllThreadsFinished();

			if (totalWaits > 0 && !dma.IsWaitingForDMA(DMASource.CPU))
			{
				//CPU DMA Slot was allocated
				totalWaits--;
			}

			agnus.FlushBitplanes();
		}

		private Thread emuThread;

		public void Start()
		{
			foreach (var t in emulationThreads)
				t.Start();

			Thread.Sleep(1000);

			emuThread = new Thread(Emulate);
			emuThread.Name = "Amiga";
			emuThread.Start();
		}

		public static void SetEmulationMode(EmulationMode mode, bool changeWhileLocked = false)
		{
			if (changeWhileLocked)
			{
				desiredEmulationMode = mode;
			}
			else
			{
				LockEmulation();
				desiredEmulationMode = mode;
				UnlockEmulation();
			}
		}

		private static volatile bool requestExitEmulationMode;
		private static volatile EmulationMode desiredEmulationMode;
		private readonly List<Thread> emulationThreads;

		public static void UnlockEmulation()
		{
			nonEmulationCompleteEvent.Set();
			emulationSemaphore.Release();
			requestExitEmulationMode = false;
		}

		public static void LockEmulation()
		{
			requestExitEmulationMode = true;
			emulationSemaphore.Wait();
		}

		public void Emulate()
		{
			Thread.CurrentThread.Priority = ThreadPriority.Highest;

			uint stepOutSp = 0xffffffff;
			bool emulationHasRun = false;

			while (emulationMode != EmulationMode.Exit)
			{
				while (!requestExitEmulationMode)
				{
					switch (emulationMode)
					{
						case EmulationMode.Running:
							RunEmulations(8);
							emulationHasRun = true;
							break;

						case EmulationMode.Step:
							do
							{
								RunEmulations(8);
							} while (totalCycles != 0 || totalWaits != 0);
							emulationHasRun = true;
							emulationMode = EmulationMode.Stopped;
							break;

						case EmulationMode.StepOut:
							var regs = cpu.GetRegs();
							if (stepOutSp == 0xffffffff) stepOutSp = regs.A[7];
							ushort ins = memoryMapper.UnsafeRead16(regs.PC);
							bool stopping = (ins == 0x4e75 || ins == 0x4e73) && regs.A[7] == stepOutSp; //rts or rte
							RunEmulations(8);
							emulationHasRun = true;
							if (stopping)
								emulationMode = EmulationMode.Stopped;
							break;

						default:
							requestExitEmulationMode = true;
							break;
					}

					if (breakpointCollection.BreakpointHit())
						emulationMode = EmulationMode.Stopped;
				}

				if (emulationMode == EmulationMode.Stopped && emulationHasRun)
				{
					UI.UI.IsDirty = true;
					emulationHasRun = false;
				}

				if (emulationMode == EmulationMode.Exit)
					break;

				desiredEmulationMode = emulationMode;

				emulationSemaphore.Release();

				nonEmulationCompleteEvent.WaitOne();

				emulationSemaphore.Wait();

				emulationMode = desiredEmulationMode;

				if (emulationMode != EmulationMode.Stopped)
					clock.Resume();
				else
					clock.Suspend();
			}
		}
	}
}
