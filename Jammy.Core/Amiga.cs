using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
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
		private readonly IBreakpointCollection breakpointCollection;

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
			ISerial serial, IMotherboard motherboard,
			IBreakpointCollection breakpointCollection, ILogger<Amiga> logger)
		{
			this.memoryMapper = memoryMapper;
			this.cpu = cpu;
			this.breakpointCollection = breakpointCollection;

			//fulfil the circular dependencies
			custom.Init(blitter, copper, audio);
			keyboard.SetCIA(ciaa);
			interrupt.Init(custom);

			//all the emulators and resetters
			emulations.Add(diskDrives);
			emulations.Add(mouse);
			emulations.Add(keyboard);
			emulations.Add(copper);
			emulations.Add(blitter);
			emulations.Add(audio);
			emulations.Add(ciaa);
			emulations.Add(ciab);
			emulations.Add(serial);
			emulations.Add(cpu);

			resetters.Add(diskController);
			resetters.Add(interrupt);
			resetters.Add(memory);
			resetters.Add(battClock);
			resetters.Add(motherboard);
			resetters.Add(custom);

			if (resetters.Any(x => x is IEmulate))
				throw new AmbiguousImplementationException();

			resetters.AddRange(emulations);

			Reset();

			emulationMode = EmulationMode.Running;
			requestExitEmulationMode = false;

			emulationSemaphore = new SemaphoreSlim(0,1);
			nonEmulationCompleteEvent = new AutoResetEvent(false);
		}

		public void Reset()
		{
			resetters.ForEach(x => x.Reset());
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
							RunEmulations(8);
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
			}
		}
	}
}
