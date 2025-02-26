namespace Jammy.AmigaTypes;

public class RxsLib
{
	public Library rl_Node { get; set; }
	public UBYTE rl_Flags { get; set; }
	public UBYTE rl_Shadow { get; set; }
	public APTR rl_SysBase { get; set; }
	public APTR rl_DOSBase { get; set; }
	public APTR rl_IeeeDPBase { get; set; }
	public LONG rl_SegList { get; set; }
	public LONG rl_NIL { get; set; }
	public LONG rl_Chunk { get; set; }
	public LONG rl_MaxNest { get; set; }
	public NexxStrPtr rl_NULL { get; set; }
	public NexxStrPtr rl_FALSE { get; set; }
	public NexxStrPtr rl_TRUE { get; set; }
	public NexxStrPtr rl_REXX { get; set; }
	public NexxStrPtr rl_COMMAND { get; set; }
	public NexxStrPtr rl_STDIN { get; set; }
	public NexxStrPtr rl_STDOUT { get; set; }
	public NexxStrPtr rl_STDERR { get; set; }
	public STRPTR rl_Version { get; set; }
	public STRPTR rl_TaskName { get; set; }
	public LONG rl_TaskPri { get; set; }
	public LONG rl_TaskSeg { get; set; }
	public LONG rl_StackSize { get; set; }
	public STRPTR rl_RexxDir { get; set; }
	public STRPTR rl_CTABLE { get; set; }
	public STRPTR rl_Notice { get; set; }
	public MsgPort rl_RexxPort { get; set; }
	public UWORD rl_ReadLock { get; set; }
	public LONG rl_TraceFH { get; set; }
	public List rl_TaskList { get; set; }
	public WORD rl_NumTask { get; set; }
	public List rl_LibList { get; set; }
	public WORD rl_NumLib { get; set; }
	public List rl_ClipList { get; set; }
	public WORD rl_NumClip { get; set; }
	public List rl_MsgList { get; set; }
	public WORD rl_NumMsg { get; set; }
	public List rl_PgmList { get; set; }
	public WORD rl_NumPgm { get; set; }
	public UWORD rl_TraceCnt { get; set; }
	public WORD rl_avail { get; set; }
}

