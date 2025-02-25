namespace Jammy.AmigaTypes;

public struct RigidDiskBlock
{
	public ULONG rdb_ID { get; set; }
	public ULONG rdb_SummedLongs { get; set; }
	public LONG rdb_ChkSum { get; set; }
	public ULONG rdb_HostID { get; set; }
	public ULONG rdb_BlockBytes { get; set; }
	public ULONG rdb_Flags { get; set; }
	public ULONG rdb_BadBlockList { get; set; }
	public ULONG rdb_PartitionList { get; set; }
	public ULONG rdb_FileSysHeaderList { get; set; }
	public ULONG rdb_DriveInit { get; set; }
	[AmigaArraySize(6)]
	public ULONG[] rdb_Reserved1 { get; set; }
	public ULONG rdb_Cylinders { get; set; }
	public ULONG rdb_Sectors { get; set; }
	public ULONG rdb_Heads { get; set; }
	public ULONG rdb_Interleave { get; set; }
	public ULONG rdb_Park { get; set; }
	[AmigaArraySize(3)]
	public ULONG[] rdb_Reserved2 { get; set; }
	public ULONG rdb_WritePreComp { get; set; }
	public ULONG rdb_ReducedWrite { get; set; }
	public ULONG rdb_StepRate { get; set; }
	[AmigaArraySize(5)]
	public ULONG[] rdb_Reserved3 { get; set; }
	public ULONG rdb_RDBBlocksLo { get; set; }
	public ULONG rdb_RDBBlocksHi { get; set; }
	public ULONG rdb_LoCylinder { get; set; }
	public ULONG rdb_HiCylinder { get; set; }
	public ULONG rdb_CylBlocks { get; set; }
	public ULONG rdb_AutoParkSeconds { get; set; }
	public ULONG rdb_HighRDSKBlock { get; set; }
	public ULONG rdb_Reserved4 { get; set; }
	[AmigaArraySize(8)]
	public char[] rdb_DiskVendor { get; set; }
	[AmigaArraySize(16)]
	public char[] rdb_DiskProduct { get; set; }
	[AmigaArraySize(4)]
	public char[] rdb_DiskRevision { get; set; }
	[AmigaArraySize(8)]
	public char[] rdb_ControllerVendor { get; set; }
	[AmigaArraySize(16)]
	public char[] rdb_ControllerProduct { get; set; }
	[AmigaArraySize(4)]
	public char[] rdb_ControllerRevision { get; set; }
	[AmigaArraySize(10)]
	public ULONG[] rdb_Reserved5 { get; set; }
}

public struct BadBlockEntry
{
	public ULONG bbe_BadBlock { get; set; }
	public ULONG bbe_GoodBlock { get; set; }
}

public struct BadBlockBlock
{
	public ULONG bbb_ID { get; set; }
	public ULONG bbb_SummedLongs { get; set; }
	public LONG bbb_ChkSum { get; set; }
	public ULONG bbb_HostID { get; set; }
	public ULONG bbb_Next { get; set; }
	public ULONG bbb_Reserved { get; set; }
	[AmigaArraySize(61)]
	public BadBlockEntry[] bbb_BlockPairs { get; set; }
}

public struct PartitionBlock
{
	public ULONG pb_ID { get; set; }
	public ULONG pb_SummedLongs { get; set; }
	public LONG pb_ChkSum { get; set; }
	public ULONG pb_HostID { get; set; }
	public ULONG pb_Next { get; set; }
	public ULONG pb_Flags { get; set; }
	[AmigaArraySize(2)]
	public ULONG[] pb_Reserved1 { get; set; }
	public ULONG pb_DevFlags { get; set; }
	[AmigaArraySize(32)]
	public UBYTE[] pb_DriveName { get; set; }
	[AmigaArraySize(15)]
	public ULONG[] pb_Reserved2 { get; set; }
	[AmigaArraySize(17)]
	public ULONG[] pb_Environment { get; set; }
	[AmigaArraySize(15)]
	public ULONG[] pb_EReserved { get; set; }
}

public struct FileSysHeaderBlock
{
	public ULONG fhb_ID { get; set; }
	public ULONG fhb_SummedLongs { get; set; }
	public LONG fhb_ChkSum { get; set; }
	public ULONG fhb_HostID { get; set; }
	public ULONG fhb_Next { get; set; }
	public ULONG fhb_Flags { get; set; }
	[AmigaArraySize(2)]
	public ULONG[] fhb_Reserved1 { get; set; }
	public ULONG fhb_DosType { get; set; }
	public ULONG fhb_Version { get; set; }
	public ULONG fhb_PatchFlags { get; set; }
	public ULONG fhb_Type { get; set; }
	public ULONG fhb_Task { get; set; }
	public ULONG fhb_Lock { get; set; }
	public ULONG fhb_Handler { get; set; }
	public ULONG fhb_StackSize { get; set; }
	public LONG fhb_Priority { get; set; }
	public LONG fhb_Startup { get; set; }
	public LONG fhb_SegListBlocks { get; set; }
	public LONG fhb_GlobalVec { get; set; }
	[AmigaArraySize(23)]
	public ULONG[] fhb_Reserved2 { get; set; }
	[AmigaArraySize(21)]
	public ULONG[] fhb_Reserved3 { get; set; }
}

public struct LoadSegBlock
{
	public ULONG lsb_ID { get; set; }
	public ULONG lsb_SummedLongs { get; set; }
	public LONG lsb_ChkSum { get; set; }
	public ULONG lsb_HostID { get; set; }
	public ULONG lsb_Next { get; set; }
	[AmigaArraySize(123)]
	public ULONG[] lsb_LoadData { get; set; }
}

