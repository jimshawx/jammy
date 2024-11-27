/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *     Functions/macros to get a variable number of bits
 * 
 */

namespace Jammy.Core.Floppy.DMS;

using UCHAR = byte;
using ULONG = uint;

public static partial class xDMS
{
	private static readonly ULONG[] mask_bits = [
		0x000000,0x000001,0x000003,0x000007,0x00000f,0x00001f,
		0x00003f,0x00007f,0x0000ff,0x0001ff,0x0003ff,0x0007ff,
		0x000fff,0x001fff,0x003fff,0x007fff,0x00ffff,0x01ffff,
		0x03ffff,0x07ffff,0x0fffff,0x1fffff,0x3fffff,0x7fffff,
		0xffffff
	];

	private static UCHAR[] indata;
	private static int indataptr;
	private static UCHAR bitcount;
	private static ULONG bitbuf;

	public static void initbitbuf(UCHAR[] @in)
	{
		bitbuf = 0;
		bitcount = 0;
		indata = @in;
		indataptr = 0;
		DROPBITS(0);
	}
}
