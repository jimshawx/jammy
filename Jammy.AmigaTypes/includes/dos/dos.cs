namespace Jammy.AmigaTypes;

public struct DateStamp
{
	public LONG ds_Days { get; set; }
	public LONG ds_Minute { get; set; }
	public LONG ds_Tick { get; set; }
}

public struct FileInfoBlock
{
	public LONG fib_DiskKey { get; set; }
	public LONG fib_DirEntryType { get; set; }
	[AmigaArraySize(108)]
	public char[] fib_FileName { get; set; }
	public LONG fib_Protection { get; set; }
	public LONG fib_EntryType { get; set; }
	public LONG fib_Size { get; set; }
	public LONG fib_NumBlocks { get; set; }
	public DateStamp fib_Date { get; set; }
	[AmigaArraySize(80)]
	public char[] fib_Comment { get; set; }
	[AmigaArraySize(36)]
	public char[] fib_Reserved { get; set; }
}

public struct InfoData
{
	public LONG id_NumSoftErrors { get; set; }
	public LONG id_UnitNumber { get; set; }
	public LONG id_DiskState { get; set; }
	public LONG id_NumBlocks { get; set; }
	public LONG id_NumBlocksUsed { get; set; }
	public LONG id_BytesPerBlock { get; set; }
	public LONG id_DiskType { get; set; }
	public BPTR id_VolumeNode { get; set; }
	public LONG id_InUse { get; set; }
}

