using System.Collections.Generic;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface IMemoryMappedDevice
	{
		public bool IsMapped(uint address);
		public List<MemoryRange> MappedRange();
		public uint Read(uint insaddr, uint address, Size size);
		public void Write(uint insaddr, uint address, uint value, Size size);
	}

	public interface IContendedMemoryMappedDevice
	{
		public uint ImmediateRead(uint insaddr, uint address, Size size);
		public void ImmediateWrite(uint insaddr, uint address, uint value, Size size);
	}

	public interface IBulkMemoryRead
	{
		public List<BulkMemoryRange> ReadBulk();
	}
}
