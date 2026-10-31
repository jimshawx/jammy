
using Jammy.Core.Interface.Interfaces;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise
{
	public class BpldatPix64AVX2 : IBpldatPix
	{
		private Vector256<ulong> p03 = Vector256<ulong>.Zero; // planes 0-3, bit 63 = current pixel
		private Vector256<ulong> p47 = Vector256<ulong>.Zero; // planes 4-7
		private Vector128<ulong> shiftCount = Vector128<ulong>.Zero;

		public void WriteBitplanes(ref ulong[] bpldat, int oddeven)
		{
			// Plane i is lane i, so two straight loads cover all 8 planes.
			// (Each Create does one bounds check; length must be >= 8.)
			Vector256<ulong> d03 = Vector256.Create(bpldat, 0);
			Vector256<ulong> d47 = Vector256.Create(bpldat, 4);

			// Even planes live in lanes 0,2 of each half; odd planes in lanes 1,3.
			Vector256<ulong> mask = oddeven == 0
				? Vector256.Create(ulong.MaxValue, 0UL, ulong.MaxValue, 0UL)
				: Vector256.Create(0UL, ulong.MaxValue, 0UL, ulong.MaxValue);

			p03 |= Avx2.ShiftLeftLogical(d03 & mask, shiftCount);
			p47 |= Avx2.ShiftLeftLogical(d47 & mask, shiftCount);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void NextPixel()
		{
			// x + x == x << 1, but runs on more execution ports than a shift
			p03 = Avx2.Add(p03, p03);
			p47 = Avx2.Add(p47, p47);
		}

		public void SetPixelBitMask(uint pixelBits)
		{
			// Align the MSB of the pixelBits-wide word with bit 63.
			shiftCount = Vector128.CreateScalar((ulong)(63 - (int)pixelBits));
		}

		public void Clear()
		{
			p03 = Vector256<ulong>.Zero;
			p47 = Vector256<ulong>.Zero;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetPixel(int planes)
		{
			uint mask = (1u << planes) - 1;

			Vector256<ulong> a = p03;
			Vector256<ulong> b = p47;

			uint pix = (uint)Avx.MoveMask(a.AsDouble())
					 | ((uint)Avx.MoveMask(b.AsDouble()) << 4);

			p03 = Avx2.Add(a, a);
			p47 = Avx2.Add(b, b);

			return pix & mask;
		}

		public void Save(JArray jobj)
		{

		}

		public void Load(JObject jobj)
		{
		}
	}

}