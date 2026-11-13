using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Threading;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core
{
	public class Amiga : IAmiga, IDebugKeys
	{
		private readonly ICPU cpu;
		private readonly IBlitter blitter;
		private readonly ICopper copper;
		private readonly IAgnus agnus;
		private readonly IDenise denise;
		private readonly IChipsetClock clock;
		private readonly IDMA dma;
		private readonly ICPUClock cpuClock;
		private readonly IChipsetDebugger debugger;
		private readonly IBreakpointCollection breakpointCollection;
		private readonly IPersistenceManager persistenceManager;
		private readonly IKickstartROM kickstart;
		private readonly ICIAMemory ciaMemory;
		private readonly ILogger logger;
		private readonly IDebugMemoryMapper memoryMapper;
		private readonly EmulationSettings settings;

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
			IAkiko akiko,
			IPSUClock psuClock, ICPUClock cpuClock, IChipsetDebugger debugger,
			IBreakpointCollection breakpointCollection, IPersistenceManager statePersister,
			IKickstartROM kickstart, ICIAMemory ciaMemory, IOptions<EmulationSettings> settings, ILogger<Amiga> logger)
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
			this.debugger = debugger;
			this.breakpointCollection = breakpointCollection;
			this.persistenceManager = statePersister;
			this.kickstart = kickstart;
			this.ciaMemory = ciaMemory;
			this.logger = logger;
			this.settings = settings.Value;

			//fulfil the circular dependencies
			custom.Init(blitter, copper, audio, agnus, denise, dma);
			keyboard.SetCIA(ciaa);
			interrupt.Init(custom, cpu);

			if (cycleExact)
			{
				//A500
				if (settings.Value.Sku == CPUSku.MC68000 && (settings.Value.ChipSet == ChipSet.OCS || settings.Value.ChipSet == ChipSet.ECS))
					SetCycleExact(2.0);
				//A1200
				else if (settings.Value.Sku == CPUSku.MC68EC020 && settings.Value.ChipSet == ChipSet.AGA)
					SetCycleExact(4.0);

				((IMoiraCPU)cpu).SetSync(this.RunChipsetEmulationForCPU);
				dma.SetSync(this.RunChipsetEmulationForRAM);
			}
			else
			{
				SetCycleExact(10.0);
				(cpu as IMoiraCPU)?.SetSync((_) => { });
				dma.SetSync(this.RunChipsetEmulationForRAMImmediate);
			}

			cpu.Initialise();

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
			emulations.Add(akiko);

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

		private bool logit = false;
		public void DebugKeyUp(int key)
		{
			if ((VK)key == VK.VK_F11) if (emulationMode == EmulationMode.Running) SetEmulationMode(EmulationMode.Snapshot);
			if ((VK)key == VK.VK_F10) logit = true;
		}

		public void DebugKeyDown(int obj) { }

		public void Reset()
		{
			resetters.ForEach(x => x.Reset());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RunAllEmulations()
		{
			for (int i = 0; i < emulations.Count; i++)
				emulations[i].Emulate();
		}

		//this is run by Agnus immediately after a Contended read/write (Chip RAM, Trapdoor RAM, Chip Reg)
		//when the emulation is not in cycle-exact mode - the read/write is executed immediately
		private ushort RunChipsetEmulationForRAMImmediate()
		{
			ushort rv = RunChipsetEmulationForRAM();
			RunChipsetEmulationForCPU(1);
			return rv;
		}

		private Regs logitRegs = new Regs();

		//this is run by Agnus immediately after a Contended read/write (Chip RAM, Trapdoor RAM, Chip Reg)
		//when the emulation is in cycle-exact mode - the read occurs immediately, but we flag the fact that 
		//the CPU used a chip RAM slot
		private ushort RunChipsetEmulationForRAM()
		{
			if (logit && clock.VerticalPos == debugger.dbugLine)
			{
				cpu.GetRegs(logitRegs);
				logger.LogTrace($"CPU  {clock} {logitRegs.PC:X8}");
			}

			//do the Reads now, do the Write at the right time
			if (!dma.IsCPUDMARequestAWrite()) 
			{ 
				dma.ExecuteCPUDMASlot();
				dma.SetCPUWaitingForDMA();
			}

			if (cpu is IMoiraCPU)
			((IMoiraCPU)cpu).CheckInterrupt();

			return dma.LastRead;
		}

		private void SetCycleExact(double ticksPerColorClock)
		{
			cckPeriod = ticksPerColorClock;
			ePeriod = 5.0 * ticksPerColorClock;

			cckCountdown = cckPeriod;
			eCountdown = ePeriod;
		}

		private double cckPeriod;
		private double ePeriod;

		private double cckCountdown;
		private double eCountdown;

		private void RunChipsetEmulationForCPU(int count)
		{
			bool isWaitingForDMA = dma.IsWaitingForDMA(DMASource.CPU);
			bool isWaitingForCIA = ciaMemory.IsPending();

			while (count > 0 || isWaitingForDMA || isWaitingForCIA)
			{
				eCountdown -= 1.0;
				if (eCountdown <= 0.0)
				{
					eCountdown += ePeriod;

					if (isWaitingForCIA)
					{
						ciaMemory.ExecutePending();
						isWaitingForCIA = false;
					}
				}

				cckCountdown -= 1.0;
				if (cckCountdown <= 0.0)
				{
					cckCountdown += cckPeriod;

					dma.TriggerHighestPriorityDMA();
					if (!dma.LastDMASlotWasUsedByChipset() && isWaitingForDMA)
					{
						dma.ExecuteCPUDMASlot();
						isWaitingForDMA = false;
					}
					clock.UpdateClock();

					clock.Emulate();
					RunAllEmulations();
				}
				count--;
			}
		}

		private bool cycleExact { get {
									if (settings.CPU != CPUType.Moira) return false;
									if (settings.Sku == CPUSku.MC68000 && (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS)) return true;
									if (settings.Sku == CPUSku.MC68EC020 && settings.ChipSet == ChipSet.AGA) return true;
									return false; } }

		public void RunEmulations()
		{
			cpu.Emulate();
			if (!cycleExact)
				RunChipsetEmulationForCPU(1);
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

		public void Emulate()
		{
			Thread.CurrentThread.Priority = ThreadPriority.Highest;

			if (!string.IsNullOrEmpty(settings.Snapshot))
			{ 
				kickstart.SetMirror(false);
				persistenceManager.Load(Path.Combine("snapshots", settings.Snapshot));
			}

			uint stepOutSp = 0xffffffff;
			bool emulationHasRun = false;

			while (emulationMode != EmulationMode.Exit)
			{
				while (!requestExitEmulationMode)
				{
					switch (emulationMode)
					{
						case EmulationMode.Running:
							RunEmulations();
							emulationHasRun = true;
							break;

						case EmulationMode.Step:
							RunEmulations();
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
							{ 
								emulationMode = EmulationMode.Stopped;
								stepOutSp = 0xffffffff;
							}
							break;

						case EmulationMode.Snapshot:
							persistenceManager.Save(Path.Combine("snapshots", $"snap-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json"));
							logger.LogTrace("Snapshot Recorded!");
							emulationMode = EmulationMode.Running;
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
