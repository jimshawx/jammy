namespace Jammy.AmigaTypes;

public struct SCSICmd
{
	public UWORDPtr scsi_Data { get; set; }
	public ULONG scsi_Length { get; set; }
	public ULONG scsi_Actual { get; set; }
	public UBYTEPtr scsi_Command { get; set; }
	public UWORD scsi_CmdLength { get; set; }
	public UWORD scsi_CmdActual { get; set; }
	public UBYTE scsi_Flags { get; set; }
	public UBYTE scsi_Status { get; set; }
	public UBYTEPtr scsi_SenseData { get; set; }
	public UWORD scsi_SenseLength { get; set; }
	public UWORD scsi_SenseActual { get; set; }
}

