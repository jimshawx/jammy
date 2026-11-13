using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public abstract class Memory : IMemoryMappedDevice, IBulkMemoryRead, IDebuggableMemory
	{
		private uint _addressMask;
		protected uint addressMask {
										set
										{ 
											_addressMask = value;
											wordLimit = _addressMask - 1;
											longLimit = _addressMask - 3;
										} 
										get => _addressMask;
									}
		private uint wordLimit;
		private uint longLimit;

		protected byte[] memory;
		protected MemoryRange memoryRange;

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public virtual List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange>{memoryRange};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint Read(uint insaddr, uint address, Size size)
		{
			address &= _addressMask;
			switch (size)
			{
				case Size.Byte:
					return memory[address];

				case Size.Word:
					if (address <= wordLimit)
						return BinaryPrimitives.ReadUInt16BigEndian(memory.AsSpan((int)address));

					return (uint)((memory[address] << 8)
								 + memory[(address + 1) & _addressMask]);

				case Size.Long:
					if (address <= longLimit)
						return BinaryPrimitives.ReadUInt32BigEndian(memory.AsSpan((int)address));

					return (uint)((memory[address] << 24)
								+ (memory[(address + 1) & _addressMask] << 16)
								+ (memory[(address + 2) & _addressMask] << 8)
								+ memory[(address + 3) & _addressMask]);
			}
			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			address &= _addressMask;
			switch (size)
			{
				case Size.Byte:
					memory[address] = (byte)value;
					return;

				case Size.Word:
					if (address <= wordLimit)
					{
						BinaryPrimitives.WriteUInt16BigEndian(memory.AsSpan((int)address), (ushort)value);
						return;
					}
					memory[address] = (byte)(value >> 8);
					memory[(address + 1) & _addressMask] = (byte)value;
					return;

				case Size.Long:
					if (address <= longLimit)
					{ 
						BinaryPrimitives.WriteUInt32BigEndian(memory.AsSpan((int)address), value);
						return;
					}
					memory[address] = (byte)(value >> 24);
					memory[(address + 1) & _addressMask] = (byte)(value >> 16);
					memory[(address + 2) & _addressMask] = (byte)(value >> 8);
					memory[(address + 3) & _addressMask] = (byte)value;
					return;
			}
		}

		public uint ImmediateRead(uint insaddr, uint address, Size size)
		{
			return Read(insaddr, address, size);
		}

		public void ImmediateWrite(uint insaddr, uint address, uint value, Size size)
		{
			Write(insaddr, address, value, size);
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