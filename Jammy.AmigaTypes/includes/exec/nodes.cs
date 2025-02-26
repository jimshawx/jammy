namespace Jammy.AmigaTypes;

public class Node
{
	public NodePtr ln_Succ { get; set; }
	public NodePtr ln_Pred { get; set; }
	public NodeType ln_Type { get; set; }
	public BYTE ln_Pri { get; set; }
	public charPtr ln_Name { get; set; }
}

public class MinNode
{
	public MinNodePtr mln_Succ { get; set; }
	public MinNodePtr mln_Pred { get; set; }
}

