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
		string GetString(List<AddressRange> rng);
		string GetString(uint start, ulong length);
		void ClearMapping();
	}

	public class MemoryDump : IMemoryDump
	{
		private readonly Dictionary<uint, int> addressToLine = new Dictionary<uint, int>();
		private readonly List<BulkMemoryRange> bulkMemoryRanges = new List<BulkMemoryRange>();

		public MemoryDump(IEnumerable<byte> b)
		{
			bulkMemoryRanges.Add(new BulkMemoryRange { Memory = b.ToArray(), Start = 0 });
		}

		public MemoryDump(List<BulkMemoryRange> ranges)
		{
			this.bulkMemoryRanges = ranges.Where(x=>x.Memory.Length > 0).OrderBy(x=>x.Start).ToList();
		}

		public void ClearMapping()
		{
			addressToLine.Clear();
		}

		private int line;
		private string AllBlocksToString(List<AddressRange> ranges)
		{
			ranges = AddressRange.NoOverlaps(ranges);

			line = 0;
			var sb = new StringBuilder();
			foreach (var range in ranges.OrderBy(x => x.Start))
			{
				foreach (var bulkMemory in bulkMemoryRanges)
				{
					//intersect the two ranges, and write that block
					if (range.Start >= bulkMemory.Start && range.Start < bulkMemory.End)
					{
						//the requested start is within the range
						var m = new AddressRange();
						m.Start = range.Start;

						if (range.End < bulkMemory.End)
						{
							//the reqested end is within the range
							m.Length = range.End - m.Start;
						}
						else
						{
							m.Length = bulkMemory.End - m.Start;
						}

						m.Start -= bulkMemory.Start;

						sb.Append(BlockToString(m, bulkMemory.Start, bulkMemory.Memory));
					}
					else if (range.End > bulkMemory.Start && range.End <= bulkMemory.End)
					{
						var m = new AddressRange();                     
						//the requested range end is within the range

						if (range.Start >= bulkMemory.Start)
						{
							//the requested start is within the range
							m.Start = range.Start;
						}
						else
						{
							m.Start = bulkMemory.Start;
						}

						m.Length = range.End - m.Start;
						m.Start -= bulkMemory.Start;

						sb.Append(BlockToString(m, bulkMemory.Start, bulkMemory.Memory));
					}
				}
			}

			return sb.ToString();
		}

		private string BlockToString(AddressRange range, uint baseAddress, byte[] memory)
		{
			var sb = new StringBuilder(256000);

			uint start = range.Start;
			ulong size = range.Length;

			if (addressToLine.ContainsKey(baseAddress + start))
				return string.Empty;

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

		public string GetString(List<AddressRange> rng)
		{
			return AllBlocksToString(rng);
		}

		public string GetString(uint start, ulong length)
		{
			return AllBlocksToString(new List<AddressRange>
			{
				new AddressRange (start, length),
			});
		}
	}
}
