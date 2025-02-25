namespace Jammy.AmigaTypes;

public struct IORequest
{
	public Message io_Message { get; set; }
	public DevicePtr io_Device { get; set; }
	public UnitPtr io_Unit { get; set; }
	public UWORD io_Command { get; set; }
	public UBYTE io_Flags { get; set; }
	public BYTE io_Error { get; set; }
}

public struct IOStdReq
{
	public Message io_Message { get; set; }
	public DevicePtr io_Device { get; set; }
	public UnitPtr io_Unit { get; set; }
	public UWORD io_Command { get; set; }
	public UBYTE io_Flags { get; set; }
	public BYTE io_Error { get; set; }
	public ULONG io_Actual { get; set; }
	public ULONG io_Length { get; set; }
	public APTR io_Data { get; set; }
	public ULONG io_Offset { get; set; }
}

