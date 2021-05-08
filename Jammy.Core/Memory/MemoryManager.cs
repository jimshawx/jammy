using System.Collections.Generic;
using System.Linq;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public class MemoryManager : IMemoryManager
	{
		public MemoryMappedDeviceCollection MappedDevice { get; }
		public MemoryMappedDeviceCollection DebugMappedDevice { get; }

		public MemoryManager(IOptions<EmulationSettings> settings)
		{
			MappedDevice = new MemoryMappedDeviceCollection(settings.Value.AddressBits);
			DebugMappedDevice = new MemoryMappedDeviceCollection(settings.Value.AddressBits);
		}

		public void AddDevices(List<IMemoryMappedDevice> devs)
		{
			MappedDevice.AddRange(devs);
			DebugMappedDevice.AddRange(devs.Where(x=> x is IDebuggableMemory));

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
	}

	public class MemoryMappedDeviceCollection
	{
		private readonly List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();
		private readonly IMemoryMappedDevice[] mapping = new IMemoryMappedDevice[0x10000];
		private readonly uint memoryMask;

		public MemoryMappedDeviceCollection(int addressBits)
		{
			memoryMask = (uint)((1ul<<addressBits) - 1);
		}

		public void BuildMappedDevices()
		{
			foreach (var dev in devices.Select(x => new {device = x, range = x.MappedRange()}))
			{
				foreach (var range in dev.range)
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
	}

	public interface IMemoryManager
	{
		MemoryMappedDeviceCollection MappedDevice { get; }
		MemoryMappedDeviceCollection DebugMappedDevice { get; }
		void AddDevice(IMemoryMappedDevice device);
		void AddDevices(List<IMemoryMappedDevice> devs);
		void RefreshDevices();
	}
}