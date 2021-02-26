using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core
{
	public class MemoryMapper : IMemoryMapper
	{
		private readonly ILogger<MemoryMapper> logger;
		private IMemoryInterceptor interceptor;
		private readonly List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();

		private readonly IMemoryMappedDevice[][] mappedDevice = { new IMemoryMappedDevice[0x100], new IMemoryMappedDevice[0x100]};

		public MemoryMapper(IMemory memory, ICIAAOdd ciaa, ICIABEven ciab, IChips custom, IBattClock battClock, ILogger<MemoryMapper> logger)
		{
			this.logger = logger;
			devices.Add(memory);
			devices.Add(ciaa);
			devices.Add(ciab);
			devices.Add(custom);
			devices.Add(battClock);
			BuildMappedDevices();
		}

		public MemoryMapper(List<IMemoryMappedDevice> memoryDevices)
		{
			devices.AddRange(memoryDevices);
			BuildMappedDevices();
		}

		public void AddMemoryIntercept(IMemoryInterceptor interceptor)
		{
			this.interceptor = interceptor;
		}

		private void BuildMappedDevices()
		{
			foreach (var dev in devices.Select(x => new { device = x, range = x.MappedRange()}) )
			{
				uint start = dev.range.Start>>16;
				uint end = (dev.range.Start + dev.range.Length)>>16;
				for (uint i = start; i < end; i++)
				{
					if (dev.device.IsMapped(i<<16))
						mappedDevice[0][i] = dev.device;
					if (dev.device.IsMapped((i << 16) + 1))
						mappedDevice[1][i] = dev.device;
				}
			}
		}

		readonly MemoryRange memoryRange = new MemoryRange(0x0, 0x1000000);

		public bool IsMapped(uint address)
		{
			return true;
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint value = mappedDevice[address&1][address >> 16].Read(insaddr, address, size);
			if (interceptor != null) interceptor.Read(insaddr, address, value, size);
			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (interceptor != null) interceptor.Write(insaddr, address, value, size);
			mappedDevice[address&1][address>>16].Write(insaddr, address, value, size);
		}
	}
}
