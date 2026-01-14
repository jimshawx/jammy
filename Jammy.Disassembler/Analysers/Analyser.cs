using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Jammy.Interface;
using Jammy.Types;
using Jammy.Types.Kickstart;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

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
		private readonly IDisassembler disassembler;
		private readonly IEADatabase eaDatabase;
		private readonly EmulationSettings settings;

		public Analyser(IKickstartAnalysis kickstartAnalysis, ILabeller labeller,
			IDebugMemoryMapper mem, IOptions<EmulationSettings> settings,
			IDisassembler disassembler, IEADatabase eaDatabase,
			ILogger<Analyser> logger, IKickstartROM kickstartROM, IAnalysis analysis, IDiskAnalysis diskAnalysis)
		{
			this.kickstartAnalysis = kickstartAnalysis;
			this.labeller = labeller;
			this.logger = logger;
			this.kickstartROM = kickstartROM;
			this.analysis = analysis;
			this.settings = settings.Value;
			this.mem = mem;
			this.disassembler = disassembler;
			this.eaDatabase = eaDatabase;

			diskAnalysis.Extract();
			
			LoadLVOs();
			LoadLVO2_1();
			StartUp();
			Analysis();
			ROMTags();
			Labeller();
			//NoNL();
			DeDupe();
			LoadComments();

			kickstartAnalysis.ShowRomTags();

			//analysis.DeleteAnalysis();
			analysis.SaveAnalysis();
			//analysis.ResetAnalysis();
			//analysis.LoadAnalysis();
		}

		public void UpdateAnalysis()
		{
			ROMTags();
			var ranges = mem.GetBulkRanges();
			foreach (var range in mem.GetBulkRanges().Select(x=>new MemoryRange(x.Start, x.Length)))
				Analysis(range);
			DeDupe();
		}

		public void ClearSomeAnalysis()
		{
			analysis.ClearSomeAnalysis();
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

		private LVOType GetLVOType(string currentLib)
		{
			if (currentLib.EndsWith(".library")) return LVOType.Library;
			if (currentLib.EndsWith(".resource")) return LVOType.Resource;
			if (currentLib.EndsWith(".device")) return LVOType.Device;
			return LVOType.Empty;
		}

		private void LoadLVOs()
		{
			try
			{
				string filename = "LVOs.i.txt";
				using (var f = File.OpenText(filename))
				{
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
							var lvoType = GetLVOType(currentLib);
							analysis.SetLVO(currentLib, new LVOCollection(lvoType));
						}
						else
						{
							var bits = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
							analysis.AddLVO(currentLib, new LVO
							{
								Name = bits[0].Substring(4),//strip off _LVO
								Offset = int.Parse(bits[2])
							});
						}
					}
				}
			}
			catch
			{
				logger.LogTrace($"Can't parse the LVOs file");
			}
		}

		private void LoadLVO2_1()
		{
			try
			{
				string filename = "LVO_2.1.txt";
				using (var f = File.OpenText(filename))
				{
					string currentLib = string.Empty;
					for (; ; )
					{
						string line = f.ReadLine();
						if (line == null) break;
						if (line.Length < 4) continue;

						if (int.TryParse(line.Substring(0,4), out _))
						{
							var lvo = line.TrimStart().Split(' ')[3];
							var fn = lvo.Split('(');

							var parmNames = new List<string>();
							var parmRegs = new List<string>();
							string fnName = fn[0];
							if (fn.Length >= 2) parmNames.AddRange(fn[1].TrimEnd(')').Split(',', StringSplitOptions.RemoveEmptyEntries));
							if (fn.Length >= 3) parmRegs.AddRange(fn[2].TrimEnd(')').Split([',', '/'], StringSplitOptions.RemoveEmptyEntries));

							if (parmNames.Any() || parmRegs.Any())
								analysis.AugmentLVO(fnName, parmNames, parmRegs);
						}
					}
				}
			}
			catch
			{
				logger.LogTrace($"Can't parse the LVO2.1 file");
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
			if (!kickstartROM.IsPresent())
			{
				FindROMTags();
				return;
			}
			ExtractROMTags();
			ExtractExecBase();
		}

		private class ExecLocation
		{
			public string Version { get; }
			public string Kickstart { get; }
			public string System { get; }
			public uint Checksum { get; }
			public uint Address { get; }
			public uint CRC { get; }

			public ExecLocation(string version, string kickstart, string system, uint checksum, uint crc, uint address)
			{
				Version = version;
				Kickstart = kickstart;
				System = system;
				Checksum = checksum;
				CRC = crc;
				Address = address;
			}
		}

		private ExecLocation[] execLocations = {
			new ExecLocation("1.2", "1.0", "",0x00000001, 0x00000001, 0x0),
			new ExecLocation("31.34", "1.1", "", 0x00000002, 0x00000002, 0x0),
			new ExecLocation("33.166", "1.2", "", 0x00000003,0x00000003,0x0),
			new ExecLocation("33.180", "1.2", "", 0x00000004,0x00000004,0x0),
			new ExecLocation("33.192", "1.2", "A500/A1000/A2000", 0x56F2E2A6,0x56F2E2A6,0xFC1A40),
			new ExecLocation("34.2", "1.3", "A3000", 0x150B7DB3,0x150B7DB3,0xFC1A7C),
			new ExecLocation("34.2","1.3", "A500", 0x15267DB3,0x15267DB3,0xFC1A7C),
			new ExecLocation("36.1000","2.0","A3000", 0x953958D2, 0x953958D2,0xF82034),
			new ExecLocation("37.132","2.04","A500+", 0x000B927C,0x000B927C,0xF81F84),
			new ExecLocation("37.151","2.05","A600", 0xDB27680D,0xDB27680D,0xF81FB0),
			new ExecLocation("37.132","2.04","A3000", 0x54876DAB,0x54876DAB,0xF82000),
			new ExecLocation("40.9", "3.1", "A3000", 0x97DC36A2,0x97DC36A2,0xF823CC),
			new ExecLocation("40.9", "3.1", "A4000", 0xF90A56C0,0xF90A56C0,0xF823B4),
			new ExecLocation("40.9", "3.1", "CD32", 0x8f4549a5,0x8f4549a5,0xF823B8),
			new ExecLocation("40.10", "3.1", "A500/A600/A2000", 0x9FDEEEF6,0x9FDEEEF6,0xF8236C),
			new ExecLocation("40.10", "3.1", "A1200", 0x87BA7A3E,0x87BA7A3E,0xF8236C),
			new ExecLocation("40.10", "3.1", "A3000", 0x0CC4ABE0,0x0CC4ABE0,0xF8238C),
			new ExecLocation("40.10", "3.1", "A4000", 0x45C3145E,0x45C3145E,0xF82374),
			new ExecLocation("40.10", "3.1", "A4000", 0xE20F9194,0xE20F9194,0xF82374),
		};

		private void ExtractExecBase()
		{
			var version = kickstartAnalysis.GetVersion();
			uint checksum = kickstartAnalysis.GetChecksum();
			uint crc32 = kickstartAnalysis.GetCRC();
			byte[] sha1 = kickstartAnalysis.GetSHA1();

			logger.LogTrace($"Kickstart {version.Major}.{version.Minor} Checksum {checksum:X8} CRC32 {crc32:X8} SHA1 {Convert.ToHexString(sha1)}");

			var execLoc = execLocations.SingleOrDefault(x => x.Checksum == checksum);
			if (execLoc != null)
				ExtractFunctionTable(execLoc.Address, NT_Type.NT_LIBRARY, "exec.library", Size.Word);
			else
				logger.LogTrace($"Did not find Execbase Function Table for {version.Major}.{version.Minor}");
		}

		private void FindROMTags()
		{
			var sb = new StringBuilder();
			var romtags = new List<Resident>();
			foreach (var range in mem.GetBulkRanges())
			{
				uint i = 0;
				while (i < range.Length)
				{
					if (mem.UnsafeRead16(i) == KickstartAnalysis.RTC_MATCHWORD)
					{
						var r = new Resident();
						romtags.Add(r);

						r.MatchWord = mem.UnsafeRead16(i);
						r.MatchTag = mem.UnsafeRead32(i+2);
						r.EndSkip = mem.UnsafeRead32(i+6);
						r.Flags = (RTF)mem.UnsafeRead8(i+10);
						r.Version = mem.UnsafeRead8(i+11);
						r.Type = (NT_Type)mem.UnsafeRead8(i + 12);
						r.Pri = (sbyte)mem.UnsafeRead8(i + 13);
						r.NamePtr = mem.UnsafeRead32(i + 14);
						r.IdStringPtr = mem.UnsafeRead32(i + 18);
						r.Init = mem.UnsafeRead32(i + 22);

						char c;
						uint np;

						sb.Clear();
						np = r.NamePtr;
						while ((c = (char)mem.UnsafeRead8(np++))!=0)
							sb.Append(c);
						r.Name = sb.ToString();

						sb.Clear();
						np = r.IdStringPtr;
						while ((c = (char)mem.UnsafeRead8(np++)) != 0)
							sb.Append(c);
						r.IdString = sb.ToString();

						i += 26;
					}
					else
					{
						i += 2;
					}
				}
			}
			ExtractROMTags(romtags);
		}

		private void ExtractROMTags()
		{
			var romtags = kickstartAnalysis.GetRomTags();
			ExtractROMTags(romtags);
		}

		private void ExtractROMTags(List<Resident> romtags)
		{
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
					MakeMemType(address, MemType.Long, null);
					uint size = mem.UnsafeRead32(address);
					address += 4;
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
						ExtractStructureInit(structure, size, tag.Name);

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

		public void ExtractFunction(uint address, string name)
		{
			if (!string.IsNullOrWhiteSpace(name)) return;

			analysis.AddHeader(address, "");
			analysis.AddHeader(address, "---------------------------------------------------------------------------");
			analysis.AddHeader(address, $"\t{name}");
			analysis.AddHeader(address, "---------------------------------------------------------------------------");
			analysis.AddHeader(address, "");
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
					//analysis.AddHeader(u, "");
					//analysis.AddHeader(u, "---------------------------------------------------------------------------");
					//analysis.AddHeader(u, $"\t{lvo}");
					//analysis.AddHeader(u, "---------------------------------------------------------------------------");
					//analysis.AddHeader(u, "");

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
					//analysis.AddHeader(u, "");
					//analysis.AddHeader(u, "---------------------------------------------------------------------------");
					//analysis.AddHeader(u, $"\t{lvo}");
					//analysis.AddHeader(u, "---------------------------------------------------------------------------");
					//analysis.AddHeader(u, "");

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

		private class Member
		{
			public uint Offset { get;set;}
			public uint Value { get;set;}
			public Size Size { get;set;}
			public uint End { get
								{
									if (Size == Size.Byte) return Offset + 1;
									if (Size == Size.Byte) return Offset + 2; 
									return Offset+4;
								}
							}

			public Member(uint offset, uint value, Size size)
			{
				Offset = offset;
				Value = value;
				Size = size;
			}
		}

		private List<Member> Pad(List<Member> r, uint header, uint structSize)
		{
			if (!r.Any()) return r;

			r = r.OrderBy(m => m.Offset).ToList();
			
			//if the structure doesn't start at 0, add a byte and the padding below will fill in any more gaps
			if (r[0].Offset != 0)
				r.Insert(0, new Member(0, 0, Size.Byte));

			//pad the structure to the end of the allocated size
			if (r.Last().End != structSize-1)
				r.Add(new Member(structSize-1,0,Size.Byte));

			var pads = new List<Member>();

			var last = r.First();
			foreach (var v in r.Skip(1))
			{
				for (uint i = last.End; i < v.Offset; i++)
					pads.Add(new Member(i, 0, Size.Byte));

				last = v;
			}

			return r.Concat(pads).ToList();
		}

		private void CommentStructureInit(uint address, uint structSize, string libName)
		{
			uint header = address;
			byte c;
			var r = new List<Member>();
			uint offset = 0;

			while ((c = mem.UnsafeRead8(address++)) != 0x00)
			{
				int dest = (c>>6)&3;
				int size = (c>>4)&3;
				int count = (c & 15)+1;

				uint value=0;
				Size s=Size.Byte;
				uint inc=0;

				switch (size)
				{
					case 0: s = Size.Long; inc = 4; break;
					case 1: s = Size.Word; inc = 2; break;
					case 2: s = Size.Byte; inc = 1; break;
					case 3: analysis.AddHeader(header, "error!"); return;
				}

				switch (dest)
				{
					case 0: //count is how many 'value' to copy
						for (int i = 0; i < count; i++)
						{
							r.Add(new Member(offset, mem.UnsafeRead(address, s), s));
							offset += inc;
							address += inc;
						}
						break;

					case 1: //count is how many times to copy 'value'
						value = mem.UnsafeRead(address, s);
						address += inc;
						for (int i = 0; i < count; i++)
						{
							r.Add(new Member(offset, value, s));
							offset += inc;
						}
						break;

					case 2: //destination offset is next byte
						offset = mem.UnsafeRead8(address);
						address += 1;
						for (int i = 0; i < count; i++)
						{ 
							value = mem.UnsafeRead(address, s);
							r.Add(new Member(offset, value, s));
							offset += inc;
							address += inc;
						}
						break;

					case 3: //destination offset is next 24bits
						offset = mem.UnsafeRead32(address)>>8;
						address += 3;
						for (int i = 0; i < count; i++)
						{
							value = mem.UnsafeRead(address, s);
							r.Add(new Member(offset, value, s));
							offset += inc;
							address += inc;
						}

						break;
				}

				//next command byte is always on an even boundary
				if ((address &1 )!=0) address++;
			}

			analysis.AddHeader(header, ""); 
			analysis.AddHeader(header, $"        ;init struct {libName} @{header:X8}");
			r = Pad(r, header, structSize);//pad the uninitialised areas with 0
			foreach (var v in r.OrderBy(x => x.Offset))
			{
				if (v.Size == Size.Byte) analysis.AddHeader(header, $"        ;${v.Offset,-4:x} {v.Value:x2}");
				if (v.Size == Size.Word) analysis.AddHeader(header, $"        ;${v.Offset,-4:x} {v.Value:x4}");
				if (v.Size == Size.Long) analysis.AddHeader(header, $"        ;${v.Offset,-4:x} {v.Value:x8}");
			}
			analysis.AddHeader(header, "");
		}
		private readonly string[] sizes = { "L","W","B",""};
		private readonly string[] codes = { "Copy","Repeat","Offset Copy","APTR Offset Copy" };

		public void ExtractStructureInit(uint address, uint structSize, string libName)
		{
			CommentStructureInit(address, structSize, libName);

			uint header = address;
			byte c;

			while ((c = mem.UnsafeRead8(address)) != 0x00)
			{
				MakeMemType(address, MemType.Byte, null);

				int dest = (c >> 6) & 3;
				int size = (c >> 4) & 3;
				int count = (c & 15)+1;

				analysis.AddComment(address, $"{dest:X2}:{size:X2}:{count-1:X4} {codes[dest]} {sizes[size]} x {count}");
				address++;

				MemType s = MemType.Byte;
				uint inc = 0;

				switch (size)
				{
					case 0: s = MemType.Long; inc = 4; break;
					case 1: s = MemType.Word; inc = 2; break;
					case 2: s = MemType.Byte; inc = 1; break;
					case 3: analysis.AddHeader(header, "error!"); return;
				}

				switch (dest)
				{
					case 0: //count is how many 'value' to copy
						for (int i = 0; i < count; i++)
						{
							MakeMemType(address, s, null);
							address += inc;
						}
						break;

					case 1: //count is how many times to copy 'value'
						MakeMemType(address, s, null);
						address += inc;
						break;

					case 2: //destination offset is next byte
						MakeMemType(address, MemType.Byte, null);
						address += 1;
						for (int i = 0; i < count; i++)
						{
							MakeMemType(address, s, null);
							address += inc;
						}
						break;

					case 3: //destination offset is next 24bits
						MakeMemType(address, MemType.Byte, null);
						MakeMemType(address+1, MemType.Byte, null);
						MakeMemType(address+2, MemType.Byte, null);
						address += 3;
						for (int i = 0; i < count; i++)
						{
							MakeMemType(address, s, null);
							address += inc;
						}
						break;
				}

				//next command byte is always on an even boundary
				if ((address & 1) != 0)
				{
					MakeMemType(address, MemType.Byte, null);
					analysis.AddComment(address, "pad");
					address++;
				}
			}
			MakeMemType(address, MemType.Byte, null);
			analysis.AddComment(address, "end");
		}

		private string LVO(NT_Type type, string name, int idx)
		{
			if (name == null)
				return "";

			var lvos = analysis.GetLVOs();
			if (lvos.TryGetValue(name, out var lvolist))
			{
				var lvo = lvolist.LVOs.SingleOrDefault(x => x.Index == idx);
				if (lvo != null)
					return lvo.GetFnSignature();
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
			Analysis(kickstartROM.MappedRange().First());
		}

		private void Analysis(MemoryRange range)
		{
			uint i;

			i = range.Start;
			foreach (uint s in mem.AsULong(range.Start))
			{
				//rts/rte 0000
				if (s == 0x4e750000 || s == 0x4e730000)
					MakeMemType(i + 2, MemType.Word, null);
				//bra 0000
				if ((s & 0xff00ffff) == 0x60000000)
					MakeMemType(i + 2, MemType.Word, null);
				//jmp 0000
				if ((s & 0xffc0ffff) == 0x4ec00000)
					MakeMemType(i + 2, MemType.Word, null);

				i += 4;
			}

			i = range.Start;
			foreach (ushort s in mem.AsUWord(range.Start))
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

				//rtd $xxxx
				if (s == 0x4e74)
					analysis.AddHeader(i + 4, "");

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
			if (string.IsNullOrEmpty(settings.KickStartDisassembly))
				return;

			try
			{
				var dirs = Directory.GetDirectories(settings.LVODirectory);

				var fullPath = dirs.SingleOrDefault(x => x.Contains(settings.KickStartDisassembly));
				if (Directory.Exists(fullPath))
				{
					var files = Directory.GetFiles(fullPath, "*_disassembly.txt");
					foreach (var file in files)
						LoadComment(file);
				}
			}
			catch {}
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

			ReadFile(fullPath);
			
			uint currentAddress = 0;
			var hdrs = new List<string> { "" };

			for (; ; )
			{
				string line = ReadLine();
				if (line == null) break;
				if (line == "^Z") break;//EOF

				var split = line.SplitSmart(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

				if (string.IsNullOrWhiteSpace(line) || line.StartsWith('*') || line.TrimStart().StartsWith(';'))
				{
					//whitespace, lines starting with * or ; are all headers.
					hdrs.Add(line);
				}
				else if (line.StartsWith("----------"))
				{
					//todo: lines like this are usually labels - should add them to the labeller
					//----------
					//		label
					//----------
					string label = PeekLine(0);
					string next	= PeekLine(1);
					if (next.StartsWith("----------"))
					{
						//if the label is all empty, just ignore the whole thing
						if (!string.IsNullOrWhiteSpace(label))
							labeller.AddLabel(currentAddress, label.Trim());
						ConsumeLine();
						ConsumeLine();
					}
					else
					{  
						hdrs.Add(line);
					}
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

		private List<string> fileLines = new List<string>();
		private void ReadFile(string fullPath)
		{
			fileLines.Clear();
			using (var f = File.OpenText(fullPath))
			{
				string line;
				do
				{
					line = f.ReadLine();
					fileLines.Add(line);
				} while (line != null && line != "^Z");
			}
		}
		
		private string ReadLine()
		{
			var line = fileLines.FirstOrDefault();
			if (fileLines.Count > 0) fileLines.RemoveAt(0);
			return line;
		}

		private string PeekLine(int offset)
		{
			if (fileLines.Count > offset)
				return fileLines[offset];
			return string.Empty;
		}

		private void ConsumeLine()
		{
			ReadLine();
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
				//todo: this is expensive!
				if (!mem.MappedRange().Any(x=>x.Contains(address)))
					return 0;

				var asm = disassembler.Disassemble(address, mem.GetEnumerable(address, Disassembler.LONGEST_X86_INSTRUCTION));
				for (uint i = address; i < address + asm.Bytes.Length; i++)
					analysis.SetMemType(i, type);
			}
			return 0;
		}

		private readonly HashSet<string> analysed = new HashSet<string>();
		public void AnalyseLibraryBase(string library, uint baseAddress)
		{
			//OpenLibrary just returned this address, so there should be a bunch of
			//jmp instructions just before there pointing at the library functions

			//avoid doing it twice
			if (analysed.Contains(library)) return;

			//0100 111011 111 001 (imm32)
			//4EF9 #imm32

			var lvos = analysis.GetLVOs(library);
			bool found = false;
			foreach (var lvo in lvos.LVOs)
			{ 
				uint address = (uint)(baseAddress + lvo.Offset);

				//name the jump table entry
				eaDatabase.Add(address, lvo.Name/*+"()"*/);

				if (mem.UnsafeRead16(address) != 0x4ef9) break;
				uint lvoaddress = mem.UnsafeRead32(address+2);

				//name the actual function
				eaDatabase.Add(lvoaddress, lvo.Name/* + "()"*/);
				labeller.AddLabel(lvoaddress, lvo.Name);

				analysis.AddComment(address, lvo.Name);
				//ExtractFunction(lvoaddress, $"{lvo.Name}()");

				found = true;
			}
			if (found)
				analysed.Add(library);
		}

		public void GenerateDisassemblies()
		{
			kickstartAnalysis.GenerateDisassemblies();
		}
	}
}