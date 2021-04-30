using System;
using System.Collections.Generic;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;
using RunAmiga.Extensions.Extensions;
using RunAmiga.Interface;
using RunAmiga.Types.AmigaTypes;

namespace RunAmiga.Disassembler.TypeMapper
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
			memoryRange = new MemoryRange(0, (uint)memory.Length);
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

		public IEnumerable<byte> GetEnumerable(int start, long length)
		{
			return memory[start..(int)(start+length)];
		}

		public IEnumerable<byte> GetEnumerable(int start)
		{
			return memory[start..];
		}

		public IEnumerable<uint> AsULong(int start)
		{
			return memory.AsULong();
		}

		public IEnumerable<ushort> AsUWord(int start)
		{
			return memory.AsUWord();
		}

		public int Length => memory.Length;

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange};
		}

		public List<BulkMemoryRange> GetBulkRanges()
		{
			return new List<BulkMemoryRange>
			{
				new BulkMemoryRange {Memory = memory, StartAddress = memoryRange.Start}
			};
		}
	}
}