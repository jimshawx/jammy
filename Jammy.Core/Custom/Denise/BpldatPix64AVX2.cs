using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise;

public class BpldatPix64AVX2 : IBpldatPix
{
	//[Persist]
	private int pixelMaskBit = 31;

	//[Persist] //handled manually
	private Vector256<uint> bpldatpix = Vector256<uint>.Zero;

	private readonly uint[] bits = new uint[8];

	private readonly ulong[] stashedBits = new ulong[8];
	private int stashedEven, stashedOdd;
	private ushort stashedBitcount = 1;

	public void WriteBitplanes(ref ulong[] bpldat, int even, int odd)
	{
		ulong Swizzle(ulong value)
		{
			return (value >> 48) | (value << 48) | ((value >> 16) & 0xffff0000ul) | ((value << 16) & 0xffff00000000ul);
		}

		for (int i = 0; i < 8; i++)
			stashedBits[i] = Swizzle(bpldat[i]);
		stashedEven = even;
		stashedOdd = odd;

		Unstash16B();
	}

	private void Unstash16B()
	{
		//clock out the next set of 16 bits
		int even = stashedEven;
		int odd = stashedOdd;

		ulong[] bpldat = stashedBits;

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

		for (int i = 0; i < 8; i++)
			stashedBits[i] >>= 16;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void NextPixel()
	{
		bpldatpix = Avx2.ShiftLeftLogical(bpldatpix, 1);

		stashedBitcount = ushort.RotateRight(stashedBitcount,1);
		if (stashedBitcount == 1)
			Unstash16B();
	}

	public void SetPixelBitMask(uint pixelBits)
	{
		stashedBitcount = 1;
		pixelMaskBit = 31;
	}

	public void Clear()
	{
		bpldatpix = Vector256<uint>.Zero;
	}

	private static readonly Vector256<uint> index = Vector256.Create(7+24u,6+24u,5+24u,4+24u,3+24u,2+24u,1+24u,0+24u);

	private static readonly uint[] planeMasks = 
		[
			0b00000000,
			0b00000001,
			0b00000011,
			0b00000111,
			0b00001111,
			0b00011111,
			0b00111111,
			0b01111111,
			0b11111111,
		];

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint GetPixel(int planes)
	{
		var pixelMask = Vector256.Create((uint)(1 << pixelMaskBit));

		var pixelBits = Avx2.ShiftRightLogicalVariable(Avx2.And(bpldatpix, pixelMask), index);

		Vector128<uint> or128 = Sse2.Or(pixelBits.GetLower(), pixelBits.GetUpper());

		Vector128<uint> shuf1 = Sse2.Shuffle(or128, 0b_10_11_00_01); // [2,3,0,1]
		Vector128<uint> or64 = Sse2.Or(or128, shuf1);

		Vector128<uint> shuf2 = Sse2.Shuffle(or64, 0b_01_00_11_10); // [1,0,3,2]
		Vector128<uint> pix = Sse2.Or(or64, shuf2);

		NextPixel();

		return pix.ToScalar() & planeMasks[planes];
	}

	public void Save(JArray jobj)
	{
		var jo = new JObject();
		jo["id"] = "pixels";
		jo.Add("pixelBitMask", pixelMaskBit);
		var bpldatpix32 = new uint[8];
		for (int i  = 0; i < 8; i++)
			bpldatpix32[i] = bpldatpix.GetElement(i);
		jo.Add("bpldatpix", JToken.FromObject(bpldatpix32));
		jobj.Add(jo);
	}

	public void Load(JObject obj)
	{
		if (!PersistenceManager.Is(obj, "pixels")) return;

		pixelMaskBit = int.Parse((string)obj.GetValue("pixelBitMask"));
		var bpldatpix32 = new uint[8];
		obj.GetValue("bpldatpix")
				.Select(x => uint.Parse((string)x))
				.ToArray()
				.CopyTo(bpldatpix32, 0);
		bpldatpix = Vector256.Create(bpldatpix32);
	}
}
