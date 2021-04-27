using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Memory;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	// HRM pp 431
	public class Zorro : IZorro
	{
		private readonly IMemoryManager memoryManager;
		private readonly ILogger logger;

		private readonly List<ZorroConfiguration> configurations = new List<ZorroConfiguration>();

		public Zorro(IMemoryManager memoryManager, ILogger<Zorro> logger)
		{
			this.memoryManager = memoryManager;
			this.logger = logger;
		}

		public void AddConfiguration(ZorroConfiguration zorroConfiguration)
		{
			this.configurations.Add(zorroConfiguration);
		}

		public bool IsMapped(uint address)
		{
			return address >= 0xe80000 && address < 0xf00000;
		}

		private readonly MemoryRange mappedRange = new MemoryRange(0x00e80000, 0x80000);

		public MemoryRange MappedRange()
		{
			return mappedRange;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			byte value = 0xff;

			address -= 0xe80000;
			//0 = byte 0, high bits
			//2 = byte 0, low bits
			//4 = byte 1, hi
			//6 = byte 1, lo
			//...

			uint index = address / 4;
			if (index == 0) value = 0;

			if (index < 32 && configurations.Any())
			{
				var expansion = configurations[0];
				byte v = expansion.Config[index];
				if (index != 0) v = (byte)~v;
				if ((address & 2) == 0)
					value = (byte)(v & 0xf0);
				else
					value = (byte)(v << 4);
			}

			//logger.LogTrace($"Expansion R {insaddr:X8} {address:X8} {value:X2} {size}");

			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Expansion W {insaddr:X8} {address:X8} {value:X8} {size}");

			if (!configurations.Any())
				return;

			address -= 0xe80000;

			if (address == 0x4A)
				configurations[0].BaseAddress |= ((value & 0xf0) >> 4) << 16;
			
			if (address == 0x48)
				configurations[0].BaseAddress |= ((value & 0xf0) >> 4) << 20;

			if (address == 0x46)
				configurations[0].BaseAddress |= ((value & 0xf0) >> 4) << 24;

			if (address == 0x44)
				configurations[0].BaseAddress |= ((value & 0xf0) >> 4) << 28;

			//writing here finishes the configuration (Zorro II)
			if (address == 0x48)
			{
				logger.LogTrace($"{configurations[0].Name} configured at {configurations[0].BaseAddress:X8}");
				configurations[0].IsConfigured = true;
				
				if (configurations[0].Mapping == ZorroConfiguration.MappingType.MemoryMapped)
					AddNewMemoryDevice(configurations[0]);
				
				configurations.RemoveAt(0);
			}

			//shut up (OK then!)
			if (address == 0x4C || address == 0x4e)
				configurations.RemoveAt(0);
		}

		private void AddNewMemoryDevice(ZorroConfiguration configuration)
		{
			var zorroRAM = new ZorroRAM(configuration.BaseAddress, configuration.Size);
			memoryManager.AddDevice(zorroRAM);
		}
	}
}
