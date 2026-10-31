
using Jammy.Core.Interface.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise
{
	public class BpldatPix64AVX2V2 : IBpldatPix
	{
		// One byte per pixel, bit p = plane p. Slots 0-63 hold the pixels still to come;
		// 64-127 is overflow room for a write that starts late in the window.
		private readonly byte[] buf = new byte[128];
		private int p; // slot of the next pixel out, 0-63

		// pshufb controls: output byte i takes the source byte holding word bit (width-1-i).
		private Vector256<byte> shufA; // output pixels 0-31
		private Vector256<byte> shufB; // output pixels 32-63 (64-bit words only)
		private bool wide;

		// Output byte i tests bit (7 - (i & 7)) of its source byte: 0x80, 0x40, ... 0x01
		private static readonly Vector256<byte> BitSel =
			Vector256.Create(0x0102040810204080UL).AsByte();

		public BpldatPix64AVX2V2() => SetPixelBitMask(63);

		public void SetPixelBitMask(uint pixelBits)
		{
			const ulong L = 0x0101010101010101UL; // L * n = source byte n, eight times
			const ulong Z = 0x8080808080808080UL; // pshufb writes 0 when bit 7 of the control is set

			switch (pixelBits) // width - 1
			{
				case 15: // 16 pixels: source bytes 1, 0
					shufA = Vector256.Create(L * 1, L * 0, Z, Z).AsByte();
					wide = false;
					break;
				case 31: // 32 pixels: source bytes 3, 2, 1, 0
					shufA = Vector256.Create(L * 3, L * 2, L * 1, L * 0).AsByte();
					wide = false;
					break;
				default: // 63: 64 pixels: source bytes 7..4, then 3..0
					shufA = Vector256.Create(L * 7, L * 6, L * 5, L * 4).AsByte();
					shufB = Vector256.Create(L * 3, L * 2, L * 1, L * 0).AsByte();
					wide = true;
					break;
			}
		}

		// Plane contribution to 32 pixel bytes: bit (planeBit) set where the word bit is set.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<byte> Spread(Vector256<byte> word, Vector256<byte> ctl, Vector256<byte> planeBit)
		{
			Vector256<byte> sel = Avx2.Shuffle(word, ctl) & BitSel;   // isolate each pixel's bit
			return Avx2.CompareEqual(sel, BitSel) & planeBit;         // 0xFF if set -> plane bit
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Merge(ref ulong src, int first) // first = 0 (even planes) or 1 (odd planes)
		{
			// vpbroadcastq: the whole word lands in both 128-bit lanes, so in-lane pshufb can reach it.
			Vector256<byte> w0 = Vector256.Create(Unsafe.Add(ref src, first)).AsByte();
			Vector256<byte> w1 = Vector256.Create(Unsafe.Add(ref src, first + 2)).AsByte();
			Vector256<byte> w2 = Vector256.Create(Unsafe.Add(ref src, first + 4)).AsByte();
			Vector256<byte> w3 = Vector256.Create(Unsafe.Add(ref src, first + 6)).AsByte();

			Vector256<byte> bit0 = Vector256.Create((byte)(1 << first));
			Vector256<byte> bit1 = Vector256.Create((byte)(4 << first));
			Vector256<byte> bit2 = Vector256.Create((byte)(16 << first));
			Vector256<byte> bit3 = Vector256.Create((byte)(64 << first));

			ref byte dst = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buf), p);

			Vector256<byte> a = Spread(w0, shufA, bit0) | Spread(w1, shufA, bit1)
							  | Spread(w2, shufA, bit2) | Spread(w3, shufA, bit3);
			(Vector256.LoadUnsafe(ref dst) | a).StoreUnsafe(ref dst);

			if (wide)
			{
				Vector256<byte> b = Spread(w0, shufB, bit0) | Spread(w1, shufB, bit1)
								  | Spread(w2, shufB, bit2) | Spread(w3, shufB, bit3);
				(Vector256.LoadUnsafe(ref dst, 32) | b).StoreUnsafe(ref dst, 32);
			}
		}

		public void WriteBitplanes(ref ulong[] bpldat, int oddeven)
		{
			ref ulong src = ref MemoryMarshal.GetArrayDataReference(bpldat);
			if (oddeven == 0) Merge(ref src, 0);
			else if (oddeven == 1) Merge(ref src, 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetPixel(int planes)
		{
			ref byte slot = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buf), p);
			uint pix = slot;
			slot = 0; // consumed; later writes OR into zeros
			Advance();
			return pix & ((1u << planes) - 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void NextPixel()
		{
			Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(buf), p) = 0;
			Advance();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Advance()
		{
			int i = p + 1;
			if (i == 64) { Rebase(); i = 0; }
			p = i;
		}

		// Slots 0-63 are all consumed (and zeroed) by now, so slide the overflow down.
		[MethodImpl(MethodImplOptions.NoInlining)]
		private void Rebase()
		{
			ref byte r = ref MemoryMarshal.GetArrayDataReference(buf);
			Vector256.LoadUnsafe(ref r, 64).StoreUnsafe(ref r);
			Vector256.LoadUnsafe(ref r, 96).StoreUnsafe(ref r, 32);
			Vector256<byte>.Zero.StoreUnsafe(ref r, 64);
			Vector256<byte>.Zero.StoreUnsafe(ref r, 96);
		}

		public void Clear()
		{
			Array.Clear(buf);
			p = 0;
		}

		public void Save(JArray jobj)
		{

		}

		public void Load(JObject jobj)
		{
		}
	}

}