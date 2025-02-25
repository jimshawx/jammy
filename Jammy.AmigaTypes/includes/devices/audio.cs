namespace Jammy.AmigaTypes;

public struct IOAudio
{
	public IORequest ioa_Request { get; set; }
	public WORD ioa_AllocKey { get; set; }
	public UBYTEPtr ioa_Data { get; set; }
	public ULONG ioa_Length { get; set; }
	public UWORD ioa_Period { get; set; }
	public UWORD ioa_Volume { get; set; }
	public UWORD ioa_Cycles { get; set; }
	public Message ioa_WriteMsg { get; set; }
}

