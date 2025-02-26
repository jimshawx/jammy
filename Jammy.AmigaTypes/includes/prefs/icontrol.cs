namespace Jammy.AmigaTypes;

public class IControlPrefs
{
	[AmigaArraySize(4)]
	public LONG[] ic_Reserved { get; set; }
	public UWORD ic_TimeOut { get; set; }
	public WORD ic_MetaDrag { get; set; }
	public ULONG ic_Flags { get; set; }
	public UBYTE ic_WBtoFront { get; set; }
	public UBYTE ic_FrontToBack { get; set; }
	public UBYTE ic_ReqTrue { get; set; }
	public UBYTE ic_ReqFalse { get; set; }
}

