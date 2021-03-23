using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Custom;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core
{
	public class MemoryMapper : IMemoryMapper
	{
		private readonly ILogger logger;
		private IMemoryInterceptor interceptor;
		private readonly List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();

		private readonly IMemoryMappedDevice [] mappedDevice = new IMemoryMappedDevice[0x100];

		private readonly uint memoryMask;

		public MemoryMapper(IMemory memory, ICIAMemory ciaMemory, IChips custom, IBattClock battClock, IZorro expansion, ILogger<MemoryMapper> logger)
		{
			this.logger = logger;
			devices.Add(memory);
			memoryMask = (uint)(memory.GetMemoryArray().Length - 1);
			devices.Add(ciaMemory);
			devices.Add(custom);
			devices.Add(battClock);
			devices.Add(expansion);
			BuildMappedDevices();
		}

		public MemoryMapper(List<IMemoryMappedDevice> memoryDevices)
		{
			var memory = (IMemory)memoryDevices.Single(x => x is IMemory);
			memoryMask = (uint)(memory.GetMemoryArray().Length - 1);

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
						mappedDevice[i] = dev.device;
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
			address &= memoryMask;
			uint value = mappedDevice[address >> 16].Read(insaddr, address, size);
			if (interceptor != null) interceptor.Read(insaddr, address, value, size);
			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			address &= memoryMask;
			if (interceptor != null) interceptor.Write(insaddr, address, value, size);
			mappedDevice[address>>16].Write(insaddr, address, value, size);
		}
	}
}
