/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types
{
	public enum MemType : byte
	{
		Unknown,
		Code,
		Byte,
		Word,
		Long,
		Str
	}

	public class MemTypeCollection
	{
		public const int MEMTYPE_BLOCKSIZE = 1 << MEMTYPE_SHIFT;
		public const int MEMTYPE_MASK = MEMTYPE_BLOCKSIZE - 1;
		public const int MEMTYPE_SHIFT = 20;
		public const int MEMTYPE_NUM_BLOCKS = 1 << (32 - MEMTYPE_SHIFT);

		public MemType[][] memTypes;

		public MemTypeCollection(MemType[][] memTypes)
		{
			this.memTypes = memTypes;
		}

		public MemType this[int i]
		{
			get
			{
				var block = memTypes[(uint)i >> MEMTYPE_SHIFT];
				if (block == null) return MemType.Unknown;
				return block[i & MEMTYPE_MASK];
			}
		}

		public MemType this[uint i] => this[(int)i];
	}
}
