namespace Jammy.AmigaTypes;

public class Preferences
{
	public BYTE FontHeight { get; set; }
	public UBYTE PrinterPort { get; set; }
	public UWORD BaudRate { get; set; }
	public timeval KeyRptSpeed { get; set; }
	public timeval KeyRptDelay { get; set; }
	public timeval DoubleClick { get; set; }
	[AmigaArraySize((1 + 16 + 1) * 2)]
	public UWORD[] PointerMatrix { get; set; }
	public BYTE XOffset { get; set; }
	public BYTE YOffset { get; set; }
	public UWORD color17 { get; set; }
	public UWORD color18 { get; set; }
	public UWORD color19 { get; set; }
	public UWORD PointerTicks { get; set; }
	public UWORD color0 { get; set; }
	public UWORD color1 { get; set; }
	public UWORD color2 { get; set; }
	public UWORD color3 { get; set; }
	public BYTE ViewXOffset { get; set; }
	public BYTE ViewYOffset { get; set; }
	public WORD ViewInitX { get; set; }
	public WORD ViewInitY { get; set; }
	public BOOL EnableCLI { get; set; }
	public UWORD PrinterType { get; set; }
	[AmigaArraySize(30)]
	public UBYTE[] PrinterFilename { get; set; }
	public UWORD PrintPitch { get; set; }
	public UWORD PrintQuality { get; set; }
	public UWORD PrintSpacing { get; set; }
	public UWORD PrintLeftMargin { get; set; }
	public UWORD PrintRightMargin { get; set; }
	public UWORD PrintImage { get; set; }
	public UWORD PrintAspect { get; set; }
	public UWORD PrintShade { get; set; }
	public WORD PrintThreshold { get; set; }
	public UWORD PaperSize { get; set; }
	public UWORD PaperLength { get; set; }
	public UWORD PaperType { get; set; }
	public UBYTE SerRWBits { get; set; }
	public UBYTE SerStopBuf { get; set; }
	public UBYTE SerParShk { get; set; }
	public UBYTE LaceWB { get; set; }
	[AmigaArraySize(30)]
	public UBYTE[] WorkName { get; set; }
	public BYTE RowSizeChange { get; set; }
	public BYTE ColumnSizeChange { get; set; }
	public UWORD PrintFlags { get; set; }
	public UWORD PrintMaxWidth { get; set; }
	public UWORD PrintMaxHeight { get; set; }
	public UBYTE PrintDensity { get; set; }
	public UBYTE PrintXOffset { get; set; }
	public UWORD wb_Width { get; set; }
	public UWORD wb_Height { get; set; }
	public UBYTE wb_Depth { get; set; }
	public UBYTE ext_size { get; set; }
}

