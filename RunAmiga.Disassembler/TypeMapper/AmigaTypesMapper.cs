using System;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Types.AmigaTypes;

namespace RunAmiga.Disassembler.TypeMapper
{
	public static class AmigaTypesMapper
	{
		public static uint GetSize(object s)
		{
			if (s.GetType() == typeof(SByte) || s.GetType() == typeof(Byte) || s.GetType() == typeof(NodeType)) return 1;
			if (s.GetType() == typeof(Int16) || s.GetType() == typeof(UInt16)) return 2;
			if (s.GetType() == typeof(Int32) || s.GetType() == typeof(UInt32) || s.GetType() == typeof(UInt32) || s.GetType() == typeof(UInt32)) return 4;
			throw new ArgumentOutOfRangeException();
		}

		public static object MapSimple(IDebugMemoryMapper memory, Type type, uint addr)
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
}