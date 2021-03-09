using System;
using System.Collections.Generic;
using System.Text;
using RunAmiga.Core.Types.Types.Kickstart;

namespace RunAmiga.Disassembler
{
	public class KickstartAnalysis
	{
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

		public static List<Resident> GetRomTags(byte[] bytes, uint rombase)
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

						rt.EndSkip = (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]);
						i += 4;
						rt.Flags = (RTF)bytes[i++];
						rt.Version = bytes[i++];
						rt.Type = (NT_Type)bytes[i++];
						rt.Pri = (sbyte)bytes[i++];

						{
							uint s = (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]);
							i += 4;
							rt.NamePtr = s;
							s -= rombase;
							var n = new StringBuilder();
							while (bytes[s] != 0)
							{
								if (bytes[s] == 0xd)
								{
									n.Append(",CR");
									s++;
								}
								else if (bytes[s] == 0xa)
								{
									n.Append(",LF");
									s++;
								}
								else n.Append(Convert.ToChar(bytes[s++]));
							}

							rt.Name = n.ToString();
						}

						{
							uint s = (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]);
							i += 4;
							rt.IdStringPtr = s;
							s -= rombase;
							var n = new StringBuilder();
							while (bytes[s] != 0)
							{
								if (bytes[s] == 0xd)
								{
									n.Append(",CR");
									s++;
								}
								else if (bytes[s] == 0xa)
								{
									n.Append(",LF");
									s++;
								}
								else n.Append(Convert.ToChar(bytes[s++]));
							}

							rt.IdString = n.ToString();
						}

						rt.Init = (uint)((bytes[i] << 24) + (bytes[i + 1] << 16) + (bytes[i + 2] << 8) + bytes[i + 3]);
						i += 4;

						resident.Add(rt);

						i = (int)(rt.EndSkip - rombase - 2);
					}
				}
			}

			return resident;
		}

		public static List<string> ROMTagLines(Resident r)
		{
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

			var lines = new List<string>();

			lines.Add($"RTC_MATCHWORD (start of ROMTAG marker)");
			lines.Add($"RT_MATCHTAG   (pointer to RTC_MATCHWORD)");
			lines.Add($"RT_ENDSKIP    (pointer to end of code)");
			lines.Add($"RT_FLAGS      ({r.Flags})");
			lines.Add($"RT_VERSION    (version number = {r.Version})");
			lines.Add($"RT_TYPE       ({r.Type})");
			lines.Add($"RT_PRI        (priority = {r.Pri})");
			lines.Add($"RT_NAME       (pointer to name)");
			lines.Add($"RT_IDSTRING   (pointer to ID string)");
			if ((r.Flags & RTF.RTF_AUTOINIT) != 0)
				lines.Add($"RT_INIT       (autoinit data address)");
			else
				lines.Add($"RT_INIT       (execution address)");

			return lines;
		}

		/*
				private void Disassemble(List<Resident> resident)
				{
					var memory = new Memory("Kickstart", ServiceProviderFactory.ServiceProvider.GetRequiredService<ILogger<Kickstart>>();
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
		*/
	}
}