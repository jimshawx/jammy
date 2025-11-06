using Jammy.Core;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Debugger;
using Jammy.Disassembler;
using Jammy.Disassembler.Analysers;
using Jammy.Disassembler.TypeMapper;
using Jammy.Extensions.Extensions;
using Jammy.Interface;
using Jammy.Types.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
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
		private IDisassembler disassembler;
		private ServiceProvider serviceProvider;
		private ILogger logger;
		private IHunkProcessor hunkProcessor;
		private IRomTagProcessor romTagProcessor;
		private IAnalyser analyser;
		private IDisassembly disassembly;
		private IDebugMemoryMapper memory;

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

				//just for the full disassembler
				.AddSingleton<IDisassembly, Disassembly>()
				.AddSingleton<IDisassembler, Disassembler.Disassembler>()
				.AddSingleton<IEADatabase, EADatabase>()
				.AddSingleton<IInstructionAnalysisDatabase, InstructionAnalysisDatabase>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<IAnalysis, Analysis>()
				.AddSingleton<IAnalyser, Analyser>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<IDiskAnalysis, DiskAnalysis>()
				.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
				.AddSingleton<IKickstartROM, KickstartROM>()
				.AddSingleton<IExtendedKickstartROM, ExtendedKickstartROM>()
				.AddSingleton<IDiskAnalysis, DiskAnalysis>()
				.AddSingleton<IObjectMapper, ObjectMapper>()
				.AddSingleton<IDMA>(x => new Mock<IDMA>().Object)
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("DisassemblerTest"))
				.AddSingleton<TestMemory>()
				.AddSingleton<ITestMemory>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryMapper>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IDebugMemoryMapper>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryMappedDevice>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryManager, MemoryManager>()
				.Configure<EmulationSettings>(o => configuration.GetSection("DisassemblerTest").Bind(o))

				.BuildServiceProvider();

			logger = serviceProvider.GetRequiredService<ILogger<DisassemblerTest>>();
			hunkProcessor = serviceProvider.GetRequiredService<IHunkProcessor>();
			romTagProcessor = serviceProvider.GetRequiredService<IRomTagProcessor>();

			memory = serviceProvider.GetRequiredService<IDebugMemoryMapper>();
			analyser = serviceProvider.GetRequiredService<IAnalyser>();
			disassembly = serviceProvider.GetRequiredService<IDisassembly>();

			disassembler = serviceProvider.GetRequiredService<IDisassembler>();
		}

		[Test]
		public void TestDis()
		{
			const string libName = "mpega060FPU.library";

			int librarySize = LoadLibrary(0x10000, libName);

			var dis = disassembly.DisassembleTxt(new List<AddressRange> { new AddressRange(0x10000, (ulong)librarySize) }, new DisassemblyOptions { IncludeComments = true });
			logger.LogTrace(Environment.NewLine + dis);

			logger.LogTrace($"loaded {libName} at {0x10000:X8}");
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

		private int LoadLibrary(uint loadAddress, string libName)
		{
			var lib = File.ReadAllBytes(libName);

			var code = hunkProcessor.RetrieveHunks(lib).First(x => x.HunkType == HUNK.HUNK_CODE);
			var libw = code.Content.AsUWord().ToArray();
			for (uint i = 0; i < libw.Length; i++)
				memory.UnsafeWrite16(loadAddress + i * 2, libw[i]);

			romTagProcessor.FindAndFixupROMTags(memory.GetBulkRanges().Single().Memory, loadAddress);
			analyser.UpdateAnalysis();

			return code.Content.Length;
		}

		private int LoadExe(uint loadAddress, string libName)
		{
			var lib = File.ReadAllBytes(libName);

			var code = hunkProcessor.RetrieveHunks(lib).First(x => x.HunkType == HUNK.HUNK_CODE);
			var libw = code.Content.AsUWord().ToArray();
			for (uint i = 0; i < libw.Length; i++)
				memory.UnsafeWrite16(loadAddress + i * 2, libw[i]);

			return code.Content.Length;
		}

		[Test]
		public void TestDisassmbler()
		{
			const string libName = "mathieeedoubbas.library";

			int librarySize = LoadLibrary(0x10000, libName);

			var dis = disassembly.DisassembleTxt(new List<AddressRange>{new AddressRange(0x10000, (ulong)librarySize)}, new DisassemblyOptions { IncludeComments = true, UpperCase = true});
			logger.LogTrace(Environment.NewLine + dis);

			logger.LogTrace($"loaded {libName} at {0x10000:X8}");
		}

		[Test]
		public void TestDisassmblerExe()
		{
			const string libName = "cputest";

			int librarySize = LoadExe(0x10000, libName);

			var dis = disassembly.DisassembleTxt(new List<AddressRange> { new AddressRange(0x10000, (ulong)librarySize) }, new DisassemblyOptions { IncludeComments = true, UpperCase = true });
			logger.LogTrace(Environment.NewLine + dis);

			logger.LogTrace($"loaded {libName} at {0x10000:X8}");
		}
	}
}