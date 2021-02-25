using System.Collections.Generic;
using RunAmiga.Core.Interfaces;
using RunAmiga.Core.Types;

namespace RunAmiga.Core
{
	public class MemoryMapper : IMemoryMapper
	{
		private readonly List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();

		public MemoryMapper(IMemory memory, ICIAAOdd ciaa, ICIABEven ciab, IChips custom, IBattClock battClock)
		{
			devices.Add(memory);
			devices.Add(ciaa);
			devices.Add(ciab);
			devices.Add(custom);
			devices.Add(battClock);
		}

		public MemoryMapper(List<IMemoryMappedDevice> memoryDevices)
		{
			devices.AddRange(memoryDevices);
		}

		public void AddMapper(IMemoryMappedDevice memoryDevice)
		{
			devices.Insert(0, memoryDevice);
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
