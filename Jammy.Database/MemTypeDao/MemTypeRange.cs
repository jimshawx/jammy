using Jammy.Database.Types;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.MemTypeDao
{
	public enum RangeType
	{
		Code=1,
		Byte,
		Word,
		Long,
		Str
	}

	public class MemTypeRange : BaseDbObject
	{
		public RangeType Type { get; set; }
		public uint Address { get; set; }	
		public ulong Size { get; set; }
	}
}
