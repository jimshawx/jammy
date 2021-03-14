using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;
using RunAmiga.Core.Types.Types.Kickstart;
using RunAmiga.Extensions.Extensions;

namespace RunAmiga.Disassembler.Analysers
{
	public class Analyser : IAnalyser
	{
		private readonly ByteArrayWrapper mem;

		private readonly IKickstartAnalysis kickstartAnalysis;
		private readonly ILabeller labeller;
		private readonly ILogger logger;
		private readonly Disassembler disassembler;

		private readonly Dictionary<uint, Comment> comments = new Dictionary<uint, Comment>();
		private readonly Dictionary<uint, Header> headers = new Dictionary<uint, Header>();
		private readonly Dictionary<string, List<LVO>> lvos = new Dictionary<string, List<LVO>>();
		private readonly MemType[] memType;

		public Analyser(IKickstartAnalysis kickstartAnalysis, ILabeller labeller,
			IMemory memory, IOptions<EmulationSettings> settings, ILogger<Analyser> logger)
		{
			this.kickstartAnalysis = kickstartAnalysis;
			this.labeller = labeller;
			this.logger = logger;

			memType = new MemType[settings.Value.MemorySize];

			mem = new ByteArrayWrapper(memory.GetMemoryArray());
			disassembler = new Disassembler();
			
			LoadLVOs();
			StartUp();
			Analysis();
			ROMTags();
			Labeller();
			DeDupe();
			LoadComments(settings.Value);
		}

		public MemType[] GetMemTypes()
		{
			return memType;
		}

		public Dictionary<uint, Header> GetHeaders()
		{
			return headers;
		}

		public Dictionary<uint, Comment> GetComments()
		{
			return comments;
		}

		private void Labeller()
		{
			var labels = labeller.GetLabels();

			foreach (var label in labels.Values)
				AddHeader(label.Address, $"{label.Name}:");
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
						lvos[currentLib] = new List<LVO>();
					}
					else
					{
						var bits = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
						lvos[currentLib].Add(new LVO
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

				AddHeader(address, "");
				AddHeader(address, $"\t; The {tag.Name} RomTag Structure");
				AddHeader(address, "");

				MakeMemType(address, MemType.Word, null); AddComment(address, com[0]); address += 2;
				MakeMemType(address, MemType.Long, null); AddComment(address, com[1]); address += 4;
				MakeMemType(address, MemType.Long, null); AddComment(address, com[2]); address += 4;
				MakeMemType(address, MemType.Byte, null); AddComment(address, com[3]); address++;
				MakeMemType(address, MemType.Byte, null); AddComment(address, com[4]); address++;
				MakeMemType(address, MemType.Byte, null); AddComment(address, com[5]); address++;
				MakeMemType(address, MemType.Byte, null); AddComment(address, com[6]); address++;
				MakeMemType(address, MemType.Long, null); AddComment(address, com[7]); address += 4;
				MakeMemType(address, MemType.Long, null); AddComment(address, com[8]); address += 4;
				MakeMemType(address, MemType.Long, null); AddComment(address, com[9]); address += 4;
				AddHeader(address, "");

				MakeMemType(tag.NamePtr, MemType.Str, null);
				MakeMemType(tag.IdStringPtr, MemType.Str, null);

				if ((tag.Flags & RTF.RTF_AUTOINIT) != 0)
				{
					address = tag.Init;

					AddHeader(address, "");
					AddHeader(address, $"\t; {tag.Name} init struct");
					AddComment(address, "size");
					MakeMemType(address, MemType.Long, null); address += 4;
					uint fntable = mem.ReadLong(address);
					AddComment(address, "vectors");
					MakeMemType(address, MemType.Long, null); address += 4;
					uint structure = mem.ReadLong(address);
					AddComment(address, "init struct");
					MakeMemType(address, MemType.Long, null); address += 4;
					uint fninit = mem.ReadLong(address);
					AddComment(address, "init");
					MakeMemType(address, MemType.Long, null); address += 4;
					AddHeader(address, "");

					if (structure != 0)
					{
						address = structure;
						ushort s;
						while ((s = mem.ReadWord(address)) != 0x0000)
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
						AddHeader(address, "");
					}

					if (fntable != 0)
					{
						ushort s;

						address = fntable;

						AddHeader(address, "");
						AddHeader(address, $"\t; {tag.Name} vectors");

						s = mem.ReadWord(address);
						int idx = 0;
						if (s == 0xFFFF)
						{
							MakeMemType(address, MemType.Word, null);
							address += 2;
							while ((s = mem.ReadWord(address)) != 0xFFFF)
							{
								uint u = fntable + s;
								string lvo = LVO(tag, idx);
								AddHeader(u, "");
								AddHeader(u, "---------------------------------------------------------------------------");
								AddHeader(u, $"\t{lvo}");
								AddHeader(u, "---------------------------------------------------------------------------");
								AddHeader(u, "");

								AddComment(address, $"\tjmp ${u:X6}\t{(idx + 1) * -6}\t{lvo}");
								MakeMemType(address, MemType.Word, null);
								address += 2;
								idx++;
							}

							MakeMemType(address, MemType.Word, null);
							address += 2;
							AddHeader(address, "");
						}
						else
						{
							uint u;
							while ((u = mem.ReadLong(address)) != 0xFFFFFFFF)
							{
								string lvo = LVO(tag, idx);
								AddHeader(u, "");
								AddHeader(u, "---------------------------------------------------------------------------");
								AddHeader(u, $"\t{lvo}");
								AddHeader(u, "---------------------------------------------------------------------------");
								AddHeader(u, "");

								AddComment(address, $"\tjmp ${u:X6}\t{(idx + 1) * -6}\t{lvo}");
								MakeMemType(address, MemType.Long, null);
								address += 4;
								idx++;
							}

							MakeMemType(address, MemType.Long, null);
							address += 4;
							AddHeader(address, "");
						}
					}

					if (fninit != 0)
					{
						address = fninit;

						AddHeader(address, "");
						AddHeader(address, $"\t; {tag.Name} init");
						AddHeader(address, "");
					}
				}
				else
				{
					if (tag.Init != 0)
					{
						address = tag.Init;

						AddHeader(address, "");
						AddHeader(address, $"\t; {tag.Name} init");
						AddHeader(address, "");
					}
				}
			}
		}

		private readonly string[] fixedLVOs = { "LibOpen", "LibClose", "LibExpunge", "LibReserved", "DevBeginIO", "DevAbortIO" };

		private string LVO(Resident res, int idx)
		{
			if (res.Type == NT_Type.NT_LIBRARY)
			{
				if (idx < 4) return $"{fixedLVOs[idx]}() {res.Name}";
			}
			else if (res.Type == NT_Type.NT_DEVICE)
			{
				if (idx < 6) return $"{fixedLVOs[idx]}() {res.Name}";
			}

			if (lvos.TryGetValue(res.Name, out var lvolist))
			{
				var lvo = lvolist.SingleOrDefault(x => x.Index == idx);
				if (lvo != null)
					return $"{lvo.Name}()";
			}

			return "";
		}

		private void DeDupe()
		{
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


		private void AddComment(uint address, string s)
		{
			comments[address] = new Comment { Address = address, Text = s };
		}

		private void Analysis()
		{
			uint i;

			i = 0;
			foreach (uint s in mem.AsULong())
			{
				if (s == 0x4e750000)
					MakeMemType(i + 2, MemType.Word, null);
				i += 4;
			}

			i = 0;
			foreach (ushort s in mem.AsUWord())
			{
				//bra
				if ((s & 0xff00) == 0x6000)
				{
					byte d = (byte)s;
					uint target;
					if (d == 0)
					{
						AddHeader(i + 4, "");
						target = (uint)(short)mem.ReadWord(i + 2);
					}
					else if (d == 0xff)
					{
						AddHeader(i + 6, "");
						target = mem.ReadLong(i + 2);
					}
					else
					{
						AddHeader(i + 2, "");
						target = (uint)(sbyte)d;
					}
					AddHeader(i + target + 2, "");
				}

				//bsr
				if ((s & 0xff00) == 0x6100)
				{
					byte d = (byte)s;
					uint target;
					if (d == 0)
						target = (uint)(short)mem.ReadWord(i + 2);
					else if (d == 0xff)
						target = mem.ReadLong(i + 2);
					else
						target = (uint)(sbyte)d;
					AddHeader(target + i + 2, "");
				}

				//jmp
				if ((s & 0xffc0) == 0x4ec0)
					AddHeader(i + 2, "");

				//rts
				if (s == 0x4e75)
					AddHeader(i + 2, "");

				//rte
				if (s == 0x4e73)
					AddHeader(i + 2, "");

				//movem.l r,-(a7)
				if (s == 0b01001_0_001_1_100_111)
					AddHeader(i, "");

				//link
				if ((s & 0xfff8) == 0x4e50)
					AddHeader(i, "");

				//Disable()
				//FC37B2  33FC 4000 00DF F09A move.w    #$4000,$DFF09A
				//FC37BA  522E 0126           addq.b    #1,$0126(a6)
				if (s == 0x33fc &&
					mem.ReadWord(i + 2) == 0x4000 &&
					mem.ReadWord(i + 4) == 0x00DF &&
					mem.ReadWord(i + 6) == 0xF09A &&
					(mem.ReadWord(i + 8) & 0x5228) == 0x5228 &&
					mem.ReadWord(i + 10) == 0x126)
				{
					AddHeader(i, "");
					AddComment(i, "Disable()");
					AddHeader(i + 12, "");
				}

				//Enable()
				//FC37E4  532E 0126           subq.b    #1,$0126(a6)
				//FC37E8  6C08                bge.b     #$FC37F2
				//FC37EA  33FC C000 00DF F09A move.w    #$C000,$DFF09A
				if ((s & 0x5328) == 0x5328 &&
					mem.ReadWord(i + 2) == 0x126 &&
					mem.ReadWord(i + 4) == 0x6C08 &&
					mem.ReadWord(i + 6) == 0x33FC &&
					mem.ReadWord(i + 8) == 0xC000 &&
					mem.ReadWord(i + 10) == 0x00DF &&
					mem.ReadWord(i + 12) == 0xF09A)
				{
					AddHeader(i, "");
					AddComment(i, "Enable()");
					AddHeader(i + 14, "");
				}

				//todo: other candidates

				i += 2;
			}
		}

		private void AddHeader(uint address, string hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header { Address = address };

			headers[address].TextLines.Add(hdr);
		}

		private void AddHeader(uint address, List<string> hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header { Address = address };

			headers[address].TextLines.AddRange(hdr);
		}

		private void ReplaceHeader(uint address, string hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header { Address = address };

			headers[address].TextLines.Clear();
			headers[address].TextLines.Add(hdr);
		}

		private void ReplaceHeader(uint address, List<string> hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header { Address = address };

			headers[address].TextLines.Clear();
			headers[address].TextLines.AddRange(hdr);
		}

		private void LoadComments(EmulationSettings settings)
		{
			if (settings.KickStart == "1.2")
			{
				LoadComment("exec_disassembly.txt");
				LoadComment("trackdisk.device_disassembly.txt");
				LoadComment("strap_disassembly.txt");
				LoadComment("misc.resource_disassembly.txt");
				LoadComment("keymap.resource_disassembly.txt");
				LoadComment("timer.device_disassembly.txt");
				LoadComment("cia.resource_disassembly.txt");
				LoadComment("potgo.resource_disassembly.txt");
				LoadComment("ramlib.library_disassembly.txt");
				LoadComment("workbench.task_disassembly.txt");
				LoadComment("mathffp.library_disassembly.txt");
				LoadComment("layers.library_disassembly.txt");
				LoadComment("intuition.library_disassembly.txt");
				LoadComment("graphics.library_disassembly.txt");
				LoadComment("expansion.library_disassembly.txt");
				LoadComment("dos.library_disassembly.txt");
				LoadComment("disk.resource_disassembly.txt");
				LoadComment("audio.device_disassembly.txt");
			}
		}

		private void LoadComment(string filename)
		{
			var hex6 = new Regex(@"^[\d|a-f|A-F]{6}", RegexOptions.Compiled);
			var hex2 = new Regex(@"^[\d|a-f|A-F]{2}$", RegexOptions.Compiled);
			var hex4 = new Regex(@"^[\d|a-f|A-F]{4}$", RegexOptions.Compiled);
			var hex8 = new Regex(@"^[\d|a-f|A-F]{8}$", RegexOptions.Compiled);
			var reg = new Regex("^[A|D][0-7]$", RegexOptions.Compiled);

			using (var f = File.OpenText(Path.Combine("c:/source/programming/amiga/", filename)))
			{
				if (f == null)
				{
					logger.LogTrace($"Can't find {filename} comments file");
					return;
				}

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
								ReplaceHeader(currentAddress, hdrs);
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
										comments[currentAddress] = new Comment { Address = currentAddress, Text = string.Join(" ", split.Skip(i)) };
									}

									currentAddress = nextAddress;
								}
								else
								{
									//it's code
									if (split.Length > 3)
									{
										//the comments are what's left after the second split, usually starting at column 49
										comments[currentAddress] = new Comment { Address = currentAddress, Text = string.Join(" ", split.Skip(3)) };
									}
									else if (split.Length < 3)
									{
										//it's probably comments
										comments[currentAddress] = new Comment { Address = currentAddress, Text = string.Join(" ", split.Skip(1)) };
									}
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
								comments[currentAddress] = new Comment { Address = currentAddress, Text = string.Join(" ", split.Skip(i)) };
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

		private uint MakeMemType(uint address, MemType type, string str)
		{
			if (address >= memType.Length) return 0;

			if (type == MemType.Byte) { memType[address] = type; return 1; }
			else if (type == MemType.Word) { memType[address] = type; memType[address + 1] = type; return 2; }
			else if (type == MemType.Long) { memType[address] = type; memType[address + 1] = type; memType[address + 2] = type; memType[address + 3] = type; return 4; }
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
						memType[a] = type;
						c++;
					} while (mem.ReadByte(a++) != 0);

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
						memType[i] = type;

					return c;
				}
			}
			else if (type == MemType.Code)
			{
				var asm = disassembler.Disassemble(address, mem.GetSpan().Slice((int)address));
				for (uint i = address; i < address + asm.Bytes.Length; i++)
					memType[i] = type;
			}
			return 0;
		}
	}
}