using System;
using System.Collections.Generic;
using System.Linq;
using Jammy.Extensions.Extensions;

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

			track = track.Concat(new MFMGap().AsEnumerable());

			return track.ToArray();
		}
	}
}
