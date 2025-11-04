using Jammy.Core.Interface.Interfaces;
using Newtonsoft.Json.Linq;
using System.Linq;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Denise
{
	public class BpldatPix64AVX2 : IBpldatPix
	{
		private readonly IBpldatPix bp = new BpldatPix32AVX2();

		private readonly ulong[] stashBits = new ulong[8];
		private int even, odd;
		private ushort stashedBitcount = 1;
		private uint pixelBits;
		private int stashShift;

		public void Clear()
		{
			bp.Clear();
		}

		public uint GetPixel(int planes)
		{
			uint pix = bp.GetPixel(planes);
			NextPixel64();
			return pix;
		}

		public void Load(JObject obj)
		{
			bp.Load(obj);
		}

		public void NextPixel()
		{
			bp.NextPixel();
			NextPixel64();
		}

		private void NextPixel64()
		{
			stashedBitcount = ushort.RotateRight(stashedBitcount, 1);
			if (stashedBitcount == 1)
				Unstash16B();
		}

		public void Save(JArray obj)
		{
			bp.Save(obj);
		}

		public void SetPixelBitMask(uint pixelBits)
		{
			this.pixelBits = pixelBits;
			stashedBitcount = 1;
			bp.SetPixelBitMask(15);
		}

		public void WriteBitplanes(ref ulong[] bpldat, int even, int odd)
		{
			for (int i = 0; i < 8; i++)
				stashBits[i] = bpldat[i];

			this.even = even;
			this.odd = odd;

			stashedBitcount = 1;
			stashShift = (int)pixelBits - 15;
			Unstash16B();
		}

		private void Unstash16B()
		{
			if (stashShift >= 0)
			{ 
				ulong[] bpldat = stashBits.Select(x => (x >> stashShift) & 0xffff).ToArray();
				stashShift -= 16;

				bp.WriteBitplanes(ref bpldat, even, odd);
			}
		}
	}
}
