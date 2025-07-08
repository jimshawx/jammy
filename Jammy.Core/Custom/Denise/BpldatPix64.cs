using Jammy.Core.Interface.Interfaces;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise;

public class BpldatPix64 : IBpldatPix
{
	//[Persist]
	private int pixelMaskBit;

	//[Persist] //handled manually
	private readonly ValueTuple<ulong, ulong>[] bpldatpix = new ValueTuple<ulong, ulong>[8];

	public void WriteBitplanes(ref ulong[] bpldat, int even, int odd)
	{
		for (int i = 0; i < 8; i++)
		{
			if ((i & 1) != 0)
				Or(ref bpldatpix[i], bpldat[i], 16 - odd);
			else
				Or(ref bpldatpix[i], bpldat[i], 16 - even);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void NextPixel()
	{
		for (int i = 0; i < 8; i++)
		{
			bpldatpix[i].Item1 <<= 1;
			bpldatpix[i].Item1 |= bpldatpix[i].Item2 >> 63;
			bpldatpix[i].Item2 <<= 1;
		}
	}

	public void SetPixelBitMask(uint pixelBits)
	{
		pixelMaskBit = (int)(pixelBits + 16);
	}

	public void Clear()
	{
		for (int i = 0; i < 8; i++)
			bpldatpix[i].Item1 = bpldatpix[i].Item2 = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint GetPixel(int planes)
	{
		uint pix = 0;
		uint b = 1;
		for (int i = 0; i < planes; i++, b <<= 1)
			pix |= IsBitSet(ref bpldatpix[i], pixelMaskBit) ? b : 0;

		NextPixel();

		return pix;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsBitSet(ref ValueTuple<ulong, ulong> bp, int bit)
	{
		// mask is 0 if bit < 64, ulong.MaxValue if bit >= 64
		ulong mask = (ulong)-(bit >> 6); // (bit >> 6) is 0 for 0-63, 1 for 64-127
		int shift = bit & 63;
		ulong value = bp.Item2 & ~mask | bp.Item1 & mask;
		return (value >> shift & 1UL) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Or(ref ValueTuple<ulong, ulong> bp, ulong bits, int shift)
	{
		bp.Item1 |= bits >> 64 - shift;
		bp.Item2 |= bits << shift;
	}

	public void Save(JArray jo)
	{
		var obj = new JObject();
		obj.Add("pixelBitMask", pixelMaskBit);
		var bpldatpix128 = bpldatpix.Select(x=> new UInt128(x.Item1, x.Item2));
		obj.Add("bpldatpix", JToken.FromObject(bpldatpix128));
		jo.Add(obj);
	}

	public void Load(JObject obj)
	{
		pixelMaskBit = int.Parse((string)obj.GetValue("pixelBitMask"));
		obj.GetValue("bpldatpix")
				.Select(x => new ValueTuple<ulong, ulong>((ulong)(UInt128.Parse((string)x) >> 64), (ulong)UInt128.Parse((string)x)))
				.ToArray()
				.CopyTo(bpldatpix, 0);
	}
}
