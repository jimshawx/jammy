namespace Jammy.AmigaTypes;

public struct IoBuff
{
	public RexxRsrc iobNode { get; set; }
	public APTR iobRpt { get; set; }
	public LONG iobRct { get; set; }
	public LONG iobDFH { get; set; }
	public APTR iobLock { get; set; }
	public LONG iobBct { get; set; }
	[AmigaArraySize(204)]
	public BYTE[] iobArea { get; set; }
}

public struct RexxMsgPort
{
	public RexxRsrc rmp_Node { get; set; }
	public MsgPort rmp_Port { get; set; }
	public List rmp_ReplyList { get; set; }
}

