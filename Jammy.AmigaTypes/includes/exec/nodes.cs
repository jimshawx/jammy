namespace Jammy.AmigaTypes;

public struct Node
{
	public NodePtr ln_Succ { get; set; }
	public NodePtr ln_Pred { get; set; }
	public UBYTE ln_Type { get; set; }
	public BYTE ln_Pri { get; set; }
	public charPtr ln_Name { get; set; }
}

public struct MinNode
{
	public MinNodePtr mln_Succ { get; set; }
	public MinNodePtr mln_Pred { get; set; }
}

