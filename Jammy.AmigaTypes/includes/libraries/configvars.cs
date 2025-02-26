namespace Jammy.AmigaTypes;

public class ConfigDev
{
	public Node cd_Node { get; set; }
	public UBYTE cd_Flags { get; set; }
	public UBYTE cd_Pad { get; set; }
	public ExpansionRom cd_Rom { get; set; }
	public APTR cd_BoardAddr { get; set; }
	public ULONG cd_BoardSize { get; set; }
	public UWORD cd_SlotAddr { get; set; }
	public UWORD cd_SlotSize { get; set; }
	public APTR cd_Driver { get; set; }
	public ConfigDevPtr cd_NextCD { get; set; }
	[AmigaArraySize(4)]
	public ULONG[] cd_Unused { get; set; }
}

public class CurrentBinding
{
	public ConfigDevPtr cb_ConfigDev { get; set; }
	public UBYTEPtr cb_FileName { get; set; }
	public UBYTEPtr cb_ProductString { get; set; }
	public UBYTEPtrPtr cb_ToolTypes { get; set; }
}

