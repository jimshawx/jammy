using Jammy.Core.Interface.Interfaces;
using System;

/*
	Copyright 2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Floppy
{
	public class MFMDisk : IDisk
	{
		private readonly byte[] data;
		private readonly MFM mfmEncoder;

		public MFMDisk(byte[] data)
		{
			this.data = data;
			this.mfmEncoder = new MFM();
		}

		public byte[] GetTrack(uint track, uint side)
		{
			if (track >= 80 || side >= 2)
				throw new ArgumentOutOfRangeException($"Track or head number is out of range {track}.{side}");

			return	mfmEncoder.EncodeTrack((track << 1) + side, data, 0x4489);
		}
	}
}
