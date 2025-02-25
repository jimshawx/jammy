namespace Jammy.AmigaTypes;

public struct GamePortTrigger
{
	public UWORD gpt_Keys { get; set; }
	public UWORD gpt_Timeout { get; set; }
	public UWORD gpt_XDelta { get; set; }
	public UWORD gpt_YDelta { get; set; }
}

