
namespace RunAmiga.Types
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

	using System.Linq;
	using System;
	using System.Diagnostics;
	using System.Text;
	using System.Collections.Generic;


	public enum NodeType
	{
		NT_UNKNOWN	= 0,
		NT_TASK		= 1,	/* Exec task */
		NT_INTERRUPT	= 2,
		NT_DEVICE	= 3,
		NT_MSGPORT	= 4,
		NT_MESSAGE	= 5,	/* Indicates message currently pending */
		NT_FREEMSG	= 6,
		NT_REPLYMSG	= 7,	/* Message has been replied */
		NT_RESOURCE	= 8,
		NT_LIBRARY	= 9,
		NT_MEMORY	= 10,
		NT_SOFTINT	= 11,	/* Internal flag used by SoftInits */
		NT_FONT		= 12,
		NT_PROCESS	= 13,	/* AmigaDOS Process */
		NT_SEMAPHORE	= 14,
		NT_SIGNALSEM	= 15,	/* signal semaphores */
		NT_BOOTNODE	= 16,
		NT_KICKMEM	= 17,
		NT_GRAPHICS	= 18,
		NT_DEATHMESSAGE	= 19,

		NT_USER		= 254,	/* User node types work down from here */
		NT_EXTENDED	= 255,
	}

	//using TaskPtr = System.UInt32;
	//using NodePtr = System.UInt32;
	//using MinNodePtr = System.UInt32;
	public interface IWrappedPtr { }
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

	public class ExecBase : ObjectWalk
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

		public SoftIntList[] SoftInts {get; set;} = new SoftIntList[5];

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
		///* The next ULONG contains the system "E" clock frequency,
		//** expressed in Hertz.	The E clock is used as a timebase for
		//** the Amiga's 8520 I/O chips. (E is connected to "02").
		//** Typical values are 715909 for NTSC, or 709379 for PAL.
		//*/
		//public ULONG ex_EClockFrequency { get; set; }   /* (readable) */
		//public ULONG ex_CacheControl { get; set; }  /* Private to CacheControl calls */
		//public ULONG ex_TaskID { get; set; }        /* Next available task ID */

		//public ULONG ex_PuddleSize { get; set; }
		//public ULONG ex_PoolThreshold { get; set; }
		//public MinList ex_PublicPool { get; set; }

		//public APTR ex_MMULock { get; set; }        /* private */

		//public UBYTE[] ex_Reserved { get; set; } = new UBYTE[12];

	}

	public class ExecBaseMapper
	{
		private IMemory memory;

		public ExecBaseMapper(IMemory memory)
		{
			this.memory = memory;
		}

		HashSet<long> lookup = new HashSet<long>();

		StringBuilder sb;

		private uint MapObject(Type type, object obj, uint addr, int depth)
		{
			if (lookup.Contains(addr+type.GetHashCode()))
			{
				//Logger.WriteLine($"Visited {addr:X8} again for {type.Name}");
				return 0;
			}
			lookup.Add(addr+type.GetHashCode());

			uint startAddr = addr;
			var properties = type.GetProperties().OrderBy(x => x.MetadataToken).ToList();

			if (!properties.Any())
				throw new ApplicationException();

			uint lastAddr = addr;
			foreach (var prop in properties)
			{
				if (depth == 0)
					sb.Append($"{addr:X8} {addr-0xc00276:X4} {addr - 0xc00276,5} {prop.Name,-25} {prop.PropertyType}\n");

				if (prop.Name == "ln_Pred")
				{
					addr += 4;
					continue;
				}

				object rv = null;
				var propType = prop.PropertyType;
				try
				{
					if (typeof(IWrappedPtr).IsAssignableFrom(propType))
					{
						if (propType == typeof(TaskPtr))
						{
							var tp = new TaskPtr();
							tp.Address = memory.Read32(addr); addr += 4;
							if (tp.Address != 0 && tp.Address < 0x1000000)
							{
								tp.Task = new Task();
								MapObject(typeof(Task), tp.Task, tp.Address, depth+1);
							}
							else
							{
								tp = null;
							}
							rv = tp;
						}
						else if (propType == typeof(NodePtr))
						{
							var tp = new NodePtr();
							tp.Address = memory.Read32(addr); addr += 4;
							if (tp.Address != 0 && tp.Address < 0x1000000)
							{
								tp.Node = new Node();
								MapObject(typeof(Node), tp.Node, tp.Address, depth+1);
							}
							else
							{
								tp = null;
							}
							rv = tp;
						}
						else if (propType == typeof(MinNodePtr))
						{
							var tp = new MinNodePtr();
							tp.Address = memory.Read32(addr); addr += 4;
							if (tp.Address != 0 && tp.Address < 0x1000000)
							{
								tp.MinNode = new MinNode();
								MapObject(typeof(MinNode), tp.MinNode, tp.Address, depth+1);
							}
							else
							{
								tp = null;
							}
							rv = tp;
						}
						else
						{
							throw new NotImplementedException();
						}
					}
					else if (propType == typeof(String))
					{
						rv = MapString(addr);
						addr += 4;
					}
					else if (propType.BaseType == typeof(Array))
					{
						var array = (Array)prop.GetValue(obj);
						var arrayType = array.GetType().GetElementType();

						if (arrayType.BaseType == typeof(object))
						{ 
							for (int i = 0; i < array.Length; i++)
							{
								array.SetValue(Activator.CreateInstance(arrayType), i);
								addr += MapObject(arrayType, array.GetValue(i), addr, depth+1);
							}
						}
						else
						{
							for (int i = 0; i < array.Length; i++)
							{
								object s = MapSimple(arrayType, addr);
								array.SetValue(s, i);

								if (s.GetType() == typeof(BYTE) || s.GetType() == typeof(UBYTE) || s.GetType() == typeof(NodeType)) addr++;
								else if (s.GetType() == typeof(WORD) || s.GetType() == typeof(UWORD)) addr += 2;
								else if (s.GetType() == typeof(LONG) || s.GetType() == typeof(ULONG) || s.GetType() == typeof(APTR) || s.GetType() == typeof(FunctionPtr)) addr += 4;
								else throw new ApplicationException();
							}
						}
						rv = array;
					}
					else if (propType == typeof(List))
					{
						var list = new List();
						rv = list;
						uint size = MapObject(propType, rv, addr, depth + 1);
						//it's an empty list
						if (list.lh_TailPred == null || list.lh_TailPred.Address == addr)
							list.lh_Head = list.lh_Tail = list.lh_TailPred = null;
						addr += size;
					}
					else if (propType.BaseType == typeof(object))
					{
						rv = Activator.CreateInstance(propType);
						addr += MapObject(propType, rv, addr, depth+1);
					}
					else
					{
						rv = MapSimple(propType, addr);
						if (rv.GetType() == typeof(BYTE) || rv.GetType() == typeof(UBYTE) || rv.GetType() == typeof(NodeType)) addr++;
						else if (rv.GetType() == typeof(WORD) || rv.GetType() == typeof(UWORD)) addr += 2;
						else if (rv.GetType() == typeof(LONG) || rv.GetType() == typeof(ULONG) || rv.GetType() == typeof(APTR) || rv.GetType() == typeof(FunctionPtr)) addr += 4;
						else throw new ApplicationException();
					}

					//Logger.WriteLine($"{addr:X8} {prop.Name}");
					prop.SetValue(obj, rv);
				}
				catch (NullReferenceException ex)
				{
					Logger.WriteLine($"Problem Mapping {prop.Name} was null\n{ex}");
				}
				catch (Exception ex)
				{
					if (rv != null)
						Logger.WriteLine($"Problem Mapping {prop.Name} {prop.PropertyType} != {rv.GetType()}\n{ex}");
					else
						Logger.WriteLine($"Problem Mapping {prop.Name} {prop.PropertyType}\n{ex}");
				}
			}
			//Logger.WriteLine($"{addr-startAddr}");
			return addr - startAddr;
		}

		public string FromAddress()
		{
			lookup.Clear();

			sb = new StringBuilder();

			var execbase = new ExecBase();
			uint execAddress = memory.Read32(4);
			if (execAddress == 0xc00276)
				MapObject(typeof(ExecBase), execbase, execAddress, 0);

			//Logger.WriteLine(execbase.ToString());
			return execbase.ToString() + "\n"+ sb.ToString();
		}

		private object MapSimple(Type type, uint addr)
		{
			if (type == typeof(NodeType)) return (NodeType)memory.Read8(addr);
			if (type == typeof(BYTE)) return (BYTE)memory.Read8(addr);
			if (type == typeof(UBYTE)) return (UBYTE)memory.Read8(addr);
			if (type == typeof(UWORD)) return (UWORD)memory.Read16(addr);
			if (type == typeof(WORD)) return (WORD)memory.Read16(addr);
			if (type == typeof(ULONG)) return (ULONG)memory.Read32(addr);
			if (type == typeof(LONG)) return (LONG)memory.Read32(addr);
			if (type == typeof(APTR)) return (APTR)memory.Read32(addr);
			if (type == typeof(FunctionPtr)) return (FunctionPtr)memory.Read32(addr);
			throw new ApplicationException();
		}

		public string MapString(uint addr)
		{
			uint str = memory.Read32(addr);
			//Logger.WriteLine($"String @{addr:X8}->{str:X8}");
			if (str == 0)
				return "(null)";

			var sb = new StringBuilder();
			for (; ; )
			{
				byte c = memory.Read8(str);
				if (c == 0)
					return sb.ToString();

				sb.Append(Convert.ToChar(c));
				str++;
			}
		}
	}
}
