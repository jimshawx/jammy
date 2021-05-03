using System.Collections.Generic;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.IDE
{
	public class A3000DiskController : IA3000DiskController
	{
		private readonly ISCSIController scsiController;
		private readonly MemoryRange memoryRange;

		public A3000DiskController(ISCSIController scsiController)
		{
			this.scsiController = scsiController;
			memoryRange = new MemoryRange(0xdd0000, 0x10000);
		}

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> { memoryRange };
		}

		public void Reset()
		{
			scsiController.Reset();
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			return scsiController.Read(insaddr, address, size);
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			scsiController.Write(insaddr, address, value, size);
		}
	}
}