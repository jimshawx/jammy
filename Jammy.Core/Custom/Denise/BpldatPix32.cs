using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Runtime.CompilerServices;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise;

public class BpldatPix32 : IBpldatPix
{
	//[Persist]
	private int pixelMaskBit;

	//[Persist] //handled manually
	private static readonly uint[] bpldatpix = new uint[8];

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
			bpldatpix[i] <<= 1;
	}

	public void SetPixelBitMask(uint pixelBits)
	{
		pixelMaskBit = (int)(pixelBits + 16);
	}

	public void Clear()
	{
		for (int i = 0; i < 8; i++)
			bpldatpix[i] = 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint GetPixel(int planes)
	{
		uint pix = 0;
		uint b = 1;
		for (int i = 0; i < planes; i++, b <<= 1)
			pix |= IsBitSet(bpldatpix[i], pixelMaskBit) ? b : 0;

		NextPixel();

		return pix;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool IsBitSet(uint bp, int bit)
	{
		return (bp&1<<bit)!=0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Or(ref uint bp, ulong bits, int shift)
	{
		bp |= (uint)(bits<<shift);
	}

	public void Save(JArray jobj)
	{
		var jo = new JObject();
		jo["id"] = "pixels";
		jo.Add("pixelBitMask", pixelMaskBit);
		jo.Add("bpldatpix", JToken.FromObject(bpldatpix));
		jobj.Add(jo);
	}

	public void Load(JObject obj)
	{
		if (!PersistenceManager.Is(obj, "pixels")) return;

		pixelMaskBit = int.Parse((string)obj.GetValue("pixelBitMask"));
		obj.GetValue("bpldatpix")
				.Select(x=>uint.Parse((string)x))
				.ToArray()
				.CopyTo(bpldatpix, 0);
	}
}
