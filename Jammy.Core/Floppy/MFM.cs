using System;
using System.Collections.Generic;
using System.Linq;
using Jammy.Extensions.Extensions;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Floppy
{
	public class MFM
	{
		public class MFMGap
		{
			private const int GAP_SIZE = 720;
			private const byte MFM_FILL = 0xaa;

			private readonly byte[] dest = new byte[GAP_SIZE];

			public MFMGap()
			{
				Array.Fill(dest, MFM_FILL);
			}

			public IEnumerable<byte> AsEnumerable()
			{
				return dest;
			}
		}

		public class MFMSector
		{
			private const int SECTOR_SIZE = 1088;
			private const byte MFM_FILL = 0xaa;

			private readonly byte[] dest = new byte[SECTOR_SIZE];
			private int offset = 0;

			public IEnumerable<byte> AsEnumerable()
			{
				return dest;
			}

			private uint ReadLong(int i)
			{
				return (uint)((dest[i] << 24) |
				              (dest[i + 1] << 16) |
				              (dest[i + 2] << 8) |
				              dest[i + 3]);
			}

			private void WriteLong(uint v, byte fill)
			{
				foreach (byte b in BitConverter.GetBytes(v).Reverse().OddEven())
					dest[offset++] = (byte)(b | fill);
			}

			public MFMSector Skip(int to)
			{
				offset = to;
				return this;
			}

			public MFMSector Checksum(int start, int finish)
			{
				uint checksum = 0;
				for (int i = start; i < finish; i += 4)
					checksum ^= ReadLong(i);

				WriteLong(checksum, MFM_FILL);
				return this;
			}

			public MFMSector Preamble()
			{
				Pad(4);
				return this;
			}

			public MFMSector Sync(ushort sync)
			{
				dest[offset++] = (byte)(sync >> 8);
				dest[offset++] = (byte)sync;
				dest[offset++] = (byte)(sync >> 8);
				dest[offset++] = (byte)sync;
				return this;
			}

			public MFMSector SectorHeader(uint track, uint sector)
			{
				uint v = 0xff000000 | (track << 16) | (sector << 8) | (11 - sector);
				WriteLong(v, 0);
				return this;
			}

			public MFMSector Pad(int count)
			{
				while (count-- > 0)
					dest[offset++] = MFM_FILL;
				return this;
			}

			public MFMSector Data(Span<byte> span)
			{
				foreach (byte b in span.OddEven())
					dest[offset++] = (byte)(b | MFM_FILL);
				return this;
			}
		}

		public byte[] EncodeTrack(uint trackNo, byte[] src, ushort sync)
		{
			IEnumerable<byte> track = new byte[0];

			for (uint sector = 0; sector < 11; sector++)
			{
				track = track.Concat(new MFMSector()
					.Preamble()
					.Sync(sync)
					.SectorHeader(trackNo, sector)
					.Pad(32)
					.Skip(64)
					.Data(new Span<byte>(src, (int)((trackNo * 11 + sector) * 512), 512))
					.Skip(48)
					.Checksum(8, 48)
					.Checksum(64, 1088).AsEnumerable());
			}

			//pad to 12688 bytes
			//track = track.Concat(new MFMGap().AsEnumerable());

			//pad at the start
			track = new MFMGap().AsEnumerable().Concat(track);

			return track.ToArray();
		}

		public static byte[] DecodeTrack(ushort[] mfm)
		{
			var trackBytes = new byte[11 * 512];
			int sectorsFound = 0;
			int i = 0;

			while (i < mfm.Length - 600 && sectorsFound < 11)
			{
				if (mfm[i] == 0x4489 && mfm[i + 1] == 0x4489)
				{
					int src = i + 2;

					// sector header (4 mfm words)
					// bytes: [format, track, sector, sectorsToEnd]
					var headerInfo = DecodeMfmOddEven(mfm.Skip(src).Take(4).ToArray(), 2);
					byte sectorNum = headerInfo[2];
					src += 4;

					// label (16 mfm words)
					// header checksum (4 mfm words)
					// data checksum (4 mfm words)
					src += 16 + 4 + 4;

					// sector data (512 mfm words)
					if (src + 512 <= mfm.Length && sectorNum < 11)
					{
						var data = DecodeMfmOddEven(mfm.Skip(src).Take(512).ToArray(), 256);
						Buffer.BlockCopy(data, 0, trackBytes, sectorNum * 512, 512);
						sectorsFound++;

						src += 512;
					}

					i = src;
				}
				else
				{
					i++;
				}
			}

			return trackBytes;
		}

		//all odd words followed by all even words
		public static byte[] DecodeMfmOddEven(ushort[] mfmBlock, int numWords)
		{
			var output = new byte[numWords * 2];
			int oddPtr = 0;
			int evenPtr = numWords;
			int byteIndex = 0;

			for (int j = 0; j < numWords; j++)
			{
				uint oddBits = (uint)((mfmBlock[oddPtr++] << 1) & 0xAAAA);
				uint evenBits = (uint)(mfmBlock[evenPtr++] & 0x5555);

				ushort dataWord = (ushort)(oddBits | evenBits);

				output[byteIndex++] = (byte)(dataWord >> 8);
				output[byteIndex++] = (byte)(dataWord & 0xFF);
			}

			return output;
		}

		//interleaved odd/even words
		public static byte[] DecodeMfmWords(ushort[] mfmBlock, int numWords)
		{
			var output = new byte[numWords * 2];
			int mfmPtr = 0;
			int byteIndex = 0;

			for (int j = 0; j < numWords; j++)
			{
				uint oddBits = (uint)((mfmBlock[mfmPtr++] << 1) & 0xAAAA);
				uint evenBits = (uint)(mfmBlock[mfmPtr++] & 0x5555);

				ushort dataWord = (ushort)(oddBits | evenBits);

				output[byteIndex++] = (byte)(dataWord >> 8);
				output[byteIndex++] = (byte)(dataWord & 0xFF);
			}

			return output;
		}

		//interleaved odd/even bits
		public static byte[] DecodeMfmBits(ushort[] mfmBlock, int numWords)
		{
			var output = new byte[numWords];

			for (int i = 0; i < numWords; i++)
			{
				uint mfm = (uint)(mfmBlock[i] & 0x5555);

				mfm = (mfm | (mfm >> 1)) & 0x3333;
				mfm = (mfm | (mfm >> 2)) & 0x0F0F;
				mfm = (mfm | (mfm >> 4)) & 0x00FF;

				output[i] = (byte)mfm;
			}

			return output;
		}
	}
}
