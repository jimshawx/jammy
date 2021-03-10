using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types.Kickstart;

namespace RunAmiga.Disassembler
{
	public class KickstartAnalysis : IKickstartAnalysis
	{
		private readonly IMemory memory;
		private readonly ILogger logger;

		private const int RTC_MATCHWORD = 0x4AFC;

		public KickstartAnalysis(IMemory memory, ILogger<KickstartAnalysis> logger)
		{
			this.memory = memory;
			this.logger = logger;
		}

		public List<Resident> GetRomTags()
		{
			return GetRomTags(memory.GetMemoryArray(), 0);
		}

		private static List<Resident> GetRomTags(byte[] bytes, uint rombase)
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
	}
}