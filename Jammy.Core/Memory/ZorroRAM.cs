using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public static class ZorroExpansion
	{
		//Z2 RAM Expansion
		public static byte[] BaseConfig_Z2 = new byte[]
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

		//Z3 RAM Expansion
		public static byte[] BaseConfig_Z3 = new byte[]
		{
			0b10_1_0_0_101,//type, size
			0,//product number
			//0b1_0_0_0_1010,//location, size
			0b1_0_0_1_0000,//location, size
			0,//reserved
			0x24,0x06,//manufacturer hi/low
			0x00,0x00,0x00,0x00,//serial number
			0x00,0x00,//optional rom vector hi/lo (boot rom location, if bit 4 of byte 0 is set)
			0,0,0,0,0,//reserved
			0,0,//address base (written to)
			0,//shut up (written to)
			0,0,0,0,0,0,0,0,0,0,0,0,//reserved
		};

		public static byte[] ConfigForSize(byte[] cfg, float size, uint serial = 0)
		{
			cfg = cfg.ToArray();
			switch (size)
			{
				//Z2 sizes
				case 8.0f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b000); break;
				case 0.0625f: cfg[0] = (byte)((cfg[0] & 0xfc) | 0b001); break;
				case 0.125f:  cfg[0] = (byte)((cfg[0] & 0xfc) | 0b010); break;
				case 0.25f:   cfg[0] = (byte)((cfg[0] & 0xfc) | 0b011); break;
				case 0.5f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b100); break;
				case 1.0f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b101); break;
				case 2.0f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b110); break;
				case 4.0f:    cfg[0] = (byte)((cfg[0] & 0xfc) | 0b111); break;

				//Z3 sizes
				case 16.0f:   cfg[0] = (byte)((cfg[0] & 0xfc) | 0b000); break;
				case 32.0f:   cfg[0] = (byte)((cfg[0] & 0xfc) | 0b001); break;
				case 64.0f:   cfg[0] = (byte)((cfg[0] & 0xfc) | 0b010); break;
				case 128.0f:  cfg[0] = (byte)((cfg[0] & 0xfc) | 0b011); break;
				case 256.0f:  cfg[0] = (byte)((cfg[0] & 0xfc) | 0b100); break;
				case 512.0f:  cfg[0] = (byte)((cfg[0] & 0xfc) | 0b110); break;
				case 1024.0f: cfg[0] = (byte)((cfg[0] & 0xfc) | 0b111); break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			//extended size for Z3 > 8MB
			if ((cfg[0] >> 6) == 2 && size > 8.0f) cfg[2] |= 1 << 5;

			cfg[6] = (byte)(serial >> 24);
			cfg[7] = (byte)(serial >> 16);
			cfg[8] = (byte)(serial >> 8);
			cfg[9] = (byte)serial;

			return cfg;
		}
	}

	public class ZorroConfigurator : IZorroConfigurator
	{
		public ZorroConfigurator(IZorro2 zorro2, IZorro3 zorro3, IEnumerable<IExpansionROM> expansionRoms, IOptions<EmulationSettings> settings)
		{
			if (!string.IsNullOrEmpty(settings.Value.ZorroIIMemory))
			{
				var expansions = settings.Value.ZorroIIMemory
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(x=>Convert.ToSingle(x,CultureInfo.InvariantCulture));

				foreach (var v in expansions.Where(x =>x != 0.0))
					((IZorro)zorro2).AddConfiguration(new ZorroConfiguration
					{
						Config = ZorroExpansion.ConfigForSize(ZorroExpansion.BaseConfig_Z2,v),
						Name = $"{v}MB ZII RAM Expansion",
						Size = (uint)(v * 1024.0f * 1024.0f)
					});
			}

			if (!string.IsNullOrEmpty(settings.Value.ZorroIIIMemory))
			{
				var expansions = settings.Value.ZorroIIIMemory
					.Split(',', StringSplitOptions.RemoveEmptyEntries)
					.Select(x => Convert.ToSingle(x,CultureInfo.InvariantCulture));

				foreach (var v in expansions.Where(x => x != 0.0))
					((IZorro)zorro3).AddConfiguration(new ZorroConfiguration
					{
						Config = ZorroExpansion.ConfigForSize(ZorroExpansion.BaseConfig_Z3, v),
						Name = $"{v}MB ZIII RAM Expansion",
						Size = (uint)(v * 1024.0f * 1024.0f)
					});
			}

			foreach (var rom in expansionRoms)
				((IZorro)zorro2).AddConfiguration(rom.GetConfiguration(), rom);
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
