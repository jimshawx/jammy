namespace Jammy.AmigaTypes;

public class PrinterPSPrefs
{
	[AmigaArraySize(4)]
	public LONG[] ps_Reserved { get; set; }
	public UBYTE ps_DriverMode { get; set; }
	public UBYTE ps_PaperFormat { get; set; }
	[AmigaArraySize(2)]
	public UBYTE[] ps_Reserved1 { get; set; }
	public LONG ps_Copies { get; set; }
	public LONG ps_PaperWidth { get; set; }
	public LONG ps_PaperHeight { get; set; }
	public LONG ps_HorizontalDPI { get; set; }
	public LONG ps_VerticalDPI { get; set; }
	public UBYTE ps_Font { get; set; }
	public UBYTE ps_Pitch { get; set; }
	public UBYTE ps_Orientation { get; set; }
	public UBYTE ps_Tab { get; set; }
	[AmigaArraySize(8)]
	public UBYTE[] ps_Reserved2 { get; set; }
	public LONG ps_LeftMargin { get; set; }
	public LONG ps_RightMargin { get; set; }
	public LONG ps_TopMargin { get; set; }
	public LONG ps_BottomMargin { get; set; }
	public LONG ps_FontPointSize { get; set; }
	public LONG ps_Leading { get; set; }
	[AmigaArraySize(8)]
	public UBYTE[] ps_Reserved3 { get; set; }
	public LONG ps_LeftEdge { get; set; }
	public LONG ps_TopEdge { get; set; }
	public LONG ps_Width { get; set; }
	public LONG ps_Height { get; set; }
	public UBYTE ps_Image { get; set; }
	public UBYTE ps_Shading { get; set; }
	public UBYTE ps_Dithering { get; set; }
	public UBYTE ps_Transparency { get; set; }
	[AmigaArraySize(8)]
	public UBYTE[] ps_Reserved4 { get; set; }
	public UBYTE ps_Aspect { get; set; }
	public UBYTE ps_ScalingType { get; set; }
	public UBYTE ps_ScalingMath { get; set; }
	public UBYTE ps_Centering { get; set; }
	[AmigaArraySize(8)]
	public UBYTE[] ps_Reserved5 { get; set; }
}

