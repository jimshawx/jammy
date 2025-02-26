namespace Jammy.AmigaTypes;

public class DosEnvec
{
	public ULONG de_TableSize { get; set; }
	public ULONG de_SizeBlock { get; set; }
	public ULONG de_SecOrg { get; set; }
	public ULONG de_Surfaces { get; set; }
	public ULONG de_SectorPerBlock { get; set; }
	public ULONG de_BlocksPerTrack { get; set; }
	public ULONG de_Reserved { get; set; }
	public ULONG de_PreAlloc { get; set; }
	public ULONG de_Interleave { get; set; }
	public ULONG de_LowCyl { get; set; }
	public ULONG de_HighCyl { get; set; }
	public ULONG de_NumBuffers { get; set; }
	public ULONG de_BufMemType { get; set; }
	public ULONG de_MaxTransfer { get; set; }
	public ULONG de_Mask { get; set; }
	public LONG de_BootPri { get; set; }
	public ULONG de_DosType { get; set; }
	public ULONG de_Baud { get; set; }
	public ULONG de_Control { get; set; }
	public ULONG de_BootBlocks { get; set; }
}

public class FileSysStartupMsg
{
	public ULONG fssm_Unit { get; set; }
	public BSTR fssm_Device { get; set; }
	public BPTR fssm_Environ { get; set; }
	public ULONG fssm_Flags { get; set; }
}

public class DeviceNode
{
	public BPTR dn_Next { get; set; }
	public ULONG dn_Type { get; set; }
	public MsgPortPtr dn_Task { get; set; }
	public BPTR dn_Lock { get; set; }
	public BSTR dn_Handler { get; set; }
	public ULONG dn_StackSize { get; set; }
	public LONG dn_Priority { get; set; }
	public BPTR dn_Startup { get; set; }
	public BPTR dn_SegList { get; set; }
	public BPTR dn_GlobalVec { get; set; }
	public BSTR dn_Name { get; set; }
}

