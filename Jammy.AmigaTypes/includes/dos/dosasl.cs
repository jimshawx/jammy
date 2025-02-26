namespace Jammy.AmigaTypes;

public class AnchorPath
{
	public AChainPtr ap_Base { get; set; }
	public AChainPtr ap_Last { get; set; }
	public LONG ap_BreakBits { get; set; }
	public LONG ap_FoundBreak { get; set; }
	public BYTE ap_Flags { get; set; }
	public BYTE ap_Reserved { get; set; }
	public WORD ap_Strlen { get; set; }
	public FileInfoBlock ap_Info { get; set; }
	[AmigaArraySize(1)]
	public UBYTE[] ap_Buf { get; set; }
}

public class AChain
{
	public AChainPtr an_Child { get; set; }
	public AChainPtr an_Parent { get; set; }
	public BPTR an_Lock { get; set; }
	public FileInfoBlock an_Info { get; set; }
	public BYTE an_Flags { get; set; }
	[AmigaArraySize(1)]
	public UBYTE[] an_String { get; set; }
}

