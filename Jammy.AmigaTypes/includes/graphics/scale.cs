namespace Jammy.AmigaTypes;

public class BitScaleArgs
{
	public UWORD bsa_SrcX { get; set; }
	public UWORD bsa_SrcY { get; set; }
	public UWORD bsa_SrcWidth { get; set; }
	public UWORD bsa_SrcHeight { get; set; }
	public UWORD bsa_XSrcFactor { get; set; }
	public UWORD bsa_YSrcFactor { get; set; }
	public UWORD bsa_DestX { get; set; }
	public UWORD bsa_DestY { get; set; }
	public UWORD bsa_DestWidth { get; set; }
	public UWORD bsa_DestHeight { get; set; }
	public UWORD bsa_XDestFactor { get; set; }
	public UWORD bsa_YDestFactor { get; set; }
	public BitMapPtr bsa_SrcBitMap { get; set; }
	public BitMapPtr bsa_DestBitMap { get; set; }
	public ULONG bsa_Flags { get; set; }
	public UWORD bsa_XDDA { get; set; }
	public UWORD bsa_YDDA { get; set; }
	public LONG bsa_Reserved1 { get; set; }
	public LONG bsa_Reserved2 { get; set; }
}

