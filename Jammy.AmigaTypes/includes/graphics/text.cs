namespace Jammy.AmigaTypes;

public class TextAttr
{
	public STRPTR ta_Name { get; set; }
	public UWORD ta_YSize { get; set; }
	public UBYTE ta_Style { get; set; }
	public UBYTE ta_Flags { get; set; }
}

public class TTextAttr
{
	public STRPTR tta_Name { get; set; }
	public UWORD tta_YSize { get; set; }
	public UBYTE tta_Style { get; set; }
	public UBYTE tta_Flags { get; set; }
	public TagItemPtr tta_Tags { get; set; }
}

public class TextFont
{
	public Message tf_Message { get; set; }
	public UWORD tf_YSize { get; set; }
	public UBYTE tf_Style { get; set; }
	public UBYTE tf_Flags { get; set; }
	public UWORD tf_XSize { get; set; }
	public UWORD tf_Baseline { get; set; }
	public UWORD tf_BoldSmear { get; set; }
	public UWORD tf_Accessors { get; set; }
	public UBYTE tf_LoChar { get; set; }
	public UBYTE tf_HiChar { get; set; }
	public APTR tf_CharData { get; set; }
	public UWORD tf_Modulo { get; set; }
	public APTR tf_CharLoc { get; set; }
	public APTR tf_CharSpace { get; set; }
	public APTR tf_CharKern { get; set; }
}

public class TextFontExtension
{
	public UWORD tfe_MatchWord { get; set; }
	public UBYTE tfe_Flags0 { get; set; }
	public UBYTE tfe_Flags1 { get; set; }
	public TextFontPtr tfe_BackPtr { get; set; }
	public MsgPortPtr tfe_OrigReplyPort { get; set; }
	public TagItemPtr tfe_Tags { get; set; }
	public UWORDPtr tfe_OFontPatchS { get; set; }
	public UWORDPtr tfe_OFontPatchK { get; set; }
}

public class ColorFontColors
{
	public UWORD cfc_Reserved { get; set; }
	public UWORD cfc_Count { get; set; }
	public UWORDPtr cfc_ColorTable { get; set; }
}

public class ColorTextFont
{
	public TextFont ctf_TF { get; set; }
	public UWORD ctf_Flags { get; set; }
	public UBYTE ctf_Depth { get; set; }
	public UBYTE ctf_FgColor { get; set; }
	public UBYTE ctf_Low { get; set; }
	public UBYTE ctf_High { get; set; }
	public UBYTE ctf_PlanePick { get; set; }
	public UBYTE ctf_PlaneOnOff { get; set; }
	public ColorFontColorsPtr ctf_ColorFontColors { get; set; }
	[AmigaArraySize(8)]
	public APTR[] ctf_CharData { get; set; }
}

public class TextExtent
{
	public UWORD te_Width { get; set; }
	public UWORD te_Height { get; set; }
	public Rectangle te_Extent { get; set; }
}

