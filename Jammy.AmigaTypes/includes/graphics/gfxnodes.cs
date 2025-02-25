namespace Jammy.AmigaTypes;

public struct ExtendedNode
{
	public NodePtr xln_Succ { get; set; }
	public NodePtr xln_Pred { get; set; }
	public UBYTE xln_Type { get; set; }
	public BYTE xln_Pri { get; set; }
	public charPtr xln_Name { get; set; }
	public UBYTE xln_Subsystem { get; set; }
	public UBYTE xln_Subtype { get; set; }
	public LONG xln_Library { get; set; }
	public FunctionPtr xln_Init { get; set; }
}

