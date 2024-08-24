/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Types.Types
{
	public struct FastUInt128_
	{
		private ulong lo;
		public void Or(ulong bits, int shift) { lo |= bits << shift; }
		public void Zero() { lo = 0; }
		public void SetBit(int bit) { lo = 1UL << bit; }
		public void Shl1() { lo <<= 1; }
		public bool IsBitSet(int bit) { return (lo & (1UL << bit)) != 0; }
	}

	public struct FastUInt128
	{
		private ulong hi, lo;

		public void Or(ulong bits, int shift)
		{
			hi |= bits >> (64 - shift);
			lo |= bits << shift;
		}

		public void Zero()
		{
			hi = lo = 0;
		}

		//public void Set(UInt128 bits)
		//{
		//	lo = (ulong)bits;
		//	hi = (ulong)(bits >> 64);
		//}

		public void SetBit(int bit)
		{
			if (bit >= 64)
			{
				hi = 1UL << (bit - 64);
				lo = 0;
			}
			else
			{
				hi = 0;
				lo = 1UL << bit;
			}
		}

		public void Shl1()
		{
			hi <<= 1;
			if ((long)lo < 0) hi |= 1;
			lo <<= 1;
		}

		//public bool AnyBitsSet(ref FastUInt128 other)
		//{
		//	return ((hi & other.hi) | (lo & other.lo)) != 0;
		//}

		public bool IsBitSet(int bit)
		{
			if (bit >= 64)
				return (hi & (1UL << (bit - 64))) != 0;
			return (lo & (1UL << bit)) != 0;
		}
	}
}
