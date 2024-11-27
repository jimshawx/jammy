/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *
 *
 */

namespace Jammy.Core.Floppy.DMS;

using USHORT = ushort;
using UCHAR = byte;

public static partial class xDMS
{
	private const int QBITMASK = 0xff;
	private static USHORT quick_text_loc;

	public static USHORT Unpack_QUICK(UCHAR[] @in, UCHAR[] @out, USHORT origsize)
	{
		USHORT i, j;
		int outptr = 0;

		initbitbuf(@in);

		while (outptr < origsize) {
			if (GETBITS(1) != 0)
			{
				DROPBITS(1);
				@out[outptr++] = text[quick_text_loc++ & QBITMASK] = (UCHAR)GETBITS(8); DROPBITS(8);
			}
			else
			{
				DROPBITS(1);
				j = (USHORT)(GETBITS(2) + 2); DROPBITS(2);
				i = (USHORT)(quick_text_loc - GETBITS(8) - 1); DROPBITS(8);
				while (j--!=0)
				{
					@out[outptr++] = text[quick_text_loc++ & QBITMASK] = text[i++ & QBITMASK];
				}
			}
		}
		quick_text_loc = (USHORT)((quick_text_loc + 5) & QBITMASK);

		return 0;
	}
}