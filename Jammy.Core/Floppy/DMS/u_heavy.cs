
/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *
 *     Lempel-Ziv-Huffman decompression functions used in Heavy 1 & 2 
 *     compression modes. Based on LZH decompression functions from
 *     UNIX LHA made by Masaru Oki
 *
 */
namespace Jammy.Core.Floppy.DMS;

using USHORT = ushort;
using UCHAR = byte;
using ULONG = uint;

public static partial class xDMS
{
	private const int NC = 510;
	private const int NPT = 20;
	private const int N1 = 510;
	private const int OFFSET = 253;

	private static USHORT[] left = new USHORT[2 * NC - 1], right = new USHORT[2 * NC - 1 + 9];
	private static UCHAR[] c_len = new UCHAR[NC], pt_len = new UCHAR[NPT];
	private static USHORT[] c_table = new USHORT[4096], pt_table = new USHORT[256];
	private static USHORT lastlen, np;
	private static USHORT heavy_text_loc;

	public static USHORT Unpack_HEAVY(UCHAR[]@in, UCHAR[]@out, UCHAR flags, USHORT origsize)
	{
		USHORT j, i, c, bitmask;
		int outptr = 0;

		/*  Heavy 1 uses a 4Kb dictionary,  Heavy 2 uses 8Kb  */

		if ((flags & 8)!=0)
		{
			np = 15;
			bitmask = 0x1fff;
		}
		else
		{
			np = 14;
			bitmask = 0x0fff;
		}

		initbitbuf(@in);

		if ((flags & 2)!=0)
		{
			if (read_tree_c()!=0) return 1;
			if (read_tree_p() != 0) return 2;
		}

		while (outptr < origsize) {
			c = decode_c();
			if (c < 256)
			{
				@out[outptr++] = text[heavy_text_loc++ & bitmask] = (UCHAR)c;
			}
			else
			{
				j = (USHORT)(c - OFFSET);
				i = (USHORT)(heavy_text_loc - decode_p() - 1);
				while (j--!=0) @out[outptr++] = text[heavy_text_loc++ & bitmask] = text[i++ & bitmask];
			}
		}

		return 0;
	}

	private static USHORT decode_c()
	{
		USHORT i, j, m;

		j = c_table[GETBITS(12)];
		if (j < N1)
		{
			DROPBITS(c_len[j]);
		}
		else
		{
			DROPBITS(12);
			i = GETBITS(16);
			m = 0x8000;
			do
			{
				if ((i & m)!=0) j = right[j];
				else j = left[j];
				m >>= 1;
			} while (j >= N1);
			DROPBITS((byte)(c_len[j] - 12));
		}
		return j;
	}



	private static USHORT decode_p()
	{
		USHORT i, j, m;

		j = pt_table[GETBITS(8)];
		if (j < np)
		{
			DROPBITS(pt_len[j]);
		}
		else
		{
			DROPBITS(8);
			i = GETBITS(16);
			m = 0x8000;
			do
			{
				if ((i & m)!=0) j = right[j];
				else j = left[j];
				m >>= 1;
			} while (j >= np);
			DROPBITS((byte)(pt_len[j] - 8));
		}

		if (j != np - 1)
		{
			if (j > 0)
			{
				j = (USHORT)(GETBITS((byte)(i = (USHORT)(j - 1))) | (1U << (j - 1)));
				DROPBITS((byte)i);
			}
			lastlen = j;
		}

		return lastlen;

	}

	private static USHORT read_tree_c()
	{
		USHORT i, n;

		n = GETBITS(9);
		DROPBITS(9);
		if (n > 0)
		{
			for (i = 0; i < n; i++)
			{
				c_len[i] = (UCHAR)GETBITS(5);
				DROPBITS(5);
			}
			for (i = n; i < 510; i++) c_len[i] = 0;
			if (make_table(510, c_len, 12, c_table)!=0) return 1;
		}
		else
		{
			n = GETBITS(9);
			DROPBITS(9);
			for (i = 0; i < 510; i++) c_len[i] = 0;
			for (i = 0; i < 4096; i++) c_table[i] = n;
		}
		return 0;
	}



	private static USHORT read_tree_p()
	{
		USHORT i, n;

		n = GETBITS(5);
		DROPBITS(5);
		if (n > 0)
		{
			for (i = 0; i < n; i++)
			{
				pt_len[i] = (UCHAR)GETBITS(4);
				DROPBITS(4);
			}
			for (i = n; i < np; i++) pt_len[i] = 0;
			if (make_table(np, pt_len, 8, pt_table)!=0) return 1;
		}
		else
		{
			n = GETBITS(5);
			DROPBITS(5);
			for (i = 0; i < np; i++) pt_len[i] = 0;
			for (i = 0; i < 256; i++) pt_table[i] = n;
		}
		return 0;
	}
}
