using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public interface IExpansion : IEmulate, IMemoryMappedDevice { }

	public class Expansion : IExpansion
	{
		private readonly ILogger logger;

		public Expansion(ILogger<Expansion> logger)
		{
			this.logger = logger;
		}

		public void Emulate(ulong cycles)
		{
		}

		public void Reset()
		{
		}

		public bool IsMapped(uint address)
		{
			return address >= 0xe00000 && address < 0xf00000;
		}

		private readonly MemoryRange mappedRange = new MemoryRange(0x00e00000, 0x100000);

		public MemoryRange MappedRange()
		{
			return mappedRange;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			logger.LogTrace($"Expansion R {insaddr:X8} {address:X8} {size}");
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Expansion W {insaddr:X8} {address:X8} {value:X8} {size}");
		}
	}
}