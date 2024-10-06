using System;
using System.Collections.Generic;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Jammy.Interface;
using Jammy.Types.AmigaTypes;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Disassembler.TypeMapper
{
	public class AmigaTypesMapper : IAmigaTypesMapper
	{
		private readonly IDebugMemoryMapper memory;
		public AmigaTypesMapper(IDebugMemoryMapper memory)
		{
			this.memory = memory;
		}

		public uint GetSize(object s)
		{
			if (s.GetType() == typeof(SByte) || s.GetType() == typeof(Byte) || s.GetType() == typeof(NodeType)) return 1;
			if (s.GetType() == typeof(Int16) || s.GetType() == typeof(UInt16)) return 2;
			if (s.GetType() == typeof(Int32) || s.GetType() == typeof(UInt32) || s.GetType() == typeof(UInt32) || s.GetType() == typeof(UInt32)) return 4;
			throw new ArgumentOutOfRangeException();
		}

		public object MapSimple(Type type, uint addr)
		{
			if (type == typeof(NodeType)) return (NodeType)memory.UnsafeRead8(addr);
			if (type == typeof(SByte)) return (SByte)memory.UnsafeRead8(addr);
			if (type == typeof(Byte)) return (Byte)memory.UnsafeRead8(addr);
			if (type == typeof(UInt16)) return (UInt16)memory.UnsafeRead16(addr);
			if (type == typeof(Int16)) return (Int16)memory.UnsafeRead16(addr);
			if (type == typeof(UInt32)) return (UInt32)memory.UnsafeRead32(addr);
			if (type == typeof(Int32)) return (Int32)memory.UnsafeRead32(addr);
			if (type == typeof(UInt32)) return (UInt32)memory.UnsafeRead32(addr);
			if (type == typeof(UInt32)) return (UInt32)memory.UnsafeRead32(addr);
			throw new ArgumentOutOfRangeException();
		}
	}


	public class ByteArrayDebugMemoryMapper : IDebugMemoryMapper
	{
		private readonly byte[] memory;
		private MemoryRange memoryRange;

		public ByteArrayDebugMemoryMapper(byte[] memory)
		{
			this.memory = memory;
			memoryRange = new MemoryRange(0, (ulong)memory.Length);
		}

		public uint FindSequence(byte[] bytes)
		{
			throw new NotImplementedException();
		}

		public byte UnsafeRead8(uint address)
		{
			return memory[address];
		}

		public ushort UnsafeRead16(uint address)
		{
			return (ushort)((memory[address] << 8) + memory[address + 1]);
		}

		public uint UnsafeRead32(uint address)
		{
			return (uint)((memory[address]<<24)+(memory[address + 1] << 16) + (memory[address + 2] << 8) + memory[address + 3]);
		}
		
		public uint UnsafeRead(uint address, Size size)
		{
			if (size == Size.Byte) return UnsafeRead8(address);
			if (size == Size.Word) return UnsafeRead16(address);
			return UnsafeRead32(address);
		}

		public void UnsafeWrite(uint address, uint value, Size size)
		{
			if (size == Size.Byte) UnsafeWrite8(address, (byte)value);
			if (size == Size.Word) UnsafeWrite16(address, (ushort)value);
			if (size == Size.Long) UnsafeWrite32(address, value);
		}

		public void UnsafeWrite32(uint address, uint value)
		{
			memory[address] = (byte)(value >> 24);
			memory[address + 1] = (byte)(value>>16);
			memory[address+2] = (byte)(value >> 8);
			memory[address + 3] = (byte)value;
		}

		public void UnsafeWrite16(uint address, ushort value)
		{
			memory[address] = (byte)(value >> 8);
			memory[address + 1] = (byte)value;
		}

		public void UnsafeWrite8(uint address, byte value)
		{
			memory[address] = value;
		}

		public IEnumerable<byte> GetEnumerable(uint start, ulong length)
		{
			return memory[(int)start..(int)(start+length)];
		}

		public IEnumerable<byte> GetEnumerable(uint start)
		{
			return memory[(int)start..];
		}

		public IEnumerable<uint> AsULong(uint start)
		{
			return memory.AsULong();
		}

		public IEnumerable<ushort> AsUWord(uint start)
		{
			return memory.AsUWord();
		}

		public ulong Length => (ulong)memory.Length;

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange};
		}

		public List<BulkMemoryRange> GetBulkRanges()
		{
			return new List<BulkMemoryRange>
			{
				new BulkMemoryRange {Memory = memory, Start = memoryRange.Start}
			};
		}

		public string GetString(uint str)
		{
			var sb = new StringBuilder();
			for (; ; )
			{
				byte c = UnsafeRead8(str);
				if (c == 0)
					return sb.ToString();

				sb.Append(Convert.ToChar(c));
				str++;
			}
		}
	}
}