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
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise
{
	public class BpldatPix32AVX2V2 : IBpldatPix
	{
		private Vector256<uint> bpldatpix = Vector256<uint>.Zero;

		public void WriteBitplanes(ref ulong[] bpldat, int even, int odd)
		{
			Span<uint> bits = stackalloc uint[8];

			bits[0] = (uint)(bpldat[0] << (16 - even));
			bits[1] = (uint)(bpldat[1] << (16 - odd));
			bits[2] = (uint)(bpldat[2] << (16 - even));
			bits[3] = (uint)(bpldat[3] << (16 - odd));
			bits[4] = (uint)(bpldat[4] << (16 - even));
			bits[5] = (uint)(bpldat[5] << (16 - odd));
			bits[6] = (uint)(bpldat[6] << (16 - even));
			bits[7] = (uint)(bpldat[7] << (16 - odd));

			ref uint bitsRef = ref MemoryMarshal.GetReference(bits);
			bpldatpix |= Vector256.LoadUnsafe(ref bitsRef);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void NextPixel()
		{
			bpldatpix = Avx2.ShiftLeftLogical(bpldatpix, 1);
		}

		public void SetPixelBitMask(uint pixelBits)
		{
		}

		public void Clear()
		{
			bpldatpix = Vector256<uint>.Zero;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetPixel(int planes)
		{
			uint mask = (1u << planes) - 1;

			uint pix = (uint)Avx2.MoveMask(bpldatpix.AsSingle());
			bpldatpix = Avx2.ShiftLeftLogical(bpldatpix, 1);
			return pix & mask;
		}

		public void Save(JArray obj)
		{
			var jo = new JObject();
			jo["id"] = "pixels";
			jo.Add("pixelBitMask", 31);
			var bpldatpix32 = new uint[8];
			for (int i = 0; i < 8; i++)
				bpldatpix32[i] = bpldatpix.GetElement(i);
			jo.Add("bpldatpix", JToken.FromObject(bpldatpix32));
			obj.Add(jo);
		}

		public void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "pixels")) return;

			//pixelMaskBit = int.Parse((string)obj.GetValue("pixelBitMask"));
			var bpldatpix32 = new uint[8];
			obj.GetValue("bpldatpix")
					.Select(x => uint.Parse((string)x))
					.ToArray()
					.CopyTo(bpldatpix32, 0);
			bpldatpix = Vector256.Create(bpldatpix32);
		}
	}
}
