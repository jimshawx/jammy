namespace Jammy.AmigaTypes;

public struct FileRequester
{
	[AmigaArraySize(4)]
	public UBYTE[] fr_Reserved0 { get; set; }
	public STRPTR fr_File { get; set; }
	public STRPTR fr_Drawer { get; set; }
	[AmigaArraySize(10)]
	public UBYTE[] fr_Reserved1 { get; set; }
	public WORD fr_LeftEdge { get; set; }
	public WORD fr_TopEdge { get; set; }
	public WORD fr_Width { get; set; }
	public WORD fr_Height { get; set; }
	[AmigaArraySize(2)]
	public UBYTE[] fr_Reserved2 { get; set; }
	public LONG fr_NumArgs { get; set; }
	public WBArgPtr fr_ArgList { get; set; }
	public APTR fr_UserData { get; set; }
	[AmigaArraySize(8)]
	public UBYTE[] fr_Reserved3 { get; set; }
	public STRPTR fr_Pattern { get; set; }
}

public struct FontRequester
{
	[AmigaArraySize(8)]
	public UBYTE[] fo_Reserved0 { get; set; }
	public TextAttr fo_Attr { get; set; }
	public UBYTE fo_FrontPen { get; set; }
	public UBYTE fo_BackPen { get; set; }
	public UBYTE fo_DrawMode { get; set; }
	public UBYTE fo_Reserved1 { get; set; }
	public APTR fo_UserData { get; set; }
	public WORD fo_LeftEdge { get; set; }
	public WORD fo_TopEdge { get; set; }
	public WORD fo_Width { get; set; }
	public WORD fo_Height { get; set; }
	public TTextAttr fo_TAttr { get; set; }
}

public struct ScreenModeRequester
{
	public ULONG sm_DisplayID { get; set; }
	public ULONG sm_DisplayWidth { get; set; }
	public ULONG sm_DisplayHeight { get; set; }
	public UWORD sm_DisplayDepth { get; set; }
	public UWORD sm_OverscanType { get; set; }
	public BOOL sm_AutoScroll { get; set; }
	public ULONG sm_BitMapWidth { get; set; }
	public ULONG sm_BitMapHeight { get; set; }
	public WORD sm_LeftEdge { get; set; }
	public WORD sm_TopEdge { get; set; }
	public WORD sm_Width { get; set; }
	public WORD sm_Height { get; set; }
	public BOOL sm_InfoOpened { get; set; }
	public WORD sm_InfoLeftEdge { get; set; }
	public WORD sm_InfoTopEdge { get; set; }
	public WORD sm_InfoWidth { get; set; }
	public WORD sm_InfoHeight { get; set; }
	public APTR sm_UserData { get; set; }
}

public struct DisplayMode
{
	public Node dm_Node { get; set; }
	public DimensionInfo dm_DimensionInfo { get; set; }
	public ULONG dm_PropertyFlags { get; set; }
}

