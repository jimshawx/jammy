namespace Jammy.AmigaTypes;

public struct CardHandle
{
	public Node cah_CardNode { get; set; }
	public InterruptPtr cah_CardRemoved { get; set; }
	public InterruptPtr cah_CardInserted { get; set; }
	public InterruptPtr cah_CardStatus { get; set; }
	public UBYTE cah_CardFlags { get; set; }
}

public struct DeviceTData
{
	public ULONG dtd_DTsize { get; set; }
	public ULONG dtd_DTspeed { get; set; }
	public UBYTE dtd_DTtype { get; set; }
	public UBYTE dtd_DTflags { get; set; }
}

public struct CardMemoryMap
{
	public UBYTEPtr cmm_CommonMemory { get; set; }
	public UBYTEPtr cmm_AttributeMemory { get; set; }
	public UBYTEPtr cmm_IOMemory { get; set; }
}

public struct TP_AmigaXIP
{
	public UBYTE TPL_CODE { get; set; }
	public UBYTE TPL_LINK { get; set; }
	[AmigaArraySize(4)]
	public UBYTE[] TP_XIPLOC { get; set; }
	public UBYTE TP_XIPFLAGS { get; set; }
	public UBYTE TP_XIPRESRV { get; set; }
}

