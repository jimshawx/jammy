using System;
using System.Collections.Generic;
using RunAmiga.Extensions.Extensions;

namespace RunAmiga.Core.Types.Types
{
	public class ByteArrayWrapper
	{
		private readonly byte[] mem;

		public ByteArrayWrapper(byte[] mem)
		{
			this.mem = mem;
		}

		public int Length => mem.Length;

		public uint ReadLong(uint i)
		{
			return (uint)((mem[i] << 24) + (mem[i + 1] << 16) + (mem[i + 2] << 8) + mem[i + 3]);
		}

		public ushort ReadWord(uint i)
		{
			return (ushort)((mem[i] << 8) + mem[i + 1]);
		}

		public byte ReadByte(uint i)
		{
			return mem[i];
		}

		public ReadOnlySpan<byte> GetSpan()
		{
			return new ReadOnlySpan<byte>(mem);
		}

		public IEnumerable<uint> AsULong()
		{
			return mem.AsULong();
		}

		public IEnumerable<ushort> AsUWord()
		{
			return mem.AsUWord();
		}
	}
}