using System.Collections.Generic;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface IDebugMemoryMapper 
	{
		uint FindSequence(byte[] bytes);
		byte UnsafeRead8(uint address);
		ushort UnsafeRead16(uint address);
		uint UnsafeRead32(uint address);
		uint UnsafeRead(uint address, Size size);
		void UnsafeWrite32(uint address, uint value);
		void UnsafeWrite16(uint address, ushort value);
		void UnsafeWrite8(uint address, byte value);
		void UnsafeWrite(uint address, uint value, Size size);
		IEnumerable<byte> GetEnumerable(uint start, ulong length);
		IEnumerable<uint> AsULong(uint start, ulong length);
		IEnumerable<ushort> AsUWord(uint start, ulong length);
		ulong Length { get; }
		ulong End { get; }
		List<MemoryRange> MappedRange();
		List<BulkMemoryRange> GetBulkRanges();
		string GetString(uint address);
	}
}