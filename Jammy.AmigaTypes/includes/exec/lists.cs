namespace Jammy.AmigaTypes;

public class List
{
	public NodePtr lh_Head { get; set; }
	public NodePtr lh_Tail { get; set; }
	public NodePtr lh_TailPred { get; set; }
	public UBYTE lh_Type { get; set; }
	public UBYTE l_pad { get; set; }
}

public class MinList
{
	public MinNodePtr mlh_Head { get; set; }
	public MinNodePtr mlh_Tail { get; set; }
	public MinNodePtr mlh_TailPred { get; set; }
}

