namespace Jammy.AmigaTypes;

public struct RecordLock
{
	public BPTR rec_FH { get; set; }
	public ULONG rec_Offset { get; set; }
	public ULONG rec_Length { get; set; }
	public ULONG rec_Mode { get; set; }
}

