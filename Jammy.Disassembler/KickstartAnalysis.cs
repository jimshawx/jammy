using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Interface;
using Jammy.Types.Debugger;
using Jammy.Types.Kickstart;
using Jammy.Types.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Disassembler
{
	public class KickstartAnalysis : IKickstartAnalysis
	{
		private readonly IDebugMemoryMapper memory;
		private readonly ILogger logger;
		private readonly IKickstartROM kickstartROM;
		private readonly IExtendedKickstartROM extendedKickstartROM;
		private readonly IDisassembly disassembly;
		private readonly EmulationSettings settings;

		public const int RTC_MATCHWORD = 0x4AFC;

		public KickstartAnalysis(IDebugMemoryMapper memory, ILogger<KickstartAnalysis> logger, IKickstartROM kickstartROM,
			IExtendedKickstartROM extendedKickstartROM,
			IOptions<EmulationSettings> settings, IDisassembly disassembly)
		{
			this.memory = memory;
			this.logger = logger;
			this.kickstartROM = kickstartROM;
			this.extendedKickstartROM = extendedKickstartROM;
			this.disassembly = disassembly;
			this.settings = settings.Value;
		}

		public List<Resident> GetRomTags()
		{
			return GetRomTags(kickstartROM, memory, 0).Union(GetRomTags(extendedKickstartROM, memory, 0)).ToList();
		}

		public KickstartVersion GetVersion()
		{
			uint kickstartBaseAddress = kickstartROM.MappedRange().First().Start;
			return new KickstartVersion
			{
				Major = (ushort)kickstartROM.DebugRead(kickstartBaseAddress + 0x10, Size.Word),
				Minor = (ushort)kickstartROM.DebugRead(kickstartBaseAddress + 0x12, Size.Word)
			};
		}

		public uint GetChecksum()
		{
			//This is the CRC32 embedded in the ROM
			var mappedRange = kickstartROM.MappedRange().First();
			return kickstartROM.DebugRead((uint)(mappedRange.Start + mappedRange.Length - 24), Size.Long);
		}

		public uint GetCRC()
		{
			//This is a CRC32 of the ROM data (it should match the one in the ROM)
			var mappedRange = kickstartROM.MappedRange().First();
			var crc = new Crc32();
			crc.Append(memory.GetEnumerable(mappedRange.Start, mappedRange.Length-24).ToArray());
			return crc.GetCurrentHashAsUInt32();
		}

		public byte[] GetSHA1()
		{
			//This is a SHA1 of the ROM data
			var mappedRange = kickstartROM.MappedRange().First();
			return SHA1.HashData(memory.GetEnumerable(mappedRange.Start, mappedRange.Length).ToArray());
		}

		private static List<Resident> GetRomTags(IMemoryMappedDevice kickstartROM, IDebugMemoryMapper memory, uint rombase)
		{
			var resident = new List<Resident>();
			var range = kickstartROM.MappedRange().First();
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

		private void Disassemble(List<Resident> resident, IMemoryDump memoryDump)
		{
			for (int i = 0; i < resident.Count; i++)
			{
				disassembly.Clear();
				memoryDump.ClearMapping();

				var rt = resident[i];
				var endAddress = 0xfffff0u;
				if (i != resident.Count - 1)
					endAddress = resident[i + 1].MatchTag;

				var ranges = new DisassemblyRanges();
				ranges.Add(new AddressRange(rt.MatchTag, endAddress - rt.MatchTag + 1));

				string asm = disassembly.DisassembleTxt(ranges,
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
							 $"*  The following is a complete disassembly of the Amiga {settings.KickStartDisassembly,4}               *\n" +
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
				dmp.AppendLine();
				dmp.AppendLine("^Z");
				dmp.AppendLine(memoryDump.GetString(rt.MatchTag & 0xffffffe0u, endAddress - rt.MatchTag + 1 + 31));

				File.WriteAllText($"{rt.Name}_disassembly.txt", dmp.ToString());
			}

			disassembly.Clear();
			memoryDump.ClearMapping();
		}

		public void ShowRomTags()
		{
			var resident = GetRomTags();
			foreach (var rt in resident)
				logger.LogTrace($"{rt.MatchTag:X8} {rt.Name} {rt.Flags} v:{rt.Version} {rt.Type} pri:{rt.Pri} init:{rt.Init:X8} {rt.IdString}");
		}

		public void GenerateDisassemblies(IMemoryDump memoryDump)
		{
			var resident = GetRomTags();
			Disassemble(resident, memoryDump);
		}
	}
}