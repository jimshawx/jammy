using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class Motherboard : IMotherboard
	{
		private readonly ILogger logger;

		public Motherboard(ILogger<BattClock> logger)
		{
			this.logger = logger;
		}

		public void Reset()
		{
			reg_DE0002 = 0x80;
		}

		readonly MemoryRange memoryRange = new MemoryRange(0xde0000, 0x10000);

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		private byte reg_DE0000;
		private byte reg_DE0001;
		private byte reg_DE0002;

		public uint Read(uint insaddr, uint address, Size size)
		{
			logger.LogTrace($"[MOBO] R {address:X8} @ {insaddr:X8} {size}");
			if (address == 0xde0000) return reg_DE0000;
			if (address == 0xde0001) return reg_DE0001;
			if (address == 0xde0002) return reg_DE0002;
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"[MOBO] W {address:X8} @ {insaddr:X8} {size} {value:X8}");
			if (address == 0xde0000) reg_DE0000 = (byte)value;
			if (address == 0xde0001) reg_DE0001 = (byte)value;
			if (address == 0xde0002) reg_DE0002 = (byte)value;
		}
	}
}
