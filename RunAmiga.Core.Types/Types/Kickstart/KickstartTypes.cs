using System;

namespace RunAmiga.Core.Types.Types.Kickstart
{
	[Flags]
	public enum RTF
	{
		RTF_AUTOINIT = (1 << 7),    /* rt_Init points to data structure */
		RTF_AFTERDOS = (1 << 2),
		RTF_SINGLETASK = (1 << 1),
		RTF_COLDSTART = (1 << 0)
	}

	public enum NT_Type
	{
		NT_UNKNOWN = 0,
		NT_TASK = 1, /* Exec task */
		NT_INTERRUPT = 2,
		NT_DEVICE = 3,
		NT_MSGPORT = 4,
		NT_MESSAGE = 5,  /* Indicates message currently pending */
		NT_FREEMSG = 6,
		NT_REPLYMSG = 7, /* Message has been replied */
		NT_RESOURCE = 8,
		NT_LIBRARY = 9,
		NT_MEMORY = 10,
		NT_SOFTINT = 11, /* Internal flag used by SoftInits */
		NT_FONT = 12,
		NT_PROCESS = 13, /* AmigaDOS Process */
		NT_SEMAPHORE = 14,
		NT_SIGNALSEM = 15,   /* signal semaphores */
		NT_BOOTNODE = 16,
		NT_KICKMEM = 17,
		NT_GRAPHICS = 18,
		NT_DEATHMESSAGE = 19,

		NT_USER = 254,   /* User node types work down from here */
		NT_EXTENDED = 255
	}

	public class Resident
	{
		public ushort MatchWord { get; set; } /* word to match on (ILLEGAL)	*/
		public uint MatchTag { get; set; }    /* pointer to the above	*/
		public uint EndSkip { get; set; }     /* address to continue scan	*/
		public RTF Flags { get; set; }        /* various tag flags		*/
		public byte Version { get; set; }     /* release version number	*/
		public NT_Type Type { get; set; }     /* type of module (NT_XXXXXX)	*/
		public sbyte Pri { get; set; }        /* initialization priority */
		public string Name { get; set; }      /* pointer to node name	*/
		public uint NamePtr { get; set; }
		public string IdString { get; set; }  /* pointer to identification string */
		public uint IdStringPtr { get; set; }
		public uint Init { get; set; }        /* pointer to init code	*/
	}
}