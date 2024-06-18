using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Jammy.Interface;
using Jammy.Types;
using Jammy.Types.Debugger;
using Jammy.Types.Options;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Disassembler
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

			Clear();
		}

		public void Clear()
		{
			globalAddressToLine.Clear();
			globalLineToAddress.Clear();
		}

		public class AddressEntry
		{
			public int Line { get; set; }
			public uint Address { get; set; }
			public List<string> Lines { get; } = new List<string>();
		}

		private readonly Dictionary<uint, AddressEntry> globalAddressToLine = new Dictionary<uint, AddressEntry>();
		private readonly Dictionary<int, uint> globalLineToAddress = new Dictionary<int, uint>();

		private List<AddressRange> NoOverlaps(List<AddressRange> ranges)
		{
			var merge = new List<AddressRange>();

			foreach (var incoming in ranges)
			{
				//remove any existing ranges completely contained in the incoming
				merge.RemoveAll(x=>incoming.Contains(x));

				//incoming doesn't overlap anything, add it, and we're done
				if (!merge.Any(x => x.Overlaps(incoming))) { merge.Add(incoming); continue; }

				//incoming is contained entirely within another, ignore it
				if (merge.Any(x=>x.Contains(incoming))) continue;

				//it partly overlaps one or more existing ranges
				foreach (var merged in merge)
				{
					if (incoming.Overlaps(merged))
					{
						//that means incoming contains the start or the end of merged

						//it contains the start, so extend it to the end
						if (merged.Contains(incoming.Start))
							merged.End = incoming.End;
						else
							merged.Start= incoming.Start;
					}
				}
			}
			return merge;
		}

		public string DisassembleTxt(List<AddressRange > ranges, DisassemblyOptions options)
		{
			var lines = new List<string>();

			ranges = NoOverlaps(ranges);

			foreach (var range in ranges.OrderBy(x=>x.Start))
			{
				uint address = range.Start;
				uint size = (uint)range.Length;
				lines.AddRange(DisassembleBlock(options, address, size).SelectMany(x=>x.Lines));
			}
			return string.Join('\n', lines);
		}

		private char SafeToChar(byte b)
		{
			if (b < 32 || b > 127) return '.';
			return (char)b;
		}

		private IEnumerable<AddressEntry> DisassembleBlock(DisassemblyOptions options, uint address, uint size)
		{
			logger.LogTrace($"Disassembling Block {address:X8} {size}");
			int line = 0;

			var tmp = new StringBuilder();
			var memType = analysis.GetMemTypes();
			var comments = analysis.GetComments();
			var headers = analysis.GetHeaders();

			uint startAddress = address;
			uint endAddress = address + size;

			Dictionary<uint, AddressEntry> addressToLine = new Dictionary<uint, AddressEntry>();
			Dictionary<int, uint> lineToAddress = new Dictionary<int, uint>();

			while (address < endAddress)
			{
				while (memType[address] != MemType.Byte && memType[address] != MemType.Str && (address & 1) != 0)
					address++;

				var ade = new AddressEntry();
				ade.Address = address;
				addressToLine.Add(address, ade);

				if (options.IncludeComments && headers.TryGetValue(address, out Header hdrs))
				{
					foreach (var hdr in hdrs.TextLines)
					{
						ade.Lines.Add(hdr);
						line++;
					}
				}

				ade.Line = line;

				lineToAddress.Add(line, address);

				uint lineAddress = address;

				string asm;

				if (options.IncludeBreakpoints)
					asm = breakpoints.IsBreakpoint(address) ? "*" : " ";
				else
					asm = "";

				if (memType[address] != MemType.Code && memType[address] != MemType.Unknown)
				{
					if (memType[address] == MemType.Byte)
					{
						if (options.Full32BitAddress)
							asm = $"{address:X8}  {memory.UnsafeRead8(address):X2}";
						else
							asm = $"{address:X6}  {memory.UnsafeRead8(address):X2}";
						address += 1;
					}
					else if (memType[address] == MemType.Word)
					{
						if (options.Full32BitAddress)
							asm = $"{address:X8}  {memory.UnsafeRead16(address):X4}";
						else
							asm = $"{address:X6}  {memory.UnsafeRead16(address):X4}";

						address += 2;
					}
					else if (memType[address] == MemType.Long)
					{
						if (options.Full32BitAddress)
							asm = $"{address:X8}  {memory.UnsafeRead32(address):X8}";
						else
							asm = $"{address:X6}  {memory.UnsafeRead32(address):X8}";
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
									tmp.Append(SafeToChar(memory.UnsafeRead8(address)));
									address++;
								}

								tmp.Append('"');

								address--;
								str.Add(tmp.ToString());
							}

							address++;
						}

						if (options.Full32BitAddress)
							asm = $"{lineAddress:X8}  {string.Join(',', str)}";
						else
							asm = $"{lineAddress:X6}  {string.Join(',', str)}";
					}
				}
				else
				{
					if ((address & 1) != 0)
					{
						if (options.Full32BitAddress)
							asm = $"{address:X8}  {memory.UnsafeRead8(address):X2}";
						else
							asm = $"{address:X6}  {memory.UnsafeRead8(address):X2}";
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
					}
				}

				if (options.IncludeComments)
				{
					if (comments.TryGetValue(lineAddress, out Comment comment))
					{
						if (asm.Length < 64)
							ade.Lines.Add($"{asm.PadRight(64)} {comment.Text}");
						else
							ade.Lines.Add($" {comment.Text}");
					}
					else
					{
						ade.Lines.Add(asm);
					}
				}
				else
				{
					ade.Lines.Add(asm);
				}

				line++;
			}

			endAddress = address;

			MergeBlock(addressToLine, lineToAddress, startAddress, endAddress);

			return addressToLine.Select(x=>x.Value);
		}

		private void MergeBlock(Dictionary<uint, AddressEntry> addressToLine, Dictionary<int, uint> lineToAddress, uint startAddress, uint endAddress)
		{
			logger.LogTrace($"Merging Block {startAddress:X8} {endAddress:X8}");
			//any lines lower than incoming start line can be left alone
			//any lines higher than that need to be incremented by the number of lines in the incoming block

			//start line of the incoming block
			int firstLine = GetAddressLine(startAddress);
			int lineCount = addressToLine.Sum(x => x.Value.Lines.Count);
			int overwritten = 0;

			//count the lines that'll be overwritten (any of those within the range of the incoming block)

			//remove any existing address mappings in the range
			{
				var removals = globalAddressToLine.Where(x => x.Key >= startAddress && x.Key < endAddress);
				foreach (var u in removals)
				{
					overwritten += globalAddressToLine[u.Key].Lines.Count;
					globalAddressToLine.Remove(u.Key);
				}
				lineCount -= overwritten;
			}

			//update remaining existing addresses pointing to lines
			{
				foreach (var v in globalAddressToLine.Values.Where(x => x.Line >= firstLine))
					v.Line += lineCount;
			}
			//update remaining existing lines pointing to addresses
			{
				var updates = globalLineToAddress.Where(x=>x.Key >= firstLine).ToList();
				foreach (var u in updates)
					globalLineToAddress.Remove(u.Key);
				foreach (var u in updates)
					globalLineToAddress.Add(u.Key + lineCount, u.Value);
			}

			//remove any existing line mappings in the range
			{
				//var removals = globalLineToAddress.Where(x => x.Key >= firstLine && x.Key < firstLine + lineCount);
				var removals = globalLineToAddress.Where(x => x.Value >= startAddress && x.Value < endAddress).ToList();
				int validateOverwritten = removals.Count;
				foreach (var u in removals)
					globalLineToAddress.Remove(u.Key);
				if (validateOverwritten != overwritten)
					Trace.WriteLine($"Overwritten mismatch {overwritten} <> {validateOverwritten}");
			}

			Check(1);

			//merge in the incoming data, it will overwrite any existing keys
			foreach (var kvp in addressToLine)
			{
				kvp.Value.Line += firstLine;
				globalAddressToLine[kvp.Key] = kvp.Value;
			}

			foreach (var kvp in lineToAddress)
				globalLineToAddress[kvp.Key + firstLine] = kvp.Value;

			Check(2);
		}

		private void Check(int chk)
		{
			//address to line -> line to address = identity
			foreach (var g in globalAddressToLine)
			{
				int line = g.Value.Line;
				uint a = GetLineAddress(line);
				if (g.Key != a)
				{
					logger.LogTrace($"Line Map Check {chk} Fail {g.Key:X8} {a:X8} {line}");
					break;
				}
			}
		}

		public int GetAddressLine(uint address)
		{
			if (globalAddressToLine.TryGetValue(address, out AddressEntry line))
				return line.Line;

			//do a quick scan above and below
			//uint inc = 1;
			//int sign = 1;
			//while (Math.Abs(inc) < 16)
			//{
			//	address += (uint)(sign * inc);
			//	if (globalAddressToLine.TryGetValue(address, out int linex))
			//		return linex;
			//	if (sign == -1)
			//		inc++;
			//	sign = -sign;
			//}

			//we want the maximum line number (plus one) which is less than address, if any
			if (globalAddressToLine.Any(x=>x.Value.Line < address))
				return globalAddressToLine.Where(x=>x.Value.Line < address)
										.Max(x => x.Value.Line) + 1;

			return 0;
		}

		public uint GetLineAddress(int line)
		{
			if (globalLineToAddress.TryGetValue(line, out uint address))
				return address;
			return 0;
		}

		public AddressEntry GetAddressEntry(uint address)
		{
			if (globalAddressToLine.TryGetValue(address, out AddressEntry line))
				return line;
			return null;
		}

		public string GetDisassembly(uint addressStart, long addressEnd)
		{
			return string.Join("\n", globalAddressToLine
				.Where(x=>x.Key >= addressStart && x.Key < addressEnd)
				.OrderBy(x=>x.Key)
				.SelectMany(x=>x.Value.Lines));
		}

		public IDisassemblyView DisassemblyView(uint address, int linesBefore, int linesAfter, DisassemblyOptions options)
		{
			address &= 0xfffffffe;

			var startLine = GetAddressLine(address);

			long addressStart = address;
			long addressEnd = address;

			if (GetLineAddress(startLine) != address)
			{
				//it's not been disassembled yet
				var lines = DisassembleBlock(options, (uint)Math.Max((long)address-100,0), 0x10000);
				addressEnd = lines.Last().Address;
			}

			//it exists exactly
			if (GetLineAddress(startLine) == address)
			{
				long searchAddress = address;
				searchAddress = address;
				while (linesBefore >= 0 && searchAddress > 0)
				{
					var v = GetAddressEntry((uint)searchAddress);
					if (v != null)
					{
						linesBefore -= v.Lines.Count;
						addressStart = searchAddress;
					}

					searchAddress -= 2;
				}

				searchAddress = address;
				while (linesAfter > 0 && searchAddress < Math.Min((long)address + 0x10000, 0x100000000))
				{
					var v = GetAddressEntry((uint)searchAddress);
					if (v != null)
					{
						linesAfter += v.Lines.Count;
						addressEnd = searchAddress;
					}

					searchAddress += 2;
				}
			}

			var rv = new DisassemblyView(this, GetAddressLine((uint)addressStart), GetDisassembly((uint)addressStart, addressEnd));
			return rv;
		}

		public IDisassemblyView FullDisassemblyView(DisassemblyOptions options)
		{
			return new DisassemblyView(this, 0, GetDisassembly(0, 0x1000000));
		}
	}

	public class DisassemblyView : IDisassemblyView
	{
		private readonly int startLine;
		private readonly IDisassembly disassembly;
		private readonly string text;

		public DisassemblyView(IDisassembly disassembly, int startLine, string asm)
		{
			this.startLine = startLine;
			this.disassembly = disassembly;

			text = asm;
		}

		//given an address, return the line in the view
		public int GetAddressLine(uint address)
		{
			return disassembly.GetAddressLine(address) - startLine;
		}

		//given a line in the view, return the address
		public uint GetLineAddress(int line)
		{
			return disassembly.GetLineAddress(line + startLine);
		}

		public string Text => text;
	}

	public class MemoryDumpView : IMemoryDumpView
	{
		private readonly IMemoryDump memoryDump;
		private readonly string mem;

		public MemoryDumpView(IMemoryDump memoryDump, string mem)
		{
			this.memoryDump = memoryDump;
			this.mem = mem;
		}

		//given an address, return the line in the view
		public int AddressToLine(uint address)
		{
			return memoryDump.AddressToLine(address);
		}

		public string Text => mem;
	}
}
