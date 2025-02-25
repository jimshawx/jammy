namespace Jammy.AmigaTypes;

public struct QueryHeader
{
	public ULONG StructID { get; set; }
	public ULONG DisplayID { get; set; }
	public ULONG SkipID { get; set; }
	public ULONG Length { get; set; }
}

public struct DisplayInfo
{
	public QueryHeader Header { get; set; }
	public UWORD NotAvailable { get; set; }
	public ULONG PropertyFlags { get; set; }
	public Point Resolution { get; set; }
	public UWORD PixelSpeed { get; set; }
	public UWORD NumStdSprites { get; set; }
	public UWORD PaletteRange { get; set; }
	public Point SpriteResolution { get; set; }
	[AmigaArraySize(4)]
	public UBYTE[] pad { get; set; }
	[AmigaArraySize(2)]
	public ULONG[] reserved { get; set; }
}

public struct DimensionInfo
{
	public QueryHeader Header { get; set; }
	public UWORD MaxDepth { get; set; }
	public UWORD MinRasterWidth { get; set; }
	public UWORD MinRasterHeight { get; set; }
	public UWORD MaxRasterWidth { get; set; }
	public UWORD MaxRasterHeight { get; set; }
	public Rectangle Nominal { get; set; }
	public Rectangle MaxOScan { get; set; }
	public Rectangle VideoOScan { get; set; }
	public Rectangle TxtOScan { get; set; }
	public Rectangle StdOScan { get; set; }
	[AmigaArraySize(14)]
	public UBYTE[] pad { get; set; }
	[AmigaArraySize(2)]
	public ULONG[] reserved { get; set; }
}

public struct MonitorInfo
{
	public QueryHeader Header { get; set; }
	public MonitorSpecPtr Mspc { get; set; }
	public Point ViewPosition { get; set; }
	public Point ViewResolution { get; set; }
	public Rectangle ViewPositionRange { get; set; }
	public UWORD TotalRows { get; set; }
	public UWORD TotalColorClocks { get; set; }
	public UWORD MinRow { get; set; }
	public WORD Compatibility { get; set; }
	[AmigaArraySize(36)]
	public UBYTE[] pad { get; set; }
	public Point DefaultViewPosition { get; set; }
	public ULONG PreferredModeID { get; set; }
	[AmigaArraySize(2)]
	public ULONG[] reserved { get; set; }
}

public struct NameInfo
{
	public QueryHeader Header { get; set; }
	[AmigaArraySize(32)]
	public UBYTE[] Name { get; set; }
	[AmigaArraySize(2)]
	public ULONG[] reserved { get; set; }
}

