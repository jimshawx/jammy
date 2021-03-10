using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Options;
using RunAmiga.Core.Types.Types;
using RunAmiga.Core.Types.Types.Kickstart;
using RunAmiga.Extensions.Extensions;

namespace RunAmiga.Disassembler
{
	public class Disassembly : IDisassembly
	{
		private readonly byte[] mem;
		private readonly IBreakpointCollection breakpoints;
		private readonly IOptions<EmulationSettings> settings;
		private readonly Disassembler disassembler;
		private readonly ILabeller labeller;
		
		private readonly Dictionary<uint, Comment> comments = new Dictionary<uint, Comment>();
		private readonly Dictionary<uint, Header> headers = new Dictionary<uint, Header>();
		private readonly Dictionary<string, List<LVO>> lvos = new Dictionary<string, List<LVO>>();
		private readonly ILogger logger;
		private readonly IKickstartAnalysis kickstartAnalysis;

		private enum MemType : byte
		{
			Unknown,
			Code,
			Byte,
			Word,
			Long,
			Str
		}
		private readonly MemType[] memType = new MemType[16 * 1024 * 1024];

		public Disassembly(IMemory memory, IBreakpointCollection breakpoints, IOptions<EmulationSettings> settings,
			ILogger<Disassembly> logger, IKickstartAnalysis kickstartAnalysis, ILabeller labeller)
		{
			this.logger = logger;
			this.kickstartAnalysis = kickstartAnalysis;
			//this.memory = memory;
			mem = memory.GetMemoryArray();
			this.breakpoints = breakpoints;
			this.settings = settings;
			this.labeller = labeller;
			disassembler = new Disassembler();

			Array.Clear(memType, 0, memType.Length);

			LoadLVOs();
			StartUp();
			Analysis();
			ROMTags();
			DeDupe();
			LoadComments(settings.Value);
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
				for (;;)
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
						var bits = line.Split(new []{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
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
					uint fntable = ReadLong(address);
					AddComment(address, "vectors");
					MakeMemType(address, MemType.Long, null); address += 4;
					uint structure = ReadLong(address);
					AddComment(address, "init struct");
					MakeMemType(address, MemType.Long, null); address += 4;
					uint fninit = ReadLong(address);
					AddComment(address, "init");
					MakeMemType(address, MemType.Long, null); address += 4;
					AddHeader(address, "");

					if (structure != 0)
					{
						address = structure;
						ushort s;
						while ((s = ReadWord(address)) != 0x0000)
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

						s = ReadWord(address);
						int idx = 0;
						if (s == 0xFFFF)
						{
							MakeMemType(address, MemType.Word, null);
							address += 2;
							while ((s = ReadWord(address)) != 0xFFFF)
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
							while ((u = ReadLong(address)) != 0xFFFFFFFF)
							{
								string lvo = LVO(tag, idx);
								AddHeader(u,"");
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

		private readonly string[] fixedLVOs = {"LibOpen", "LibClose", "LibExpunge", "LibReserved", "DevBeginIO", "DevAbortIO" };

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

		private uint ReadLong(uint i)
		{
			return (uint)((mem[i] << 24) + (mem[i + 1] << 16) + (mem[i + 2] << 8) + mem[i + 3]);
		}

		private ushort ReadWord(uint i)
		{
			return (ushort)((mem[i] << 8) + mem[i + 1]);
		}

		private byte ReadByte(uint i)
		{
			return mem[i];
		}

		private void AddComment(uint address, string s)
		{
			comments[address] = new Comment {Address = address, Text = s};
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
						target = (uint)(short)ReadWord(i + 2);
					}
					else if (d == 0xff)
					{
						AddHeader(i + 6, "");
						target = ReadLong(i+2);
					}
					else
					{
						AddHeader(i + 2, "");
						target = (uint)(sbyte)d;
					}
					AddHeader(i+target+2, "");
				}

				//bsr
				if ((s & 0xff00) == 0x6100)
				{
					byte d = (byte)s;
					uint target;
					if (d == 0)
						target = (uint)(short)ReadWord(i + 2);
					else if (d == 0xff)
						target = ReadLong(i + 2);
					else
						target = (uint)(sbyte)d;
					AddHeader(target+i+2, "");
				}

				//jmp
				if ((s & 0xffc0) == 0x4ec0)
					AddHeader(i+2, "");

				//rts
				if (s == 0x4e75)
					AddHeader(i+2, "");

				//rte
				if (s == 0x4e73)
					AddHeader(i+2, "");

				//movem.l r,-(a7)
				if (s == 0b01001_0_001_1_100_111)
					AddHeader(i, "");

				//link
				if ((s&0xfff8)==0x4e50)
					AddHeader(i, "");

				//Disable()
				//FC37B2  33FC 4000 00DF F09A move.w    #$4000,$DFF09A
				//FC37BA  522E 0126           addq.b    #1,$0126(a6)
				if (s == 0x33fc &&
				    ReadWord(i+2)==0x4000 &&
				    ReadWord(i+4) == 0x00DF &&
				    ReadWord(i+6) == 0xF09A &&
				    (ReadWord(i+8)&0x5228)==0x5228 &&
				    ReadWord(i+10)== 0x126)
				{
					AddHeader(i, "");
					AddComment(i, "Disable()");
					AddHeader(i+12, "");
				}

				//Enable()
				//FC37E4  532E 0126           subq.b    #1,$0126(a6)
				//FC37E8  6C08                bge.b     #$FC37F2
				//FC37EA  33FC C000 00DF F09A move.w    #$C000,$DFF09A
				if ((s & 0x5328) == 0x5328 &&
				    ReadWord(i + 2) == 0x126 &&
				    ReadWord(i + 4) == 0x6C08 &&
				    ReadWord(i + 6) == 0x33FC &&
				    ReadWord(i + 8) == 0xC000 &&
				    ReadWord(i + 10) == 0x00DF &&
				    ReadWord(i + 12) == 0xF09A)
				{
					AddHeader(i, "");
					AddComment(i, "Enable()");
					AddHeader(i+14,"");
				}

				//todo: other candidates

				i += 2;
			}
		}

		private void AddHeader(uint address, string hdr)
		{
			if (!headers.ContainsKey(address))
				headers[address] = new Header {Address = address};

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
				var hdrs = new List<string> {""};

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
										if (hex2.IsMatch(split[i]))  nextAddress  += MakeMemType(nextAddress,MemType.Byte, split[i]); 
										else if (hex4.IsMatch(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Word, split[i]);
										else if (hex8.IsMatch(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Long, split[i]);
										else if (IsString(split[i])) nextAddress += MakeMemType(nextAddress, MemType.Str, split[i]);
										i++;
									}

									//the comments are what's left after the i'th split
									if (split.Length > i)
									{
										comments[currentAddress] = new Comment {Address = currentAddress, Text = string.Join(" ", split.Skip(i))};
									}

									currentAddress = nextAddress;
								}
								else
								{
									//it's code
									if (split.Length > 3)
									{
										//the comments are what's left after the second split, usually starting at column 49
										comments[currentAddress] = new Comment {Address = currentAddress, Text = string.Join(" ", split.Skip(3))};
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
			if (type == MemType.Byte) { memType[address] = type; return 1;}
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
					} while (mem[a++] != 0);

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
				var asm = disassembler.Disassemble(address, mem.AsSpan((int)address));
				for (uint i = address; i < address + asm.Bytes.Length; i++)
					memType[i] = type;
			}
			return 0;
		}

		private readonly Dictionary<uint, int> addressToLine = new Dictionary<uint, int>();
		private readonly Dictionary<int, uint> lineToAddress = new Dictionary<int, uint>();

		public string DisassembleTxt(List<Tuple<uint, uint>> ranges, List<uint> restartsList, DisassemblyOptions options)
		{
			addressToLine.Clear();
			lineToAddress.Clear();

			var restarts = (IEnumerable<uint>)restartsList.OrderBy(x => x).ToList();

			var memorySpan = new ReadOnlySpan<byte>(mem);
			var txt = new StringBuilder();
			var tmp = new StringBuilder();
			int line = 0;

			foreach (var range in ranges.OrderBy(x=>x.Item1))
			{
				uint address = range.Item1;
				uint size = range.Item2;
				uint addressEnd = address + size;
				while (address < addressEnd)
				{
					while (memType[address] != MemType.Byte && memType[address] != MemType.Str && (address & 1) != 0)
						address++;

					if (restarts.Any())
					{
						if (address == restarts.First())
						{
							restarts = restarts.Skip(1);
						}
						else if (address > restarts.First() && address - restarts.First() < 0x20)
						{
							address = restarts.First();
							restarts = restarts.Skip(1);
						}
					}

					if (labeller.HasLabel(address))
					{
						txt.Append($"{labeller.LabelName(address)}:\n");
						line++;
					}

					if (options.IncludeComments && headers.TryGetValue(address, out Header hdrs))
					{
						foreach (var hdr in hdrs.TextLines)
						{
							txt.AppendLine(hdr);
							line++;
						}
					}

					addressToLine.Add(address, line);
					lineToAddress.Add(line, address);

					uint lineAddress = address;

					if (options.IncludeBreakpoints)
						txt.Append(breakpoints.IsBreakpoint(address) ? '*' : ' ');

					string asm = "";
					if (memType[address] != MemType.Code && memType[address] != MemType.Unknown)
					{
						if (memType[address] == MemType.Byte)
						{
							asm = $"{address:X6}  { ReadByte(address):X2}";
							address += 1;
						}
						else if (memType[address] == MemType.Word)
						{
							asm = $"{address:X6}  { ReadWord(address):X4}";
							address += 2;
						}
						else if (memType[address] == MemType.Long)
						{
							asm = $"{address:X6}  { ReadLong(address):X8}";
							address += 4;
						}
						else if (memType[address] == MemType.Str)
						{
							var str = new List<string>();

							while (memType[address] == MemType.Str)
							{
								if (mem[address] == 0)
								{
									str.Add("00");
									address++;
									break;
								}
								else if (mem[address] == 0xD)
								{
									str.Add("CR");
								}
								else if (mem[address] == 0xA)
								{ 
									str.Add("LF");
								}
								else
								{
									tmp.Clear();
									tmp.Append('"');
									while (mem[address] != 0 && mem[address] != 0x0d && mem[address] != 0xa)
									{
										tmp.Append(Convert.ToChar(mem[address]));
										address++;
									}
									tmp.Append('"');

									address--;
									str.Add(tmp.ToString());
								}

								address++;
							}
							
							asm = $"{address:X6}  {string.Join(',', str)}";
						}
					}
					else
					{
						if ((address & 1) != 0)
						{
							asm = $"{address:X6}  { mem[address]:X2}";
							address += 1;
						}
						else
						{
							var dasm = disassembler.Disassemble(address, memorySpan.Slice((int)address, Math.Min(12, (int)(0x1000000 - address))));
							asm = dasm.ToString(options);

							uint start = address, end = (uint)(address + dasm.Bytes.Length);
							for (uint i = start; i < end && i < mem.Length; i++)
							{
								if (memType[i] != MemType.Code && memType[i] != MemType.Unknown)
								{
									break; //todo: the instruction overlapped something we know isn't code, so we could mark it as data
								}

								address++;
							}

							//address += (uint)dasm.Bytes.Length;
						}
					}

					if (options.IncludeComments)
					{
						if (comments.TryGetValue(lineAddress, out Comment comment))
						{
							if (asm.Length < 64)
								txt.AppendLine($"{asm.PadRight(64)} {comment.Text}");
							else
								txt.AppendLine($" {comment.Text}");
						}
						else
						{
							txt.AppendLine(asm);
						}
					}
					else
					{
						txt.AppendLine(asm);
					}

					line++;
				}
			}
			return txt.ToString();
		}

		public int GetAddressLine(uint address)
		{
			if (addressToLine.TryGetValue(address, out int line))
				return line;

			uint inc = 1;
			int sign = 1;
			while (Math.Abs(inc) < 16)
			{
				address += (uint)(sign * inc);
				if (addressToLine.TryGetValue(address, out int linex))
					return linex;
				if (sign == -1)
					inc++;
				sign = -sign;
			}

			return 0;
		}

		public uint GetLineAddress(int line)
		{
			if (lineToAddress.TryGetValue(line, out uint address))
				return address;
			return 0;
		}

		public string DisassembleAddress(uint pc)
		{
			var dasm = disassembler.Disassemble(pc, new ReadOnlySpan<byte>(mem).Slice((int)pc, Math.Min(12, (int)(0x1000000 - pc))));
			return dasm.ToString();
		}

		private void Disassemble(List<Resident> resident)
		{
			for (int i = 0; i < resident.Count; i++)
			{
				var rt = resident[i];
				var endAddress = 0xfffff0u;
				if (i != resident.Count - 1)
					endAddress = resident[i + 1].MatchTag;

				var dmp = new StringBuilder();
				string asm = DisassembleTxt(new List<Tuple<uint, uint>>
					{
						new Tuple<uint, uint>(rt.MatchTag, endAddress - rt.MatchTag + 1)
					}, new List<uint>(),
					new DisassemblyOptions { IncludeBytes = false, CommentPad = true, IncludeComments = true });


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
							 $"*  The following is a complete disassembly of the Amiga {settings.Value.KickStart,4}               *\n" +
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

		public void ShowRomTags()
		{
			var resident = kickstartAnalysis.GetRomTags();
			foreach (var rt in resident)
				logger.LogTrace($"{rt.MatchTag:X8}\n{rt.Name}\n{rt.IdString}\n{rt.Flags}\nv:{rt.Version}\n{rt.Type}\npri:{rt.Pri}\ninit:{rt.Init:X8}\n");

			//Disassemble(resident, disassembly, settings);

			//KickLogo.KSLogo(this);
		}
	}
}
