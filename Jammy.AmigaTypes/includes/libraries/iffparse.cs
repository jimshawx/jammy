namespace Jammy.AmigaTypes;

public struct IFFHandle
{
	public ULONG iff_Stream { get; set; }
	public ULONG iff_Flags { get; set; }
	public LONG iff_Depth { get; set; }
}

public struct IFFStreamCmd
{
	public LONG sc_Command { get; set; }
	public APTR sc_Buf { get; set; }
	public LONG sc_NBytes { get; set; }
}

public struct ContextNode
{
	public MinNode cn_Node { get; set; }
	public LONG cn_ID { get; set; }
	public LONG cn_Type { get; set; }
	public LONG cn_Size { get; set; }
	public LONG cn_Scan { get; set; }
}

public struct LocalContextItem
{
	public MinNode lci_Node { get; set; }
	public ULONG lci_ID { get; set; }
	public ULONG lci_Type { get; set; }
	public ULONG lci_Ident { get; set; }
}

public struct StoredProperty
{
	public LONG sp_Size { get; set; }
	public UBYTEPtr sp_Data { get; set; }
}

public struct CollectionItem
{
	public CollectionItemPtr ci_Next { get; set; }
	public LONG ci_Size { get; set; }
	public UBYTEPtr ci_Data { get; set; }
}

public struct ClipboardHandle
{
	public IOClipReq cbh_Req { get; set; }
	public MsgPort cbh_CBport { get; set; }
	public MsgPort cbh_SatisfyPort { get; set; }
}

