using System.Diagnostics;
using System.IO;
using System.Threading;

namespace runamiga
{
	class Machine
	{
		private CPU cpu;
		private Custom custom;
		private CIA cia;

		public Machine()
		{
			cia = new CIA();
			custom = new Custom();
			cpu = new CPU(cia, custom);
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

			cpu.BulkWrite(0xfc0000, rom, 256*1024);

			emuThread = new Thread(Emulate);
			emuThread.Start();
		}

		private void Emulate(object o)
		{
			for (;;)
				RunEmulations();
		}
	}
}
