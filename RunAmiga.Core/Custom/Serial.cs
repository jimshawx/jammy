using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;

namespace RunAmiga.Core.Custom
{
	public class Serial : ISerial
	{
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;

		public Serial(IInterrupt interrupt, IEmulationWindow emulationWindow, ILogger<Serial> logger)
		{
			this.interrupt = interrupt;
			this.logger = logger;

			emulationWindow.SetKeyHandlers(AddKeyDown, AddKeyUp);
		}

		private void AddKeyDown(int key)
		{
			serin.Enqueue((byte)key);
		}

		private void AddKeyUp(int key)
		{

		}

		public void Reset()
		{
			serdat = serper = 0;
			serout.Clear();
			serin.Clear();
		}

		public void Emulate(ulong cycles)
		{
			if ((serdat & (ushort)SERDAT.TBE) != 0)
			{
				if (serin.TryDequeue(out byte c))
				{
					serdat = (ushort)(c & charMask);
					serdat |= stopBit;
					interrupt.AssertInterrupt(Interrupt.RBF);
				}
			}
		}

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			switch (address)
			{
				case ChipRegs.SERPER:
					value = serper;
					break;

				case ChipRegs.SERDAT: break;

				case ChipRegs.SERDATR:
					value = serdat;
					break;
			}

			return value;
		}


		private ushort serper;
		private ushort serdat;

		private ushort charMask = 0xff;
		private ushort stopBit = 0x100;

		private readonly StringBuilder serout = new StringBuilder();
		private readonly ConcurrentQueue<byte> serin = new ConcurrentQueue<byte>();

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.SERPER:
					serper = value;
					//logger.LogTrace($"Baud {value & 0x7fff} = {1000000.0 / (((value & 0x7fff) + 1) * 0.27936)} NTSC");
					logger.LogTrace($"SERPER W {((value & 0x8000) != 0 ? "9bit" : "8bit")} Baud {value & 0x7fff} = {1000000.0 / (((value & 0x7fff) + 1) * 0.28194)} PAL");

					if ((value & 0x8000) != 0)
					{
						charMask = 0x1ff;
						stopBit = 0x200;
					}
					else
					{
						charMask = 0xff;
						stopBit = 0x100;
					}

					break;

				case ChipRegs.SERDAT:
					char c = (char)(value & charMask);

					//logger.LogTrace($"SERDAT {c}");

					if (c >= 32)
						serout.Append(c);

					if (serout.Length > 80 || c < 32)
					{
						logger.LogTrace(serout.ToString());
						serout.Clear();
					}

					serdat = (ushort)(value & 0x3ff);
					serdat |= (ushort)SERDAT.TBE;
					interrupt.AssertInterrupt(Interrupt.TBE);

					break;

				case ChipRegs.SERDATR: break;
			}
		}

		public void WriteINTREQ(ushort intreq)
		{
			//need to mirror TBE/RBF into serdatr
			//if ((intreq & (1 << (int)Interrupt.TBE)) != 0)
			//	serdat |= (ushort)SERDAT.TBE;
			//else
			//	serdat &= (ushort)~SERDAT.TBE;

			if ((intreq & (1 << (int)Interrupt.RBF)) != 0)
				serdat |= (ushort)SERDAT.RBF;
			else
				serdat &= (ushort)~(SERDAT.RBF|SERDAT.RBF);
		}

		[Flags]
		public enum SERDAT : ushort
		{
			D0=1,
			D1=2,
			D2 = 4,
			D3 = 8,
			D4= 16,
			D5 = 32,
			D6 = 64,
			D7 = 128,
			D8_STP8 = 256,
			STP9 = 512,
			Unused = 1024,
			RXD=2048,
			TSRE=4096,
			TBE=8192,
			RBF=16384,
			OVRUN=32768
		}
	}
}