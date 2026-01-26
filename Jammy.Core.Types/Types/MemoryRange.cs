using System;
using System.Collections.Generic;
using System.Linq;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Types.Types
{
	//simple MemoryRange class used by the emulation
	public class MemoryRange
	{
		public MemoryRange(uint start, ulong length)
		{
			Start = start;
			Length = length;
		}

		public uint Start { get; set; }
		public ulong Length { get; set; }

		private ulong End => Start + Length;

		public bool Contains(uint location)
		{
			return location >= Start && location < End;
		}
	}

	//more complex classes used by the front end and debuggers
	public class AddressRange
	{
		/// <summary>
		/// inclusive start position
		/// </summary>
		public uint Start { get; set; }

		/// <summary>
		/// exclusive (one past the end) end position
		/// </summary>
		public ulong End { get { return Start + Length; } set { Length = value - Start; } }

		public ulong Length { get; set; }

		public AddressRange() { }

		public AddressRange(uint start, ulong length)
		{
			this.Start = start;
			this.Length = length;
		}

		public bool Overlaps(AddressRange other)
		{
			return !(this.Start >= other.End || this.End <= other.Start);
		}

		public bool Contains(AddressRange other)
		{
			return other.Start >= this.Start && other.End <= this.End;
		}

		public bool Contains(uint location)
		{
			return location >= Start && location <= End;
		}

		public bool IsEmpty()
		{
			return Start == End;
		}

		public static List<AddressRange> NoOverlaps(List<AddressRange> ranges)
		{
			var merge = new List<AddressRange>();

			foreach (var incoming in ranges)
			{
				//remove any existing ranges completely contained in the incoming
				merge.RemoveAll(x => incoming.Contains(x));

				//incoming doesn't overlap anything, add it, and we're done
				if (!merge.Any(x => x.Overlaps(incoming))) { merge.Add(incoming); continue; }

				//incoming is contained entirely within another, ignore it
				if (merge.Any(x => x.Contains(incoming))) continue;

				//it partly overlaps one or more existing ranges
				foreach (var merged in merge)
				{
					if (incoming.Overlaps(merged))
					{
						//that means incoming contains the start or the end of merged

						//it contains the start, so extend it to the end
						if (merged.Contains(incoming.Start))
							merged.End = incoming.End;
						else
							merged.Start = incoming.Start;
					}
				}
			}
			return merge;
		}
	}

	public class BulkMemoryRange : AddressRange
	{
		public byte[] Memory { get; set; } = [];

		public new ulong End
		{
			get => Start + Length;
			set => throw new NotSupportedException();
		}

		public new ulong Length
		{
			get => (ulong)Memory.Length;
			set => throw new NotSupportedException();
		}
	}

	public interface IAddressRanges
	{
		void Add(AddressRange range);
		void Add(uint start, ulong size);
		void AddRange(List<AddressRange> ranges);
		List<AddressRange> GetRanges();
	}

	public class AddressRanges
	{
		private readonly List<AddressRange> ranges = new List<AddressRange>();

		public void Add(uint start, ulong length)
		{
			ranges.Add(new AddressRange(start, length));
		}

		public void Add(AddressRange range)
		{
			ranges.Add(range);
		}

		public void AddRange(List<AddressRange> ranges)
		{
			ranges.AddRange(ranges);
		}

		public List<AddressRange> GetRanges()
		{
			//tidy up the ranges on demand
			var tmp = AddressRange.NoOverlaps(ranges);
			ranges.Clear();
			ranges.AddRange(tmp);

			return ranges;
		}
	}
}

