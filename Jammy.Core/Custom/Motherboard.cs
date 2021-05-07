using System.Collections.Generic;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

namespace Jammy.Core.Custom
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
			reg_COLDSTART = 0x80;//cold reboot
			reg_RAMSEY = 7;//required to pass boot up checks
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
		private byte reg_TIMEOUT;//TIMEOUT
		private byte reg_TOENB;//TOENB*
		private byte reg_COLDSTART;//COLDSTART (bit 7 set on cold start)
		private byte reg_RAMSEY;//some kind of RAMSEY flags that won't boot unless set to 7
		private byte reg_RAMSEYID;//RAMSEY chip version

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size != Size.Byte) throw new InvalidCustomRegisterSizeException(insaddr, address, size);
			logger.LogTrace($"[MOBO] R {address:X8} @ {insaddr:X8} {size}");
			if (address == 0xde0000) return reg_TIMEOUT;
			if (address == 0xde0001) return reg_TOENB;
			if (address == 0xde0002) return reg_COLDSTART;
			if (address == 0xde0003) return reg_RAMSEY;
			if (address == 0xde0043) return reg_RAMSEYID;
			if (address == 0xde1000) return (uint)(GayleCheck()>>8);
			if (address == 0xde1002) return (uint)(GaryCheck()>>8);
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size != Size.Byte) throw new InvalidCustomRegisterSizeException(insaddr, address, size);
			logger.LogTrace($"[MOBO] W {address:X8} @ {insaddr:X8} {size} {value:X8}");
			if (address == 0xde0000) reg_TIMEOUT = (byte)value;
			else if (address == 0xde0001) reg_TOENB = (byte)value;
			else if (address == 0xde0002) reg_COLDSTART = (byte)value;
			else if (address == 0xde0003) reg_RAMSEY = (byte)value;
			else if (address == 0xde0043) reg_RAMSEYID = (byte)value;
			else if (address == 0xde1000) { gayleBits = GAYLE_BITS; logger.LogTrace("GAYLE Check"); }
			else if (address == 0xde1002) { garyBits = GARY_BITS; logger.LogTrace("GARY Check"); }
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
		private const int GARY_BITS = 0x0000;
		private int garyBits = GARY_BITS;
		private ushort GaryCheck()
		{
			ushort v = (ushort)(garyBits & 0x8000);
			int c = (garyBits >> 15) & 1;
			garyBits += garyBits + c;
			return v;
		}

	}
}
