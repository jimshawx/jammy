using runamiga.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace runamiga
{
	public class Machine
	{
		private CPU cpu;
		private Custom custom;
		private CIA cia;
		private EmulationMode emulationMode;

		public Machine()
		{
			cia = new CIA();
			custom = new Custom();
			cpu = new CPU(cia, custom);
			emulationMode = EmulationMode.Stopped;
			//targetEmulationMode = emulationMode;
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
			byte[] rom; rom = File.ReadAllBytes("../../../kick12.rom");
			Debug.Assert(rom.Length == 256 * 1024);

			cpu.BulkWrite(0xfc0000, rom, 256 * 1024);
			//cpu.Disassemble(0xfc0000);
		}

		public void Start()
		{
			emuThread = new Thread(Emulate);
			emuThread.Start();
		}

		public CPU GetCPU()
		{
			return cpu;
		}

		//private EmulationMode targetEmulationMode;
		public void SetEmulationMode(EmulationMode mode)
		{
			emulationMode = mode;
			//targetEmulationMode = mode;
			//while (emulationMode != targetEmulationMode)
			//	Thread.Yield();
		}

		private void Emulate(object o)
		{
			cia.Reset();
			custom.Reset();
			cpu.Reset();

			for (;;)
			{
				switch (emulationMode)
				{
					case EmulationMode.Stopped:
						Thread.Sleep(100);
						break;
					case EmulationMode.Running:
						RunEmulations();
						break;
					case EmulationMode.Step:
						RunEmulations();
						//targetEmulationMode = EmulationMode.Stopped;
						emulationMode = EmulationMode.Stopped;
						break;
					default:
						throw new ApplicationException("unknown emulation mode");
				}
				//emulationMode = targetEmulationMode;
			}
		}
	}
}
