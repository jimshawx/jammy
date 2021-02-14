using System.Collections.Generic;
using RunAmiga.Types;

namespace RunAmiga
{
	public class MemoryMapper : IMemoryMappedDevice
	{
		private readonly List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();

		public MemoryMapper(IMemoryMappedDevice debugger, IMemoryMappedDevice memory, IMemoryMappedDevice ciaa, IMemoryMappedDevice ciab, IMemoryMappedDevice custom)
		{
			devices.Add(debugger);
			devices.Add(memory);
			devices.Add(ciaa);
			devices.Add(ciab);
			devices.Add(custom);
		}

		public MemoryMapper(List<IMemoryMappedDevice> memoryDevices)
		{
			devices.AddRange(memoryDevices);
		}

		public bool IsMapped(uint address)
		{
			return true;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint value=0;
			foreach (var x in devices)
				if (x.IsMapped(address))
					value = x.Read(insaddr, address, size);
			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			foreach (var x in devices)
				if (x.IsMapped(address))
					x.Write(insaddr, address, value, size);
		}
	}
}
