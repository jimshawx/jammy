namespace Jammy.AmigaTypes;

public struct BootNode
{
	public Node bn_Node { get; set; }
	public UWORD bn_Flags { get; set; }
	public APTR bn_DeviceNode { get; set; }
}

public struct ExpansionBase
{
	public Library LibNode { get; set; }
	public UBYTE Flags { get; set; }
	public UBYTE eb_Private01 { get; set; }
	public ULONG eb_Private02 { get; set; }
	public ULONG eb_Private03 { get; set; }
	public CurrentBinding eb_Private04 { get; set; }
	public List eb_Private05 { get; set; }
	public List MountList { get; set; }
}

