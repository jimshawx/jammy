using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;

namespace RunAmiga.Core.Custom
{
	public class Serial : ISerial
	{
		private readonly IInterrupt interrupt;
		private readonly ISerialConsole serialConsole;
		private readonly ILogger logger;

		public Serial(IInterrupt interrupt, ISerialConsole serialConsole, ILogger<Serial> logger)
		{
			this.interrupt = interrupt;
			this.serialConsole = serialConsole;
			this.logger = logger;
		}

		public void Reset()
		{
			serdat = 0;
			serper = (ushort)SERDAT.TBE;
			serialConsole.Reset();
		}

		private uint serialInTimer = 0;
		private int tbeCounter = 0;
		private void AssertTBE()
		{
			tbeCounter = 2;
		}

		public void Emulate(ulong cycles)
		{
			if (tbeCounter > 0)
			{
				tbeCounter--;
				if (tbeCounter == 0)
					interrupt.AssertInterrupt(Interrupt.TBE);
			}

			serialInTimer++;
			if (serialInTimer >= 20)
			{
				serialInTimer -= 20;
				if ((serdat & (ushort)SERDAT.RBF) == 0)
				{
					int c = serialConsole.ReadChar();
					if (c != -1 && c!= 0x0d && c != 0xa)
					{
						serdat &= 0xfc00;
						serdat |= (ushort)(c & charMask);
						serdat |= stopBit;
						interrupt.AssertInterrupt(Interrupt.RBF);
					}
				}
			}
		}

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			switch (address)
			{
				case ChipRegs.SERPER: value = serper; break;
				case ChipRegs.SERDAT: break;
				case ChipRegs.SERDATR: value = serdat; break;
			}
			return value;
		}


		private ushort serper;
		private ushort serdat;

		//8N1
		private ushort charMask = 0xff;
		private ushort stopBit = 0x100;

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
					serialConsole.WriteChar(c);

					//TBE interrupt needs to be triggered a little later
					AssertTBE();

					break;

				case ChipRegs.SERDATR: break;
			}
		}

		public void WriteINTREQ(ushort intreq)
		{
			//need to mirror TBE/RBF into serdatr
			if ((intreq & (1 << (int)Interrupt.TBE)) != 0)
				serdat |= (ushort)SERDAT.TBE;
			else
				serdat &= (ushort)~SERDAT.TBE;

			if ((intreq & (1 << (int)Interrupt.RBF)) != 0)
				serdat |= (ushort)SERDAT.RBF;
			else
				serdat &= (ushort)~(SERDAT.RBF | SERDAT.OVRUN);
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

	public class EmulationConsole : ISerialConsole
	{
		private readonly ILogger logger;
		private readonly StringBuilder serout = new StringBuilder();
		private readonly ConcurrentQueue<byte> serin = new ConcurrentQueue<byte>();

		public EmulationConsole(IEmulationWindow emulationWindow, ILogger<EmulationConsole> logger)
		{
			this.logger = logger;
			emulationWindow.SetKeyHandlers(AddKeyDown, AddKeyUp);
		}

		private void AddKeyDown(int key) { serin.Enqueue((byte)key); }
		private void AddKeyUp(int key) { }

		public int ReadChar()
		{
			if (serin.TryDequeue(out byte c))
				return c;
			return -1;
		}

		public void WriteChar(int c)
		{
			if (c >= 32 && c <= 255)
				serout.Append((char)c);

			if (serout.Length > 80 || c < 32)
			{
				logger.LogTrace(serout.ToString());
				serout.Clear();
			}
		}

		public void Reset()
		{
			serout.Clear();
			serin.Clear();
		}
	}

	public class ANSIConsole : ISerialConsole, IDisposable
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AllocConsole();
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool AttachConsole(uint dwProcessId);
		[DllImport("kernel32.dll")]
		static extern bool FreeConsole();
		[DllImport("kernel32.dll")]
		static extern uint GetLastError();
		[DllImport("kernel32.dll")]
		static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
		[DllImport("kernel32.dll")]
		static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint dwMode);
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetStdHandle(int nStdHandle);

		private const uint ERROR_ACCESS_DENIED = 5;
		private const uint ERROR_INVALID_HANDLE = 6;
		private const uint ERROR_INVALID_PARAMETER = 87;

		private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
		private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
		private const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200;

		private const int STD_INPUT_HANDLE = -10;
		private const int STD_OUTPUT_HANDLE = -11;

		bool consoleAllocated = true;

		public ANSIConsole()
		{
			if (!AllocConsole())
			{
				consoleAllocated = false;

				//ERROR_ACCESS_DENIED means we're already attached to a console
				if (GetLastError() != ERROR_ACCESS_DENIED)
				{
					Trace.WriteLine($"AllocConsole LastError {GetLastError()}");
					if (!AttachConsole(0xffffffff))
					{
						Trace.WriteLine($"AttachConsole LastError {GetLastError()}");
						Trace.WriteLine("Can't get a console for logging");
						return;
					}

					//attached to an existing console, need to call FreeConsole()
					consoleAllocated = true;
				}
			}

			//enable ANSI/VT100 mode
			{
				var handle = GetStdHandle(STD_OUTPUT_HANDLE);
				GetConsoleMode(handle, out uint mode);
				mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING ;//| DISABLE_NEWLINE_AUTO_RETURN;
				SetConsoleMode(handle, mode);
			}

			{
				var handle = GetStdHandle(STD_INPUT_HANDLE);
				GetConsoleMode(handle, out uint mode);
				mode |= ENABLE_VIRTUAL_TERMINAL_INPUT;
				SetConsoleMode(handle, mode);
			}

			Console.Clear();
		}

		public int ReadChar()
		{
			if (Console.KeyAvailable)
				return Console.In.Read();
			return -1;
		}

		public void WriteChar(int c)
		{
			if (c == 0xc)
				Console.Clear();
			else
				Console.Out.Write((char)c);
		}

		public void Reset() { }

		public void Dispose()
		{
			if (consoleAllocated)
				FreeConsole();
			consoleAllocated = false;
		}
	}
}