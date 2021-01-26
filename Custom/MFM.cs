using System;

namespace RunAmiga.Custom
{
	public static class MFM
	{
		private const byte MFM_FILLB = 0xaa;
		private const uint MFM_FILLL = 0xaaaaaaaa;
		private const uint MFM_MASK = 0x55555555;
		private const int FLOPPY_GAP_BYTES = 720;

		private static void FloppySectorMfmEncode(uint tra, uint sec, Span<byte> src, Span<byte> dest, uint sync)
		{
			uint tmp, odd, even, hck = 0, dck = 0;
			int x;

			/* Preamble and sync */

			dest[0]= 0xaa;
			dest[1]= 0xaa;
			dest[2]= 0xaa;
			dest[3]= 0xaa;
			dest[4]= (byte)(sync >> 8);
			dest[5]= (byte)(sync & 0xff);
			dest[6]= (byte)(sync >> 8);
			dest[7]= (byte)(sync & 0xff);

			/* Track and sector info */

			tmp = 0xff000000 | (tra << 16) | (sec << 8) | (11 - sec);
			even = (tmp & MFM_MASK);
			odd = ((tmp >> 1) & MFM_MASK);
			dest[8]= (byte)((odd & 0xff000000) >> 24);
			dest[9] = (byte)((odd & 0xff0000) >> 16);
			dest[10]= (byte)((odd & 0xff00) >> 8);
			dest[11]= (byte)(odd & 0xff);
			dest[12]= (byte)((even & 0xff000000) >> 24);
			dest[13]= (byte)((even & 0xff0000) >> 16);
			dest[14]= (byte)((even & 0xff00) >> 8);
			dest[15]= (byte)(even & 0xff);

			/* Fill unused space */

			for (x = 16; x < 48; x++)
			{
				dest[x] = MFM_FILLB;
			}

			/* Encode data section of sector */

			for (x = 64; x < 576; x++)
			{
				tmp = src[x - 64];
				odd = (tmp & 0x55);
				even = (tmp >> 1) & 0x55;
				dest[x]= (byte)(even | MFM_FILLB);
				dest[x + 512] = (byte)(odd | MFM_FILLB);
			}

			/* Calculate checksum for unused space */

			for (x = 8; x < 48; x += 4)
			{
				hck ^= (((uint) dest[x]) << 24) | (((uint) dest[x + 1]) << 16) |
					(((uint) dest[x + 2]) << 8) | ((uint) dest[x + 3]);
			}
			even = odd = hck;
			odd >>= 1;
			even |= MFM_FILLL;
			odd |= MFM_FILLL;
			dest[48]= (byte)((odd & 0xff000000) >> 24);
			dest[49] = (byte)((odd & 0xff0000) >> 16);
			dest[50]= (byte)((odd & 0xff00) >> 8);
			dest[51]= (byte)(odd & 0xff);
			dest[52]= (byte)((even & 0xff000000) >> 24);
			dest[53]= (byte)((even & 0xff0000) >> 16);
			dest[54]= (byte)((even & 0xff00) >> 8);
			dest[55]= (byte)(even & 0xff);

			/* Calculate checksum for data section */

			for (x = 64; x < 1088; x += 4)
			{
				dck ^= (((uint)dest[x]) << 24) | (((uint)dest[x + 1]) << 16) |
					(((uint)dest[x + 2]) << 8) | ((uint)dest[x + 3]);
			}
			even = odd = dck;
			odd >>= 1;
			even |= MFM_FILLL;
			odd |= MFM_FILLL;
			dest[56]= (byte)((odd & 0xff000000) >> 24);
			dest[57]= (byte)((odd & 0xff0000) >> 16);
			dest[58]= (byte)((odd & 0xff00) >> 8);
			dest[59] = (byte)(odd & 0xff);
			dest[60]= (byte)((even & 0xff000000) >> 24);
			dest[61]= (byte)((even & 0xff0000) >> 16);
			dest[62]= (byte)((even & 0xff00) >> 8);
			dest[63]= (byte)(even & 0xff);
		}

		private static void FloppyGapMfmEncode(Span<byte> dst)
		{
			int i;
			for (i = 0; i < FLOPPY_GAP_BYTES; i++)
			{
				dst[i] = MFM_FILLB;
			}
		}

		public static void FloppyTrackMfmEncode(uint track, byte[] src, byte[] dst, ushort sync)
		{
			int i;
			for (i = 0; i < 11; i++)
			{
				FloppySectorMfmEncode(track, (uint)i, new Span<byte>(src, i * 512, 512), new Span<byte>(dst, i * 1088, 1088), sync);
			}
			FloppyGapMfmEncode(new Span<byte>(dst, 11 * 1088, FLOPPY_GAP_BYTES));
		}
	}
}