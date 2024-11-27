namespace Jammy.Core.Floppy.DMS;

using USHORT = ushort;

public static partial class xDMS
{
	private static USHORT GETBITS(byte n) { return ((USHORT)(bitbuf >> (bitcount-(n)))); }
	private static void DROPBITS(byte n) {bitbuf &= mask_bits[bitcount-=(n)]; while (bitcount<16) {bitbuf = (bitbuf << 8) | indata[indataptr++];  bitcount += 8;}}
}

