
namespace RunAmiga.Types
{
	using UBYTE = System.Byte;
	using BYTE = System.SByte;
	using UWORD = System.UInt16;
	using WORD = System.Int16;
	using ULONG = System.UInt32;
	using LONG = System.Int32;

	using APTR = System.UInt32;

	using TaskPtr = System.UInt32;
	using NodePtr = System.UInt32;
	using MinNodePtr = System.UInt32;
	using FunctionPtr = System.UInt32;

	using CharPtr = System.String;

	/*
	 * Full featured list header.
	 */
	public class List
	{
		NodePtr lh_Head;
		NodePtr lh_Tail;
		NodePtr lh_TailPred;
		UBYTE lh_Type;
		UBYTE l_pad;
	};  /* word aligned */

	/*
	 * Minimal List Header - no type checking
	 */
	public class MinList
	{
		MinNodePtr mlh_Head;
		MinNodePtr mlh_Tail;
		MinNodePtr mlh_TailPred;
	};  /* longword aligned */

	/*
	 *	List Node Structure.	Each member in a list starts with a Node
	 */

	public class Node
	{
		NodePtr ln_Succ;    /* Pointer to next (successor) */
		NodePtr ln_Pred;    /* Pointer to previous (predecessor) */
		UBYTE ln_Type;
		BYTE ln_Pri;        /* Priority, for sorting */
		CharPtr ln_Name;        /* ID string, null terminated */
	};  /* Note: word aligned */

	/* minimal node -- no type checking possible */
	public class MinNode
	{
		MinNodePtr mln_Succ;
		MinNodePtr mln_Pred;
	};

	/*------ Library Base Structure ----------------------------------*/
	/* Also used for Devices and some Resources */
	public class Library
	{
		Node lib_Node;
		UBYTE lib_Flags;
		UBYTE lib_pad;
		UWORD lib_NegSize;      /* number of bytes before library */
		UWORD lib_PosSize;      /* number of bytes after library */
		UWORD lib_Version;      /* major */
		UWORD lib_Revision;  /* minor */
		APTR lib_IdString;      /* ASCII identification */
		ULONG lib_Sum;          /* the checksum itself */
		UWORD lib_OpenCnt;      /* number of current opens */
	};  /* Warning: size is not a longword multiple! */

	/* Please use Exec functions to modify task structure fields, where available.
	 */
	public class Task
	{
		Node tc_Node;
		UBYTE tc_Flags;
		UBYTE tc_State;
		BYTE tc_IDNestCnt;      /* intr disabled nesting*/
		BYTE tc_TDNestCnt;      /* task disabled nesting*/
		ULONG tc_SigAlloc;      /* sigs allocated */
		ULONG tc_SigWait;       /* sigs we are waiting for */
		ULONG tc_SigRecvd;      /* sigs we have received */
		ULONG tc_SigExcept;  /* sigs we will take excepts for */
		UWORD tc_TrapAlloc;  /* traps allocated */
		UWORD tc_TrapAble;      /* traps enabled */
		APTR tc_ExceptData;  /* points to except data */
		APTR tc_ExceptCode;  /* points to except code */
		APTR tc_TrapData;       /* points to trap code */
		APTR tc_TrapCode;       /* points to trap data */
		APTR tc_SPReg;          /* stack pointer		*/
		APTR tc_SPLower;        /* stack lower bound	*/
		APTR tc_SPUpper;        /* stack upper bound + 2*/
		FunctionPtr tc_Switch;      /* task losing CPU		*/
		FunctionPtr tc_Launch;      /* task getting CPU	*/
		List tc_MemEntry;       /* Allocated memory. Freed by RemTask() */
		APTR tc_UserData;       /* For use by the task; no restrictions! */
	};

	public class IntVector
	{   /* For EXEC use ONLY! */
		APTR iv_Data;
		FunctionPtr iv_Code;
		NodePtr iv_Node;
	};

	public class SoftIntList
	{   /* For EXEC use ONLY! */
		List sh_List;
		UWORD sh_Pad;
	};

	public class ExecBase
	{
		Library LibNode; /* Standard library node */

		/******** Static System Variables ********/

		UWORD SoftVer;  /* kickstart release number (obs.) */
		WORD LowMemChkSum;  /* checksum of 68000 trap vectors */
		ULONG ChkBase;  /* system base pointer complement */
		APTR ColdCapture;   /* coldstart soft capture vector */
		APTR CoolCapture;   /* coolstart soft capture vector */
		APTR WarmCapture;   /* warmstart soft capture vector */
		APTR SysStkUpper;   /* system stack base	(upper bound) */
		APTR SysStkLower;   /* top of system stack (lower bound) */
		ULONG MaxLocMem;    /* top of chip memory */
		APTR DebugEntry;    /* global debugger entry point */
		APTR DebugData; /* global debugger data segment */
		APTR AlertData; /* alert data segment */
		APTR MaxExtMem; /* top of extended mem, or null if none */

		UWORD ChkSum;   /* for all of the above (minus 2) */

		/****** Interrupt Related ***************************************/

		IntVector[] IntVects = new IntVector[16];

		/****** Dynamic System Variables *************************************/

		TaskPtr ThisTask; /* pointer to current task (readable) */

		ULONG IdleCount;    /* idle counter */
		ULONG DispCount;    /* dispatch counter */
		UWORD Quantum;  /* time slice quantum */
		UWORD Elapsed;  /* current quantum ticks */
		UWORD SysFlags; /* misc internal system flags */
		BYTE IDNestCnt; /* interrupt disable nesting count */
		BYTE TDNestCnt; /* task disable nesting count */

		UWORD AttnFlags;    /* special attention flags (readable) */

		UWORD AttnResched;  /* rescheduling attention */
		APTR ResModules;    /* resident module array pointer */
		APTR TaskTrapCode;
		APTR TaskExceptCode;
		APTR TaskExitCode;
		ULONG TaskSigAlloc;
		UWORD TaskTrapAlloc;

		/****** System Lists (private!) ********************************/

		List MemList;
		List ResourceList;
		List DeviceList;
		List IntrList;
		List LibList;
		List PortList;
		List TaskReady;
		List TaskWait;

		SoftIntList[] SoftInts = new SoftIntList[5];

		/****** Other Globals *******************************************/

		LONG[] LastAlert = new LONG[4];

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
		UBYTE VBlankFrequency;  /* (readable) */
		UBYTE PowerSupplyFrequency; /* (readable) */

		List SemaphoreList;

		/* these next two are to be able to kickstart into user ram.
		** KickMemPtr holds a singly linked list of MemLists which
		** will be removed from the memory list via AllocAbs.	If
		** all the AllocAbs's succeeded, then the KickTagPtr will
		** be added to the rom tag list.
		*/
		APTR KickMemPtr;    /* ptr to queue of mem lists */
		APTR KickTagPtr;    /* ptr to rom tag queue */
		APTR KickCheckSum;  /* checksum for mem and tags */

		/****** V36 Exec additions start here **************************************/

		UWORD ex_Pad0;
		ULONG ex_LaunchPoint;       /* Private to Launch/Switch */
		APTR ex_RamLibPrivate;
		/* The next ULONG contains the system "E" clock frequency,
		** expressed in Hertz.	The E clock is used as a timebase for
		** the Amiga's 8520 I/O chips. (E is connected to "02").
		** Typical values are 715909 for NTSC, or 709379 for PAL.
		*/
		ULONG ex_EClockFrequency;   /* (readable) */
		ULONG ex_CacheControl;  /* Private to CacheControl calls */
		ULONG ex_TaskID;        /* Next available task ID */

		ULONG ex_PuddleSize;
		ULONG ex_PoolThreshold;
		MinList ex_PublicPool;

		APTR ex_MMULock;        /* private */

		UBYTE[] ex_Reserved = new UBYTE[12];
	}

	public class ExecBaseMapper
	{
		public void FromAddress(uint addr)
		{
			foreach (var p in typeof(ExecBase).GetProperties())
			{

			}
		}
	}
}
