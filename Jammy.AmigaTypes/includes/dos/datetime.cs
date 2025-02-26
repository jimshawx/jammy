namespace Jammy.AmigaTypes;

public class DateTime
{
	public DateStamp dat_Stamp { get; set; }
	public UBYTE dat_Format { get; set; }
	public UBYTE dat_Flags { get; set; }
	public UBYTEPtr dat_StrDay { get; set; }
	public UBYTEPtr dat_StrDate { get; set; }
	public UBYTEPtr dat_StrTime { get; set; }
}

