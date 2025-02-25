namespace Jammy.AmigaTypes;

public struct Isrvstr
{
	public Node is_Node { get; set; }
	public IsrvstrPtr Iptr { get; set; }
	public FunctionPtr code { get; set; }
	public FunctionPtr ccode { get; set; }
	public int Carg { get; set; }
}

