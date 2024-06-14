/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Types.Types
{
	public enum Size
	{
		Byte,
		Word,
		Long,
		Extension
	}

	public struct Point
	{
		public int X { get; set; }
		public int Y { get; set; }
	}

	public class AddressRange
	{
		public uint Start { get; set; }
		public ulong End { get { return Start + Length; } set { Length = value - Start; } }
		public ulong Length { get; set; }

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
	}
}
