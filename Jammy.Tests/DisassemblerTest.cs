using Jammy.Core.Types.Types;
using Jammy.Disassembler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
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
			var file = File.ReadAllBytes("mathieeesingtrans.library");

			var hunks = hunkProcessor.RetrieveHunks(file);

			var codeHunk = hunks.FirstOrDefault(x => x.HunkType == HUNK.HUNK_CODE);
			ClassicAssert.IsNotNull(codeHunk);

			uint codeStart = 0;

			if (codeHunk.Content.Length >= 2)
			{
				if (codeHunk.Content[0] == 0x4e && codeHunk.Content[1] == 0x75)
				{
					logger.LogTrace("Starts with RTS");
					codeStart += 2;
				}
			}

			var romTag = romTagProcessor.ExtractRomTag(codeHunk.Content[(int)codeStart..]);
			if (romTag != null)
			{
				codeStart += RomTagProcessor.RomTagSize;
				logger.LogTrace(romTag.NameString);
				logger.LogTrace(romTag.IdString);
			}

			var codeBytes = codeHunk.Content[(int)codeStart..];
			var sb = new StringBuilder();
			uint pc = 0;
			for (; ; )
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
							sb.AppendLine($"{pc:X6} dd {vec:X8}");
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
							sb.AppendLine($"{pc:X6} dw {vec:X4}");
							codeBytes = codeBytes[2..];
							pc += 2;
						} while (pc < romTag.InitStruc.Vectors.End);
					}
					continue;
				}

				if (!romTag.InitStruc.Struct.IsEmpty() && pc == romTag.InitStruc.Struct.Start)
				{
					sb.AppendLine("; init struct");
					do
					{
						sb.AppendLine($"{pc:X6} db {codeBytes[0]:X2}");
						codeBytes = codeBytes[1..];
						pc++;
					} while (pc < romTag.InitStruc.Struct.End);
					continue;
				}

				if (!romTag.InitStruc.LibInit.IsEmpty() && pc == romTag.InitStruc.LibInit.Start)
				{
					sb.AppendLine("; lib init");
					do
					{
						uint b0 = codeBytes[0];
						uint b1 = codeBytes[1];
						uint b2 = codeBytes[2];
						uint b3 = codeBytes[3];
						uint vec = (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
						sb.AppendLine($"{pc:X6} dd {vec:X8}");
						codeBytes = codeBytes[4..];
						pc += 4;
					} while (pc < romTag.InitStruc.LibInit.End);
					continue;
				}
				
				var dis = disassembler.Disassemble(pc, codeBytes);
				sb.AppendLine(dis.ToString());
				if (dis.Bytes.Length >= codeBytes.Length) break;
				if (dis.Bytes.Length >= 2 &&
					((dis.Bytes[0] == 0x4E && dis.Bytes[1] == 0x75) ||//RTS
					(dis.Bytes[0] == 0x60) ||//BRA
					(dis.Bytes[0] == 0x4E && (dis.Bytes[1]&0xC0)==0xC0)))//JMP
					sb.AppendLine();
				codeBytes = codeBytes[dis.Bytes.Length..];
				pc += (uint)dis.Bytes.Length;
			}
			logger.LogTrace("\r\n"+sb.ToString());
		}
	}
}