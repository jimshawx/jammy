namespace Jammy.AmigaTypes;

public class Interrupt
{
	public Node is_Node { get; set; }
	public APTR is_Data { get; set; }
	public FunctionPtr is_Code { get; set; }
}

public class IntVector
{
	public APTR iv_Data { get; set; }
	public FunctionPtr iv_Code { get; set; }
	public NodePtr iv_Node { get; set; }
}

public class SoftIntList
{
	public List sh_List { get; set; }
	public UWORD sh_Pad { get; set; }
}

