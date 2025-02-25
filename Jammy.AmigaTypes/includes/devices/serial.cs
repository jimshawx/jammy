namespace Jammy.AmigaTypes;

public struct IOTArray
{
	public ULONG TermArray0 { get; set; }
	public ULONG TermArray1 { get; set; }
}

public struct IOExtSer
{
	public IOStdReq IOSer { get; set; }
	public ULONG io_CtlChar { get; set; }
	public ULONG io_RBufLen { get; set; }
	public ULONG io_ExtFlags { get; set; }
	public ULONG io_Baud { get; set; }
	public ULONG io_BrkTime { get; set; }
	public IOTArray io_TermArray { get; set; }
	public UBYTE io_ReadLen { get; set; }
	public UBYTE io_WriteLen { get; set; }
	public UBYTE io_StopBits { get; set; }
	public UBYTE io_SerFlags { get; set; }
	public UWORD io_Status { get; set; }
}

