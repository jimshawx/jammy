namespace Jammy.AmigaTypes;

public struct CopIns
{
	public WORD OpCode { get; set; }
//BROKEN - union not supported in C#
	public _u3 u3 { get; set; }
}

public struct _u3
{
	public CopListPtr nxtlist { get; set; }
	public _u4 u4 { get; set; }
}

public struct _u4
{
//BROKEN - union not supported in C#
	public _u1 u1 { get; set; }
//BROKEN - union not supported in C#
	public _u2 u2 { get; set; }
}

public struct _u1
{
	public WORD VWaitPos { get; set; }
	public WORD DestAddr { get; set; }
}

public struct _u2
{
	public WORD HWaitPos { get; set; }
	public WORD DestData { get; set; }
}




public struct cprlist
{
	public cprlistPtr Next { get; set; }
	public UWORDPtr start { get; set; }
	public WORD MaxCount { get; set; }
}

public struct CopList
{
	public CopListPtr Next { get; set; }
	public CopListPtr _CopList { get; set; }
	public ViewPortPtr _ViewPort { get; set; }
	public CopInsPtr CopIns { get; set; }
	public CopInsPtr CopPtr { get; set; }
	public UWORDPtr CopLStart { get; set; }
	public UWORDPtr CopSStart { get; set; }
	public WORD Count { get; set; }
	public WORD MaxCount { get; set; }
	public WORD DyOffset { get; set; }
	public UWORDPtr Cop2Start { get; set; }
	public UWORDPtr Cop3Start { get; set; }
	public UWORDPtr Cop4Start { get; set; }
	public UWORDPtr Cop5Start { get; set; }
}

public struct UCopList
{
	public UCopListPtr Next { get; set; }
	public CopListPtr FirstCopList { get; set; }
	public CopListPtr CopList { get; set; }
}

public struct copinit
{
	[AmigaArraySize(2)]
	public UWORD[] vsync_hblank { get; set; }
	[AmigaArraySize(4)]
	public UWORD[] diwstart { get; set; }
	[AmigaArraySize(4)]
	public UWORD[] diagstrt { get; set; }
	[AmigaArraySize((2*8*2))]
	public UWORD[] sprstrtup { get; set; }
	[AmigaArraySize(2)]
	public UWORD[] wait14 { get; set; }
	[AmigaArraySize(2)]
	public UWORD[] norm_hblank { get; set; }
	[AmigaArraySize(4)]
	public UWORD[] genloc { get; set; }
	[AmigaArraySize((2*2))]
	public UWORD[] jump { get; set; }
	[AmigaArraySize(2)]
	public UWORD[] wait_forever { get; set; }
	[AmigaArraySize(4)]
	public UWORD[] sprstop { get; set; }
}

