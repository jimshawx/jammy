using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types.Debugger
{
	public interface IMemoryDump
	{
		int AddressToLine(uint address);
	}

	public class MemoryDump : IMemoryDump
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
			//complete hack to remove KS mirror range, if present
			this.memoryRanges.RemoveAll(x=>x.StartAddress == 0 && (x.EndAddress == 0x80000 || x.EndAddress == 0x40000));
		}

		public void ClearMapping()
		{
			addressToLine.Clear();
		}

		private int line;
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
						//the requested start is within the range
						var m = new MemRange();
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
			var sb = new StringBuilder(256000);

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

		public override string ToString()
		{
			return AllBlocksToString(new List<MemRange>
				{
					new MemRange ( 0x000000, 0x10000),
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
