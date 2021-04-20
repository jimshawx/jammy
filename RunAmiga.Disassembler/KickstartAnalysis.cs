using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Interface;
using RunAmiga.Types.Debugger;
using RunAmiga.Types.Kickstart;
using RunAmiga.Types.Options;

namespace RunAmiga.Disassembler
{
	public class KickstartAnalysis : IKickstartAnalysis
	{
		private readonly IDebugMemoryMapper memory;
		private readonly ILogger logger;
		private readonly IKickstartROM kickstartROM;
		private readonly IDisassembly disassembly;
		private readonly EmulationSettings settings;

		private const int RTC_MATCHWORD = 0x4AFC;

		public KickstartAnalysis(IDebugMemoryMapper memory, ILogger<KickstartAnalysis> logger, IKickstartROM kickstartROM,
			IOptions<EmulationSettings> settings, IDisassembly disassembly)
		{
			this.memory = memory;
			this.logger = logger;
			this.kickstartROM = kickstartROM;
			this.disassembly = disassembly;
			this.settings = settings.Value;

			ShowRomTags();
		}

		public List<Resident> GetRomTags()
		{
			return GetRomTags(kickstartROM, memory, 0);
		}

		private static List<Resident> GetRomTags(IKickstartROM kickstartROM, IDebugMemoryMapper memory, uint rombase)
		{
			var resident = new List<Resident>();
			var range = kickstartROM.MappedRange();
			for (uint i = range.Start; i < range.Start + range.Length; i += 2)
			{
				ushort matchWord = (ushort)memory.UnsafeRead16(i);
				if (matchWord == RTC_MATCHWORD)
				{
					uint matchTag = memory.UnsafeRead32(i + 2);
					if (matchTag == i + rombase)
					{
						i += 6;
						var rt = new Resident();

						rt.MatchWord = RTC_MATCHWORD;
						rt.MatchTag = matchTag;

						rt.EndSkip = memory.UnsafeRead32(i); i += 4;
						rt.Flags = (RTF)memory.UnsafeRead8(i++);
						rt.Version = (byte)memory.UnsafeRead8(i++);
						rt.Type = (NT_Type)memory.UnsafeRead8(i++);
						rt.Pri = (sbyte)memory.UnsafeRead8(i++);

						{
							uint s = memory.UnsafeRead32(i);
							i += 4;
							rt.NamePtr = s;
							s -= rombase;
							var n = new StringBuilder();
							while (memory.UnsafeRead8(s) != 0)
							{
								if (memory.UnsafeRead8(s) == 0xd)
								{
									n.Append(",CR"); s++;
								}
								else if (memory.UnsafeRead8(s) == 0xa)
								{
									n.Append(",LF"); s++;
								}
								else
								{
									n.Append(Convert.ToChar(memory.UnsafeRead8(s++)));
								}
							}

							rt.Name = n.ToString();
						}

						{
							uint s = memory.UnsafeRead32(i);
							i += 4;
							rt.IdStringPtr = s;
							s -= rombase;
							var n = new StringBuilder();
							while (memory.UnsafeRead8(s) != 0)
							{
								if (memory.UnsafeRead8(s) == 0xd)
								{
									n.Append(",CR"); s++;
								}
								else if (memory.UnsafeRead8(s) == 0xa)
								{
									n.Append(",LF"); s++;
								}
								else
								{
									n.Append(Convert.ToChar(memory.UnsafeRead8(s++)));
								}
							}

							rt.IdString = n.ToString();
						}

						rt.Init = memory.UnsafeRead32(i); i += 4;

						resident.Add(rt);

						i = (uint)(rt.EndSkip - rombase - 2);
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

		private void Disassemble(List<Resident> resident, MemoryDump memoryDump)
		{
			for (int i = 0; i < resident.Count; i++)
			{
				memoryDump.ClearMapping();

				var rt = resident[i];
				var endAddress = 0xfffff0u;
				if (i != resident.Count - 1)
					endAddress = resident[i + 1].MatchTag;

				string asm = disassembly.DisassembleTxt(new List<Tuple<uint, uint>>
					{
						new Tuple<uint, uint>(rt.MatchTag, endAddress - rt.MatchTag + 1)
					},
					new DisassemblyOptions { IncludeBytes = false, CommentPad = true, IncludeComments = true });

				var dmp = new StringBuilder();
				if (!asm.TrimStart().StartsWith("******"))
				{
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
							 "*  \"Copyright (C) 1985-1993, Commodore-Amiga, Inc.\"                        *\n" +
							 "*  \"All Rights Reserved.\"                                                  *\n" +
							 "*                                                                          *\n" +
							 "****************************************************************************\n" +
							 "");
				}

				dmp.Append(asm);

				dmp.AppendLine("^Z");
				dmp.AppendLine(memoryDump.ToString(rt.MatchTag & 0xffffffe0u, endAddress - rt.MatchTag + 1 + 31));

				File.WriteAllText($"{rt.Name}_disassembly.txt", dmp.ToString());
			}
		}

		public void ShowRomTags()
		{
			var resident = GetRomTags();
			foreach (var rt in resident)
				logger.LogTrace($"{rt.MatchTag:X8}\n{rt.Name}\n{rt.IdString}\n{rt.Flags}\nv:{rt.Version}\n{rt.Type}\npri:{rt.Pri}\ninit:{rt.Init:X8}\n");

			if (settings.Disassemblies == Feature.Enabled)
			{
				//var memoryDump = new MemoryDump(memory.GetEnumerable(0));
				var memoryDump = new MemoryDump(memory.GetBulkRanges());
				Disassemble(resident, memoryDump);
			}

			//KickLogo.KSLogo(this);
		}
	}
}