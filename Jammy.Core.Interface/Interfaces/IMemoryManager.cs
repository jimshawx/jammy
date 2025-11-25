using System;
using System.Collections.Generic;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface IMemoryManager
	{
		IMemoryMappedDeviceCollection MappedDevice { get; }
		IMemoryMappedDeviceCollection DebugMappedDevice { get; }
		void AddDevice(IMemoryMappedDevice device);
		void AddDevices(List<IMemoryMappedDevice> devs);
		void RefreshDevices();
	}
}
