namespace Jammy.AmigaTypes;

public struct MsgPort
{
	public Node mp_Node { get; set; }
	public UBYTE mp_Flags { get; set; }
	public UBYTE mp_SigBit { get; set; }
	public voidPtr mp_SigTask { get; set; }
	public List mp_MsgList { get; set; }
}

public struct Message
{
	public Node mn_Node { get; set; }
	public MsgPortPtr mn_ReplyPort { get; set; }
	public UWORD mn_Length { get; set; }
}

