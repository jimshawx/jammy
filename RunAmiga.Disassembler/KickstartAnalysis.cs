using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Options;
using RunAmiga.Core.Types.Types.Kickstart;

namespace RunAmiga.Disassembler
{
	public class KickstartAnalysis
	{
		private const int RTC_MATCHWORD = 0x4AFC;

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

		public static void Disassemble(List<Resident> resident, IDisassembly disassembly, EmulationSettings settings)
		{
			for (int i = 0; i < resident.Count; i++)
			{
				var rt = resident[i];
				var endAddress = 0xfffff0u;
				if (i != resident.Count - 1)
					endAddress = resident[i + 1].MatchTag;

				var dmp = new StringBuilder();
				string asm = disassembly.DisassembleTxt(new List<Tuple<uint, uint>>
					{
						new Tuple<uint, uint>(rt.MatchTag, endAddress - rt.MatchTag + 1)
					}, new List<uint>(),
					new DisassemblyOptions { IncludeBytes = false, CommentPad = true, IncludeComments = true});


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
							 $"*  The following is a complete disassembly of the Amiga {settings.KickStart,4}               *\n" +
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
							 //"\n" +
							 //$"\t; The {rt.Name} RomTag Structure\n" +
							 //"\n");
							 "");

				//uint b = rt.MatchTag;
				//dmp.AppendLine($"{b:X6}  {rt.MatchWord:X4}                                    RTC_MATCHWORD   (start of ROMTAG marker)"); b += 2;
				//dmp.AppendLine($"{b:X6}  {rt.MatchTag:X8}                                RT_MATCHTAG     (pointer RTC_MATCHWORD)"); b += 4;
				//dmp.AppendLine($"{b:X6}  {rt.EndSkip:X8}                                RT_ENDSKIP      (pointer to end of code)"); b += 4;
				//dmp.AppendLine($"{b:X6}  {rt.Flags:X2}                                      RT_FLAGS        ({rt.Flags})"); b += 1;
				//dmp.AppendLine($"{b:X6}  {rt.Version:X2}                                      RT_VERSION      (version number = {rt.Version})"); b += 1;
				//dmp.AppendLine($"{b:X6}  {rt.Type:X2}                                      RT_TYPE         ({rt.Type})"); b += 1;
				//dmp.AppendLine($"{b:X6}  {rt.Pri:X2}                                      RT_PRI          (priority = {rt.Pri})"); b += 1;
				//dmp.AppendLine($"{b:X6}  {rt.NamePtr:X8}                                RT_NAME         (pointer to name)"); b += 4;
				//dmp.AppendLine($"{b:X6}  {rt.IdStringPtr:X8}                                RT_IDSTRING     (pointer to ID string)"); b += 4;
				//dmp.AppendLine($"{b:X6}  {rt.Init:X8}                                RT_INIT         (execution address)"); b += 4;
				//dmp.AppendLine($"{b:X6}");

				dmp.Append(asm);

				//var mem = new MemoryDump(memory.GetMemoryArray());
				//dmp.AppendLine(mem.ToString(rt.MatchTag & 0xffffffe0u, endAddress - rt.MatchTag + 1 + 31));

				File.WriteAllText($"{rt.Name}_disassembly.txt", dmp.ToString());
			}

		}

		public static void Run(IMemory memory, ILogger logger, IDisassembly disassembly, EmulationSettings settings)
		{
			var resident = GetRomTags(memory.GetMemoryArray(), 0);
			foreach (var rt in resident)
				logger.LogTrace($"{rt.MatchTag:X8}\n{rt.Name}\n{rt.IdString}\n{rt.Flags}\nv:{rt.Version}\n{rt.Type}\npri:{rt.Pri}\ninit:{rt.Init:X8}\n");

			//Disassemble(resident, disassembly, settings);

			//KickLogo.KSLogo(this);
		}
	}
}