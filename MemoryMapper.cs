using System.Collections.Generic;
using System.Linq;
using RunAmiga.Types;

namespace RunAmiga
{
	public class MemoryMapper : IMemoryMappedDevice
	{
		private readonly MemoryReplay memoryReplay;
		private List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();

		public MemoryMapper(IMemoryMappedDevice debugger, IMemoryMappedDevice memory, IMemoryMappedDevice cia, IMemoryMappedDevice custom, MemoryReplay memoryReplay)
		{
			this.memoryReplay = memoryReplay;
			devices.Add(debugger);
			devices.Add(memoryReplay);
			devices.Add(memory);
			devices.Add(cia);
			devices.Add(custom);
		}

		private List<IMemoryMappedDevice> MemoryMap(uint address)
		{
			return devices.Where(x => x.IsMapped(address)).ToList();
		}

		public bool IsMapped(uint address)
		{
			return true;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint value=0;
			MemoryMap(address).ForEach(x => value = x.Read(insaddr, address, size));

			memoryReplay.LogRead(insaddr, address, size, value);

			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			MemoryMap(address).ForEach(x => x.Write(insaddr, address, value, size));

			memoryReplay.LogWrite(insaddr, address, size, value);
		}
	}
}
