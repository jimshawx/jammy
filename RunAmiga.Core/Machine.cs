using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
		private readonly IChips custom;
		private readonly IDebugMemoryMapper memoryMapper;

		private static EmulationMode emulationMode = EmulationMode.Stopped;

		private static SemaphoreSlim emulationSemaphore;

		private readonly List<IEmulate> emulations = new List<IEmulate>();

		public Machine(IInterrupt interrupt, IDebugMemoryMapper memoryMapper, IBattClock battClock, 
			ICIAAOdd ciaa, ICIABEven ciab, IChips custom, 
			ICPU cpu, IKeyboard keyboard, IBlitter blitter, ICopper copper, IAudio audio,
			IBreakpointCollection breakpointCollection)
		{
			this.memoryMapper = memoryMapper;
			this.custom = custom;
			this.cpu = cpu;
			this.breakpointCollection = breakpointCollection;

			custom.Init(blitter, copper, audio);

			keyboard.SetCIA(ciaa);

			interrupt.Init(custom);

			emulations.Add(battClock);
			emulations.Add(ciaa);
			emulations.Add(ciab);
			emulations.Add(custom);
			emulations.Add(cpu);
			emulations.Add(interrupt);

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
			emuThread = new Task(Emulate);
			emuThread.Start();
		}

		public static void SetEmulationMode(EmulationMode mode, bool changeWhileLocked = false)
		{
			if (changeWhileLocked)
				emulationMode = mode;
			else
			{
				emulationModeChange = mode;
				while (emulationModeChange != EmulationMode.NoChange)
					Thread.Yield();
			}
		}

		public static void UnlockEmulation()
		{
			emulationSemaphore.Release();
		}

		public static void LockEmulation()
		{
			emulationModeChange = EmulationMode.LockAccess;
			emulationSemaphore.Wait();
			emulationModeChange = EmulationMode.NoChange;
		}

		private static EmulationMode emulationModeChange;

		public void Emulate()
		{
			uint stepOutSp = 0xffffffff;

			emulationMode = EmulationMode.Stopped;
			emulationModeChange = EmulationMode.NoChange;

			emulationSemaphore.Wait();

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
					}

					if (breakpointCollection.BreakpointHit())
						emulationModeChange = EmulationMode.Stopped;
				}

				var newEmulationMode = emulationModeChange;

				if (newEmulationMode == EmulationMode.Exit)
					break;

				if (newEmulationMode == EmulationMode.Stopped)
				{
					stepOutSp = 0xffffffff;
					UI.UI.IsDirty = true;
				}

				if (newEmulationMode == EmulationMode.LockAccess)
				{
					emulationSemaphore.Release();
					//Locker should now be able to do its work
					while (emulationModeChange == EmulationMode.LockAccess)
						Thread.Yield();
					emulationSemaphore.Wait();
					//do not update emulation mode
				}
				else
				{
					emulationMode = newEmulationMode;
				}

				emulationModeChange = EmulationMode.NoChange;
			}
			emulationSemaphore.Release();
		}

		public void Reset()
		{
			emulations.ForEach(x => x.Reset());
		}
	}
}
