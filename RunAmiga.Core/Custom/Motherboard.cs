using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class Motherboard : IMotherboard
	{
		private readonly ILogger logger;

		public Motherboard(ILogger<Motherboard> logger)
		{
			this.logger = logger;
		}

		public void Reset()
		{
			reg_DE0002 = 0x80;//cold reboot
			reg_DE0003 = 7;//required to pass boot up checks
		}

		readonly MemoryRange memoryRange = new MemoryRange(0xde0000, 0x10000);

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange};
		}

		//a3000p.pdf The Amiga 3000+ System Specification
		//NB. A3000+ was never released. A4000 uses some of it.
		private byte reg_DE0000;//TIMEOUT
		private byte reg_DE0001;//TOENB*
		private byte reg_DE0002;//COLDSTART
		private byte reg_DE0003;//some kind of RAMSEY flags that won't boot unless set to 7
		private byte reg_DE0043;//RAMSEY chip version

		public uint Read(uint insaddr, uint address, Size size)
		{
			logger.LogTrace($"[MOBO] R {address:X8} @ {insaddr:X8} {size}");
			if (address == 0xde0000) return reg_DE0000;
			if (address == 0xde0001) return reg_DE0001;
			if (address == 0xde0002) return reg_DE0002;
			if (address == 0xde0003) return reg_DE0003;
			if (address == 0xde0043) return reg_DE0043;
			if (address == 0xde1000) return (uint)(GayleCheck()>>8);
			if (address == 0xde1002) return (uint)(GaryCheck()>>8);
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"[MOBO] W {address:X8} @ {insaddr:X8} {size} {value:X8}");
			if (address == 0xde0000) reg_DE0000 = (byte)value;
			else if (address == 0xde0001) reg_DE0001 = (byte)value;
			else if (address == 0xde0002) reg_DE0002 = (byte)value;
			else if (address == 0xde0003) reg_DE0003 = (byte)value;
			else if (address == 0xde0043) reg_DE0043 = (byte)value;
			else if (address == 0xde1000) gayleBits = GAYLE_BITS;
			else if (address == 0xde1002) garyBits = GARY_BITS;
			else if (address == 0xde109a) { /*something writes 0xbfff here at boot time */ }
			else	logger.LogTrace($"W {address:X6} not mapped");
		}


		//todo: A4000 says 0xD1
		private const int GAYLE_BITS = 0xD0D0;
		private int gayleBits = GAYLE_BITS;
		private ushort GayleCheck()
		{
			ushort v = (ushort)(gayleBits & 0x8000);
			int c = (gayleBits >> 15) & 1;
			gayleBits += gayleBits + c;
			return v;
		}

		//todo: Fat Gary A3000, A4000. don't know what this does yet.
		private const int GARY_BITS = 0xFFFF;
		private int garyBits = GARY_BITS;
		private ushort GaryCheck()
		{
			return (ushort)garyBits;
		}

	}
}
