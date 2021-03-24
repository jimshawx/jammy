using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class Akiko : IAkiko
	{
		private readonly ILogger logger;

		public Akiko(ILogger<Akiko> logger)
		{
			this.logger = logger;
		}

		public bool IsMapped(uint address)
		{
			return mappedRange.Contains(address);
		}

		//Akiko registers are mapped between $b80000 to $b87fff and registers repeat after every 64 bytes
		private readonly MemoryRange mappedRange = new MemoryRange(0xb80000, 0x8000);

		public MemoryRange MappedRange()
		{
			return mappedRange;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			logger.LogTrace($"Akiko Read {address:X8} @{insaddr:X8} {size}");
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Akiko Write {address:X8} @{insaddr:X8} {value:X8} {size}");
		}
	}
}