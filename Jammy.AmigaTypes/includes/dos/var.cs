namespace Jammy.AmigaTypes;

public struct LocalVar
{
	public Node lv_Node { get; set; }
	public UWORD lv_Flags { get; set; }
	public UBYTEPtr lv_Value { get; set; }
	public ULONG lv_Len { get; set; }
}

