namespace Jammy.AmigaTypes;

public class GlyphEngine
{
	public LibraryPtr gle_Library { get; set; }
	public charPtr gle_Name { get; set; }
}

public class GlyphMap
{
	public UWORD glm_BMModulo { get; set; }
	public UWORD glm_BMRows { get; set; }
	public UWORD glm_BlackLeft { get; set; }
	public UWORD glm_BlackTop { get; set; }
	public UWORD glm_BlackWidth { get; set; }
	public UWORD glm_BlackHeight { get; set; }
	public FIXED glm_XOrigin { get; set; }
	public FIXED glm_YOrigin { get; set; }
	public WORD glm_X0 { get; set; }
	public WORD glm_Y0 { get; set; }
	public WORD glm_X1 { get; set; }
	public WORD glm_Y1 { get; set; }
	public FIXED glm_Width { get; set; }
	public UBYTEPtr glm_BitMap { get; set; }
}

public class GlyphWidthEntry
{
	public MinNode gwe_Node { get; set; }
	public UWORD gwe_Code { get; set; }
	public FIXED gwe_Width { get; set; }
}

