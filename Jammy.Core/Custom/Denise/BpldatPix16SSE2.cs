using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Newtonsoft.Json.Linq;
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
	public class BpldatPix16SSE2 : IBpldatPix
	{
		private Vector128<ushort> bpldatpix = Vector128<ushort>.Zero;

		public void WriteBitplanes(ref ulong[] bpldat, int oddeven)
		{
			ref ulong p = ref MemoryMarshal.GetArrayDataReference(bpldat);

			if (oddeven == 0)
			{
				bpldatpix |= Vector128.Create(
					(ushort)p, 0,
					(ushort)Unsafe.Add(ref p, 2), 0,
					(ushort)Unsafe.Add(ref p, 4), 0,
					(ushort)Unsafe.Add(ref p, 6), 0);
			}
			else if (oddeven == 1)
			{
				bpldatpix |= Vector128.Create(
					0, (ushort)Unsafe.Add(ref p, 1),
					0, (ushort)Unsafe.Add(ref p, 3),
					0, (ushort)Unsafe.Add(ref p, 5),
					0, (ushort)Unsafe.Add(ref p, 7));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void NextPixel()
		{
			//bpldatpix = Sse2.ShiftLeftLogical(bpldatpix, 1);
			//bpldatpix <<= 1;
			bpldatpix += bpldatpix;
		}

		public void SetPixelBitMask(uint pixelBits)
		{
		}

		public void Clear()
		{
			bpldatpix = Vector128<ushort>.Zero;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public uint GetPixel(int planes)
		{
			uint mask = (1u << planes) - 1;

			// Pack 8x int16 -> 8x int8 (sign bit preserved by signed saturation).
			// Packing with Zero fills the upper 8 bytes with 0, so MoveMask's
			// upper 8 bits are guaranteed clear.
			Vector128<sbyte> packed = Sse2.PackSignedSaturate(bpldatpix.AsInt16(), Vector128<short>.Zero);
			uint pix = (uint)Sse2.MoveMask(packed.AsByte());

			//bpldatpix = Sse2.ShiftLeftLogical(bpldatpix, 1);
			//bpldatpix <<= 1;
			bpldatpix += bpldatpix;
			return pix & mask;
		}

		public void Save(JArray obj)
		{
			var jo = new JObject();
			jo["id"] = "pixels";
			jo.Add("pixelBitMask", 31);
			var bpldatpix32 = new ushort[8];
			for (int i = 0; i < 8; i++)
				bpldatpix32[i] = bpldatpix.GetElement(i);
			jo.Add("bpldatpix", JToken.FromObject(bpldatpix32));
			obj.Add(jo);
		}

		public void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "pixels")) return;

			//pixelMaskBit = int.Parse((string)obj.GetValue("pixelBitMask"));
			var bpldatpix32 = new ushort[8];
			obj.GetValue("bpldatpix")
					.Select(x => uint.Parse((string)x))
					.ToArray()
					.CopyTo(bpldatpix32, 0);
			bpldatpix = Vector128.Create(bpldatpix32);
		}
	}
}
