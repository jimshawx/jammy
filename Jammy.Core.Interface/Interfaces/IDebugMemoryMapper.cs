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
		void UnsafeWrite32(uint address, uint value);
		void UnsafeWrite16(uint address, ushort value);
		void UnsafeWrite8(uint address, byte value);
		IEnumerable<byte> GetEnumerable(int start, long length);
		IEnumerable<byte> GetEnumerable(int start);
		IEnumerable<uint> AsULong(int start);
		IEnumerable<ushort> AsUWord(int start);
		int Length { get; }
		List<MemoryRange> MappedRange();
		List<BulkMemoryRange> GetBulkRanges();
	}
}