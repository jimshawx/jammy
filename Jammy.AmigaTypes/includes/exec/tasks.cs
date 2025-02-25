namespace Jammy.AmigaTypes;

public struct Task
{
	public Node tc_Node { get; set; }
	public UBYTE tc_Flags { get; set; }
	public UBYTE tc_State { get; set; }
	public BYTE tc_IDNestCnt { get; set; }
	public BYTE tc_TDNestCnt { get; set; }
	public ULONG tc_SigAlloc { get; set; }
	public ULONG tc_SigWait { get; set; }
	public ULONG tc_SigRecvd { get; set; }
	public ULONG tc_SigExcept { get; set; }
	public UWORD tc_TrapAlloc { get; set; }
	public UWORD tc_TrapAble { get; set; }
	public APTR tc_ExceptData { get; set; }
	public APTR tc_ExceptCode { get; set; }
	public APTR tc_TrapData { get; set; }
	public APTR tc_TrapCode { get; set; }
	public APTR tc_SPReg { get; set; }
	public APTR tc_SPLower { get; set; }
	public APTR tc_SPUpper { get; set; }
	public FunctionPtr tc_Switch { get; set; }
	public FunctionPtr tc_Launch { get; set; }
	public List tc_MemEntry { get; set; }
	public APTR tc_UserData { get; set; }
}

public struct StackSwapStruct
{
	public APTR stk_Lower { get; set; }
	public ULONG stk_Upper { get; set; }
	public APTR stk_Pointer { get; set; }
}

