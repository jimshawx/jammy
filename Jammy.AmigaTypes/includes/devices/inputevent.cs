namespace Jammy.AmigaTypes;

public struct IEPointerPixel
{
	public ScreenPtr iepp_Screen { get; set; }
	public _iepp_Position iepp_Position { get; set; }
}

public struct _iepp_Position
{
	public WORD X { get; set; }
	public WORD Y { get; set; }
}


public struct IEPointerTablet
{
	public _iept_Range iept_Range { get; set; }
	public _iept_Value iept_Value { get; set; }
	public WORD iept_Pressure { get; set; }
}

public struct _iept_Range
{
	public UWORD X { get; set; }
	public UWORD Y { get; set; }
}

public struct _iept_Value
{
	public UWORD X { get; set; }
	public UWORD Y { get; set; }
}


public struct InputEvent
{
	public InputEventPtr ie_NextEvent { get; set; }
	public UBYTE ie_Class { get; set; }
	public UBYTE ie_SubClass { get; set; }
	public UWORD ie_Code { get; set; }
	public UWORD ie_Qualifier { get; set; }
//BROKEN - union not supported in C#
	public _ie_position ie_position { get; set; }
	public timeval ie_TimeStamp { get; set; }
}

public struct _ie_position
{
	public _ie_xy ie_xy { get; set; }
	public APTR ie_addr { get; set; }
	public _ie_dead ie_dead { get; set; }
}

public struct _ie_xy
{
	public WORD ie_x { get; set; }
	public WORD ie_y { get; set; }
}

public struct _ie_dead
{
	public UBYTE ie_prev1DownCode { get; set; }
	public UBYTE ie_prev1DownQual { get; set; }
	public UBYTE ie_prev2DownCode { get; set; }
	public UBYTE ie_prev2DownQual { get; set; }
}



