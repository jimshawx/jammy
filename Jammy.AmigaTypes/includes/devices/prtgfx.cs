namespace Jammy.AmigaTypes;

public struct PrtInfo
{
	public FunctionPtr pi_render { get; set; }
	public RastPortPtr pi_rp { get; set; }
	public RastPortPtr pi_temprp { get; set; }
	public UWORDPtr pi_RowBuf { get; set; }
	public UWORDPtr pi_HamBuf { get; set; }
//BROKEN - union not supported in C#
	public colorEntryPtr pi_ColorMap { get; set; }
//BROKEN - union not supported in C#
	public colorEntryPtr pi_ColorInt { get; set; }
//BROKEN - union not supported in C#
	public colorEntryPtr pi_HamInt { get; set; }
//BROKEN - union not supported in C#
	public colorEntryPtr pi_Dest1Int { get; set; }
//BROKEN - union not supported in C#
	public colorEntryPtr pi_Dest2Int { get; set; }
	public UWORDPtr pi_ScaleX { get; set; }
	public UWORDPtr pi_ScaleXAlt { get; set; }
	public UBYTEPtr pi_dmatrix { get; set; }
	public UWORDPtr pi_TopBuf { get; set; }
	public UWORDPtr pi_BotBuf { get; set; }
	public UWORD pi_RowBufSize { get; set; }
	public UWORD pi_HamBufSize { get; set; }
	public UWORD pi_ColorMapSize { get; set; }
	public UWORD pi_ColorIntSize { get; set; }
	public UWORD pi_HamIntSize { get; set; }
	public UWORD pi_Dest1IntSize { get; set; }
	public UWORD pi_Dest2IntSize { get; set; }
	public UWORD pi_ScaleXSize { get; set; }
	public UWORD pi_ScaleXAltSize { get; set; }
	public UWORD pi_PrefsFlags { get; set; }
	public ULONG pi_special { get; set; }
	public UWORD pi_xstart { get; set; }
	public UWORD pi_ystart { get; set; }
	public UWORD pi_width { get; set; }
	public UWORD pi_height { get; set; }
	public ULONG pi_pc { get; set; }
	public ULONG pi_pr { get; set; }
	public UWORD pi_ymult { get; set; }
	public UWORD pi_ymod { get; set; }
	public WORD pi_ety { get; set; }
	public UWORD pi_xpos { get; set; }
	public UWORD pi_threshold { get; set; }
	public UWORD pi_tempwidth { get; set; }
	public UWORD pi_flags { get; set; }
}

