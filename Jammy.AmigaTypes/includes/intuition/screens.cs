namespace Jammy.AmigaTypes;

public class DrawInfo
{
	public UWORD dri_Version { get; set; }
	public UWORD dri_NumPens { get; set; }
	public UWORDPtr dri_Pens { get; set; }
	public TextFontPtr dri_Font { get; set; }
	public UWORD dri_Depth { get; set; }
	public _dri_Resolution dri_Resolution { get; set; }
	public ULONG dri_Flags { get; set; }
	[AmigaArraySize(7)]
	public ULONG[] dri_Reserved { get; set; }
}

public class _dri_Resolution
{
	public UWORD X { get; set; }
	public UWORD Y { get; set; }
}


public class Screen
{
	public ScreenPtr NextScreen { get; set; }
	public WindowPtr FirstWindow { get; set; }
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public WORD MouseY { get; set; }
	public WORD MouseX { get; set; }
	public UWORD Flags { get; set; }
	public UBYTEPtr Title { get; set; }
	public UBYTEPtr DefaultTitle { get; set; }
	public BYTE BarHeight { get; set; }
	public BYTE BarVBorder { get; set; }
	public BYTE BarHBorder { get; set; }
	public BYTE MenuVBorder { get; set; }
	public BYTE MenuHBorder { get; set; }
	public BYTE WBorTop { get; set; }
	public BYTE WBorLeft { get; set; }
	public BYTE WBorRight { get; set; }
	public BYTE WBorBottom { get; set; }
	public TextAttrPtr Font { get; set; }
	public ViewPort ViewPort { get; set; }
	public RastPort RastPort { get; set; }
	public BitMap BitMap { get; set; }
	public Layer_Info LayerInfo { get; set; }
	public GadgetPtr FirstGadget { get; set; }
	public UBYTE DetailPen { get; set; }
	public UBYTE BlockPen { get; set; }
	public UWORD SaveColor0 { get; set; }
	public LayerPtr BarLayer { get; set; }
	public UBYTEPtr ExtData { get; set; }
	public UBYTEPtr UserData { get; set; }
}

public class NewScreen
{
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public WORD Depth { get; set; }
	public UBYTE DetailPen { get; set; }
	public UBYTE BlockPen { get; set; }
	public UWORD ViewModes { get; set; }
	public UWORD Type { get; set; }
	public TextAttrPtr Font { get; set; }
	public UBYTEPtr DefaultTitle { get; set; }
	public GadgetPtr Gadgets { get; set; }
	public BitMapPtr CustomBitMap { get; set; }
}

public class ExtNewScreen
{
	public WORD LeftEdge { get; set; }
	public WORD TopEdge { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	public WORD Depth { get; set; }
	public UBYTE DetailPen { get; set; }
	public UBYTE BlockPen { get; set; }
	public UWORD ViewModes { get; set; }
	public UWORD Type { get; set; }
	public TextAttrPtr Font { get; set; }
	public UBYTEPtr DefaultTitle { get; set; }
	public GadgetPtr Gadgets { get; set; }
	public BitMapPtr CustomBitMap { get; set; }
	public TagItemPtr Extension { get; set; }
}

public class PubScreenNode
{
	public Node psn_Node { get; set; }
	public ScreenPtr psn_Screen { get; set; }
	public UWORD psn_Flags { get; set; }
	public WORD psn_Size { get; set; }
	public WORD psn_VisitorCount { get; set; }
	public TaskPtr psn_SigTask { get; set; }
	public UBYTE psn_SigBit { get; set; }
}

