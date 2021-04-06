using System;
using System.Linq;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{
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

		public static byte[] ConfigForSize(float size)
		{
			var cfg = Config_8MB.ToArray();
			switch (size)
			{
				case 8.0f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b000); break;
				case 0.0625f: cfg[0] = (byte)((cfg[0] & 0xfc) | 0b001); break;
				case 0.125f:  cfg[0] = (byte)((cfg[0] & 0xfc) | 0b010); break;
				case 0.25f:   cfg[0] = (byte)((cfg[0] & 0xfc) | 0b011); break;
				case 0.5f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b100); break;
				case 1.0f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b101); break;
				case 2.0f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b110); break;
				case 4.0f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b111); break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return cfg;
		}
	}

	public class ZorroConfigurator : IZorroConfigurator
	{
		public ZorroConfigurator(IZorro zorro, IOptions<EmulationSettings> settings)
		{
			if (!string.IsNullOrEmpty(settings.Value.ZorroIIMemory))
			{
				var expansions = settings.Value.ZorroIIMemory
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(Convert.ToSingle);

				foreach (var v in expansions)
					zorro.AddConfiguration(new ZorroConfiguration
					{
						Config = RamExpansion.ConfigForSize(v),
						Name = $"{v}MB RAM Expansion",
						Size = (uint)(v * 1024.0f * 1024.0f)
					});
			}
		}
	}

	public class ZorroRAM : Memory, IZorroRAM
	{
		public ZorroRAM(uint address, uint size)
		{
			memory = new byte[size];
			memoryRange = new MemoryRange(address, size);
			addressMask = (uint)(memoryRange.Length - 1);
		}
	}
}
