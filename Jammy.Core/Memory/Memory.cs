using System;
using System.Collections.Generic;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public abstract class Memory : IMemoryMappedDevice, IBulkMemoryRead, IDebuggableMemory
	{
		protected uint addressMask = 0;
		protected byte[] memory;
		protected MemoryRange memoryRange;

		//protected Memory(uint addressMask, byte[] memory, MemoryRange memoryRange)
		//{
		//	this.addressMask = addressMask;
		//	this.memory = memory;
		//	this.memoryRange = memoryRange;
		//}

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public virtual List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange>{memoryRange};
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size == Size.Word) return (uint)((memory[address & addressMask] << 8)
			                                     + memory[(address + 1) & addressMask]);

			if (size == Size.Byte) return memory[address & addressMask];

			if (size == Size.Long) return (uint)((memory[address & addressMask] << 24)
			                                     + (memory[(address + 1) & addressMask] << 16)
			                                     + (memory[(address + 2) & addressMask] << 8)
			                                     + memory[(address + 3) & addressMask]);

			throw new ArgumentOutOfRangeException();
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size == Size.Word)
			{
				memory[address & addressMask] = (byte)(value >> 8);
				memory[(address + 1) & addressMask] = (byte)value;
				return;
			}

			if (size == Size.Byte)
			{
				memory[address & addressMask] = (byte)value;
				return;
			}

			if (size == Size.Long)
			{
				memory[address & addressMask] = (byte)(value >> 24);
				memory[(address + 1) & addressMask] = (byte)(value >> 16);
				memory[(address + 2) & addressMask] = (byte)(value >> 8);
				memory[(address + 3) & addressMask] = (byte)value;
				return;
			}

			throw new ArgumentOutOfRangeException();
		}

		public virtual List<BulkMemoryRange> ReadBulk()
		{
			return new List<BulkMemoryRange>
				{
					new BulkMemoryRange
					{
						Start = memoryRange.Start,
						Memory = (byte[])memory.Clone()
					}
				};
		}

		public uint DebugRead(uint address, Size size)
		{
			return Read(0, address, size);
		}

		public void DebugWrite(uint address, uint value, Size size)
		{
			Write(0, address, value, size);
		}
	}
}