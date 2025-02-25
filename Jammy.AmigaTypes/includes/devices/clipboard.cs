namespace Jammy.AmigaTypes;

public struct ClipboardUnitPartial
{
	public Node cu_Node { get; set; }
	public ULONG cu_UnitNum { get; set; }
}

public struct IOClipReq
{
	public Message io_Message { get; set; }
	public DevicePtr io_Device { get; set; }
	public ClipboardUnitPartialPtr io_Unit { get; set; }
	public UWORD io_Command { get; set; }
	public UBYTE io_Flags { get; set; }
	public BYTE io_Error { get; set; }
	public ULONG io_Actual { get; set; }
	public ULONG io_Length { get; set; }
	public STRPTR io_Data { get; set; }
	public ULONG io_Offset { get; set; }
	public LONG io_ClipID { get; set; }
}

public struct SatisfyMsg
{
	public Message sm_Msg { get; set; }
	public UWORD sm_Unit { get; set; }
	public LONG sm_ClipID { get; set; }
}

public struct ClipHookMsg
{
	public ULONG chm_Type { get; set; }
	public LONG chm_ChangeCmd { get; set; }
	public LONG chm_ClipID { get; set; }
}

