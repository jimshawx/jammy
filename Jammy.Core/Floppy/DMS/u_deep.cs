
/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *
 *     Lempel-Ziv-DynamicHuffman decompression functions used in Deep
 *     mode.
 *     Most routines ripped from LZHUF written by  Haruyasu Yoshizaki
 *
 */

namespace Jammy.Core.Floppy.DMS;

using USHORT = ushort;
using UCHAR = byte;
using System;

public static partial class xDMS
{
	private static USHORT deep_text_loc;
	private static bool init_deep_tabs = true;

	private const int DBITMASK = 0x3fff;  /*  uses 16Kb dictionary  */

	private const int F = 60; /* lookahead buffer size */
	private const int THRESHOLD = 2;
	private const int N_CHAR = (256 - THRESHOLD + F);/* kinds of characters (character code = 0..N_CHAR-1) */
	private const int T = (N_CHAR * 2 - 1);   /* size of table */
	private const int R = (T - 1);       /* position of root */
	private const int MAX_FREQ = 0x8000;     /* updates tree when the */

	private static USHORT []freq = new USHORT[T + 1]; /* frequency table */

	private static USHORT []prnt = new USHORT[T + N_CHAR]; /* pointers to parent nodes, except for the */
	/* elements [T..T + N_CHAR - 1] which are used to get */
	/* the positions of leaves corresponding to the codes. */

	private static USHORT [] son = new USHORT[T];   /* pointers to child nodes (son[], son[] + 1) */

	private static void Init_DEEP_Tabs()
	{
		USHORT i, j;

		for (i = 0; i < N_CHAR; i++)
		{
			freq[i] = 1;
			son[i] = (USHORT)(i + T);
			prnt[i + T] = i;
		}
		i = 0; j = N_CHAR;
		while (j <= R)
		{
			freq[j] = (USHORT)(freq[i] + freq[i + 1]);
			son[j] = i;
			prnt[i] = prnt[i + 1] = j;
			i += 2; j++;
		}
		freq[T] = 0xffff;
		prnt[R] = 0;

		init_deep_tabs = false;
	}

	public static USHORT Unpack_DEEP(UCHAR[] @in, UCHAR[] @out, USHORT origsize)
	{
		USHORT i, j, c;
		int outptr = 0;

		initbitbuf(@in);

		if (init_deep_tabs) Init_DEEP_Tabs();

		while (outptr < origsize)
		{
			c = DecodeChar();
			if (c < 256)
			{
				@out[outptr++] = text[deep_text_loc++ & DBITMASK] = (UCHAR)c;
			}
			else
			{
				j = (USHORT)(c - 255 + THRESHOLD);
				i = (USHORT)(deep_text_loc - DecodePosition() - 1);
				while (j--!=0) @out[outptr++] = text[deep_text_loc++ & DBITMASK] = text[i++ & DBITMASK];
			}
		}

		deep_text_loc = (USHORT)((deep_text_loc + 60) & DBITMASK);

		return 0;
	}

	private static USHORT DecodeChar()
	{
		USHORT c;

		c = son[R];

		/* travel from root to leaf, */
		/* choosing the smaller child node (son[]) if the read bit is 0, */
		/* the bigger (son[]+1} if 1 */
		while (c < T)
		{
			c = son[c + GETBITS(1)];
			DROPBITS(1);
		}
		c -= T;
		update(c);
		return c;
	}

	private static USHORT DecodePosition()
	{
		USHORT i, j, c;

		i = GETBITS(8); DROPBITS(8);
		c = (USHORT)(d_code[i] << 8);
		j = d_len[i];
		i = (USHORT)(((i << j) | GETBITS((byte)j)) & 0xff); DROPBITS((byte)j);

		return (USHORT)(c | i);
	}

	/* reconstruction of tree */

	private static void reconst()
	{
		USHORT i, j, k, f, l;

		/* collect leaf nodes in the first half of the table */
		/* and replace the freq by (freq + 1) / 2. */
		j = 0;
		for (i = 0; i < T; i++)
		{
			if (son[i] >= T)
			{
				freq[j] = (USHORT)((freq[i] + 1) / 2);
				son[j] = son[i];
				j++;
			}
		}
		/* begin constructing tree by connecting sons */
		for (i = 0, j = N_CHAR; j < T; i += 2, j++)
		{
			k = (USHORT)(i + 1);
			f = freq[j] = (USHORT)(freq[i] + freq[k]);
			for (k = (USHORT)(j - 1); f < freq[k]; k--) ;
			k++;
			l = (USHORT)((j - k) * 2);
			//memmove(&freq[k + 1], &freq[k], l);
			Array.Copy(freq,k, freq, k+1, l );
			freq[k] = f;
			//memmove(&son[k + 1], &son[k], l);
			Array.Copy(son, k, son, k+1, l);
			son[k] = i;
		}
		/* connect prnt */
		for (i = 0; i < T; i++)
		{
			if ((k = son[i]) >= T)
			{
				prnt[k] = i;
			}
			else
			{
				prnt[k] = prnt[k + 1] = i;
			}
		}
	}

	/* increment frequency of given code by one, and update tree */

	private static void update(USHORT c)
	{
		USHORT i, j, k, l;

		if (freq[R] == MAX_FREQ)
		{
			reconst();
		}
		c = prnt[c + T];
		do
		{
			k = ++freq[c];

			/* if the order is disturbed, exchange nodes */
			if (k > freq[l = (USHORT)(c + 1)])
			{
				while (k > freq[++l]) ;
				l--;
				freq[c] = freq[l];
				freq[l] = k;

				i = son[c];
				prnt[i] = l;
				if (i < T) prnt[i + 1] = l;

				j = son[l];
				son[l] = i;

				prnt[j] = c;
				if (j < T) prnt[j + 1] = c;
				son[c] = j;

				c = l;
			}
		} while ((c = prnt[c]) != 0); /* repeat up to root */
	}
}