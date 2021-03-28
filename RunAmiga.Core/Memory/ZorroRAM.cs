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
	}

	public class ZorroConfigurator : IZorroConfigurator
	{
		public ZorroConfigurator(IZorro zorro, IOptions<EmulationSettings> settings)
		{
			if (settings.Value.ZorroIIMemory != 0.0)
			{
				zorro.AddConfiguration(new ZorroConfiguration { Config = RamExpansion.Config_8MB, Name = "8MB RAM Expansion", Size = 8 * 1024 * 1024 });

				//zorro.AddConfiguration(new Configurations { Config = RamExpansion.Config_2MB, Name = "2MB RAM Expansion"});
				//zorro.AddConfiguration(new Configurations { Config = RamExpansion.Config_4MB, Name = "4MB RAM Expansion" });
				//zorro.AddConfiguration(new Configurations { Config = RamExpansion.Config_2MB, Name = "2MB RAM Expansion" });
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
