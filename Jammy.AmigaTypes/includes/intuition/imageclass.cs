namespace Jammy.AmigaTypes;

public struct impFrameBox
{
	public ULONG MethodID { get; set; }
	public IBoxPtr imp_ContentsBox { get; set; }
	public IBoxPtr imp_FrameBox { get; set; }
	public DrawInfoPtr imp_DrInfo { get; set; }
	public ULONG imp_FrameFlags { get; set; }
}

public struct impDraw
{
	public ULONG MethodID { get; set; }
	public RastPortPtr imp_RPort { get; set; }
	public _imp_Offset imp_Offset { get; set; }
	public ULONG imp_State { get; set; }
	public DrawInfoPtr imp_DrInfo { get; set; }
	public _imp_Dimensions imp_Dimensions { get; set; }
}

public struct _imp_Offset
{
	public WORD X { get; set; }
	public WORD Y { get; set; }
}

public struct _imp_Dimensions
{
	public WORD Width { get; set; }
	public WORD Height { get; set; }
}

public struct impErase
{
	public ULONG MethodID { get; set; }
	public RastPortPtr imp_RPort { get; set; }
	public _imp_Offset imp_Offset { get; set; }
	public _imp_Dimensions imp_Dimensions { get; set; }
}

public struct impHitTest
{
	public ULONG MethodID { get; set; }
	public _imp_Point imp_Point { get; set; }
	public _imp_Dimensions imp_Dimensions { get; set; }
}

public struct _imp_Point
{
	public WORD X { get; set; }
	public WORD Y { get; set; }
}
