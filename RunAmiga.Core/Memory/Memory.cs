using System;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{
	public abstract class Memory : IMemoryMappedDevice
	{
		private protected uint addressMask = 0;
		private protected byte[] memory;
		private protected MemoryRange memoryRange;

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

		public MemoryRange MappedRange()
		{
			return memoryRange;
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
	}
}