/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *
 *     Main decompression functions used in MEDIUM mode
 *
 */

namespace Jammy.Core.Floppy.DMS;

using USHORT = ushort;
using UCHAR = byte;

public static partial class xDMS
{
	private const int MBITMASK = 0x3fff;
	private static USHORT medium_text_loc;

	public static USHORT Unpack_MEDIUM(UCHAR[] @in, UCHAR[] @out, USHORT origsize)
	{
		USHORT i, j, c;
		UCHAR u;
		int outptr = 0;

		initbitbuf(@in);

		while (outptr < origsize)
		{
			if (GETBITS(1) != 0)
			{
				DROPBITS(1);
				@out[outptr++] = text[medium_text_loc++ & MBITMASK] = (UCHAR)GETBITS(8);
				DROPBITS(8);
			}
			else
			{
				DROPBITS(1);
				c = GETBITS(8); DROPBITS(8);
				j = (USHORT)(d_code[c] + 3);
				u = d_len[c];
				c = (USHORT)(((c << u) | GETBITS(u)) & 0xff); DROPBITS(u);
				u = d_len[c];
				c = (USHORT)((d_code[c] << 8) | (((c << u) | GETBITS(u)) & 0xff)); DROPBITS(u);
				i = (USHORT)(medium_text_loc - c - 1);

				while (j--!=0) @out[outptr++] = text[medium_text_loc++ & MBITMASK] = text[i++ & MBITMASK];

			}
		}
		medium_text_loc = (USHORT)((medium_text_loc + 66) & MBITMASK);

		return 0;
	}
}


