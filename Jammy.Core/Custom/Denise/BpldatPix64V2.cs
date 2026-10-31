using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise;

public class BpldatPix64V2 : IBpldatPix
{
	//[Persist]
	private int alignShift;

	//[Persist] //handled manually
	private readonly ulong[] bpldatpix = new ulong[8];

	public void WriteBitplanes(ref ulong[] bpldat, int oddeven)
	{
		for (int i = oddeven; i < 8; i+=2)
		{
			bpldatpix[i] |= bpldat[i] << alignShift;
		}
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
		alignShift = 63 - (int)pixelBits;
	}

	public void Clear()
	{
		Array.Clear(bpldatpix);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public uint GetPixel(int planes)
	{
		ulong v7 = bpldatpix[7];
		ulong v6 = bpldatpix[6];
		ulong v5 = bpldatpix[5];
		ulong v4 = bpldatpix[4];
		ulong v3 = bpldatpix[3];
		ulong v2 = bpldatpix[2];
		ulong v1 = bpldatpix[1];
		ulong v0 = bpldatpix[0];

		uint pix = (uint)(v7 >> 63);
		pix = (pix << 1) + (uint)(v6 >> 63);
		pix = (pix << 1) + (uint)(v5 >> 63);
		pix = (pix << 1) + (uint)(v4 >> 63);
		pix = (pix << 1) + (uint)(v3 >> 63);
		pix = (pix << 1) + (uint)(v2 >> 63);
		pix = (pix << 1) + (uint)(v1 >> 63);
		pix = (pix << 1) + (uint)(v0 >> 63);

		bpldatpix[7] = v7 << 1;
		bpldatpix[6] = v6 << 1;
		bpldatpix[5] = v5 << 1;
		bpldatpix[4] = v4 << 1;
		bpldatpix[3] = v3 << 1;
		bpldatpix[2] = v2 << 1;
		bpldatpix[1] = v1 << 1;
		bpldatpix[0] = v0 << 1;

		return pix & ((1u << planes) - 1);
	}

	public void Save(JArray jobj)
	{
		var jo = new JObject();
		jo["id"] = "pixels";
		jo.Add("alignShift", alignShift);
		jo.Add("bpldatpix", JToken.FromObject(bpldatpix));
		jobj.Add(jo);
	}

	public void Load(JObject obj)
	{
		if (!PersistenceManager.Is(obj, "pixels")) return;

		alignShift = int.Parse((string)obj.GetValue("alignShift"));
		obj.GetValue("bpldatpix")
						.Select(x => ulong.Parse((string)x))
						.ToArray()
						.CopyTo(bpldatpix, 0);
	}
}
