namespace Jammy.AmigaTypes;

public struct ClockData
{
	public UWORD sec { get; set; }
	public UWORD min { get; set; }
	public UWORD hour { get; set; }
	public UWORD mday { get; set; }
	public UWORD month { get; set; }
	public UWORD year { get; set; }
	public UWORD wday { get; set; }
}

