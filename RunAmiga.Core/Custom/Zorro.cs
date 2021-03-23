using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public interface IZorro :  IMemoryMappedDevice { }

	internal static class RamExpansion
	{
		//8MB RAM Expansion
		public static byte[] Config_8MB = new byte[]
		{
			0b11_1_0_0_000,//type, size
			0,//product number
			//0b1_0_0_0_1010,//location, size
			0b1_0_0_0_0000,//location, size
			0,//reserved
			0x24,0x06,//manufacturer hi/low
			0x00,0x00,0x00,0x00,//serial number
			0x00,0x00,//optional rom vector hi/lo (boot rom location, if bit 4 of byte 0 is set)
			0,0,0,0,0,//reserved
			0,0,//address base (written to)
			0,//shut up (written to)
			0,0,0,0,0,0,0,0,0,0,0,0,//reserved
		};

		//2MB RAM Expansion
		public static byte[] Config_2MB = new byte[]
		{
			0b11_1_0_0_110,//type, size
			0,//product number
			//0b1_0_0_0_1010,//location, size
			0b1_0_0_0_0000,//location, size
			0,//reserved
			0x24,0x06,//manufacturer hi/low
			0x00,0x00,0x00,0x00,//serial number
			0x00,0x00,//optional rom vector hi/lo (boot rom location, if bit 4 of byte 0 is set)
			0,0,0,0,0,//reserved
			0,0,//address base (written to)
			0,//shut up (written to)
			0,0,0,0,0,0,0,0,0,0,0,0,//reserved
		};

		//4MB RAM Expansion
		public static byte[] Config_4MB = new byte[]
		{
			0b11_1_0_0_111,//type, size
			0,//product number
			//0b1_0_0_0_1010,//location, size
			0b1_0_0_0_0000,//location, size
			0,//reserved
			0x24,0x06,//manufacturer hi/low
			0x00,0x00,0x00,0x00,//serial number
			0x00,0x00,//optional rom vector hi/lo (boot rom location, if bit 4 of byte 0 is set)
			0,0,0,0,0,//reserved
			0,0,//address base (written to)
			0,//shut up (written to)
			0,0,0,0,0,0,0,0,0,0,0,0,//reserved
		};
	}

	// HRM pp 431
	public class Zorro : IZorro
	{
		private readonly ILogger logger;

		public class Configurations
		{
			public string Name { get; set; }
			public bool IsConfigured { get; set; }
			public uint BaseAddress { get; set; }
			public byte [] Config { get; set; }
		}

		private readonly List<Configurations> configurations = new List<Configurations>();

		public Zorro(ILogger<Zorro> logger)
		{
			this.logger = logger;
			configurations.Add( new Configurations{ Config = RamExpansion.Config_8MB, Name = "8MB RAM Expansion" });
			//configurations.Add(new Configurations { Config = RamExpansion.Config_2MB, Name = "2MB RAM Expansion"});
			//configurations.Add(new Configurations { Config = RamExpansion.Config_4MB, Name = "4MB RAM Expansion" });
			//configurations.Add(new Configurations { Config = RamExpansion.Config_2MB, Name = "2MB RAM Expansion" });
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

			address -= 0xe80000;

			if (address == 0x4A && configurations.Any())
				configurations[0].BaseAddress |= ((value & 0xf0) >> 4) << 16;
			
			if (address == 0x48 && configurations.Any())
				configurations[0].BaseAddress |= ((value & 0xf0) >> 4) << 20;

			if (address == 0x46 && configurations.Any())
				configurations[0].BaseAddress |= ((value & 0xf0) >> 4) << 24;

			if (address == 0x44 && configurations.Any())
				configurations[0].BaseAddress |= ((value & 0xf0) >> 4) << 28;

			//writing here finishes the configuration (Zorro II)
			if (address == 0x48 && configurations.Any())
			{
				logger.LogTrace($"{configurations[0].Name} configured at {configurations[0].BaseAddress:X8}");
				configurations[0].IsConfigured = true;
				configurations.RemoveAt(0);
			}

			//shut up (OK then!)
			if ((address == 0x4C || address == 0x4e) && configurations.Any())
				configurations.RemoveAt(0);
		}
	}
}
