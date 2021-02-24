using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Options;
using RunAmiga.Types;

namespace RunAmiga
{
	public class Kickstart
	{
		public string Path { get; set; }
		public string Name { get; set; }
		public byte[] ROM { get; set; }
		public uint Origin { get; set; }

		private ILogger logger;

		public Kickstart(string path, string name)
		{
			logger = Program.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Kickstart>();

			Path = path;
			Name = name;

			ROM = File.ReadAllBytes(path);
			Debug.Assert(ROM.Length == 512 * 1024 || ROM.Length == 256 * 1024);

			Origin = 0xfc0000;
			if (ROM.Length == 512 * 1024) Origin = 0xf80000;

			RomTags(ROM, Origin, false);

			//KickLogo.KSLogo(this);
		}


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
			public string IdString { get; set; }  /* pointer to identification string */
			public uint Init { get; set; }        /* pointer to init code	*/
		}

		private const int RTC_MATCHWORD = 0x4AFC;

		public string[] nodeType = new string[]
		{
			"NT_UNKNOWN",
			"NT_TASK",
			"NT_INTERRUPT",
			"NT_DEVICE",
			"NT_MSGPORT",
			"NT_MESSAGE",
			"NT_FREEMSG",
			"NT_REPLYMSG",
			"NT_RESOURCE",
			"NT_LIBRARY",
			"NT_MEMORY",
			"NT_SOFTINT",
			"NT_FONT",
			"NT_PROCESS",
			"NT_SEMAPHORE",
			"NT_SIGNALSEM",
			"NT_BOOTNODE",
			"NT_KICKMEM",
			"NT_GRAPHICS",
			"NT_DEATHMESSAGE",
		};

		public void RomTags(byte[] bytes, uint rombase, bool dump = false)
		{
			var resident = new List<Resident>();

			for (int i = 0; i < bytes.Length; i += 2)
			{
				ushort matchWord = (ushort)(bytes[i] * 256 + bytes[i + 1]);
				if (matchWord == RTC_MATCHWORD)
				{
					uint matchTag = (uint)((bytes[2 + i] << 24) + (bytes[2 + i + 1] << 16) + (bytes[2 + i + 2] << 8) + bytes[2 + i + 3]);
					if (matchTag == i + rombase)
					{
						i += 6;
						var rt = new Resident();

						rt.MatchWord = RTC_MATCHWORD;
						rt.MatchTag = matchTag;

						rt.EndSkip = (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]); i += 4;
						rt.Flags = (RTF)bytes[i++];
						rt.Version = bytes[i++];
						rt.Type = (NT_Type)bytes[i++];
						rt.Pri = (sbyte)bytes[i++];

						{
							uint s = (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]); i += 4;
							s -= rombase;
							var n = new StringBuilder();
							while (bytes[s] != 0)
							{
								if (bytes[s] == 0xd) { n.Append(",CR"); s++; }
								else if (bytes[s] == 0xa) { n.Append(",LF"); s++; }
								else n.Append(Convert.ToChar(bytes[s++]));
							}
							rt.Name = n.ToString();
						}

						{
							uint s = (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]); i += 4;
							s -= rombase;
							var n = new StringBuilder();
							while (bytes[s] != 0)
							{
								if (bytes[s] == 0xd) { n.Append(",CR"); s++; }
								else if (bytes[s] == 0xa) { n.Append(",LF"); s++; }
								else n.Append(Convert.ToChar(bytes[s++]));
							}
							rt.IdString = n.ToString();
						}

						rt.Init = (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]); i += 4;

						resident.Add(rt);

						i = (int)(rt.EndSkip - rombase - 2);
					}
				}
			}

			foreach (var rt in resident)
			{
				logger.LogTrace($"{rt.MatchTag:X8}\n{rt.Name}\n{rt.IdString}\n{rt.Flags}\nv:{rt.Version}\n{rt.Type}\npri:{rt.Pri}\ninit:{rt.Init:X8}\n");
			}

			if (dump)
				Disassemble(resident);
		}

		private void Disassemble(List<Resident> resident)
		{
			var memory = new Memory("Kickstart", Program.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Memory>());
			memory.SetKickstart(this);

			var disassembly = new Disassembly(memory.GetMemoryArray(), new BreakpointCollection());

			for (int i = 0; i < resident.Count; i++)
			{
				var rt = resident[i];
				var endAddress = 0xfffff0u;
				if (i != resident.Count - 1)
					endAddress = resident[i + 1].MatchTag;

				logger.LogTrace($"{rt.MatchTag:X8}\n{rt.Name}\n{rt.IdString}\n{rt.Flags}\nv:{rt.Version}\n{rt.Type}\npri:{rt.Pri}\ninit:{rt.Init:X8}\n");

				var dmp = new StringBuilder();
				string asm = disassembly.DisassembleTxt(new List<Tuple<uint, uint>>
					{
						new Tuple<uint, uint>(rt.MatchTag, endAddress - rt.MatchTag + 1)
					}, new List<uint>(),
					new DisassemblyOptions { IncludeBytes = false, CommentPad = true });


				//F8574C  4AFC                                    RTC_MATCHWORD(start of ROMTAG marker)
				//F8574E  00F8574C                                RT_MATCHTAG(pointer RTC_MATCHWORD)
				//F85752  00F86188                                RT_ENDSKIP(pointer to end of code)
				//F85756  01                                      RT_FLAGS(RTF_COLDSTART)
				//F85757  25                                      RT_VERSION(version number)
				//F85758  08                                      RT_TYPE(NT_RESOURCE)
				//F85759  2D                                      RT_PRI(priority = 45)
				//F8575A  00F85766                                RT_NAME(pointer to name)
				//F8575E  00F85798                                RT_IDSTRING(pointer to ID string)
				//F85762  00F85804                                RT_INIT(execution address)

				dmp.Append($"****************************************************************************\n" +
							 "*                                                                          *\n" +
							 "*  Comments Copyright (C) 2021 James Shaw                                  *\n" +
							 "*                                                                          *\n" +
							 "*  Release date:  2021.                                                    *\n" +
							 "*                                                                          *\n" +
							 "*  The following is a complete disassembly of the Amiga 1.2                *\n" +
							 $"*  \"{rt.Name}\"                                                    *\n" +
							 "*                                                                          *\n" +
							 "*  Absolutely no guarantee is made of the correctness of any of the        *\n" +
							 "*  information supplied below.                                             *\n" +
							 "*                                                                          *\n" +
							 "*  This work was inspired by the disassembly of AmigaOS 1.2 Exec by        *\n" +
							 "*  Markus Wandel (http://wandel.ca/homepage/execdis/exec_disassembly.txt)  *\n" +
							 "*                                                                          *\n" +
							 "*  \"AMIGA ROM Operating System and Libraries\"                              *\n" +
							 "*  \"Copyright (C) 1985, Commodore-Amiga, Inc.\"                             *\n" +
							 "*  \"All Rights Reserved.\"                                                  *\n" +
							 "*                                                                          *\n" +
							 "****************************************************************************\n" +
							 "\n" +
							 $"\t; The {rt.Name} RomTag Structure\n" +
							 "\n");

				uint b = rt.MatchTag;
				dmp.AppendLine($"{b:X6}  {memory.Read16(b):X4}                                    RTC_MATCHWORD   (start of ROMTAG marker)"); b += 2;
				dmp.AppendLine($"{b:X6}  {memory.Read32(b):X8}                                RT_MATCHTAG     (pointer RTC_MATCHWORD)"); b += 4;
				dmp.AppendLine($"{b:X6}  {memory.Read32(b):X8}                                RT_ENDSKIP      (pointer to end of code)"); b += 4;
				dmp.AppendLine($"{b:X6}  {memory.Read8(b):X2}                                      RT_FLAGS        (RTF_COLDSTART)"); b += 1;
				dmp.AppendLine($"{b:X6}  {memory.Read8(b):X2}                                      RT_VERSION      (version number)"); b += 1;
				dmp.AppendLine($"{b:X6}  {memory.Read8(b):X2}                                      RT_TYPE         ({nodeType[memory.Read8(b)]})"); b += 1;
				dmp.AppendLine($"{b:X6}  {memory.Read8(b):X2}                                      RT_PRI          (priority = {memory.Read8(b)})"); b += 1;
				dmp.AppendLine($"{b:X6}  {memory.Read32(b):X8}                                RT_NAME         (pointer to name)"); b += 4;
				dmp.AppendLine($"{b:X6}  {memory.Read32(b):X8}                                RT_IDSTRING     (pointer to ID string)"); b += 4;
				dmp.AppendLine($"{b:X6}  {memory.Read32(b):X8}                                RT_INIT         (execution address)"); b += 4;
				dmp.AppendLine($"{b:X6}");

				dmp.Append(asm);

				var mem = new MemoryDump(memory.GetMemoryArray());
				dmp.AppendLine(mem.ToString(rt.MatchTag & 0xffffffe0u, endAddress - rt.MatchTag + 1 + 31));

				File.WriteAllText($"{rt.Name}_disassembly.txt", dmp.ToString());
			}

		}
	}
}
