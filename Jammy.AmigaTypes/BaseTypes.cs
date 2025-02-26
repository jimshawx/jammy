/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

global using FunctionPtr = System.UInt32;
global using voidPtr = System.UInt32;
global using VOIDPtr = System.UInt32;

global using STRPTR = string;
global using BSTR = string;

global using IClass = System.UInt32;
global using Object = System.UInt32;
global using colorEntry = System.UInt32;
global using Tag = System.UInt32;

global using UBYTE = System.Byte;
global using BYTE = System.SByte;
global using unsignedchar = System.Byte;

global using UWORD = System.UInt16;
global using WORD = System.Int16;
global using BOOL = System.Int16;
global using unsignedshort = System.UInt16;
global using AUserStuff = System.Int16;
global using BUserStuff = System.Int16;
global using VUserStuff = System.Int16;

global using ULONG = System.UInt32;
global using LONG = System.Int32;
global using FIXED = System.Int32;

global using APTR = System.UInt32;
global using BPTR = System.UInt32;
global using CPTR = System.UInt32;

global using PLANEPTR = System.UInt32;

namespace Jammy.AmigaTypes;

public static class AmigaType
{
	private static readonly List<string> excludeTypes = ["AmigaType"];

	private static Dictionary<string, Type> types = null;

	public static Dictionary<string, Type> GetAmigaTypes()
	{
		if (types == null)
		{ 
			var allTypes = AppDomain.CurrentDomain.GetAssemblies()
				.SingleOrDefault(x => x.GetName().Name == "Jammy.AmigaTypes")
				.GetTypes()
				.ToList();

			allTypes.RemoveAll(x => excludeTypes.Contains(x.Name));

			var dic = allTypes.ToDictionary(x => x.Name);

			Thread.MemoryBarrier();

			types = dic;
		}

		return types;
	}
}

public struct Point
{
	public WORD x { get; set; }
	public WORD y { get; set; }
}

public enum NodeType : byte
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
