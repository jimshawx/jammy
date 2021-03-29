using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core
{
	public class Machine : IMachine
	{
		public class Constants
		{
			//PAL
			public const double CPUHz = 7.09379;
			public const int CyclesPerFrame = 140968;
			public const int RefreshRate = 50;
			public const int ScanlinesPerFrame = 312;
			public const int CyclesPerScanline = 452;

			//NTSC
			//public const double CPUHz = 7.15909;   //NTSC 3.579545 * 2
			//public const int CyclesPerFrame = 139682;
			//public const int RefreshRate = 60;
			//public const int ScanlinesPerFrame = 262;
			//public const int CyclesPerScanline = 533;
		}

		private readonly ICPU cpu;
		private readonly IBreakpointCollection breakpointCollection;
		private readonly ILogger logger;
		private readonly IChips custom;
		private readonly IDebugMemoryMapper memoryMapper;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;

		private readonly List<IEmulate> emulations = new List<IEmulate>();
		private readonly List<IReset> resetters = new List<IReset>();

		public Machine(IInterrupt interrupt, IDebugMemoryMapper memoryMapper, IBattClock battClock, 
			ICIAAOdd ciaa, ICIABEven ciab, IChips custom, IMemoryMapper memory,
			ICPU cpu, IKeyboard keyboard, IBlitter blitter, ICopper copper, IAudio audio,
			IDiskDrives diskDrives, IMouse mouse, IIDEController ideController,
			IBreakpointCollection breakpointCollection, ILogger<Machine> logger)
		{
			this.memoryMapper = memoryMapper;
			this.custom = custom;
			this.cpu = cpu;
			this.breakpointCollection = breakpointCollection;
			this.logger = logger;

			//fulfil the circular dependencies
			custom.Init(blitter, copper, audio);
			keyboard.SetCIA(ciaa);
			interrupt.Init(custom);

			//all the emulators and resetters
			emulations.Add(diskDrives);
			emulations.Add(mouse);
			emulations.Add(keyboard);
			emulations.Add(copper);
			emulations.Add(audio);
			emulations.Add(ciaa);
			emulations.Add(ciab);
			emulations.Add(cpu);

			resetters.Add(ideController);
			resetters.Add(interrupt);
			resetters.Add(memory);
			resetters.Add(battClock);
			resetters.Add(blitter);
			resetters.Add(custom);
			resetters.AddRange(emulations);

			Reset();

			emulationSemaphore = new SemaphoreSlim(1);
		}

		public void RunEmulations(ulong ns)
		{
			emulations.ForEach(x => x.Emulate(ns));
		}

		private Task emuThread;

		public void Start()
		{
			emuThread = new Task(Emulate, TaskCreationOptions.LongRunning);
			emuThread.Start();
		}

		public static void SetEmulationMode(EmulationMode mode, bool changeWhileLocked = false)
		{
			var logger = ServiceProviderFactory.ServiceProvider.GetRequiredService<ILogger<Machine>>();

			if (changeWhileLocked)
			{
				logger.LogTrace($"SEM1 em: {emulationMode} emc: {emulationModeChange}");
				emulationMode = mode;
				logger.LogTrace($"SEM2 em: {emulationMode} emc: {emulationModeChange}");
			}
			else
			{
				//logger.LogTrace($"SEM3 em: {emulationMode} emc: {emulationModeChange}");
				//emulationModeChange = mode;
				//logger.LogTrace($"SEM4 em: {emulationMode} emc: {emulationModeChange}");
				//while (emulationModeChange == mode)
				//	Thread.Yield();
				//logger.LogTrace($"SEM5 em: {emulationMode} emc: {emulationModeChange}");

				logger.LogTrace($"SEM3 em: {emulationMode} emc: {emulationModeChange} {Thread.CurrentThread.ManagedThreadId}");
				LockEmulation();
				logger.LogTrace($"SEM4 em: {emulationMode} emc: {emulationModeChange}");
				emulationMode = mode;
				logger.LogTrace($"SEM5 em: {emulationMode} emc: {emulationModeChange}");
				UnlockEmulation();
				logger.LogTrace($"SEM6 em: {emulationMode} emc: {emulationModeChange}");
			}
		}

		public static void UnlockEmulation()
		{
			lockedThreadId = -1;
			//emulationSemaphore.Release();
		}

		private static int lockedThreadId = -1;
		public static void LockEmulation()
		{
			var logger = ServiceProviderFactory.ServiceProvider.GetRequiredService<ILogger<Machine>>();
			logger.LogTrace($"Lock1 em: {emulationMode} emc: {emulationModeChange}");
			emulationModeChange = EmulationMode.LockAccess;
			logger.LogTrace($"Lock2 em: {emulationMode} emc: {emulationModeChange} {Thread.CurrentThread.ManagedThreadId}");
			if (lockedThreadId == Thread.CurrentThread.ManagedThreadId)
				Debugger.Break();
			
			//emulationSemaphore.Wait();
			logger.LogTrace($"Lock3 em: {emulationMode} emc: {emulationModeChange} {Thread.CurrentThread.ManagedThreadId}");
			lockedThreadId = Thread.CurrentThread.ManagedThreadId;
			emulationModeChange = EmulationMode.NoChange;
			logger.LogTrace($"Lock4 em: {emulationMode} emc: {emulationModeChange}");
		}

		private static EmulationMode emulationModeChange;

		public void Emulate()
		{
			uint stepOutSp = 0xffffffff;

			emulationMode = EmulationMode.Running;
			emulationModeChange = EmulationMode.NoChange;

			emulationSemaphore.Wait();
			lockedThreadId = Thread.CurrentThread.ManagedThreadId;

			while (emulationMode != EmulationMode.Exit)
			{
				while (emulationModeChange == EmulationMode.NoChange)
				{
					switch (emulationMode)
					{
						case EmulationMode.Running:
							RunEmulations(8);
							break;

						case EmulationMode.Step:
							RunEmulations(8);
							emulationModeChange = EmulationMode.Stopped;
							break;

						case EmulationMode.StepOut:
							var regs = cpu.GetRegs();
							if (stepOutSp == 0xffffffff) stepOutSp = regs.A[7];
							ushort ins = memoryMapper.UnsafeRead16(regs.PC);
							bool stopping = (ins == 0x4e75 || ins == 0x4e73) && regs.A[7] >= stepOutSp; //rts or rte
							RunEmulations(8);
							if (stopping)
								emulationModeChange = EmulationMode.Stopped;
							break;

						case EmulationMode.Stopped:
							Thread.Yield();
							break;
					}

					if (breakpointCollection.BreakpointHit())
						emulationModeChange = EmulationMode.Stopped;
				}

				logger.LogTrace("A");

				var newEmulationMode = emulationModeChange;

				if (newEmulationMode == EmulationMode.Exit)
					break;

				if (newEmulationMode == EmulationMode.Stopped)
				{
					logger.LogTrace("B");

					stepOutSp = 0xffffffff;
					UI.UI.IsDirty = true;
				}

				if (newEmulationMode == EmulationMode.LockAccess)
				{
					logger.LogTrace("C");

					lockedThreadId = -1;
					emulationSemaphore.Release();

					logger.LogTrace("D");

					//Locker should now be able to do its work
					while (emulationModeChange == EmulationMode.LockAccess)
						Thread.Yield();

					logger.LogTrace("E");

					emulationSemaphore.Wait();
					lockedThreadId = Thread.CurrentThread.ManagedThreadId;
					//do not update emulation mode

					logger.LogTrace("F");
				}
				else
				{
					emulationMode = newEmulationMode;
					logger.LogTrace("G");
				}

				emulationModeChange = EmulationMode.NoChange;
			}

			logger.LogTrace("H");

			emulationModeChange = EmulationMode.NoChange;
			emulationSemaphore.Release();
		}

		public void Reset()
		{
			resetters.ForEach(x => x.Reset());
		}
	}
}
