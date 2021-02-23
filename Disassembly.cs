using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
		private Dictionary<uint, Comment> comments = new Dictionary<uint, Comment>();

		public Disassembly(byte[] memory, BreakpointCollection breakpoints)
		{
			this.memory = memory;
			this.breakpoints = breakpoints;
			disassembler = new Disassembler();
			labeller = new Labeller();

			LoadComments();
		}

		private void LoadComments()
		{
			LoadComment("trackdisk.device_disassembly.txt");
			LoadComment("exec_disassembly.txt");
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
			using (var f = File.OpenText(Path.Combine("c:/source/programming/amiga/", filename)))
			{
				if (f == null)
				{
					Logger.WriteLine($"Can't find {filename} comments file");
					return;
				}

				for (; ; )
				{
					string line = f.ReadLine();
					if (line == null) break;

					//xxxxxx..instruction txt..........(column 40)comment
					if (line.Length > 38)
					{
						var addressTxt = line.Substring(0, 6);
						if (Regex.IsMatch(addressTxt, "[a-fA-F0-9]{6}"))
						{
							uint address = UInt32.Parse(addressTxt, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

							var splits = line.Split(" ", 4, StringSplitOptions.RemoveEmptyEntries);
							if (splits.Length == 4)
							{
								string comment = splits[3];
								if (comments.ContainsKey(address))
									Logger.WriteLine($"[COMMENT] dupe {address:X6} \"{comment}\"");
								else
									comments.Add(address, new Comment { Address = address, Text = comment });
							}
						}
					}
				}
			}
		}


		public void Disassemble(uint address)
		{
			var memorySpan = new ReadOnlySpan<byte>(memory);

			using (var file = File.OpenWrite("kick12.rom.asm"))
			{
				using (var txtFile = new StreamWriter(file, Encoding.UTF8))
				{
					while (address < 0x1000000)
					{
						var dasm = disassembler.Disassemble(address, memorySpan.Slice((int)address, Math.Min(12, (int)(0x1000000 - address))));
						//Logger.WriteLine(dasm);
						txtFile.WriteLine(dasm);

						address += (uint)dasm.Bytes.Length;
					}
				}
			}
		}

		private Dictionary<uint, int> addressToLine = new Dictionary<uint, int>();
		private Dictionary<int, uint> lineToAddress = new Dictionary<int, uint>();

		public string DisassembleTxt(List<Tuple<uint, uint>> ranges, List<uint> restartsList, DisassemblyOptions options)
		{
			addressToLine.Clear();
			lineToAddress.Clear();

			var restarts = (IEnumerable<uint>)restartsList.OrderBy(x => x).ToList();

			var memorySpan = new ReadOnlySpan<byte>(memory);
			var txt = new StringBuilder();

			int line = 0;

			foreach (var range in ranges)
			{
				uint address = range.Item1;
				uint size = range.Item2;
				uint addressEnd = address + size;
				while (address < addressEnd)
				{
					if (restarts.Any())
					{
						if (address == restarts.First())
						{
							restarts = restarts.Skip(1);
						}
						else if (address > restarts.First())
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
					addressToLine.Add(address, line);
					lineToAddress.Add(line, address);
					line++;
					if (options.IncludeBreakpoints)
						txt.Append(breakpoints.IsBreakpoint(address) ? '*' : ' ');
					var dasm = disassembler.Disassemble(address, memorySpan.Slice((int)address, Math.Min(12, (int)(0x1000000 - address))));

					if (options.IncludeComments)
					{
						string asm = dasm.ToString(options);
						if (comments.TryGetValue(address, out Comment comment))
						{
							if (asm.Length < 64)
								txt.Append($"{asm.PadRight(64)} {comment.Text}\n");
							else
								txt.Append($" {comment.Text}\n");
						}
						else
						{
							txt.Append($"{asm}\n");
						}
					}
					else
					{
						txt.Append($"{dasm.ToString(options)}\n");
					}

					address += (uint)dasm.Bytes.Length;
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