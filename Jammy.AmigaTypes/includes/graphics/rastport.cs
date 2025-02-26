namespace Jammy.AmigaTypes;

public class AreaInfo
{
	public WORDPtr VctrTbl { get; set; }
	public WORDPtr VctrPtr { get; set; }
	public BYTEPtr FlagTbl { get; set; }
	public BYTEPtr FlagPtr { get; set; }
	public WORD Count { get; set; }
	public WORD MaxCount { get; set; }
	public WORD FirstX { get; set; }
	public WORD FirstY { get; set; }
}

public class TmpRas
{
	public BYTEPtr RasPtr { get; set; }
	public LONG Size { get; set; }
}

public class GelsInfo
{
	public BYTE sprRsrvd { get; set; }
	public UBYTE Flags { get; set; }
	public VSpritePtr gelHead { get; set; }
	public VSpritePtr gelTail { get; set; }
	public WORDPtr nextLine { get; set; }
	public WORDPtrPtr lastColor { get; set; }
	public collTablePtr collHandler { get; set; }
	public WORD leftmost { get; set; }
	public WORD rightmost { get; set; }
	public WORD topmost { get; set; }
	public WORD bottommost { get; set; }
	public APTR firstBlissObj { get; set; }
	public APTR lastBlissObj { get; set; }
}

public class RastPort
{
	public LayerPtr Layer { get; set; }
	public BitMapPtr BitMap { get; set; }
	public UWORDPtr AreaPtrn { get; set; }
	public TmpRasPtr TmpRas { get; set; }
	public AreaInfoPtr AreaInfo { get; set; }
	public GelsInfoPtr GelsInfo { get; set; }
	public UBYTE Mask { get; set; }
	public BYTE FgPen { get; set; }
	public BYTE BgPen { get; set; }
	public BYTE AOlPen { get; set; }
	public BYTE DrawMode { get; set; }
	public BYTE AreaPtSz { get; set; }
	public BYTE linpatcnt { get; set; }
	public BYTE dummy { get; set; }
	public UWORD Flags { get; set; }
	public UWORD LinePtrn { get; set; }
	public WORD cp_x { get; set; }
	public WORD cp_y { get; set; }
	[AmigaArraySize(8)]
	public UBYTE[] minterms { get; set; }
	public WORD PenWidth { get; set; }
	public WORD PenHeight { get; set; }
	public TextFontPtr Font { get; set; }
	public UBYTE AlgoStyle { get; set; }
	public UBYTE TxFlags { get; set; }
	public UWORD TxHeight { get; set; }
	public UWORD TxWidth { get; set; }
	public UWORD TxBaseline { get; set; }
	public WORD TxSpacing { get; set; }
	public APTRPtr RP_User { get; set; }
	[AmigaArraySize(2)]
	public ULONG[] longreserved { get; set; }
	[AmigaArraySize(7)]
	public UWORD[] wordreserved { get; set; }
	[AmigaArraySize(8)]
	public UBYTE[] reserved { get; set; }
}

