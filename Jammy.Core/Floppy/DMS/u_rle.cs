/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *
 *     Run Length Encoding decompression function used in most
 *     modes after decompression by other algorithm
 *
 */
namespace Jammy.Core.Floppy.DMS;

using USHORT = ushort;
using UCHAR = byte;

public static partial class xDMS
{
	public static USHORT Unpack_RLE(UCHAR[] @in, UCHAR[] @out, USHORT origsize)
	{
		USHORT n;
		UCHAR a, b;
		int outptr = 0;
		int inptr = 0;

		while (outptr < origsize)
		{
			if ((a = @in[inptr++]) != 0x90)
				@out[outptr++] = a;
			else if ((b = @in[inptr++])==0)
				@out[outptr++] = a;
			else
			{
				a = @in[inptr++];
				if (b == 0xff)
				{
					n = @in[inptr++];
					n = (USHORT)((n << 8) + @in[inptr++]);
				}
				else
					n = b;
				if (outptr + n > origsize) return 1;
				for (int i = 0; i < n; i++)
					@out[outptr++] = a;
			}
		}
		return 0;
	}
}
