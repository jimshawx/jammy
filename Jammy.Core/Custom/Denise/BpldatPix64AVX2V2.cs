
using Jammy.Core.Interface.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise
{
	public class BpldatPix64AVX2V2 : IBpldatPix
	{
		private Vector256<ulong> hi03 = Vector256<ulong>.Zero; // Planes 0-3 (High 64 bits)
		private Vector256<ulong> hi47 = Vector256<ulong>.Zero; // Planes 4-7 (High 64 bits)
		private Vector256<ulong> lo03 = Vector256<ulong>.Zero; // Planes 0-3 (Low 64 bits)
		private Vector256<ulong> lo47 = Vector256<ulong>.Zero; // Planes 4-7 (Low 64 bits)

		private int pixelShiftBits = 0;

		public void WriteBitplanes(ref ulong[] bpldat, int even, int odd)
		{
			Span<ulong> hiBits = stackalloc ulong[8];
			Span<ulong> loBits = stackalloc ulong[8];

			ulong d0_even = bpldat[0] << pixelShiftBits;
			ulong d1_odd = bpldat[1] << pixelShiftBits;
			ulong d2_even = bpldat[2] << pixelShiftBits;
			ulong d3_odd = bpldat[3] << pixelShiftBits;
			ulong d4_even = bpldat[4] << pixelShiftBits;
			ulong d5_odd = bpldat[5] << pixelShiftBits;
			ulong d6_even = bpldat[6] << pixelShiftBits;
			ulong d7_odd = bpldat[7] << pixelShiftBits;

			// 2. Apply the scroll shift across the 128-bit boundary
			hiBits[0] = even == 0 ? d0_even : d0_even >> even;
			loBits[0] = even == 0 ? 0ul : d0_even << (64 - even);

			hiBits[1] = odd == 0 ? d1_odd : d1_odd >> odd;
			loBits[1] = odd == 0 ? 0ul : d1_odd << (64 - odd);

			hiBits[2] = even == 0 ? d2_even : d2_even >> even;
			loBits[2] = even == 0 ? 0ul : d2_even << (64 - even);

			hiBits[3] = odd == 0 ? d3_odd : d3_odd >> odd;
			loBits[3] = odd == 0 ? 0ul : d3_odd << (64 - odd);

			hiBits[4] = even == 0 ? d4_even : d4_even >> even;
			loBits[4] = even == 0 ? 0ul : d4_even << (64 - even);

			hiBits[5] = odd == 0 ? d5_odd : d5_odd >> odd;
			loBits[5] = odd == 0 ? 0ul : d5_odd << (64 - odd);

			hiBits[6] = even == 0 ? d6_even : d6_even >> even;
			loBits[6] = even == 0 ? 0ul : d6_even << (64 - even);

			hiBits[7] = odd == 0 ? d7_odd : d7_odd >> odd;
			loBits[7] = odd == 0 ? 0ul : d7_odd << (64 - odd);

			ref ulong hiRef = ref MemoryMarshal.GetReference(hiBits);
			ref ulong loRef = ref MemoryMarshal.GetReference(loBits);

			hi03 = Avx2.Or(hi03, Vector256.LoadUnsafe(ref hiRef));
			hi47 = Avx2.Or(hi47, Vector256.LoadUnsafe(ref hiRef, 4));
			lo03 = Avx2.Or(lo03, Vector256.LoadUnsafe(ref loRef));
			lo47 = Avx2.Or(lo47, Vector256.LoadUnsafe(ref loRef, 4));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void NextPixel()
		{
			// SHL 1
			var carry03 = Avx2.ShiftRightLogical(lo03, 63);
			hi03 = Avx2.ShiftLeftLogical(hi03, 1);
			hi03 = Avx2.Or(hi03, carry03);
			lo03 = Avx2.ShiftLeftLogical(lo03, 1);

			var carry47 = Avx2.ShiftRightLogical(lo47, 63);
			hi47 = Avx2.ShiftLeftLogical(hi47, 1);
			hi47 = Avx2.Or(hi47, carry47);
			lo47 = Avx2.ShiftLeftLogical(lo47, 1);
		}

		public void SetPixelBitMask(uint pixelBits)
		{
			pixelShiftBits = 63 - (int)pixelBits;
		}

		public void Clear()
		{
			hi03 = Vector256<ulong>.Zero;
			hi47 = Vector256<ulong>.Zero;
			lo03 = Vector256<ulong>.Zero;
			lo47 = Vector256<ulong>.Zero;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetPixel(int planes)
		{
			uint mask = (1u << planes) - 1;

			uint pix03 = (uint)Avx2.MoveMask(hi03.AsDouble());
			uint pix47 = (uint)Avx2.MoveMask(hi47.AsDouble());

			uint pix = pix03 | (pix47 << 4);

			NextPixel();

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