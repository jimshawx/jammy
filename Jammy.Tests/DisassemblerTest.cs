using Jammy.Core.Types.Types;
using Jammy.Disassembler;
using Jammy.Extensions.Extensions;
using Jammy.Types.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Jammy.Tests
{
	[TestFixture]
	public class DisassemblerTest
	{
		private Disassembler.Disassembler disassembler;
		private ServiceProvider serviceProvider;
		private ILogger logger;
		private IHunkProcessor hunkProcessor;
		private IRomTagProcessor romTagProcessor;

		[OneTimeSetUp]
		public void DisassemblerTestInit()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.Build();

			serviceProvider = new ServiceCollection()
				.AddLogging(x =>
							{
								x.AddConfiguration(configuration.GetSection("Logging"));
								x.AddDebug();
							})
				.AddSingleton<IHunkProcessor, HunkProcessor>()
				.AddSingleton<IRomTagProcessor, RomTagProcessor>()
				.BuildServiceProvider();

			logger = serviceProvider.GetRequiredService<ILogger<DisassemblerTest>>();
			hunkProcessor = serviceProvider.GetRequiredService<IHunkProcessor>();
			romTagProcessor = serviceProvider.GetRequiredService<IRomTagProcessor>();

			disassembler = new Disassembler.Disassembler();
		}

		[Test]
		public void TestDis()
		{
			//var file = File.ReadAllBytes("mathieeedoubtrans.library");
			var file = File.ReadAllBytes("mpega060FPU.library");

			var hunks = hunkProcessor.RetrieveHunks(file);

			var codeHunk = hunks.FirstOrDefault(x => x.HunkType == HUNK.HUNK_CODE);
			ClassicAssert.IsNotNull(codeHunk);

			bool lastWasEOB = true;
			var codeBytes = codeHunk.Content;
			var sb = new StringBuilder();
			uint pc = 0;

			if (codeHunk.Content.Length >= 2)
			{
				if (codeHunk.Content[0] == 0x4e && codeHunk.Content[1] == 0x75)
				{
					var dis = disassembler.Disassemble(pc, codeBytes);
					sb.AppendLine(dis.ToString());
					sb.AppendLine();
					codeBytes = codeBytes[2..];
					pc += 2;
				}
			}

			var romTag = romTagProcessor.ExtractRomTag(codeBytes);
			if (romTag != null)
			{
				romTag.InitStruc.Vectors.Start += pc;
				romTag.InitStruc.Struct.Start += pc;
				romTag.InitStruc.LibInit.Start += pc;

				sb.AppendLine("; rom rag");
				sb.AppendLine($"{pc:X6}  dw   {romTag.MatchWord:X4}");pc+=2;
				sb.AppendLine($"{pc:X6}  dd   {romTag.MatchTag:X8}");pc+=4;
				sb.AppendLine($"{pc:X6}  dd   {romTag.EndSkip:X8}"); pc += 4;
				sb.AppendLine($"{pc:X6}  db   {(byte)romTag.Flags:X2}");pc++;
				sb.AppendLine($"{pc:X6}  db   {romTag.Version:X2}");pc++;
				sb.AppendLine($"{pc:X6}  db   {(byte)romTag.Type:X2}"); pc++;
				sb.AppendLine($"{pc:X6}  db   {romTag.Pri:X2}"); pc++;
				sb.AppendLine($"{pc:X6}  dd   {romTag.Name} ;{romTag.NameString}"); pc += 4;
				sb.AppendLine($"{pc:X6}  dd   {romTag.Id} ;{romTag.IdString}"); pc+=4;
				sb.AppendLine($"{pc:X6}  dd   {romTag.Init:X8}");pc+=4;
				codeBytes = codeBytes[(int)RomTagProcessor.RomTagSize..];
				sb.AppendLine();
				lastWasEOB = true;

			}

			for (; ; )
			{
				if (romTag != null)
				{ 
					if (!romTag.InitStruc.Vectors.IsEmpty() && pc == romTag.InitStruc.Vectors.Start)
					{ 
						sb.AppendLine("; vectors");
						if (romTag.InitStruc.VectorSize == Size.Long)
						{
							do
							{  
								uint b0 = codeBytes[0];
								uint b1 = codeBytes[1];
								uint b2 = codeBytes[2];
								uint b3 = codeBytes[3];
								uint vec = (b0<<24)|(b1<<16)|(b2<<8)|b3;
								sb.AppendLine($"{pc:X6}  dd   {vec:X8}");
								codeBytes = codeBytes[4..];
								pc+=4;
							} while (pc < romTag.InitStruc.Vectors.End);
						}
						else
						{
							do
							{
								uint b0 = codeBytes[0];
								uint b1 = codeBytes[1];
								uint vec = (b0 << 8) | b1;
								sb.AppendLine($"{pc:X6}  dw   {vec:X4}");
								codeBytes = codeBytes[2..];
								pc += 2;
							} while (pc < romTag.InitStruc.Vectors.End);
						}
						sb.AppendLine();
						lastWasEOB = true;

						continue;
					}

					if (!romTag.InitStruc.Struct.IsEmpty() && pc == romTag.InitStruc.Struct.Start)
					{
						sb.AppendLine("; init struct");
						do
						{
							sb.AppendLine($"{pc:X6}  db   {codeBytes[0]:X2}");
							codeBytes = codeBytes[1..];
							pc++;
						} while (pc < romTag.InitStruc.Struct.End);
						sb.AppendLine();

						//todo: why is this necessary?
						if ((pc&1)!=0)
						{
							pc++;
							codeBytes = codeBytes[1..];
						}
						lastWasEOB = true;

						continue;
					}

					if (!romTag.InitStruc.LibInit.IsEmpty() && pc == romTag.InitStruc.LibInit.Start)
					{
						string[] comment = ["data size", "functions", "init struct", "init" ];
						sb.AppendLine("; lib init");
						int i = 0;
						do
						{
							uint b0 = codeBytes[0];
							uint b1 = codeBytes[1];
							uint b2 = codeBytes[2];
							uint b3 = codeBytes[3];
							uint vec = (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
							sb.AppendLine($"{pc:X6}  dd   {vec:X8}\t;{comment[i++]}");
							codeBytes = codeBytes[4..];
							pc += 4;
						} while (pc < romTag.InitStruc.LibInit.End);
						sb.AppendLine();
						lastWasEOB = true;

						continue;
					}
				
					if (pc == romTag.Id || pc == romTag.Name)
					{
						sb.Append($"{pc:X6}  dc.b \"");
						for (;;)
						{
							byte b0 = codeBytes[0];
							if (b0 == 0) {sb.Append(",00"); break; }
							if (b0 == 13) sb.Append(",CR");
							else if (b0 == 10) sb.Append(",LF");
							else if (b0 < 32 || b0 > 127) sb.Append($",{b0:X2}");
							else sb.Append((char)b0);
							codeBytes = codeBytes[1..];
							pc++;
						}
						if ((pc&1)!=0 && codeBytes[0] == 0)
						{
							sb.Append(",00");
							codeBytes = codeBytes[1..];
							pc++;
						}
						sb.AppendLine("\"");
						lastWasEOB = true;
						continue;
					}
				}

				if (codeBytes.Length >= 2 && codeBytes[0] == 0 && codeBytes[1] == 0 && lastWasEOB)
				{
					sb.AppendLine($"{pc:X6}  dw   0000");
					sb.AppendLine();
					pc += 2;
					codeBytes = codeBytes[2..];
				}
				lastWasEOB = false;

				var dis = disassembler.Disassemble(pc, codeBytes);
				sb.AppendLine(dis.ToString());
				if (dis.Bytes.Length >= codeBytes.Length) break;
				if (dis.Bytes.Length >= 2 &&
					((dis.Bytes[0] == 0x4E && dis.Bytes[1] == 0x75) ||//RTS
					(dis.Bytes[0] == 0x60) ||//BRA
					(dis.Bytes[0] == 0x4E && (dis.Bytes[1]&0xC0)==0xC0)))//JMP
				{ 
					sb.AppendLine();
					lastWasEOB = true;
				}
				codeBytes = codeBytes[dis.Bytes.Length..];
				pc += (uint)dis.Bytes.Length;
			}
			logger.LogTrace("\r\n"+sb.ToString());
		}

		//[Test]
		public void TestFMOVEM()
		{
			var sb = new StringBuilder();
			void Append(string s) { sb.Append(s); }
			for (int list = 0; list < 256; list++)
			{
				sb.Clear();

				for (int i = 0; i < 8; i++)
					if ((list & (1 << i)) != 0) Append($"fp{i}/");
				Append("\t\t\t\t");

				var ls = list << 1;
				bool dash = false;
				bool slash = false;
				for (int i = 0; i < 8; i++)
				{
					if ((ls & 3) == 0b010) { if (slash) Append("/"); Append($"fp{i}"); slash = true; }
					if ((ls & 7) == 0b111) { if (!dash) { Append("-"); dash = true; } }
					if ((ls & 6) == 0b010) { if (dash) Append($"fp{i}"); dash = false; }
					if ((ls &15) ==0b0110) { Append($"/fp{i+1}"); }
					ls >>= 1;
				}
				logger.LogTrace(sb.ToString());
			}
		}

		[Test]
		public void TestFMOVEFPCR()
		{
			var w = new List<ushort> { 0xf200, 0x9000}.ToArray();
			var dasm = disassembler.Disassemble(0,w.AsByte());
			var s = dasm.ToString(new DisassemblyOptions { IncludeBytes = false });
			//FMOVE.L        D0,FPCR
			logger.LogTrace(s);
		}

		[Test]
		public void TestFMOVE()
		{
			var w = new List<ushort> { 0xF200, 0x6400 }.ToArray();
			var dasm = disassembler.Disassemble(0, w.AsByte());
			var s = dasm.ToString(new DisassemblyOptions { IncludeBytes = false });
			//FMOVE.S        FP0,D0
			logger.LogTrace(s);
		}
		
	}
}