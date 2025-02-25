namespace Jammy.AmigaTypes;

public struct MemChunk
{
	public MemChunkPtr mc_Next { get; set; }
	public ULONG mc_Bytes { get; set; }
}

public struct MemHeader
{
	public Node mh_Node { get; set; }
	public UWORD mh_Attributes { get; set; }
	public MemChunkPtr mh_First { get; set; }
	public APTR mh_Lower { get; set; }
	public APTR mh_Upper { get; set; }
	public ULONG mh_Free { get; set; }
}

public struct MemEntry
{
//BROKEN - union not supported in C#
	public _me_Un me_Un { get; set; }
	public ULONG me_Length { get; set; }
}

public struct _me_Un
{
	public ULONG meu_Reqs { get; set; }
	public APTR meu_Addr { get; set; }
}


public struct MemList
{
	public Node ml_Node { get; set; }
	public UWORD ml_NumEntries { get; set; }
	[AmigaArraySize(1)]
	public MemEntry[] ml_ME { get; set; }
}

