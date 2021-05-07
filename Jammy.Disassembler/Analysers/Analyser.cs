using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Jammy.Interface;
using Jammy.Types;
using Jammy.Types.Kickstart;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Disassembler.Analysers
{
	public class Analyser : IAnalyser
	{
		private readonly IDebugMemoryMapper mem;
		private readonly IKickstartAnalysis kickstartAnalysis;
		private readonly ILabeller labeller;
		private readonly ILogger logger;
		private readonly IKickstartROM kickstartROM;
		private readonly IAnalysis analysis;
		private readonly Disassembler disassembler;

		private readonly EmulationSettings settings;

		public Analyser(IKickstartAnalysis kickstartAnalysis, ILabeller labeller,
			IDebugMemoryMapper mem, IOptions<EmulationSettings> settings,
			ILogger<Analyser> logger, IKickstartROM kickstartROM, IAnalysis analysis, IDiskAnalysis diskAnalysis)
		{
			this.kickstartAnalysis = kickstartAnalysis;
			this.labeller = labeller;
			this.logger = logger;
			this.kickstartROM = kickstartROM;
			this.analysis = analysis;
			this.settings = settings.Value;
			this.mem = mem;

			diskAnalysis.Extract();

			disassembler = new Disassembler();
			
			LoadLVOs();
			StartUp();
			Analysis();
			ROMTags();
			Labeller();
			//NoNL();
			DeDupe();
			LoadComments();

			kickstartAnalysis.ShowRomTags();
		}

		private void NoNL()
		{
			var headers = analysis.GetHeaders();
			foreach (var h in headers.Values)
			{
				var lines = new List<string>(h.TextLines);
				h.TextLines.Clear();
				foreach (var l in lines)
					h.TextLines.Add(l.Replace("\r\n", "").Replace("\n","".Replace("\r","")));
			}

			var comments = analysis.GetComments();
			foreach (var c in comments.Values)
			{
				string l = c.Text;
				c.Text = l.Replace("\r\n", "").Replace("\n", "".Replace("\r", ""));
			}
		}

		private void Labeller()
		{
			var labels = labeller.GetLabels();

			foreach (var label in labels.Values)
				analysis.AddHeader(label.Address, $"{label.Name}:");
		}

		private void LoadLVOs()
		{
			string filename = "LVOs.i.txt";
			using (var f = File.OpenText(Path.Combine("c:/source/programming/amiga/", filename)))
			{
				if (f == null)
				{
					logger.LogTrace($"Can't find {filename} LVOs file");
					return;
				}

				string currentLib = string.Empty;
				for (; ; )
				{
					string line = f.ReadLine();
					if (line == null) break;

					if (string.IsNullOrWhiteSpace(line))
						continue;

					if (line.StartsWith("***"))
					{
						if (!line.Contains("LVO"))
							continue;

						currentLib = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[3];
						//lvos[currentLib] = new LVOCollection();
						analysis.SetLVO(currentLib, new LVOCollection());
					}
					else
					{
						var bits = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
						//lvos[currentLib].LVOs.Add(new LVO
						analysis.AddLVO(currentLib, new LVO
						{
							Name = bits[0].Substring(4),
							Offset = int.Parse(bits[2])
						});
					}
				}
			}
		}

		private void StartUp()
		{
			MakeMemType(0, MemType.Word, null);
			MakeMemType(2, MemType.Code, null);

			MakeMemType(0xfc0000, MemType.Word, null);
			MakeMemType(0xfc0002, MemType.Code, null);
		}

		private void ROMTags()
		{
			ExtractROMTags();
			ExtractExecBase();
		}

		private class ExecLocation
		{
			public string Version;
			public string Kickstart;
			public string System;
			public uint Checksum;
			public uint Address;

			public ExecLocation(string version, string kickstart, string system, uint checksum, uint address)
			{
				Version = version;
				Kickstart = kickstart;
				System = system;
				Checksum = checksum;
				Address = address;
			}
		}

		private ExecLocation[] execLocations = {
			new ExecLocation("1.2", "1.0", "",0x00000000, 0x0),
			new ExecLocation("31.34", "1.1", "", 0x00000000,0x0),
			new ExecLocation("33.166", "1.2", "", 0x00000000,0x0),
			new ExecLocation("33.180", "1.2", "", 0x00000000,0x0),
			new ExecLocation("33.192", "1.2", "A500/A1000/A2000", 0x56F2E2A6,0xFC1A40),
			new ExecLocation("34.2", "1.3", "A3000", 0x150B7DB3,0xFC1A7C),
			new ExecLocation("34.2","1.3", "A500", 0x15267DB3,0xFC1A7C),
			new ExecLocation("36.1000","2.0","A3000", 0x953958D2,0xF82034),
			new ExecLocation("37.132","2.04","A500+", 0x000B927C,0xF81F84),
			new ExecLocation("37.151","2.05","A600", 0xDB27680D,0xF81FB0),
			new ExecLocation("37.132","2.04","A3000", 0x54876DAB,0xF82000),
			new ExecLocation("40.10", "3.1", "A500/A600/A2000", 0x9FDEEEF6,0xF8236C),
			new ExecLocation("40.10", "3.1", "A1200", 0x87BA7A3E,0xF8236C),
			new ExecLocation("40.9", "3.1", "A3000", 0x97DC36A2,0xF823CC),
			new ExecLocation("40.10", "3.1", "A3000", 0x0CC4ABE0,0xF8238C),
			new ExecLocation("40.9", "3.1", "A4000", 0xF90A56C0,0xF823B4),
			new ExecLocation("40.10", "3.1", "A4000", 0x45C3145E,0xF82374),
			new ExecLocation("40.10", "3.1", "A4000", 0xE20F9194,0xF82374),
		};

		private void ExtractExecBase()
		{
			var version = kickstartAnalysis.GetVersion();
			uint checksum = kickstartAnalysis.GetChecksum();
			logger.LogTrace($"Kickstart {version.Major}.{version.Minor} Checksum {checksum:X8}");

			var execLoc = execLocations.SingleOrDefault(x => x.Checksum == checksum);
			if (execLoc != null)
				ExtractFunctionTable(execLoc.Address, NT_Type.NT_LIBRARY, "exec.library", Size.Word);
			else
				logger.LogTrace($"Did not find Execbase Function Table for {version.Major}.{version.Minor}");
		}

		private void ExtractROMTags()
		{
			var romtags = kickstartAnalysis.GetRomTags();
			foreach (var tag in romtags)
			{
				var com = KickstartAnalysis.ROMTagLines(tag);
				uint address = tag.MatchTag;

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

				analysis.AddHeader(address, "");
				analysis.AddHeader(address, $"\t; The {tag.Name} RomTag Structure");
				analysis.AddHeader(address, "");

				MakeMemType(address, MemType.Word, null); analysis.AddComment(address, com[0]); address += 2;
				MakeMemType(address, MemType.Long, null); analysis.AddComment(address, com[1]); address += 4;
				MakeMemType(address, MemType.Long, null); analysis.AddComment(address, com[2]); address += 4;
				MakeMemType(address, MemType.Byte, null); analysis.AddComment(address, com[3]); address++;
				MakeMemType(address, MemType.Byte, null); analysis.AddComment(address, com[4]); address++;
				MakeMemType(address, MemType.Byte, null); analysis.AddComment(address, com[5]); address++;
				MakeMemType(address, MemType.Byte, null); analysis.AddComment(address, com[6]); address++;
				MakeMemType(address, MemType.Long, null); analysis.AddComment(address, com[7]); address += 4;
				MakeMemType(address, MemType.Long, null); analysis.AddComment(address, com[8]); address += 4;
				MakeMemType(address, MemType.Long, null); analysis.AddComment(address, com[9]); address += 4;
				analysis.AddHeader(address, "");

				MakeMemType(tag.NamePtr, MemType.Str, null);
				MakeMemType(tag.IdStringPtr, MemType.Str, null);

				if ((tag.Flags & RTF.RTF_AUTOINIT) != 0)
				{
					address = tag.Init;

					analysis.AddHeader(address, "");
					analysis.AddHeader(address, $"\t; {tag.Name} init struct");
					analysis.AddComment(address, "size");
					MakeMemType(address, MemType.Long, null); address += 4;
					uint fntable = mem.UnsafeRead32(address);
					analysis.AddComment(address, "vectors");
					MakeMemType(address, MemType.Long, null); address += 4;
					uint structure = mem.UnsafeRead32(address);
					analysis.AddComment(address, "init struct");
					MakeMemType(address, MemType.Long, null); address += 4;
					uint fninit = mem.UnsafeRead32(address);
					analysis.AddComment(address, "init");
					MakeMemType(address, MemType.Long, null); address += 4;
					analysis.AddHeader(address, "");

					if (structure != 0)
						ExtractStructureInit(structure);

					if (fntable != 0)
						ExtractFunctionTable(fntable, tag.Type, tag.Name);

					if (fninit != 0)
					{
						address = fninit;

						analysis.AddHeader(address, "");
						analysis.AddHeader(address, $"\t; {tag.Name} init");
						analysis.AddHeader(address, "");
					}
				}
				else
				{
					if (tag.Init != 0)
					{
						address = tag.Init;

						analysis.AddHeader(address, "");
						analysis.AddHeader(address, $"\t; {tag.Name} init");
						analysis.AddHeader(address, "");
					}
				}
			}
		}

		public void ExtractFunctionTable(uint fntable, int count, string name, Size size)
		{
			uint address = fntable;
			ushort s;
			int idx = 0;

			if (name == null) name = $"fntable_{fntable:X8}";

			analysis.AddHeader(address, "");
			analysis.AddHeader(address, $"\t; {name} vectors");

			if (size == Size.Word)
			{
				while (count-- > 0)
				{
					s = mem.UnsafeRead16(address);

					uint u = fntable + s;
					analysis.AddHeader(u, "");
					analysis.AddHeader(u, "---------------------------------------------------------------------------");
					analysis.AddHeader(u, $"\t{name}_{idx}");
					analysis.AddHeader(u, "---------------------------------------------------------------------------");
					analysis.AddHeader(u, "");

					analysis.AddComment(address, $"{name}_{idx}");
					MakeMemType(address, MemType.Word, null);
					address += 2;
					idx++;
				}
			}
			else
			{
				while (count-- > 0)
				{
					uint u = mem.UnsafeRead32(address);

					analysis.AddHeader(u, "");
					analysis.AddHeader(u, "---------------------------------------------------------------------------");
					analysis.AddHeader(u, $"\t{name}_{idx}");
					analysis.AddHeader(u, "---------------------------------------------------------------------------");
					analysis.AddHeader(u, "");

					analysis.AddComment(address, $"{name}_{idx}");
					MakeMemType(address, MemType.Long, null);
					address += 4;
					idx++;
				}
			}
			analysis.AddHeader(address, "");
		}

		public void ExtractFunctionTable(uint fntable, NT_Type type, string name, Size? size = null)
		{
			uint address = fntable;

			analysis.AddHeader(address, "");
			analysis.AddHeader(address, $"\t; {name} vectors");

			ushort s = mem.UnsafeRead16(address);
			int idx = 0;
			if (s == 0xFFFF || size == Size.Word)
			{
				if (size == null)
				{
					MakeMemType(address, MemType.Word, null);
					address += 2;
				}

				while ((s = mem.UnsafeRead16(address)) != 0xFFFF)
				{
					uint u = fntable + s;
					string lvo = LVO(type, name, idx);
					analysis.AddHeader(u, "");
					analysis.AddHeader(u, "---------------------------------------------------------------------------");
					analysis.AddHeader(u, $"\t{lvo}");
					analysis.AddHeader(u, "---------------------------------------------------------------------------");
					analysis.AddHeader(u, "");

					analysis.AddComment(address, $"\tjmp ${u:X6}\t{(idx + 1) * -6}\t{lvo}");
					MakeMemType(address, MemType.Word, null);
					address += 2;
					idx++;
				}

				MakeMemType(address, MemType.Word, null);
				address += 2;
				analysis.AddHeader(address, "");
			}
			else
			{
				uint u;
				while ((u = mem.UnsafeRead32(address)) != 0xFFFFFFFF)
				{
					string lvo = LVO(type, name, idx);
					analysis.AddHeader(u, "");
					analysis.AddHeader(u, "---------------------------------------------------------------------------");
					analysis.AddHeader(u, $"\t{lvo}");
					analysis.AddHeader(u, "---------------------------------------------------------------------------");
					analysis.AddHeader(u, "");

					analysis.AddComment(address, $"\tjmp ${u:X6}\t{(idx + 1) * -6}\t{lvo}");
					MakeMemType(address, MemType.Long, null);
					address += 4;
					idx++;
				}

				MakeMemType(address, MemType.Long, null);
				address += 4;
				analysis.AddHeader(address, "");
			}
		}

		public void ExtractStructureInit(uint address)
		{
			ushort s;
			while ((s = mem.UnsafeRead16(address)) != 0x0000)
			{
				MakeMemType(address, MemType.Word, null);
				address += 2;
				if (s == 0xE000 || s == 0xD000)
				{
					MakeMemType(address, MemType.Word, null);
					address += 2;
					MakeMemType(address, MemType.Word, null);
					address += 2;
				}
				else if (s == 0xC000)
				{
					MakeMemType(address, MemType.Word, null);
					address += 2;
					MakeMemType(address, MemType.Long, null);
					address += 4;
				}
			}

			MakeMemType(address, MemType.Word, null);
			address += 2;
			analysis.AddHeader(address, "");
		}

		private readonly string[] fixedLVOs = { "LibOpen", "LibClose", "LibExpunge", "LibReserved", "DevBeginIO", "DevAbortIO" };

		private string LVO(NT_Type type, string name, int idx)
		{
			if (type == NT_Type.NT_LIBRARY)
			{
				if (idx < 4) return $"{fixedLVOs[idx]}() {name}";
			}
			else if (type == NT_Type.NT_DEVICE)
			{
				if (idx < 6) return $"{fixedLVOs[idx]}() {name}";
			}

			var lvos = analysis.GetLVOs();
			if (lvos.TryGetValue(name, out var lvolist))
			{
				var lvo = lvolist.LVOs.SingleOrDefault(x => x.Index == idx);
				if (lvo != null)
					return $"{lvo.Name}()";
			}

			return "";
		}

		private void DeDupe()
		{
			var headers = analysis.GetHeaders();
			foreach (var vals in headers.Values)
			{
				bool lastBlank = false;
				var newHdrs = new List<string>();
				foreach (var hdr in vals.TextLines)
				{
					bool thisBlank = string.IsNullOrWhiteSpace(hdr);
					if (!(lastBlank && thisBlank))
						newHdrs.Add(hdr);
					lastBlank = thisBlank;
				}
				vals.TextLines.Clear();
				vals.TextLines.AddRange(newHdrs);
			}
		}

		private void Analysis()
		{
			uint i;

			i = kickstartROM.MappedRange().First().Start;
			foreach (uint s in mem.AsULong((int)kickstartROM.MappedRange().First().Start))
			{
				if (s == 0x4e750000)
					MakeMemType(i + 2, MemType.Word, null);
				i += 4;
			}

			i = kickstartROM.MappedRange().First().Start;
			foreach (ushort s in mem.AsUWord((int)kickstartROM.MappedRange().First().Start))
			{
				//bra
				if ((s & 0xff00) == 0x6000)
				{
					byte d = (byte)s;
					uint target;
					if (d == 0)
					{
						analysis.AddHeader(i + 4, "");
						target = (uint)(short)mem.UnsafeRead16(i + 2);
					}
					else if (d == 0xff)
					{
						analysis.AddHeader(i + 6, "");
						target = mem.UnsafeRead32(i + 2);
					}
					else
					{
						analysis.AddHeader(i + 2, "");
						target = (uint)(sbyte)d;
					}
					analysis.AddHeader(i + target + 2, "");
				}

				//bsr
				if ((s & 0xff00) == 0x6100)
				{
					byte d = (byte)s;
					uint target;
					if (d == 0)
						target = (uint)(short)mem.UnsafeRead16(i + 2);
					else if (d == 0xff)
						target = mem.UnsafeRead32(i + 2);
					else
						target = (uint)(sbyte)d;
					analysis.AddHeader(target + i + 2, "");
				}

				//jmp
				if ((s & 0xffc0) == 0x4ec0)
					analysis.AddHeader(i + 2, "");

				//rts
				if (s == 0x4e75)
					analysis.AddHeader(i + 2, "");

				//rte
				if (s == 0x4e73)
					analysis.AddHeader(i + 2, "");

				//movem.l r,-(a7)
				if (s == 0b01001_0_001_1_100_111)
					analysis.AddHeader(i, "");

				//link
				if ((s & 0xfff8) == 0x4e50)
					analysis.AddHeader(i, "");

				//Disable()
				//FC37B2  33FC 4000 00DF F09A move.w    #$4000,$DFF09A
				//FC37BA  522E 0126           addq.b    #1,$0126(a6)
				if (s == 0x33fc &&
					mem.UnsafeRead16(i + 2) == 0x4000 &&
					mem.UnsafeRead16(i + 4) == 0x00DF &&
					mem.UnsafeRead16(i + 6) == 0xF09A &&
					(mem.UnsafeRead16(i + 8) & 0x5228) == 0x5228 &&
					mem.UnsafeRead16(i + 10) == 0x126)
				{
					analysis.AddHeader(i, "");
					analysis.AddComment(i, "Disable()");
					analysis.AddHeader(i + 12, "");
				}

				//Enable()
				//FC37E4  532E 0126           subq.b    #1,$0126(a6)
				//FC37E8  6C08                bge.b     #$FC37F2
				//FC37EA  33FC C000 00DF F09A move.w    #$C000,$DFF09A
				if ((s & 0x5328) == 0x5328 &&
					mem.UnsafeRead16(i + 2) == 0x126 &&
					mem.UnsafeRead16(i + 4) == 0x6C08 &&
					mem.UnsafeRead16(i + 6) == 0x33FC &&
					mem.UnsafeRead16(i + 8) == 0xC000 &&
					mem.UnsafeRead16(i + 10) == 0x00DF &&
					mem.UnsafeRead16(i + 12) == 0xF09A)
				{
					analysis.AddHeader(i, "");
					analysis.AddComment(i, "Enable()");
					analysis.AddHeader(i + 14, "");
				}

				//todo: other candidates

				i += 2;
			}
		}


		private void LoadComments()
		{
			string fullPath = Path.Combine($"c:/source/programming/amiga/KS{settings.KickStart}");
			if (Directory.Exists(fullPath))
			{
				var files = Directory.GetFiles(fullPath, "*_disassembly.txt");
				foreach (var file in files)
					LoadComment(file);
			}
		}

		private void LoadComment(string fullPath)
		{
			var hex6 = new Regex(@"^[\d|a-f|A-F]{6}", RegexOptions.Compiled);
			var hex2 = new Regex(@"^[\d|a-f|A-F]{2}$", RegexOptions.Compiled);
			var hex4 = new Regex(@"^[\d|a-f|A-F]{4}$", RegexOptions.Compiled);
			var hex8 = new Regex(@"^[\d|a-f|A-F]{8}$", RegexOptions.Compiled);
			var reg = new Regex("^[A|D][0-7]$", RegexOptions.Compiled);

			if (!File.Exists(fullPath))
			{
				logger.LogTrace($"Can't find {Path.GetFileName(fullPath)} comments file in {Path.GetDirectoryName(fullPath)}");
				return;
			}

			using (var f = File.OpenText(fullPath))
			{
				uint currentAddress = 0;
				var hdrs = new List<string> { "" };

				for (; ; )
				{
					string line = f.ReadLine();
					if (line == null) break;
					if (line == "^Z") break;//EOF

					//if (currentAddress == 0xfc0018) System.Diagnostics.Debugger.Break();

					var split = line.SplitSmart(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

					if (string.IsNullOrWhiteSpace(line) || line.StartsWith('*') || line.TrimStart().StartsWith(';'))
					{
						//whitespace, lines starting with * or ; are all headers.
						hdrs.Add(line);
					}
					else if (reg.IsMatch(split[0]) && split.Length > 1)
					{
						//lines starting with D0-D7, A0-A7 with more following are all headers.
						hdrs.Add(line);
					}
					else
					{
						//code is always xxxxxx..asm.....maybe followed by a comment
						//data is sometimes xxxxxx.. followed by a mix of byte/word/long possibly followed by a comment
						//     or           ........ followed by a mix of byte/word/long possibly followed by a comment

						//there might be a bunch of tabs instead of spaces
						line = Regex.Replace(line, "\t", "       ");

						if (hex6.IsMatch(line))
						{
							currentAddress = uint.Parse(split[0], NumberStyles.HexNumber);

							if (hdrs.Any())
							{
								//attach any previous headers to the new address and start collecting new ones
								analysis.ReplaceHeader(currentAddress, hdrs);
								hdrs.Clear();
							}

							if (split.Length > 1)
							{
								//code or data starting with xxxxxx
								if ((hex2.IsMatch(split[1]) || hex4.IsMatch(split[1]) || hex8.IsMatch(split[1]) || IsString(split[1])))
								{
									//it's data
									uint nextAddress = currentAddress;
									int i = 1;
									while (i < split.Length && (hex2.IsMatch(split[i]) || hex4.IsMatch(split[i]) || hex8.IsMatch(split[i]) || IsString(split[i])))
									{
										if (hex2.IsMatch(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Byte, split[i]);
										else if (hex4.IsMatch(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Word, split[i]);
										else if (hex8.IsMatch(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Long, split[i]);
										else if (IsString(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Str, split[i]);
										i++;
									}

									//the comments are what's left after the i'th split
									if (split.Length > i)
									{
										analysis.AddComment(new Comment { Address = currentAddress, Text = string.Join(" ", split.Skip(i)) });
									}

									currentAddress = nextAddress;
								}
								else
								{
									//it's code
									if (split.Length > 3)
									{
										//the comments are what's left after the second split, usually starting at column 49
										analysis.AddComment(new Comment { Address = currentAddress, Text = string.Join(" ", split.Skip(3)) });
									}
									else if (split.Length < 3)
									{
										var oneWordOps = new List<string> {"rts", "nop", "illegal", "reset", "stop", "rte", "trapv", "rtr", "unknown"};
										if (split.Length < 2 || !oneWordOps.Contains(split[1]))
										{
											//it's probably comments
											analysis.AddComment(new Comment {Address = currentAddress, Text = string.Join(" ", split.Skip(1))});
										}
									}

									//== 3 means it's just disassembled code
								}
							}
						}
						else if (hex2.IsMatch(split[0]) || hex4.IsMatch(split[0]) || hex8.IsMatch(split[0]) || IsString(split[0]))
						{
							uint nextAddress = currentAddress;

							//it's data
							int i = 0;
							while (i < split.Length && (hex2.IsMatch(split[i]) || hex4.IsMatch(split[i]) || hex8.IsMatch(split[i]) || IsString(split[i])))
							{
								if (hex2.IsMatch(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Byte, split[i]);
								else if (hex4.IsMatch(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Word, split[i]);
								else if (hex8.IsMatch(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Long, split[i]);
								else if (IsString(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Str, split[i]);
								i++;
							}
							//the comments are what's left after the i'th split
							if (split.Length > i)
							{
								analysis.AddComment(new Comment { Address = currentAddress, Text = string.Join(" ", split.Skip(i)) });
							}

							currentAddress = nextAddress;
						}
						else
						{
							//it's probably header
							hdrs.Add(line);
						}
					}
				}
			}
		}

		private bool IsChr(string s)
		{
			return s == "CR" || s == "LF" || s == "00";
		}

		private bool IsStr(string s)
		{
			return s.Length > 1 && s.StartsWith('"') && s.EndsWith('"');
		}

		private bool IsString(string s)
		{
			//e.g.
			//FC34E6  "audio.device",00,00
			//FC34F4  "audio 33.4 (9 Jun 1986)",CR,LF,00
			//FC0018  "exec 33.192 (8 Oct 1986)", CR, LF, 00, 00
			//FE0DC6  "Brought to you by not a mere Wizard, but the Wizard Extraordinaire: Dale Luck!",00,00,00,00

			var bits = s.SplitSmart(',', StringSplitOptions.RemoveEmptyEntries);

			//remove any comment off the end
			if (bits.Length > 1)
				bits[^1] = bits[^1].Split(' ', StringSplitOptions.RemoveEmptyEntries).First();

			//are all the bits string or chars?
			foreach (var b in bits)
			{
				if (!IsStr(b) && !IsChr(b)) return false;
			}

			return true;
		}

		public void MarkAsType(uint address, MemType type, Size size)
		{
			if (type == MemType.Code)
			{
				MakeMemType(address, MemType.Code, null);
			}
			else if (type == MemType.Byte)
			{
				if (size == Size.Word) type = MemType.Word;
				else if (size == Size.Long) type = MemType.Long;
				MakeMemType(address, type, null);
			}
		}

		private uint MakeMemType(uint address, MemType type, string str)
		{
			if (analysis.OutOfMemtypeRange(address)) return 0;

			if (type == MemType.Byte) { analysis.SetMemType(address, type); return 1; }
			else if (type == MemType.Word) { analysis.SetMemType(address, type); analysis.SetMemType(address + 1, type); return 2; }
			else if (type == MemType.Long) { analysis.SetMemType(address, type); analysis.SetMemType(address + 1, type); analysis.SetMemType(address + 2, type); analysis.SetMemType(address + 3, type); return 4; }
			else if (type == MemType.Str)
			{
				if (str == null)
				{
					if (address == 0)
						return 0;

					uint a = address;
					uint c = 0;
					do
					{
						analysis.SetMemType(a, type);
						c++;
					} while (mem.UnsafeRead8(a++) != 0);

					return c;
				}
				else
				{
					var bits = str.SplitSmart(',', StringSplitOptions.RemoveEmptyEntries);

					//remove any comment off the end
					if (bits.Length > 1)
						bits[^1] = bits[^1].Split(' ', StringSplitOptions.RemoveEmptyEntries).First();

					uint c = 0;
					foreach (var b in bits)
					{
						if (IsStr(b)) c += (uint)b.Length - 2;
						else if (IsChr(b)) c++;
					}

					for (uint i = address; i < address + c; i++)
						analysis.SetMemType(i, type);

					return c;
				}
			}
			else if (type == MemType.Code)
			{
				var asm = disassembler.Disassemble(address, mem.GetEnumerable((int)address, 20));
				for (uint i = address; i < address + asm.Bytes.Length; i++)
					analysis.SetMemType(i, type);
			}
			return 0;
		}
	}
}