namespace Jammy.AmigaTypes;

public class impFrameBox
{
	public ULONG MethodID { get; set; }
	public IBoxPtr imp_ContentsBox { get; set; }
	public IBoxPtr imp_FrameBox { get; set; }
	public DrawInfoPtr imp_DrInfo { get; set; }
	public ULONG imp_FrameFlags { get; set; }
}

public class impDraw
{
	public ULONG MethodID { get; set; }
	public RastPortPtr imp_RPort { get; set; }
	public _imp_Offset imp_Offset { get; set; }
	public ULONG imp_State { get; set; }
	public DrawInfoPtr imp_DrInfo { get; set; }
	public _imp_Dimensions imp_Dimensions { get; set; }
}

public class _imp_Offset
{
	public WORD X { get; set; }
	public WORD Y { get; set; }
}

public class _imp_Dimensions
{
	public WORD Width { get; set; }
	public WORD Height { get; set; }
}


public class impErase
{
	public ULONG MethodID { get; set; }
	public RastPortPtr imp_RPort { get; set; }
	public _imp_Offset imp_Offset { get; set; }
	public _imp_Dimensions imp_Dimensions { get; set; }
}

public class impHitTest
{
	public ULONG MethodID { get; set; }
	public _imp_Point imp_Point { get; set; }
	public _imp_Dimensions imp_Dimensions { get; set; }
}

public class _imp_Point
{
	public WORD X { get; set; }
	public WORD Y { get; set; }
}


