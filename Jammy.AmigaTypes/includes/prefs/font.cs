namespace Jammy.AmigaTypes;

public class FontPrefs
{
	[AmigaArraySize(3)]
	public LONG[] fp_Reserved { get; set; }
	public UWORD fp_Reserved2 { get; set; }
	public UWORD fp_Type { get; set; }
	public UBYTE fp_FrontPen { get; set; }
	public UBYTE fp_BackPen { get; set; }
	public UBYTE fp_DrawMode { get; set; }
	public TextAttr fp_TextAttr { get; set; }
	[AmigaArraySize((128))]
	public BYTE[] fp_Name { get; set; }
}

