using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public class MemoryManager : IMemoryManager
	{
		private readonly ILogger logger;

		public MemoryMappedDeviceCollection MappedDevice { get; }
		public MemoryMappedDeviceCollection DebugMappedDevice { get; }

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
		private readonly ILogger logger;

		public MemoryMappedDeviceCollection(ILogger logger, int addressBits)
		{
			memoryMask = (uint)((1ul<<addressBits) - 1);
			this.logger = logger;
		}

		public void BuildMappedDevices()
		{
			foreach (var dev in devices.Select(x => new {device = x, range = x.MappedRange()}))
			{
				foreach (var range in dev.range.Where(x=>x.Length != 0))
				{
					uint start = range.Start >> 16;
					uint end = (uint)((range.Start + range.Length) >> 16);
					for (uint i = start; i < end; i++)
					{
						if (dev.device.IsMapped(i << 16))
						{
							//if (i == 0)
							//{	
							//	//logger.LogTrace($"Setting Slot 0 tid: {Thread.CurrentThread.ManagedThreadId} to {dev.device} @ {new System.Diagnostics.StackTrace(true)}");
							//	logger.LogTrace($"Setting Slot 0 tid: {Thread.CurrentThread.ManagedThreadId} to {dev.device}");
							//}
							mapping[i] = dev.device;
						}
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

		public List<IPersistableRAM> PersistableDevices()
		{
			return devices.OfType<IPersistableRAM>().ToList();
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