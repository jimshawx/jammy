namespace Jammy.AmigaTypes;

public class NewGadget
{
	public WORD ng_LeftEdge { get; set; }
	public WORD ng_TopEdge { get; set; }
	public WORD ng_Width { get; set; }
	public WORD ng_Height { get; set; }
	public UBYTEPtr ng_GadgetText { get; set; }
	public TextAttrPtr ng_TextAttr { get; set; }
	public UWORD ng_GadgetID { get; set; }
	public ULONG ng_Flags { get; set; }
	public APTR ng_VisualInfo { get; set; }
	public APTR ng_UserData { get; set; }
}

public class NewMenu
{
	public UBYTE nm_Type { get; set; }
	public STRPTR nm_Label { get; set; }
	public STRPTR nm_CommKey { get; set; }
	public UWORD nm_Flags { get; set; }
	public LONG nm_MutualExclude { get; set; }
	public APTR nm_UserData { get; set; }
}

