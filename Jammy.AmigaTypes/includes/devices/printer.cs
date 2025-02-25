namespace Jammy.AmigaTypes;

public struct IOPrtCmdReq
{
	public Message io_Message { get; set; }
	public DevicePtr io_Device { get; set; }
	public UnitPtr io_Unit { get; set; }
	public UWORD io_Command { get; set; }
	public UBYTE io_Flags { get; set; }
	public BYTE io_Error { get; set; }
	public UWORD io_PrtCommand { get; set; }
	public UBYTE io_Parm0 { get; set; }
	public UBYTE io_Parm1 { get; set; }
	public UBYTE io_Parm2 { get; set; }
	public UBYTE io_Parm3 { get; set; }
}

public struct IODRPReq
{
	public Message io_Message { get; set; }
	public DevicePtr io_Device { get; set; }
	public UnitPtr io_Unit { get; set; }
	public UWORD io_Command { get; set; }
	public UBYTE io_Flags { get; set; }
	public BYTE io_Error { get; set; }
	public RastPortPtr io_RastPort { get; set; }
	public ColorMapPtr io_ColorMap { get; set; }
	public ULONG io_Modes { get; set; }
	public UWORD io_SrcX { get; set; }
	public UWORD io_SrcY { get; set; }
	public UWORD io_SrcWidth { get; set; }
	public UWORD io_SrcHeight { get; set; }
	public LONG io_DestCols { get; set; }
	public LONG io_DestRows { get; set; }
	public UWORD io_Special { get; set; }
}

