using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{
	public class UnmappedMemory : IUnmappedMemory, IMemoryMappedDevice
	{
		private readonly ILogger logger;

		public UnmappedMemory(ILogger<UnmappedMemory> logger)
		{
			this.logger = logger;
		}

		public bool IsMapped(uint address)
		{
			return true;
		}

		private readonly MemoryRange memoryRange = new MemoryRange(0, 0x1000000);

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			//logger.LogTrace($"Unmapped Memory Read {address:X8} @{insaddr:X8} {size}");
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Unmapped Memory Write {address:X8} @{insaddr:X8} {value:X8} {size}");
		}
	}
}
