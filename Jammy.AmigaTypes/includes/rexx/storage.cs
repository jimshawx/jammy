namespace Jammy.AmigaTypes;

public struct NexxStr
{
	public LONG ns_Ivalue { get; set; }
	public UWORD ns_Length { get; set; }
	public UBYTE ns_Flags { get; set; }
	public UBYTE ns_Hash { get; set; }
	[AmigaArraySize(8)]
	public BYTE[] ns_Buff { get; set; }
}

public struct RexxArg
{
	public LONG ra_Size { get; set; }
	public UWORD ra_Length { get; set; }
	public UBYTE ra_Flags { get; set; }
	public UBYTE ra_Hash { get; set; }
	[AmigaArraySize(8)]
	public BYTE[] ra_Buff { get; set; }
}

public struct RexxMsg
{
	public Message rm_Node { get; set; }
	public APTR rm_TaskBlock { get; set; }
	public APTR rm_LibBase { get; set; }
	public LONG rm_Action { get; set; }
	public LONG rm_Result1 { get; set; }
	public LONG rm_Result2 { get; set; }
	[AmigaArraySize(16)]
	public STRPTR[] rm_Args { get; set; }
	public MsgPortPtr rm_PassPort { get; set; }
	public STRPTR rm_CommAddr { get; set; }
	public STRPTR rm_FileExt { get; set; }
	public LONG rm_Stdin { get; set; }
	public LONG rm_Stdout { get; set; }
	public LONG rm_avail { get; set; }
}

public struct RexxRsrc
{
	public Node rr_Node { get; set; }
	public WORD rr_Func { get; set; }
	public APTR rr_Base { get; set; }
	public LONG rr_Size { get; set; }
	public LONG rr_Arg1 { get; set; }
	public LONG rr_Arg2 { get; set; }
}

public struct RexxTask
{
	[AmigaArraySize(200)]
	public BYTE[] rt_Global { get; set; }
	public MsgPort rt_MsgPort { get; set; }
	public UBYTE rt_Flags { get; set; }
	public BYTE rt_SigBit { get; set; }
	public APTR rt_ClientID { get; set; }
	public APTR rt_MsgPkt { get; set; }
	public APTR rt_TaskID { get; set; }
	public APTR rt_RexxPort { get; set; }
	public APTR rt_ErrTrap { get; set; }
	public APTR rt_StackPtr { get; set; }
	public List rt_Header1 { get; set; }
	public List rt_Header2 { get; set; }
	public List rt_Header3 { get; set; }
	public List rt_Header4 { get; set; }
	public List rt_Header5 { get; set; }
}

public struct SrcNode
{
	public SrcNodePtr sn_Succ { get; set; }
	public SrcNodePtr sn_Pred { get; set; }
	public APTR sn_Ptr { get; set; }
	public LONG sn_Size { get; set; }
}

