using System.Collections.Generic;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IDE
{
	public class A3000DiskController : MemoryMappedDevice, IA3000DiskController
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

		public override uint Read(uint insaddr, uint address, Size size)
		{
			return scsiController.Read(insaddr, address, size);
		}

		public override void Write(uint insaddr, uint address, uint value, Size size)
		{
			scsiController.Write(insaddr, address, value, size);
		}
	}
}