using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Options;
using RunAmiga.Core.Types.Types;
using RunAmiga.Core.Types.Types.Kickstart;

namespace RunAmiga.Disassembler
{
	public class Disassembly : IDisassembly
	{
		private readonly IBreakpointCollection breakpoints;
		private readonly IOptions<EmulationSettings> settings;
		private readonly Disassembler disassembler;
		private readonly ILogger logger;
		private readonly IKickstartAnalysis kickstartAnalysis;
		private readonly IAnalyser analyser;
		private ByteArrayWrapper mem;

		public Disassembly(IMemory memory, IBreakpointCollection breakpoints, IOptions<EmulationSettings> settings,
			ILogger<Disassembly> logger, IKickstartAnalysis kickstartAnalysis, IAnalyser analyser)
		{
			this.logger = logger;
			this.kickstartAnalysis = kickstartAnalysis;
			this.analyser = analyser;
			mem = new ByteArrayWrapper(memory.GetMemoryArray());
			this.breakpoints = breakpoints;
			this.settings = settings;
			disassembler = new Disassembler();
		}
		private readonly Dictionary<uint, int> addressToLine = new Dictionary<uint, int>();
		private readonly Dictionary<int, uint> lineToAddress = new Dictionary<int, uint>();

		public string DisassembleTxt(List<Tuple<uint, uint>> ranges, List<uint> restartsList, DisassemblyOptions options)
		{
			addressToLine.Clear();
			lineToAddress.Clear();

			var memType = analyser.GetMemTypes();
			var comments = analyser.GetComments();
			var headers = analyser.GetHeaders();

			var restarts = (IEnumerable<uint>)restartsList.OrderBy(x => x).ToList();

			var memorySpan = mem.GetSpan();
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
							asm = $"{address:X6}  { mem.ReadByte(address):X2}";
							address += 1;
						}
						else if (memType[address] == MemType.Word)
						{
							asm = $"{address:X6}  { mem.ReadWord(address):X4}";
							address += 2;
						}
						else if (memType[address] == MemType.Long)
						{
							asm = $"{address:X6}  { mem.ReadLong(address):X8}";
							address += 4;
						}
						else if (memType[address] == MemType.Str)
						{
							var str = new List<string>();

							while (memType[address] == MemType.Str)
							{
								if (mem.ReadByte(address) == 0)
								{
									str.Add("00");
									address++;
									break;
								}
								else if (mem.ReadByte(address) == 0xD)
								{
									str.Add("CR");
								}
								else if (mem.ReadByte(address) == 0xA)
								{ 
									str.Add("LF");
								}
								else
								{
									tmp.Clear();
									tmp.Append('"');
									while (mem.ReadByte(address) != 0 && mem.ReadByte(address) != 0x0d && mem.ReadByte(address) != 0xa)
									{
										tmp.Append(Convert.ToChar(mem.ReadByte(address)));
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
							asm = $"{address:X6}  { mem.ReadByte(address):X2}";
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
			var dasm = disassembler.Disassemble(pc, mem.GetSpan().Slice((int)pc, Math.Min(12, (int)(0x1000000 - pc))));
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
