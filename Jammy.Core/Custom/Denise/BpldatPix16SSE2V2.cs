using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise
{
	public class BpldatPix16SSE2V2 : IBpldatPix
	{
		private const ulong Lsb = 0x0101010101010101UL;

		// The next 16 pixels, one byte each, bit p = plane p.
		private Vector128<byte> ring = Vector128<byte>.Zero;

		// Slot consumed most recently. The next pixel out is at (h - 1) & 15.
		private int h = 0;

		// Fallback for CPUs without BMI2: Expand[b] has bit k of b in bit 0 of byte k.
		private static readonly ulong[] Expand = BuildExpand();

		private static ulong[] BuildExpand()
		{
			var t = new ulong[256];
			for (int b = 0; b < 256; b++)
				for (int k = 0; k < 8; k++)
					t[b] |= (ulong)((b >> k) & 1) << (8 * k);
			return t;
		}

		public void WriteBitplanes(ref ulong[] bpldat, int oddeven)
		{
			ref ulong p = ref MemoryMarshal.GetArrayDataReference(bpldat); // needs >= 8 elements

			// Transpose: bit k of each plane word goes to bit <plane> of byte k.
			// Byte 15 (word bit 15, the MSB) is therefore the first pixel out.
			ulong lo = 0, hi = 0;

			if (Bmi2.X64.IsSupported)
			{
				for (int plane = oddeven; plane < 8; plane += 2)
				{
					ulong w = Unsafe.Add(ref p, plane);
					ulong m = Lsb << plane;
					lo |= Bmi2.X64.ParallelBitDeposit(w, m);      // word bits 0-7  -> bytes 0-7
					hi |= Bmi2.X64.ParallelBitDeposit(w >> 8, m); // word bits 8-15 -> bytes 8-15
				}
			}
			else
			{
				ref ulong t = ref MemoryMarshal.GetArrayDataReference(Expand);
				for (int plane = oddeven; plane < 8; plane += 2)
				{
					ulong w = Unsafe.Add(ref p, plane);
					lo |= Unsafe.Add(ref t, (int)(byte)w) << plane;
					hi |= Unsafe.Add(ref t, (int)(byte)(w >> 8)) << plane;
				}
			}

			// Rotate so byte 15 lands on the next slot to be read, then OR in.
			// ring[(k + h) & 15] = fresh[k]
			Vector128<byte> fresh = Vector128.Create(lo, hi).AsByte();
			Vector128<byte> ctl = (Vector128<byte>.Indices - Vector128.Create((byte)h)) & Vector128.Create((byte)15);
			ring |= Ssse3.Shuffle(fresh, ctl);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ref byte Slot(int i) => ref Unsafe.Add(ref Unsafe.As<Vector128<byte>, byte>(ref ring), i);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void NextPixel()
		{
			int i = (h - 1) & 15;
			h = i;
			Slot(i) = 0; // consumed; later writes must OR into zeros
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetPixel(int planes)
		{
			int i = (h - 1) & 15;
			h = i;
			ref byte slot = ref Slot(i);
			uint pix = slot;
			slot = 0;
			return pix & ((1u << planes) - 1);
		}

		public void SetPixelBitMask(uint pixelBits) { }

		public void Clear()
		{
			ring = Vector128<byte>.Zero;
			h = 0;
		}

		public void Save(JArray obj)
		{
			var jo = new JObject();
			jo["id"] = "pixels";
			jo.Add("pixelBitMask", 31);
			var bpldatpix32 = new byte[8];
			for (int i = 0; i < 8; i++)
				bpldatpix32[i] = ring.GetElement(i);
			jo.Add("bpldatpix", JToken.FromObject(bpldatpix32));
			obj.Add(jo);
		}

		public void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "pixels")) return;

			//pixelMaskBit = int.Parse((string)obj.GetValue("pixelBitMask"));
			var bpldatpix32 = new byte[8];
			obj.GetValue("bpldatpix")
					.Select(x => uint.Parse((string)x))
					.ToArray()
					.CopyTo(bpldatpix32, 0);
			ring = Vector128.Create(bpldatpix32);
		}
	}
}
