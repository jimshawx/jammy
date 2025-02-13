using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;

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
		private readonly IPersistenceManager persistenceManager;
		private readonly IKickstartROM kickstart;
		private readonly IEmulationWindow emulationWindow;
		private readonly ILogger logger;
		private readonly IDebugMemoryMapper memoryMapper;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;
		private static AutoResetEvent nonEmulationCompleteEvent;

		private readonly List<IEmulate> emulations = new List<IEmulate>();
		private readonly List<IReset> resetters = new List<IReset>();

		public Amiga(IInterrupt interrupt, IDebugMemoryMapper memoryMapper, IBattClock battClock, 
			ICIAAOdd ciaa, ICIABEven ciab, IChips custom, IMemoryMapper memory,
			ICPU cpu, IKeyboard keyboard, IBlitter blitter, ICopper copper, IAudio audio,
			IDiskDrives diskDrives, IMouse mouse, IDiskController diskController,
			ISerial serial, IMotherboard motherboard, IAgnus agnus, IDenise denise, IChipsetClock clock, IDMA dma,
			IPSUClock psuClock, ICPUClock cpuClock, IChipsetDebugger debugger,
			IBreakpointCollection breakpointCollection, IPersistenceManager statePersister,
			IKickstartROM kickstart, IEmulationWindow emulationWindow, ILogger<Amiga> logger)
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
			this.persistenceManager = statePersister;
			this.kickstart = kickstart;
			this.emulationWindow = emulationWindow;
			this.logger = logger;

			//fulfil the circular dependencies
			custom.Init(blitter, copper, audio, agnus, denise, dma);
			keyboard.SetCIA(ciaa);
			interrupt.Init(custom);

			if (cycleExact && cpu is IMoiraCPU)
			{
				((IMoiraCPU)cpu).SetSync(this.RunChipsetEmulationForCPU);
				dma.SetSync(this.RunChipsetEmulationForRAM);
			}
			else
			{
				dma.SetSync(this.RunChipsetEmulationForRAMImmediate);
			}

			cpu.Initialise();

			emulationWindow.SetKeyHandlers(AmigaKeydown, AmigaKeyup);

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

			emulations.Add(ciaa);
			emulations.Add(ciab);
			emulations.Add(psuClock);
			emulations.Add(cpuClock);
			emulations.Add(denise);

			if (!cycleExact)
				emulations.Insert(0, clock);

			resetters.Add(diskController);
			//resetters.Add(interrupt);
			resetters.Add(memory);
			resetters.Add(battClock);
			resetters.Add(motherboard);
			resetters.Add(custom);

			if (resetters.Any(x => x is IEmulate))
				throw new AmbiguousImplementationException();

			resetters.AddRange(emulations);
			resetters.Add(cpu);

			Reset();

			emulationMode = EmulationMode.Running;
			requestExitEmulationMode = false;

			emulationSemaphore = new SemaphoreSlim(0,1);
			nonEmulationCompleteEvent = new AutoResetEvent(false);
		}

		private bool takeASnapshot = false;
		private void AmigaKeyup(int key)
		{
			if ((VK)key == VK.VK_F11) takeASnapshot = true;
		}

		private void AmigaKeydown(int obj) { }

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
		private ulong kickROMReads = 0;

		private ulong totalWaits = 0;
		private uint totalCycles = 0;

		private ushort RunChipsetEmulationForRAM()
		{
			while (dma.LastDMASlotWasUsedByChipset())
			{
				clock.UpdateClock();
				clock.Emulate();
				emulations.ForEach(x => x.Emulate());
				dma.TriggerHighestPriorityDMA();
			} 
			dma.ExecuteCPUDMASlot();

			//if (clock.VerticalPos == 100)
			//	logger.LogTrace($"RAM {clock.HorizontalPos}");

			return dma.LastRead;
		}

		private ushort RunChipsetEmulationForRAMImmediate()
		{
			dma.ExecuteCPUDMASlot();
			return dma.LastRead;
		}

		private void RunChipsetEmulationForCPU(int count)
		{
			//if (clock.VerticalPos == 100)
			//{ 
			//	if (count == 1)
			//		logger.LogTrace($"S {clock.HorizontalPos}");
			//	else
			//		logger.LogTrace($"S {clock.HorizontalPos}-{clock.HorizontalPos + count - 1}");
			//}

			for (int i = 0; i < count; i++)
			{
				clock.UpdateClock();
				clock.Emulate();
				emulations.ForEach(x => x.Emulate());
				dma.TriggerHighestPriorityDMA();
			}
		}

		private bool cycleExact { get { return cpu is IMoiraCPU; } }

		public void RunEmulations()
		{
			if (cycleExact)
			{
				cpu.Emulate();
				return;
			}

			clock.UpdateClock();
			emulations.ForEach(x => x.Emulate());

			//totalWaits = 0;
			//totalCycles = 0;
			if (totalWaits == 0 && totalCycles == 0)
			{
				ulong nchipRAMReads;
				ulong nchipRAMWrites;
				ulong ntrapdoorReads;
				ulong ntrapdoorWrites;
				ulong nchipsetReads;
				ulong nchipsetWrites;
				ulong nkickROMReads;

				ulong dchipRAMReads;
				ulong dchipRAMWrites;
				ulong dtrapdoorReads;
				ulong dtrapdoorWrites;
				ulong dchipsetReads;
				ulong dchipsetWrites;
				ulong dkickROMReads;

				//in PAL, there are 312 x 227 ticks of the ChipsetClock per frame = 321 x 227 x 50 = 3,541,200 ticks per second
				//and every time round this RunEmulations loop is on of these ticks.
				//the CPU is running at twice that rate ~7.08MHz

				agnus.Bookmark();
				agnus.GetRGAReadWriteStats(out chipRAMReads, out chipRAMWrites, out trapdoorReads, out trapdoorWrites, out chipsetReads, out chipsetWrites, out kickROMReads);

				totalCycles = 0;
				//for (int i = 0; i < 2; i++)
				{
					cpu.Emulate();
					totalCycles += cpu.GetCycles();
				}

				//the last instruction took this many CPU cycles, we need to make sure we don't try to execute any more
				//CPU instructions until that many cycles have gone past.
				//since chipset cycles are two CPU cycles, then
				totalCycles /= 2;
				//we need to eat this many cycles before we try to execute any more CPU

				agnus.GetRGAReadWriteStats(out nchipRAMReads, out nchipRAMWrites, out ntrapdoorReads, out ntrapdoorWrites, out nchipsetReads, out nchipsetWrites, out nkickROMReads);

				dchipRAMReads = nchipRAMReads - chipRAMReads;
				dchipRAMWrites = nchipRAMWrites - chipRAMWrites;
				dtrapdoorReads = ntrapdoorReads - trapdoorReads;
				dtrapdoorWrites = ntrapdoorWrites - trapdoorWrites;
				dchipsetReads = nchipsetReads - chipsetReads;
				dchipsetWrites = nchipsetWrites - chipsetWrites;
				dkickROMReads = nkickROMReads - kickROMReads;

				//how many chip bus slots did that use?
				totalWaits = dchipRAMReads + dchipRAMWrites + dtrapdoorReads + dtrapdoorWrites + dchipsetReads + dchipsetWrites + dkickROMReads;
			}

			//The CPU totalCycles time includes the 'usual' instruction fetch time.
			//When running from Chip memory, that usual fetch coincides with an available DMA slot (x totalWaits)
			//(usually an even-numbered one, but this is not essential).
			//This gives the appearance of the CPU running at full speed. Only if there isn't a DMA slot
			//available will the CPU slow down.
			//NB. this code will only allocate even-numbered slots at the moment.
			//If we assume that ordinarily the DMA slots needed will fall within the CPU instruction's clocks
			//the idea is to allocate them while counting down the CPU clocks.
			//If there are still any remaining once the CPU clocks are counted down, then that will be the delay.
			//This isn't exactly how it works in reality, it's an approximation.

			//set waiting for a DMA slot
			if (totalWaits > 0)
				dma.SetCPUWaitingForDMA();

			//use up a CPU cycle
			if (totalCycles > 0)
				totalCycles--;

			//dma.DebugExecuteAllDMAActivity();

			//allocate the DMA slot
			dma.TriggerHighestPriorityDMA();

			if (totalWaits > 0 && !dma.IsWaitingForDMA(DMASource.CPU))
			{
				//CPU DMA Slot was allocated
				totalWaits--;
			}


			//agnus.FlushBitplanes();
		}

		private Thread emuThread;

		public void Start()
		{
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

		private bool loadSnapshot = false;

		public void Emulate()
		{
			//Thread.CurrentThread.Priority = ThreadPriority.Highest;

			if (loadSnapshot)
			{ 
				kickstart.SetMirror(false);
				//Desert Dream
				//persistenceManager.Load("../../state-2024-11-01-20-00-00.json");
				//persistenceManager.Load("../../state-2024-11-02-18-22-56.json");
				//Mental Hangover
				//persistenceManager.Load("../../state-2024-11-02-19-11-53.json");
				//Blitter Miracle
				persistenceManager.Load("../../state-2025-02-12-12-23-09.json");
				//Pinball Fantasies
				//persistenceManager.Load("../../state-2024-11-04-22-05-32.json");
			}

			uint stepOutSp = 0xffffffff;
			bool emulationHasRun = false;

			while (emulationMode != EmulationMode.Exit)
			{
				while (!requestExitEmulationMode)
				{
					if (takeASnapshot)
					{
						persistenceManager.Save($"../../state-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.json");
						takeASnapshot = false;
						logger.LogTrace("Snapshot Recorded!");
					}

					switch (emulationMode)
					{
						case EmulationMode.Running:
							RunEmulations();
							emulationHasRun = true;
							break;

						case EmulationMode.Step:
							do
							{
								RunEmulations();
							} while (totalCycles != 0 || totalWaits != 0);
							emulationHasRun = true;
							emulationMode = EmulationMode.Stopped;
							break;

						case EmulationMode.StepOut:
							var regs = cpu.GetRegs();
							if (stepOutSp == 0xffffffff) stepOutSp = regs.A[7];
							ushort ins = memoryMapper.UnsafeRead16(regs.PC);
							bool stopping = (ins == 0x4e75 || ins == 0x4e73) && regs.A[7] == stepOutSp; //rts or rte
							RunEmulations();
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
			}
		}
	}
}
