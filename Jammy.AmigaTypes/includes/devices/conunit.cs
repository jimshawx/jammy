namespace Jammy.AmigaTypes;

public class ConUnit
{
	public MsgPort cu_MP { get; set; }
	public WindowPtr cu_Window { get; set; }
	public WORD cu_XCP { get; set; }
	public WORD cu_YCP { get; set; }
	public WORD cu_XMax { get; set; }
	public WORD cu_YMax { get; set; }
	public WORD cu_XRSize { get; set; }
	public WORD cu_YRSize { get; set; }
	public WORD cu_XROrigin { get; set; }
	public WORD cu_YROrigin { get; set; }
	public WORD cu_XRExtant { get; set; }
	public WORD cu_YRExtant { get; set; }
	public WORD cu_XMinShrink { get; set; }
	public WORD cu_YMinShrink { get; set; }
	public WORD cu_XCCP { get; set; }
	public WORD cu_YCCP { get; set; }
	public KeyMap cu_KeyMapStruct { get; set; }
	[AmigaArraySize(80)]
	public UWORD[] cu_TabStops { get; set; }
	public BYTE cu_Mask { get; set; }
	public BYTE cu_FgPen { get; set; }
	public BYTE cu_BgPen { get; set; }
	public BYTE cu_AOLPen { get; set; }
	public BYTE cu_DrawMode { get; set; }
	public BYTE cu_Obsolete1 { get; set; }
	public APTR cu_Obsolete2 { get; set; }
	[AmigaArraySize(8)]
	public UBYTE[] cu_Minterms { get; set; }
	public TextFontPtr cu_Font { get; set; }
	public UBYTE cu_AlgoStyle { get; set; }
	public UBYTE cu_TxFlags { get; set; }
	public UWORD cu_TxHeight { get; set; }
	public UWORD cu_TxWidth { get; set; }
	public UWORD cu_TxBaseline { get; set; }
	public WORD cu_TxSpacing { get; set; }
	[AmigaArraySize((((20+1)+1)+7)/8)]
	public UBYTE[] cu_Modes { get; set; }
	[AmigaArraySize((21+8)/8)]
	public UBYTE[] cu_RawEvents { get; set; }
}

