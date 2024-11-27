/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *
 *     Makes decoding table for Heavy LZH decompression
 *     From  UNIX LHA made by Masaru Oki
 *
 */

namespace Jammy.Core.Floppy.DMS;

using USHORT = ushort;
using SHORT = short;
using UCHAR = byte;

public static partial class xDMS
{
	private static SHORT c;
	private static USHORT n, tblsiz, len, depth, maxdepth, avail;
	private static USHORT codeword, bit, TabErr;
	private static USHORT[] tbl;
	private static UCHAR[] blen;

	public static USHORT make_table(USHORT nchar, UCHAR[] bitlen, USHORT tablebits, USHORT[] table)
	{
		n = avail = nchar;
		blen = bitlen;
		tbl = table;
		tblsiz = (USHORT)(1U << tablebits);
		bit = (USHORT)(tblsiz / 2);
		maxdepth = (USHORT)(tablebits + 1);
		depth = len = 1;
		c = -1;
		codeword = 0;
		TabErr = 0;
		mktbl();    /* left subtree */
		if (TabErr != 0) return TabErr;
		mktbl();    /* right subtree */
		if (TabErr != 0) return TabErr;
		if (codeword != tblsiz) return 5;
		return 0;
	}

	private static USHORT mktbl()
	{
		USHORT i = 0;

		if (TabErr != 0) return 0;

		if (len == depth)
		{
			while (++c < n)
				if (blen[c] == len)
				{
					i = codeword;
					codeword += bit;
					if (codeword > tblsiz)
					{
						TabErr = 1;
						return 0;
					}
					while (i < codeword) tbl[i++] = (USHORT)c;
					return (USHORT)c;
				}
			c = -1;
			len++;
			bit >>= 1;
		}
		depth++;
		if (depth < maxdepth)
		{
			mktbl();
			mktbl();
		}
		else if (depth > 32)
		{
			TabErr = 2;
			return 0;
		}
		else
		{
			if ((i = avail++) >= 2 * n - 1)
			{
				TabErr = 3;
				return 0;
			}
			left[i] = mktbl();
			right[i] = mktbl();
			if (codeword >= tblsiz)
			{
				TabErr = 4;
				return 0;
			}
			if (depth == maxdepth) tbl[codeword++] = i;
		}
		depth--;
		return i;
	}
}
