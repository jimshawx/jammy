using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class IDEController : IIDEController
	{
		private readonly ILogger logger;
		private readonly MemoryRange memoryRange = new MemoryRange(0xda0000, 0x20000);

		public IDEController(ILogger<IDEController> logger)
		{
			this.logger = logger;
		}

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			logger.LogTrace($"IDE Controller Read {address:X8} @{insaddr:X8} {size}");
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"IDE Controller Write {address:X8} @{insaddr:X8} {value:X8} {size}");
		}
	}
}