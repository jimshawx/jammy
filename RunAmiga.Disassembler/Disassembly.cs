using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Options;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Disassembler
{
	public class Disassembly : IDisassembly
	{
		private readonly IBreakpointCollection breakpoints;
		private readonly Disassembler disassembler;
		private readonly ILogger logger;
		private readonly IAnalysis analysis;
		private readonly IDebugMemoryMapper memory;

		public Disassembly(IDebugMemoryMapper memory, IBreakpointCollection breakpoints,
			ILogger<Disassembly> logger, IAnalysis analysis)
		{
			this.logger = logger;
			this.analysis = analysis;
			this.memory = memory;
			this.breakpoints = breakpoints;
			disassembler = new Disassembler();
		}
		private readonly Dictionary<uint, int> addressToLine = new Dictionary<uint, int>();
		private readonly Dictionary<int, uint> lineToAddress = new Dictionary<int, uint>();

		public string DisassembleTxt(List<Tuple<uint, uint>> ranges, List<uint> restartsList, DisassemblyOptions options)
		{
			addressToLine.Clear();
			lineToAddress.Clear();

			var memType = analysis.GetMemTypes();
			var comments = analysis.GetComments();
			var headers = analysis.GetHeaders();

			var restarts = (IEnumerable<uint>)restartsList.OrderBy(x => x).ToList();

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
							asm = $"{address:X6}  { memory.UnsafeRead8(address):X2}";
							address += 1;
						}
						else if (memType[address] == MemType.Word)
						{
							asm = $"{address:X6}  { memory.UnsafeRead16(address):X4}";
							address += 2;
						}
						else if (memType[address] == MemType.Long)
						{
							asm = $"{address:X6}  { memory.UnsafeRead32(address):X8}";
							address += 4;
						}
						else if (memType[address] == MemType.Str)
						{
							var str = new List<string>();

							while (memType[address] == MemType.Str)
							{
								if (memory.UnsafeRead8(address) == 0)
								{
									str.Add("00");
									address++;
									break;
								}
								else if (memory.UnsafeRead8(address) == 0xD)
								{
									str.Add("CR");
								}
								else if (memory.UnsafeRead8(address) == 0xA)
								{ 
									str.Add("LF");
								}
								else
								{
									tmp.Clear();
									tmp.Append('"');
									while (memory.UnsafeRead8(address) != 0 && memory.UnsafeRead8(address) != 0x0d && memory.UnsafeRead8(address) != 0xa)
									{
										tmp.Append(Convert.ToChar(memory.UnsafeRead8(address)));
										address++;
									}
									tmp.Append('"');

									address--;
									str.Add(tmp.ToString());
								}

								address++;
							}
							
							asm = $"{lineAddress:X6}  {string.Join(',', str)}";
						}
					}
					else
					{
						if ((address & 1) != 0)
						{
							asm = $"{address:X6}  { memory.UnsafeRead8(address):X2}";
							address += 1;
						}
						else
						{
							var dasm = disassembler.Disassemble(address, memory.GetEnumerable((int)address, 20));
							asm = dasm.ToString(options);

							uint start = address, end = (uint)(address + dasm.Bytes.Length);
							for (uint i = start; i < end && i < memory.Length; i++)
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
	}
}
