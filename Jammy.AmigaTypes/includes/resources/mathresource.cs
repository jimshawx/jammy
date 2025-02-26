namespace Jammy.AmigaTypes;

public class MathIEEEResource
{
	public Node MathIEEEResource_Node { get; set; }
	public unsignedshort MathIEEEResource_Flags { get; set; }
	public unsignedshortPtr MathIEEEResource_BaseAddr { get; set; }
	public FunctionPtr MathIEEEResource_DblBasInit { get; set; }
	public FunctionPtr MathIEEEResource_DblTransInit { get; set; }
	public FunctionPtr MathIEEEResource_SglBasInit { get; set; }
	public FunctionPtr MathIEEEResource_SglTransInit { get; set; }
	public FunctionPtr MathIEEEResource_ExtBasInit { get; set; }
	public FunctionPtr MathIEEEResource_ExtTransInit { get; set; }
}

