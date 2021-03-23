using System;
using RunAmiga.Core.Interface.Interfaces;

namespace RunAmiga.Disassembler.AmigaTypes
{
	using UBYTE = System.Byte;
	using BYTE = System.SByte;
	using UWORD = System.UInt16;
	using WORD = System.Int16;
	using ULONG = System.UInt32;
	using LONG = System.Int32;

	using APTR = System.UInt32;
	using FunctionPtr = System.UInt32;

	using CharPtr = System.String;

	using DevicePtr = System.UInt32;
	using UnitPtr = System.UInt32;
	using VoidPtr = System.UInt32;
	using UBYTEPtr = System.UInt32;
	using ULONGPtr = System.UInt32;

	public static class AmigaTypesMapper
	{
		public static uint GetSize(object s)
		{
			if (s.GetType() == typeof(BYTE) || s.GetType() == typeof(UBYTE) || s.GetType() == typeof(NodeType)) return 1;
			if (s.GetType() == typeof(WORD) || s.GetType() == typeof(UWORD)) return 2;
			if (s.GetType() == typeof(LONG) || s.GetType() == typeof(ULONG) || s.GetType() == typeof(APTR) || s.GetType() == typeof(FunctionPtr)) return 4;
			throw new ApplicationException();
		}

		public static object MapSimple(IDebugMemoryMapper memory, Type type, uint addr)
		{
			if (type == typeof(NodeType)) return (NodeType)memory.UnsafeRead8(addr);
			if (type == typeof(BYTE)) return (BYTE)memory.UnsafeRead8(addr);
			if (type == typeof(UBYTE)) return (UBYTE)memory.UnsafeRead8(addr);
			if (type == typeof(UWORD)) return (UWORD)memory.UnsafeRead16(addr);
			if (type == typeof(WORD)) return (WORD)memory.UnsafeRead16(addr);
			if (type == typeof(ULONG)) return (ULONG)memory.UnsafeRead32(addr);
			if (type == typeof(LONG)) return (LONG)memory.UnsafeRead32(addr);
			if (type == typeof(APTR)) return (APTR)memory.UnsafeRead32(addr);
			if (type == typeof(FunctionPtr)) return (FunctionPtr)memory.UnsafeRead32(addr);
			throw new ApplicationException();
		}
	}

	public enum NodeType
	{
		NT_UNKNOWN = 0,
		NT_TASK = 1,    /* Exec task */
		NT_INTERRUPT = 2,
		NT_DEVICE = 3,
		NT_MSGPORT = 4,
		NT_MESSAGE = 5, /* Indicates message currently pending */
		NT_FREEMSG = 6,
		NT_REPLYMSG = 7,    /* Message has been replied */
		NT_RESOURCE = 8,
		NT_LIBRARY = 9,
		NT_MEMORY = 10,
		NT_SOFTINT = 11,    /* Internal flag used by SoftInits */
		NT_FONT = 12,
		NT_PROCESS = 13,    /* AmigaDOS Process */
		NT_SEMAPHORE = 14,
		NT_SIGNALSEM = 15,  /* signal semaphores */
		NT_BOOTNODE = 16,
		NT_KICKMEM = 17,
		NT_GRAPHICS = 18,
		NT_DEATHMESSAGE = 19,

		NT_USER = 254,  /* User node types work down from here */
		NT_EXTENDED = 255,
	}

	public interface IWrappedPtr { }
	public interface IWrappedPtr<T> : IWrappedPtr
	{
		uint Address { get; set; }
		T Wrapped { get; set; }
	}

	public class TaskPtr : IWrappedPtr
	{
		public uint Address { get; set; }
		public Task Task { get; set; }
	}

	public class NodePtr : IWrappedPtr
	{
		public uint Address { get; set; }
		public Node Node { get; set; }
	}

	public class MinNodePtr : IWrappedPtr
	{
		public uint Address { get; set; }
		public MinNode MinNode { get; set; }
	}

	public class MsgPortPtr : IWrappedPtr<MsgPort>
	{
		public uint Address { get; set; }
		public MsgPort Wrapped { get; set; }
	}
	/*
	 * Full featured list header.
	 */
	public class List
	{
		public NodePtr lh_Head { get; set; }
		public NodePtr lh_Tail { get; set; }
		public NodePtr lh_TailPred { get; set; }
		public NodeType lh_Type { get; set; }
		public UBYTE l_pad { get; set; }
	} /* word aligned */

	/*
	 * Minimal List Header - no type checking
	 */
	public class MinList
	{
		public MinNodePtr mlh_Head { get; set; }
		public MinNodePtr mlh_Tail { get; set; }
		public MinNodePtr mlh_TailPred { get; set; }
	} /* longword aligned */

	/*
	 *	List Node Structure.	Each member in a list starts with a Node
	 */

	public class Node
	{
		public NodePtr ln_Succ { get; set; }    /* Pointer to next (successor) */
		public NodePtr ln_Pred { get; set; }    /* Pointer to previous (predecessor) */
		public NodeType ln_Type { get; set; }
		public BYTE ln_Pri { get; set; }        /* Priority, for sorting */
		public CharPtr ln_Name { get; set; }        /* ID string, null terminated */
	} /* Note: word aligned */

	/* minimal node -- no type checking possible */
	public class MinNode
	{
		public MinNodePtr mln_Succ { get; set; }
		public MinNodePtr mln_Pred { get; set; }
	}

	/*------ Library Base Structure ----------------------------------*/
	/* Also used for Devices and some Resources */
	public class Library
	{
		public Node lib_Node { get; set; }
		public UBYTE lib_Flags { get; set; }
		public UBYTE lib_pad { get; set; }
		public UWORD lib_NegSize { get; set; }      /* number of bytes before library */
		public UWORD lib_PosSize { get; set; }      /* number of bytes after library */
		public UWORD lib_Version { get; set; }      /* major */
		public UWORD lib_Revision { get; set; }  /* minor */
		//public APTR lib_IdString { get; set; }      /* ASCII identification */
		public CharPtr lib_IdString { get; set; }
		public ULONG lib_Sum { get; set; }          /* the checksum itself */
		public UWORD lib_OpenCnt { get; set; }      /* number of current opens */
	} /* Warning: size is not a longword multiple! */

	/* Please use Exec functions to modify task structure fields, where available.
	 */
	public class Task
	{
		public Node tc_Node { get; set; }
		public UBYTE tc_Flags { get; set; }
		public UBYTE tc_State { get; set; }
		public BYTE tc_IDNestCnt { get; set; }      /* intr disabled nesting*/
		public BYTE tc_TDNestCnt { get; set; }      /* task disabled nesting*/
		public ULONG tc_SigAlloc { get; set; }      /* sigs allocated */
		public ULONG tc_SigWait { get; set; }       /* sigs we are waiting for */
		public ULONG tc_SigRecvd { get; set; }      /* sigs we have received */
		public ULONG tc_SigExcept { get; set; }  /* sigs we will take excepts for */
		public UWORD tc_TrapAlloc { get; set; }  /* traps allocated */
		public UWORD tc_TrapAble { get; set; }      /* traps enabled */
		public APTR tc_ExceptData { get; set; }  /* points to except data */
		public APTR tc_ExceptCode { get; set; }  /* points to except code */
		public APTR tc_TrapData { get; set; }       /* points to trap code */
		public APTR tc_TrapCode { get; set; }       /* points to trap data */
		public APTR tc_SPReg { get; set; }          /* stack pointer		*/
		public APTR tc_SPLower { get; set; }        /* stack lower bound	*/
		public APTR tc_SPUpper { get; set; }        /* stack upper bound + 2*/
		public FunctionPtr tc_Switch { get; set; }      /* task losing CPU		*/
		public FunctionPtr tc_Launch { get; set; }      /* task getting CPU	*/
		public List tc_MemEntry { get; set; }       /* Allocated memory. Freed by RemTask() */
		public APTR tc_UserData { get; set; }       /* For use by the task no restrictions! */
	}

	public class IntVector
	{   /* For EXEC use ONLY! */
		public APTR iv_Data { get; set; }
		public FunctionPtr iv_Code { get; set; }
		public NodePtr iv_Node { get; set; }
	}

	public class SoftIntList
	{   /* For EXEC use ONLY! */
		public List sh_List { get; set; }
		public UWORD sh_Pad { get; set; }
	}

	public class ExecBase
	{
		public Library LibNode { get; set; } /* Standard library node */

		/******** Static System Variables ********/

		public UWORD SoftVer { get; set; }  /* kickstart release number (obs.) */
		public WORD LowMemChkSum { get; set; }  /* checksum of 68000 trap vectors */
		public ULONG ChkBase { get; set; }  /* system base pointer complement */
		public APTR ColdCapture { get; set; }   /* coldstart soft capture vector */
		public APTR CoolCapture { get; set; }   /* coolstart soft capture vector */
		public APTR WarmCapture { get; set; }   /* warmstart soft capture vector */
		public APTR SysStkUpper { get; set; }   /* system stack base	(upper bound) */
		public APTR SysStkLower { get; set; }   /* top of system stack (lower bound) */
		public ULONG MaxLocMem { get; set; }    /* top of chip memory */
		public APTR DebugEntry { get; set; }    /* global debugger entry point */
		public APTR DebugData { get; set; } /* global debugger data segment */
		public APTR AlertData { get; set; } /* alert data segment */
		public APTR MaxExtMem { get; set; } /* top of extended mem, or null if none */

		public UWORD ChkSum { get; set; }   /* for all of the above (minus 2) */

		/****** Interrupt Related ***************************************/

		public IntVector[] IntVects { get; set; } = new IntVector[16];

		/****** Dynamic System Variables *************************************/

		public TaskPtr ThisTask { get; set; } /* pointer to current task (readable) */

		public ULONG IdleCount { get; set; }    /* idle counter */
		public ULONG DispCount { get; set; }    /* dispatch counter */
		public UWORD Quantum { get; set; }  /* time slice quantum */
		public UWORD Elapsed { get; set; }  /* current quantum ticks */
		public UWORD SysFlags { get; set; } /* misc internal system flags */
		public BYTE IDNestCnt { get; set; } /* interrupt disable nesting count */
		public BYTE TDNestCnt { get; set; } /* task disable nesting count */

		public UWORD AttnFlags { get; set; }    /* special attention flags (readable) */

		public UWORD AttnResched { get; set; }  /* rescheduling attention */
		public APTR ResModules { get; set; }    /* resident module array pointer */
		public APTR TaskTrapCode { get; set; }
		public APTR TaskExceptCode { get; set; }
		public APTR TaskExitCode { get; set; }
		public ULONG TaskSigAlloc { get; set; }
		public UWORD TaskTrapAlloc { get; set; }

		/****** System Lists (private!) ********************************/

		public List MemList { get; set; }
		public List ResourceList { get; set; }
		public List DeviceList { get; set; }
		public List IntrList { get; set; }
		public List LibList { get; set; }
		public List PortList { get; set; }
		public List TaskReady { get; set; }
		public List TaskWait { get; set; }

		public SoftIntList[] SoftInts { get; set; } = new SoftIntList[5];

		/****** Other Globals *******************************************/

		public LONG[] LastAlert { get; set; } = new LONG[4];

		/* these next two variables are provided to allow
		** system developers to have a rough idea of the
		** period of two externally controlled signals --
		** the time between vertical blank interrupts and the
		** external line rate (which is counted by CIA A's
		** "time of day" clock).	In general these values
		** will be 50 or 60, and may or may not track each
		** other.	These values replace the obsolete AFB_PAL
		** and AFB_50HZ flags.
		*/
		public UBYTE VBlankFrequency { get; set; }  /* (readable) */
		public UBYTE PowerSupplyFrequency { get; set; } /* (readable) */

		public List SemaphoreList { get; set; }

		/* these next two are to be able to kickstart into user ram.
		** KickMemPtr holds a singly linked list of MemLists which
		** will be removed from the memory list via AllocAbs.	If
		** all the AllocAbs's succeeded, then the KickTagPtr will
		** be added to the rom tag list.
		*/
		public APTR KickMemPtr { get; set; }    /* ptr to queue of mem lists */
		public APTR KickTagPtr { get; set; }    /* ptr to rom tag queue */
		public APTR KickCheckSum { get; set; }  /* checksum for mem and tags */

		// ExecBase used to look like this in 1.3
		public UBYTE[] ExecBaseReserved { get; set; } = new UBYTE[10];
		public UBYTE[] ExecBaseNewReserved { get; set; } = new UBYTE[20];

		/****** V36 Exec additions start here **************************************/

		//public UWORD ex_Pad0 { get; set; }
		//public ULONG ex_LaunchPoint { get; set; }       /* Private to Launch/Switch */
		//public APTR ex_RamLibPrivate { get; set; }
		// /* The next ULONG contains the system "E" clock frequency,
		// ** expressed in Hertz.	The E clock is used as a timebase for
		// ** the Amiga's 8520 I/O chips. (E is connected to "02").
		// ** Typical values are 715909 for NTSC, or 709379 for PAL.
		// */
		//public ULONG ex_EClockFrequency { get; set; }   /* (readable) */
		//public ULONG ex_CacheControl { get; set; }  /* Private to CacheControl calls */
		//public ULONG ex_TaskID { get; set; }        /* Next available task ID */

		//public ULONG ex_PuddleSize { get; set; }
		//public ULONG ex_PoolThreshold { get; set; }
		//public MinList ex_PublicPool { get; set; }

		//public APTR ex_MMULock { get; set; }        /* private */

		//public UBYTE[] ex_Reserved { get; set; } = new UBYTE[12];

	}

	public class MsgPort
	{
		public Node mp_Node { get; set; }
		public UBYTE mp_Flags { get; set; }
		public UBYTE mp_SigBit { get; set; }        /* signal bit number	*/
		public VoidPtr mp_SigTask { get; set; }       /* object to be signalled */
		public List mp_MsgList { get; set; } /* message linked list	*/
	};

	public class Message
	{
		public Node mn_Node { get; set; }
		public MsgPortPtr mn_ReplyPort { get; set; }  /* message reply port */
		public UWORD mn_Length { get; set; } /* total message length, in bytes */
		/* (include the size of the Message */
		/* structure in the length) */
	};

	public class IORequest
	{
		public Message io_Message { get; set; }
		public DevicePtr io_Device { get; set; }     /* device node pointer  */
		public UnitPtr io_Unit { get; set; }        /* unit (driver private)*/
		public UWORD io_Command { get; set; }       /* device command */
		public UBYTE io_Flags { get; set; }
		public BYTE io_Error { get; set; }          /* error or warning num */
	};

	public class timeval
	{
		public ULONG tv_secs { get; set; }
		public ULONG tv_micro { get; set; }
	}

	public class timerequest
	{
		public IORequest tr_node { get; set; }
		public timeval tr_time { get; set; }
	}

	public class KeyMap
	{
		public UBYTEPtr km_LoKeyMapTypes{ get; set; }
		public ULONGPtr km_LoKeyMap{ get; set; }
		public UBYTEPtr km_LoCapsable{ get; set; }
		public UBYTEPtr km_LoRepeatable{ get; set; }
		public UBYTEPtr km_HiKeyMapTypes{ get; set; }
		public ULONGPtr km_HiKeyMap{ get; set; }
		public UBYTEPtr km_HiCapsable{ get; set; }
		public UBYTEPtr km_HiRepeatable{ get; set; }
	}

	public class KeyMapNode
	{
		public Node kn_Node{ get; set; }    /* including name of keymap */
		public KeyMap kn_KeyMap{ get; set; }
	}

	/* the structure of keymap.resource */
	public class KeyMapResource
	{
		public Node kr_Node{ get; set; }
		public List kr_List{ get; set; }	/* a list of KeyMapNodes */
	}

	public class Unit
	{
		public MsgPort unit_MsgPort { get; set; }    /* queue for unprocessed messages */
		/* instance of msgport is recommended */
		public UBYTE unit_flags { get; set; }
		public UBYTE unit_pad { get; set; }
		public UWORD unit_OpenCnt { get; set; }     /* number of active opens */
	}


	public class ResidentPtr : IWrappedPtr<Resident>
	{
		public uint Address { get; set; }
		public Resident Wrapped { get; set; }
	}

	public class Resident
	{
		public UWORD rt_MatchWord { get; set; } /* word to match on (ILLEGAL)	*/
		public ResidentPtr rt_MatchTag { get; set; } /* pointer to the above	*/
		public APTR rt_EndSkip { get; set; }        /* address to continue scan	*/
		public UBYTE rt_Flags { get; set; }     /* various tag flags		*/
		public UBYTE rt_Version { get; set; }       /* release version number	*/
		public UBYTE rt_Type { get; set; }      /* type of module (NT_XXXXXX)	*/
		public BYTE rt_Pri { get; set; }        /* initialization priority */
		public CharPtr rt_Name { get; set; }      /* pointer to node name	*/
		public CharPtr rt_IdString { get; set; }  /* pointer to identification string */
		public APTR rt_Init { get; set; }       /* pointer to init code	*/
	}

}