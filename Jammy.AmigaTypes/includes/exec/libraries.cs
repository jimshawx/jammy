namespace Jammy.AmigaTypes;

public class Library
{
	public Node lib_Node { get; set; }
	public UBYTE lib_Flags { get; set; }
	public UBYTE lib_pad { get; set; }
	public UWORD lib_NegSize { get; set; }
	public UWORD lib_PosSize { get; set; }
	public UWORD lib_Version { get; set; }
	public UWORD lib_Revision { get; set; }
	public APTR lib_IdString { get; set; }
	public ULONG lib_Sum { get; set; }
	public UWORD lib_OpenCnt { get; set; }
}

