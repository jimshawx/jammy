using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public class MemoryManager : IMemoryManager
	{
		private readonly ILogger logger;

		public IMemoryMappedDeviceCollection MappedDevice { get; }
		public IMemoryMappedDeviceCollection DebugMappedDevice { get; }

		private readonly List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();

		public MemoryManager(ILogger<MemoryManager> logger, IOptions<EmulationSettings> settings)
		{
			MappedDevice = new MemoryMappedDeviceCollection(logger, settings.Value.AddressBits);
			DebugMappedDevice = new MemoryMappedDeviceCollection(logger, settings.Value.AddressBits);
			this.logger = logger;
		}

		public void AddDevices(List<IMemoryMappedDevice> devs)
		{
			MappedDevice.AddRange(devs);
			DebugMappedDevice.AddRange(devs.Where(x=> x is IDebuggableMemory));
			devices.AddRange(devs);

			BuildMappedDevices();
		}

		public void RefreshDevices()
		{
			BuildMappedDevices();
		}

		public void AddDevice(IMemoryMappedDevice device)
		{
			AddDevices(new List<IMemoryMappedDevice>{device});
		}

		private void BuildMappedDevices()
		{
			MappedDevice.BuildMappedDevices();
			DebugMappedDevice.BuildMappedDevices();
		}

		public IZorroRAM FindMappedDevice(uint start, ulong length)
		{
			//if there's already an active device at the start address, do nothing
			foreach (var device in devices.Where(x => !(x is UnmappedMemory)))
			{
				foreach (var range in device.MappedRange())
				{
					if (range.Start == start)
						return null;
				}
			}

			//find an inactive zorro board of the right size and activate it
			foreach (var zorro in devices.OfType<IZorroRAM>())
			{
				if (!zorro.IsActive(out ulong zlen))
				{ 
					if (zlen == length)
						return zorro;
				}
			}
			return null;
		}
	}

	public class MemoryMappedDeviceCollection : IMemoryMappedDeviceCollection
	{
		private readonly List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();
		private readonly IMemoryMappedDevice[] mapping = new IMemoryMappedDevice[0x10000];
		private readonly uint memoryMask;
		private readonly ILogger logger;

		public MemoryMappedDeviceCollection(ILogger logger, int addressBits)
		{
			memoryMask = (uint)((1ul<<addressBits) - 1);
			this.logger = logger;
		}

		public void BuildMappedDevices()
		{
			foreach (var dev in devices.OrderBy(x=>x is UnmappedMemory?0:1).Select(x => new {device = x, range = x.MappedRange()}))
			{
				foreach (var range in dev.range.Where(x=>x.Length != 0))
				{
					uint start = range.Start >> 16;
					uint end = (uint)((range.Start + range.Length) >> 16);
					for (uint i = start; i < end; i++)
					{
						if (dev.device.IsMapped(i << 16))
							mapping[i] = dev.device;
					}
				}
			}
			//Dump();
		}

		public void Add(IMemoryMappedDevice device)
		{
			devices.Add(device);
		}

		public void AddRange(IEnumerable<IMemoryMappedDevice> devs)
		{
			devices.AddRange(devs);
		}

		public IMemoryMappedDevice this[uint address] => mapping[(address&memoryMask) >> 16];

		public List<IBulkMemoryRead> BulkReadableDevices()
		{
			return devices.OfType<IBulkMemoryRead>().ToList();
		}

		public List<IPersistableRAM> PersistableDevices()
		{
			return devices.OfType<IPersistableRAM>().ToList();
		}

		private void Dump()
		{
			for (int i = 0; i < 256; i++)
				logger.LogTrace($"{i:X2} {i} {mapping[i]}");
		}
	}
}