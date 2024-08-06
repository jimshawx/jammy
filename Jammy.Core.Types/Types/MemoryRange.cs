/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

using System;

namespace Jammy.Core.Types.Types
{
	public class MemoryRange : AddressRange
	{
		public MemoryRange() : base(){ }

		public MemoryRange(uint start, ulong length) : base(start, length) { }
	}

	public class BulkMemoryRange : AddressRange
	{
		public byte[] Memory { get; set; } = [];

		public new ulong End {
			get => Start + Length;
			set => throw new NotSupportedException();
		}

		public new ulong Length {
			get => (ulong)Memory.Length;
			set => throw new NotSupportedException();
		}
	}
}
