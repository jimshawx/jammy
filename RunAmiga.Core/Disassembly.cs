using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RunAmiga.Extensions;
using RunAmiga.Options;
using RunAmiga.Types;

namespace RunAmiga
{
	public class Disassembly
	{
		private readonly byte[] memory;
		private readonly BreakpointCollection breakpoints;
		private readonly Disassembler disassembler;
		private readonly Labeller labeller;
		
		private readonly Dictionary<uint, Comment> comments = new Dictionary<uint, Comment>();
		private readonly Dictionary<uint, Header> headers = new Dictionary<uint, Header>();
		private readonly ILogger logger;

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

		public Disassembly(byte[] memory, BreakpointCollection breakpoints)
		{
			logger = ServiceProviderFactory.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Disassembly>();
			this.memory = memory;
			this.breakpoints = breakpoints;
			disassembler = new Disassembler();
			labeller = new Labeller();
			LoadComments();
		}

		private void LoadComments()
		{
			Array.Clear(memType, 0, memType.Length);

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
								headers[currentAddress] = new Header {Address = currentAddress};
								headers[currentAddress].TextLines.AddRange(hdrs);
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

			//todo: broken for embedded commas
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

		private uint MakeMemType(uint nextAddress, MemType mt, string s)
		{
			if (mt == MemType.Byte) { memType[nextAddress] = mt; return 1;}
			else if (mt == MemType.Word) { memType[nextAddress] = mt; memType[nextAddress + 1] = mt; return 2; }
			else if (mt == MemType.Long) { memType[nextAddress] = mt; memType[nextAddress + 1] = mt; memType[nextAddress + 2] = mt; memType[nextAddress + 3] = mt; return 4; }
			else if (mt == MemType.Str)
			{
				var bits = s.SplitSmart(',', StringSplitOptions.RemoveEmptyEntries);

				//remove any comment off the end
				if (bits.Length > 1)
					bits[^1] = bits[^1].Split(' ', StringSplitOptions.RemoveEmptyEntries).First();

				uint c = 0;
				foreach (var b in bits)
				{
					if (IsStr(b)) c += (uint)b.Length - 2;
					else if (IsChr(b)) c++;
				}

				for (uint i = nextAddress; i < nextAddress + c; i++)
					memType[i] = mt;

				return c;
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

			var memorySpan = new ReadOnlySpan<byte>(memory);
			var txt = new StringBuilder();
			var tmp = new StringBuilder();
			int line = 0;

			foreach (var range in ranges)
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
							asm = $"{address:X6}  { memory[address]:X2}";
							address += 1;
						}
						else if (memType[address] == MemType.Word)
						{
							asm = $"{address:X6}  { memory[address] * 256 + memory[address + 1]:X4}";
							address += 2;
						}
						else if (memType[address] == MemType.Long)
						{
							asm = $"{address:X6}  { memory[address] * 256 * 256 * 256 + memory[address + 1] * 256 * 256 + memory[address + 2] * 256 + memory[address + 3]:X8}";
							address += 4;
						}
						else if (memType[address] == MemType.Str)
						{
							var str = new List<string>();

							while (memType[address] == MemType.Str)
							{
								if (memory[address] == 0)
								{
									str.Add("00");
									address++;
									break;
								}
								else if (memory[address] == 0xD)
								{
									str.Add("CR");
								}
								else if (memory[address] == 0xA)
								{ 
									str.Add("LF");
								}
								else
								{
									tmp.Clear();
									tmp.Append('"');
									while (memory[address] != 0 && memory[address] != 0x0d && memory[address] != 0xa)
									{
										tmp.Append(Convert.ToChar(memory[address]));
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
						var dasm = disassembler.Disassemble(address, memorySpan.Slice((int)address, Math.Min(12, (int)(0x1000000 - address))));
						asm = dasm.ToString(options);
						address += (uint)dasm.Bytes.Length;
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
			var dasm = disassembler.Disassemble(pc, new ReadOnlySpan<byte>(memory).Slice((int)pc, Math.Min(12, (int)(0x1000000 - pc))));
			return dasm.ToString();
		}
	}
}