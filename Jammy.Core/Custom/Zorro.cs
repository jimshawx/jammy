using System.Collections.Generic;
using System.Linq;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	// HRM pp 431
	public class Zorro2 : ZorroBase, IZorro2
	{
		public Zorro2(IMemoryManager memoryManager, ILogger<Zorro2> logger)
		{
			this.memoryManager = memoryManager;
			this.logger = logger;
			mappedRange = new MemoryRange(0x00e80000, 0x10000);
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			byte value = 0xff;

			//Zorro 2
			//0 = byte 0, high bits
			//2 = byte 0, low bits
			//4 = byte 1, hi
			//6 = byte 1, lo
			//...

			if ((address&0xffff) > 0x7f) return value;

			uint index = (address&0xff) / 4;
			if (index == 0) value = 0;

			if (index < 32 && configurations.Any())
			{
				var expansion = configurations[0];
				byte v = expansion.Cfg.Config[index];
				if (index != 0) v = (byte)~v;
				if ((address & 2) == 0)
					value = (byte)(v & 0xf0);
				else
					value = (byte)(v << 4);
			}

			//logger.LogTrace($"Expansion R @{insaddr:X8} {address:X8} {value:X2} {size}");

			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Expansion W @{insaddr:X8} {address:X8} {value:X8} {size}");

			if (!configurations.Any())
				return;

			address &= 0xffff;

			if (address == 0x4A)
				configurations[0].Cfg.BaseAddress |= ((value & 0xf0) >> 4) << 16;
			
			if (address == 0x48)
				configurations[0].Cfg.BaseAddress |= ((value & 0xf0) >> 4) << 20;

			if (address == 0x46)
				configurations[0].Cfg.BaseAddress |= ((value & 0xf0) >> 4) << 24;

			if (address == 0x44)
				configurations[0].Cfg.BaseAddress |= ((value & 0xf0) >> 4) << 28;

			//writing here finishes the configuration (Zorro II)
			if (address == 0x48)
				CompleteConfiguration();

			//shut up (OK then!)
			if (address == 0x4C || address == 0x4E)
				configurations.RemoveAt(0);
		}
	}

	public class Zorro3 : ZorroBase, IZorro3
	{
		public Zorro3(IMemoryManager memoryManager, ILogger<Zorro2> logger)
		{
			this.memoryManager = memoryManager;
			this.logger = logger;
			mappedRange = new MemoryRange(0xff000000, 0x10000);
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			byte value = 0xff;

			//Zorro 3
			//000 = byte 0, high bits
			//100 = byte 0, low bits
			//004 = byte 1, hi
			//104 = byte 1, lo
			//...

			if ((address & 0xffff) > 0x17f) return value;

			uint index = (address & 0xff) / 4;
			if (index == 0) value = 0;

			if (index < 32 && configurations.Any())
			{
				var expansion = configurations[0];
				byte v = expansion.Cfg.Config[index];
				if (index != 0) v = (byte)~v;
				if ((address & 0x100) == 0)
					value = (byte)(v & 0xf0);
				else
					value = (byte)(v << 4);
			}

			//logger.LogTrace($"Expansion R @{insaddr:X8} {address:X8} {value:X2} {size}");

			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Expansion W @{insaddr:X8} {address:X8} {value:X8} {size}");

			if (!configurations.Any())
				return;

			address &= 0xffff;

			if (address == 0x44)
			{
				//writing here finishes the configuration (Zorro III)
				if (size == Size.Word)
					configurations[0].Cfg.BaseAddress |= (value & 0xffff) << 16;
				else
					configurations[0].Cfg.BaseAddress |= (value & 0xff) <<  24;
				CompleteConfiguration();
			}

			if (address == 0x48)
				configurations[0].Cfg.BaseAddress |= (value & 0xff) << 16;

			//shut up (OK then!)
			if (address == 0x4C || address == 0x14C)
				configurations.RemoveAt(0);
		}
	}

	public abstract class ZorroBase : IZorro
	{
		protected IMemoryManager memoryManager;
		protected ILogger logger;
		protected MemoryRange mappedRange;

		protected readonly List<ZorroCfg> configurations = new List<ZorroCfg>();

		protected class ZorroCfg
		{
			public ZorroConfiguration Cfg;
			public IExpansionROM Expansion;
		}

		public void AddConfiguration(ZorroConfiguration zorroConfiguration, IExpansionROM expansion = null)
		{
			this.configurations.Add(new ZorroCfg { Cfg = zorroConfiguration, Expansion = expansion });
		}

		public bool IsMapped(uint address)
		{
			return mappedRange.Contains(address);
		}

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> { mappedRange };
		}

		protected void CompleteConfiguration()
		{
			if (configurations[0].Cfg.BaseAddress == 0)
			{
				logger.LogTrace($"{configurations[0].Cfg.Name} failed to configure");
			}
			else
			{
				logger.LogTrace($"{configurations[0].Cfg.Name} configured at {configurations[0].Cfg.BaseAddress:X8}");
				configurations[0].Cfg.IsConfigured = true;

				if (configurations[0].Cfg.Mapping == ZorroConfiguration.MappingType.MemoryMapped)
					AddNewMemoryDevice(configurations[0]);
			}

			configurations.RemoveAt(0);
		}

		private void AddNewMemoryDevice(ZorroCfg configuration) 
		{ 
			var zorroRAM = new ZorroRAM(configuration.Cfg.BaseAddress, configuration.Cfg.Size); 
			memoryManager.AddDevice(zorroRAM);
			if (configuration.Expansion != null)
				configuration.Expansion.PopulateROM(zorroRAM);
 		}
	}
}
