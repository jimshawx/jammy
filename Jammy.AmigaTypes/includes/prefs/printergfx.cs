namespace Jammy.AmigaTypes;

public struct PrinterGfxPrefs
{
	[AmigaArraySize(4)]
	public LONG[] pg_Reserved { get; set; }
	public UWORD pg_Aspect { get; set; }
	public UWORD pg_Shade { get; set; }
	public UWORD pg_Image { get; set; }
	public WORD pg_Threshold { get; set; }
	public UBYTE pg_ColorCorrect { get; set; }
	public UBYTE pg_Dimensions { get; set; }
	public UBYTE pg_Dithering { get; set; }
	public UWORD pg_GraphicFlags { get; set; }
	public UBYTE pg_PrintDensity { get; set; }
	public UWORD pg_PrintMaxWidth { get; set; }
	public UWORD pg_PrintMaxHeight { get; set; }
	public UBYTE pg_PrintXOffset { get; set; }
	public UBYTE pg_PrintYOffset { get; set; }
}

