/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

using System.Collections.Generic;
using System.Linq;

namespace Jammy.Core.Types.Types
{
	public enum Size
	{
		Byte,
		Word,
		Long,
		Extension,
		QWord
	}

	public struct Point
	{
		public int X { get; set; }
		public int Y { get; set; }
	}

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

		public AddressRange(){}

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
}
