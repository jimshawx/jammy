using System.Collections.Generic;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Interface.Interfaces
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
		MemoryRange MappedRange();
		List<BulkMemoryRange> GetBulkRanges();
	}
}