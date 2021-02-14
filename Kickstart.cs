using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using RunAmiga.Tests;

namespace RunAmiga
{
	public class Kickstart
	{
		public string Path { get; set; }
		public string Name { get; set; }
		public byte[] ROM { get; set; }
		public uint Origin { get; set; }

		public Kickstart(string path, string name)
		{
			Path = path;
			Name = name;

			ROM = File.ReadAllBytes(path);
			Debug.Assert(ROM.Length == 512 * 1024 || ROM.Length == 256*1024);

			Origin = 0xfc0000;
			if (ROM.Length == 512 * 1024) Origin = 0xf80000;

			RomTags(ROM, Origin);

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

		public void RomTags(byte[] bytes, uint rombase)
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
				Logger.WriteLine($"{rt.MatchTag:X8}\n{rt.Name}\n{rt.IdString}\n{rt.Flags}\nv:{rt.Version}\n{rt.Type}\npri:{rt.Pri}\ninit:{rt.Init:X8}\n");
			}

		}


	}
}