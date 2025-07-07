using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise;

public class BpldatPix32AVX2 : IBpldatPix
{
	[Persist]
	private int pixelMaskBit;

	//[Persist] //handled manually
	private Vector256<uint> bpldatpix = Vector256<uint>.Zero;

	private readonly uint[] bits = new uint[8];

	public void WriteBitplanes(ref ulong[] bpldat, int even, int odd)
	{
		bits[0] = (uint)(bpldat[0] << (16 - even));
		bits[1] = (uint)(bpldat[1] << (16 - odd));
		bits[2] = (uint)(bpldat[2] << (16 - even));
		bits[3] = (uint)(bpldat[3] << (16 - odd));
		bits[4] = (uint)(bpldat[4] << (16 - even));
		bits[5] = (uint)(bpldat[5] << (16 - odd));
		bits[6] = (uint)(bpldat[6] << (16 - even));
		bits[7] = (uint)(bpldat[7] << (16 - odd));
		var newbits = Vector256.Create(bits);
		bpldatpix |= newbits;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void NextPixel()
	{
		bpldatpix <<= 1;
	}

	public void SetPixelBitMask(uint pixelBits)
	{
		pixelMaskBit = (int)(pixelBits + 16);
	}

	public void Clear()
	{
		bpldatpix = Vector256<uint>.Zero;
	}

	private static readonly Vector256<uint> index = Vector256.Create(7+24u,6+24u,5+24u,4+24u,3+24u,2+24u,1+24u,0+24u);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint GetPixel(int planes)
	{
		//uint pix = 0;
		//uint b = 1;
		//for (int i = 0; i < planes; i++, b <<= 1)
		//	pix |= (IsBitSet(ref bpldatpix[i], pixelMaskBit) ? b : 0);
		//return pix;

		var pixelMask = Vector256.Create((uint)(1 << pixelMaskBit));
		//var index = Vector256<uint>.Indices;
		//var index = Vector256.Create(7u,6u,5u,4u,3u,2u,1u,0u);

		//var pix = (bpldatpix & pixelMask) >> index;
		var pixelBits = Avx2.ShiftRightLogicalVariable(Avx2.And(bpldatpix, pixelMask), index);

		//Vector128<uint> or128 = Sse2.Or(pixelBits.GetLower(), pixelBits.GetUpper());
		//Vector128<uint> or64 = Sse2.Or(or128, Sse2.ShiftRightLogical128BitLane(or128, 8));
		//Vector128<uint> pix = Sse2.Or(or64, Sse2.ShiftRightLogical(or64, 32));
		//return pix.ToScalar();// >> 24;

		Vector128<uint> or128 = Sse2.Or(pixelBits.GetLower(), pixelBits.GetUpper());

		Vector128<uint> shuf1 = Sse2.Shuffle(or128, 0b_10_11_00_01); // [2,3,0,1]
		Vector128<uint> or64 = Sse2.Or(or128, shuf1);

		Vector128<uint> shuf2 = Sse2.Shuffle(or64, 0b_01_00_11_10); // [1,0,3,2]
		Vector128<uint> pix = Sse2.Or(or64, shuf2);

		return pix.ToScalar();// >> 24;
	}

	public void Save(JArray jo)
	{
		//jo.Add("bpldatpix", JToken.FromObject(bpldatpix));
	}

	public void Load(JObject obj)
	{
		//obj.GetValue("bpldatpix")
		//		.Select(x => new ValueTuple<ulong, ulong>(ulong.Parse((string)x["Item1"]), ulong.Parse((string)x["Item2"])))
		//		.ToArray()
		//		.CopyTo(bpldatpix, 0);
	}
}
