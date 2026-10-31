using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise;

public class BpldatPix16V2 : IBpldatPix
{
	//[Persist] //handled manually
	private static readonly ushort[] bpldatpix = new ushort[8];

	public void WriteBitplanes(ref ulong[] bpldat, int oddeven)
	{
		for (int i = oddeven; i < 8; i+=2)
			bpldatpix[i] = (ushort)bpldat[i];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void NextPixel()
	{
		bpldatpix[7] <<= 1;
		bpldatpix[6] <<= 1;
		bpldatpix[5] <<= 1;
		bpldatpix[4] <<= 1;
		bpldatpix[3] <<= 1;
		bpldatpix[2] <<= 1;
		bpldatpix[1] <<= 1;
		bpldatpix[0] <<= 1;
	}

	public void SetPixelBitMask(uint pixelBits)
	{
	}

	public void Clear()
	{
		Array.Clear(bpldatpix);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint GetPixel(int planes)
	{
		uint v7 = bpldatpix[7];
		uint v6 = bpldatpix[6];
		uint v5 = bpldatpix[5];
		uint v4 = bpldatpix[4];
		uint v3 = bpldatpix[3];
		uint v2 = bpldatpix[2];
		uint v1 = bpldatpix[1];
		uint v0 = bpldatpix[0];

		uint pix = v7 >> 15;
		pix = (pix << 1) + (v6 >> 15);
		pix = (pix << 1) + (v5 >> 15);
		pix = (pix << 1) + (v4 >> 15);
		pix = (pix << 1) + (v3 >> 15);
		pix = (pix << 1) + (v2 >> 15);
		pix = (pix << 1) + (v1 >> 15);
		pix = (pix << 1) + (v0 >> 15);

		bpldatpix[7] = (ushort)(v7 << 1);
		bpldatpix[6] = (ushort)(v6 << 1);
		bpldatpix[5] = (ushort)(v5 << 1);
		bpldatpix[4] = (ushort)(v4 << 1);
		bpldatpix[3] = (ushort)(v3 << 1);
		bpldatpix[2] = (ushort)(v2 << 1);
		bpldatpix[1] = (ushort)(v1 << 1);
		bpldatpix[0] = (ushort)(v0 << 1);

		return pix & ((1u << planes) - 1);
	}

	public void Save(JArray jobj)
	{
		var jo = new JObject();
		jo["id"] = "pixels";
		jo.Add("bpldatpix", JToken.FromObject(bpldatpix));
		jobj.Add(jo);
	}

	public void Load(JObject obj)
	{
		if (!PersistenceManager.Is(obj, "pixels")) return;

		obj.GetValue("bpldatpix")
				.Select(x=>uint.Parse((string)x))
				.ToArray()
				.CopyTo(bpldatpix, 0);
	}
}
