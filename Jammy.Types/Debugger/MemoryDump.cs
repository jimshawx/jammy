using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jammy.Core.Types.Types;

namespace Jammy.Types.Debugger
{
	public class MemoryDump
	{
		private class MemRange
		{
			public MemRange()
			{
			}

			public MemRange(uint start, ulong size)
			{
				this.start = start;
				this.size = size;
			}

			public uint start;
			public ulong size;
			public uint end => (uint)(start + size - 1);
		}

		private readonly Dictionary<uint, int> addressToLine = new Dictionary<uint, int>();
		private readonly List<BulkMemoryRange> memoryRanges = new List<BulkMemoryRange>();

		public MemoryDump(IEnumerable<byte> b)
		{
			memoryRanges.Add(new BulkMemoryRange { Memory = b.ToArray(), StartAddress = 0 });
		}

		public MemoryDump(List<BulkMemoryRange> ranges)
		{
			this.memoryRanges = ranges.Where(x=>x.Memory.Length > 0).OrderBy(x=>x.StartAddress).ToList();
		}

		public void ClearMapping()
		{
			addressToLine.Clear();
		}

		int line;
		private string AllBlocksToString(List<MemRange> ranges)
		{
			line = 0;
			var sb = new StringBuilder();
			foreach (var range in ranges.OrderBy(x => x.start))
			{
				foreach (var memoryRange in memoryRanges)
				{
					//intersect the two ranges, and write that block
					if (range.start >= memoryRange.StartAddress && range.start < memoryRange.EndAddress)
					{
						var m = new MemRange();                     //the requested start is within the range
						m.start = range.start;

						if (range.end < memoryRange.EndAddress)
						{
							//the reqested end is within the range
							m.size = range.end - m.start + 1;
						}
						else
						{
							m.size = memoryRange.EndAddress - m.start + 1;
						}

						m.start -= memoryRange.StartAddress;

						sb.Append(BlockToString(m, memoryRange.StartAddress, memoryRange.Memory));
					}
					else if (range.end >= memoryRange.StartAddress && range.end < memoryRange.EndAddress)
					{
						var m = new MemRange();                     
						//the requested range end is within the range

						if (range.start >= memoryRange.StartAddress)
						{
							//the requested start is within the range
							m.start = range.start;
						}
						else
						{
							m.start = memoryRange.StartAddress;
						}

						m.size = range.end - m.start + 1;
						m.start -= memoryRange.StartAddress;

						sb.Append(BlockToString(m, memoryRange.StartAddress, memoryRange.Memory));
					}
				}
			}

			return sb.ToString();
		}

		private string BlockToString(MemRange range, uint baseAddress, byte[] memory)
		{
			var sb = new StringBuilder();

			uint start = range.start;
			ulong size = range.size;

			for (ulong i = start; i < start + size-32; i += 32)
			{
				addressToLine.Add((uint)(i+baseAddress), line++);
				sb.Append($"{i+baseAddress:X8} ");
				for (uint k = 0; k < 4; k++)
				{
					for (uint j = 0; j < 8; j++)
					{
						sb.Append($"{memory[i + k * 8 + j]:X2}");
					}
					sb.Append(" ");
				}

				sb.Append("  ");

				for (uint k = 0; k < 32; k++)
				{
					byte c = memory[i + k];
					if (c < 31 || c >= 127) c = (byte)'.';
					sb.Append($"{Convert.ToChar(c)}");
				}

				sb.Append("\n");
			}
			return sb.ToString();
		}

		public int AddressToLine(uint address)
		{
			for (uint i = address; i >= address - 32; i--)
			{
				if (addressToLine.TryGetValue(i, out int v))
					return v;
			}
			return 0;
		}

		//public byte Read8(uint address)
		//{
		//	//if (address >= 0x1000000) { logger.LogTrace($"Memory Read Byte from {address:X8}"); return 0; }
		//	return memory[address];
		//}

		//public ushort Read16(uint address)
		//{
		//	//if (address >= 0xfffffe) { logger.LogTrace($"Memory Read Word from ${address:X8}"); return 0; }
		//	//if ((address & 1) != 0) { logger.LogTrace($"Memory Read Unaligned Word from ${address:X8}"); return 0; }
		//	return (ushort)(((ushort)memory[address] << 8) +
		//					(ushort)memory[(address + 1)]);
		//}

		public uint Read32(uint address)
		{
			foreach (var b in memoryRanges)
			{
				if (address >= b.StartAddress && address+4 < b.EndAddress)
				{
					var memory = b.Memory;
					address -= b.StartAddress;

					return ((uint)memory[address] << 24) +
					       ((uint)memory[(address + 1)] << 16) +
					       ((uint)memory[(address + 2)] << 8) +
					       (uint)memory[(address + 3)];

				}
			}
			return 0;
		}

		public override string ToString()
		{
			return AllBlocksToString(new List<MemRange>
				{
					new MemRange ( 0x000000, 0xc000),
					new MemRange ( 0xc00000, 0xa000),
					new MemRange ( 0xf80000, 0x40000),
					new MemRange ( 0xfc0000, 0x40000)
				});
		}

		public string ToString(uint start, ulong length)
		{
			return AllBlocksToString(new List<MemRange>
			{
				new MemRange (start, length),
			});
		}
	}
}
