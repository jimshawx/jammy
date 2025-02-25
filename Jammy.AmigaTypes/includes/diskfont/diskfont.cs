namespace Jammy.AmigaTypes;

public struct FontContents
{
	[AmigaArraySize(256)]
	public char[] fc_FileName { get; set; }
	public UWORD fc_YSize { get; set; }
	public UBYTE fc_Style { get; set; }
	public UBYTE fc_Flags { get; set; }
}

public struct TFontContents
{
	[AmigaArraySize(256-2)]
	public char[] tfc_FileName { get; set; }
	public UWORD tfc_TagCount { get; set; }
	public UWORD tfc_YSize { get; set; }
	public UBYTE tfc_Style { get; set; }
	public UBYTE tfc_Flags { get; set; }
}

public struct FontContentsHeader
{
	public UWORD fch_FileID { get; set; }
	public UWORD fch_NumEntries { get; set; }
}

public struct DiskFontHeader
{
	public Node dfh_DF { get; set; }
	public UWORD dfh_FileID { get; set; }
	public UWORD dfh_Revision { get; set; }
	public LONG dfh_Segment { get; set; }
	[AmigaArraySize(32)]
	public char[] dfh_Name { get; set; }
	public TextFont dfh_TF { get; set; }
}

public struct AvailFonts
{
	public UWORD af_Type { get; set; }
	public TextAttr af_Attr { get; set; }
}

public struct TAvailFonts
{
	public UWORD taf_Type { get; set; }
	public TTextAttr taf_Attr { get; set; }
}

public struct AvailFontsHeader
{
	public UWORD afh_NumEntries { get; set; }
}

