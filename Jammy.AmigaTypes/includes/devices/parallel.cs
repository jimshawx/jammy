namespace Jammy.AmigaTypes;

public struct IOPArray
{
	public ULONG PTermArray0 { get; set; }
	public ULONG PTermArray1 { get; set; }
}

public struct IOExtPar
{
	public IOStdReq IOPar { get; set; }
	public ULONG io_PExtFlags { get; set; }
	public UBYTE io_Status { get; set; }
	public UBYTE io_ParFlags { get; set; }
	public IOPArray io_PTermArray { get; set; }
}

