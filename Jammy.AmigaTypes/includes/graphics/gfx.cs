namespace Jammy.AmigaTypes;

public struct Rectangle
{
	public WORD MinX { get; set; }
	public WORD MinY { get; set; }
	public WORD MaxX { get; set; }
	public WORD MaxY { get; set; }
}

public struct Rect32
{
	public LONG MinX { get; set; }
	public LONG MinY { get; set; }
	public LONG MaxX { get; set; }
	public LONG MaxY { get; set; }
}

public struct BitMap
{
	public UWORD BytesPerRow { get; set; }
	public UWORD Rows { get; set; }
	public UBYTE Flags { get; set; }
	public UBYTE Depth { get; set; }
	public UWORD pad { get; set; }
	[AmigaArraySize(8)]
	public PLANEPTR[] Planes { get; set; }
}

