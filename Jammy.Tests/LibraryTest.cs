using System;
using System.IO;
using Jammy.Core;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Debugger;
using Jammy.Disassembler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Jammy.Interface;
using Jammy.Extensions.Extensions;
using System.Linq;
using Jammy.Core.Types.Types;
using System.Collections.Generic;
using Jammy.Types.Kickstart;
using NUnit.Framework.Legacy;
using Jammy.Core.CPU.Musashi.MC68030;
using Jammy.Core.CPU.Musashi.CSharp;
using Jammy.Core.CPU.Musashi.MC68020;
using System.Text;
using Jammy.Disassembler.Analysers;
using Jammy.Core.Memory;
using Jammy.Types.Options;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Tests
{
	[TestFixture]
	public class LibraryTest
	{
		private IHunkProcessor hunkProcessor;
		private IRomTagProcessor romTagProcessor;
		private ICPU cpu0;
		private ILogger logger;
		private IDebugMemoryMapper memory;
		private IAnalyser analyser;
		private IDisassembly disassembly;

		[OneTimeSetUp]
		public void LibraryTestInit()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.Build();

			var serviceProvider = new ServiceCollection()
				.AddLogging(x=>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					x.AddDebug();
					//x.AddDebugAsync();
				})
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("FPU"))
				.AddSingleton<IInterrupt, Core.Interrupt>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<ITracer, NullTracer>()
				//.AddSingleton<ICPU, Musashi68030CPU>()
				//.AddSingleton<ICPU, Musashi68EC020CPU>()
				.AddSingleton<ICPU, CPUWrapperMusashi>()
				.AddSingleton<TestMemory>()
				.AddSingleton<ITestMemory>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryMapper>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IDebugMemoryMapper>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryMappedDevice>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IHunkProcessor, HunkProcessor>()
				.AddSingleton<IRomTagProcessor, RomTagProcessor>()

				//extra for the full disassembler
				.AddSingleton<IDisassembly, Disassembly>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<IAnalysis, Analysis>()
				.AddSingleton<IAnalyser, Analyser>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<IDiskAnalysis, DiskAnalysis>()
				.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
				.AddSingleton<IKickstartROM, KickstartROM>()
				.AddSingleton<IMemoryManager, MemoryManager>()

				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation030").Bind(o))
				.BuildServiceProvider();

			ServiceProviderFactory.ServiceProvider = serviceProvider;

			logger = serviceProvider.GetRequiredService<ILogger<DisassemblerTest>>();
			hunkProcessor = serviceProvider.GetRequiredService<IHunkProcessor>();
			romTagProcessor = serviceProvider.GetRequiredService<IRomTagProcessor>();
			memory = serviceProvider.GetRequiredService<IDebugMemoryMapper>();
			disassembly = serviceProvider.GetRequiredService<IDisassembly>();
			analyser = serviceProvider.GetRequiredService<IAnalyser>();

			cpu0 = serviceProvider.GetRequiredService<ICPU>();
			cpu0.Reset();
			cpu0.Emulate();
		}

		private class Member
		{
			public uint Offset { get; set; }
			public uint Value { get; set; }
			public Size Size { get; set; }
			public uint End
			{
				get
				{
					if (Size == Size.Byte) return Offset + 1;
					if (Size == Size.Byte) return Offset + 2;
					return Offset + 4;
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
			if (r.Last().End != structSize - 1)
				r.Add(new Member(structSize - 1, 0, Size.Byte));

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

		private List<Member> CreateStructure(uint address, uint structSize, string libName)
		{
			uint header = address;
			byte c;
			var r = new List<Member>();
			uint offset = 0;

			while ((c = memory.UnsafeRead8(address++)) != 0x00)
			{
				int dest = (c >> 6) & 3;
				int size = (c >> 4) & 3;
				int count = (c & 15) + 1;

				uint value = 0;
				Size s = Size.Byte;
				uint inc = 0;

				switch (size)
				{
					case 0: s = Size.Long; inc = 4; break;
					case 1: s = Size.Word; inc = 2; break;
					case 2: s = Size.Byte; inc = 1; break;
					case 3: return new List<Member>();
				}

				switch (dest)
				{
					case 0: //count is how many 'value' to copy
						for (int i = 0; i < count; i++)
						{
							r.Add(new Member(offset, memory.UnsafeRead(address, s), s));
							offset += inc;
							address += inc;
						}
						break;

					case 1: //count is how many times to copy 'value'
						value = memory.UnsafeRead(address, s);
						address += inc;
						for (int i = 0; i < count; i++)
						{
							r.Add(new Member(offset, value, s));
							offset += inc;
						}
						break;

					case 2: //destination offset is next byte
						offset = memory.UnsafeRead8(address);
						address += 1;
						for (int i = 0; i < count; i++)
						{
							value = memory.UnsafeRead(address, s);
							r.Add(new Member(offset, value, s));
							offset += inc;
							address += inc;
						}
						break;

					case 3: //destination offset is next 24bits
						offset = memory.UnsafeRead32(address) >> 8;
						address += 3;
						for (int i = 0; i < count; i++)
						{
							value = memory.UnsafeRead(address, s);
							r.Add(new Member(offset, value, s));
							offset += inc;
							address += inc;
						}

						break;
				}

				//next command byte is always on an even boundary
				if ((address & 1) != 0) address++;
			}
			
			r = Pad(r, header, structSize);//pad the uninitialised areas with 0

			return r.OrderBy(x => x.Offset).ToList();
		}

		private uint CallLVO(uint libBase, int vec, uint sp, uint d0=0, uint d1=0, uint a0=0)
		{
			var regs = new Regs();
			regs.PC = (uint)(libBase+vec);
			regs.A[7] = regs.SP = regs.SSP = sp;
			regs.D[0] = d0;
			regs.D[1] = d1;
			regs.A[0] = a0;
			cpu0.SetRegs(regs);
			for (; ; )
			{
				regs = cpu0.GetRegs();

				//rts from stack frame, or rte
				if (memory.UnsafeRead16(regs.PC) == 0x4e75 && regs.A[7] == sp)
					break;
				if (memory.UnsafeRead16(regs.PC) == 0x4e73)
				{
					logger.LogTrace("Exception!"); break;
				}

				cpu0.Emulate();
			}
			return regs.D[0];
		}

		private uint Call(uint pc, uint sp, uint libBase, uint execBase)
		{
			var regs = new Regs();
			regs.PC = pc;
			regs.A[7] = regs.SP = regs.SSP = sp;
			regs.D[0] = libBase;
			regs.A[6] = execBase;
			cpu0.SetRegs(regs);

			for (;;)
			{
				regs = cpu0.GetRegs();

				//rts from stack frame, or rte
				if (memory.UnsafeRead16(regs.PC) == 0x4e75 && regs.A[7] == sp)
					break;
				if (memory.UnsafeRead16(regs.PC) == 0x4e73)
				{
					logger.LogTrace("Exception!"); break;
				}

				cpu0.Emulate();
			}
			return regs.D[0];
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

		private const int stackSize = 0x1000;

		private uint LoadLibrary(uint loadAddress, string libName, out uint stackPtr)
		{
			analyser.ClearSomeAnalysis();

			int librarySize = LoadLibrary(loadAddress, libName);

			if (loadAddress < 0x1002)
				logger.LogTrace("Not enough space to load interrupt vectors");

			//fill all trap vectors with rte
			for (uint i = 0; i < 0x1000; i+= 4)
				memory.UnsafeWrite32(i, 0x1000);
			memory.UnsafeWrite16(0x1000, 0x4e73);//rte

			var romTagLocations = romTagProcessor.FindAndFixupROMTags(memory.GetBulkRanges().Single().Memory, 0);

			RomTag romTag = null;

			if (romTagLocations.Any())
				romTag = romTagProcessor.ExtractRomTag(memory.GetBulkRanges().Single().Memory[(int)romTagLocations.Single()..]);

			//there's no rom tag
			if (romTag == null)
			{
				var dis = disassembly.DisassembleTxt(new List<AddressRange> { new AddressRange(loadAddress, (ulong)librarySize) }, new DisassemblyOptions { IncludeComments = true });
				logger.LogTrace(Environment.NewLine + dis);

				stackPtr = (uint)(loadAddress + librarySize + stackSize);
				return loadAddress;
			}

			uint libraryBase;
			if ((romTag.Flags & RTF.RTF_AUTOINIT) != 0)
			{
				//romTag.Rebase(loadAddress);

				//auto init

				//generate the vectors into memory
				uint libMem = loadAddress + (uint)librarySize;
				foreach (var vec in romTag.InitStruc.Vector.Reverse<uint>())
				{
					memory.UnsafeWrite16(libMem, 0x4ef9);//jmp #
					if (romTag.InitStruc.VectorSize == Size.Long)
						memory.UnsafeWrite32(libMem + 2, loadAddress + vec);
					else
						memory.UnsafeWrite32(libMem + 2, loadAddress + vec + romTag.InitStruc.Vectors.Start + 2);
					libMem += 6;
				}

				//this is ultimately the library base
				libraryBase = libMem;

				//decode the init struct into memory
				var struc = CreateStructure(romTag.InitStruc.Struct.Start, romTag.InitStruc.DataSize, libName);

				foreach (var m in struc)
				{
					memory.UnsafeWrite(libMem, m.Value, m.Size);

					if (m.Size == Size.Byte) libMem++;
					if (m.Size == Size.Word) libMem += 2;
					if (m.Size == Size.Long) libMem += 4;
				}

				//the stack starts after the init struct
				stackPtr = libMem + stackSize;

				//disassemble
				var dis = disassembly.DisassembleTxt(new List<AddressRange> { new AddressRange(loadAddress, (ulong)(libraryBase - loadAddress)) }, new DisassemblyOptions { IncludeComments = true });
				logger.LogTrace(Environment.NewLine + dis);

				//need to fake up some execBase in the space above the stack
				uint execBase = stackPtr;
				memory.UnsafeWrite32(4, execBase);
				memory.UnsafeWrite8(execBase + 0x129, 1 << 4);//show 68881 present in AttnFlags

				//all done, call the init function
				uint libInit = Call(romTag.InitStruc.InitFn, stackPtr, libraryBase, execBase);
				ClassicAssert.AreEqual(libInit, libraryBase);
			}
			else
			{
				stackPtr = (uint)(loadAddress + librarySize + stackSize);
				libraryBase = Call(romTag.Init, stackPtr, 0, 0);
			}
			return libraryBase;
		}

		[Test]
		public void TestMathIEEESingBas()
		{
			const string libName = "mathieeesingbas.library";

			uint libraryBase = LoadLibrary(0x10000, libName, out uint stackPtr);

			logger.LogTrace($"loaded {libName} at {libraryBase:X8}");

			//Open					- 6		16
			//Close					-12		15
			//Expunge				-18		14
			//Reserved				-24		13
			//_LVOIEEESPFix equ     -30		12
			//_LVOIEEESPFlt equ     -36		11
			//_LVOIEEESPCmp equ     -42		10
			//_LVOIEEESPTst equ     -48		9
			//_LVOIEEESPAbs equ     -54		8
			//_LVOIEEESPNeg equ     -60		7
			//_LVOIEEESPAdd equ     -66		6
			//_LVOIEEESPSub equ     -72		5
			//_LVOIEEESPMul equ     -78		4
			//_LVOIEEESPDiv equ     -84		3
			//_LVOIEEESPFloor equ   -90; Functions in V33 or higher (1.2)
			//_LVOIEEESPCeil equ    -96     1

			uint d0, d1;
			uint rv;

			//Fix
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -30, stackPtr, d0);
			logger.LogTrace($"Fix({d0:X8}) {rv:X8}");

			//Flt
			d0 = 17;
			rv = CallLVO(libraryBase, -36, stackPtr, d0);
			logger.LogTrace($"Flt({d0}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");
			d0 = 2147483647;
			rv = CallLVO(libraryBase, -36, stackPtr, d0);
			logger.LogTrace($"Flt({d0}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Cmp
			int cmp;
			d0 = 1;
			d1 = 2;
			cmp = (int)CallLVO(libraryBase, -42, stackPtr, d0, d1);
			logger.LogTrace($"Cmp({d0},{d1}) {cmp}");
			d0 = 1;
			d1 = d0-2;
			cmp = (int)CallLVO(libraryBase, -42, stackPtr, d0, d1);
			logger.LogTrace($"Cmp({d0},{d1}) {cmp}");
			d0 = 1;
			d1 = 1;
			cmp = (int)CallLVO(libraryBase, -42, stackPtr, d0, d1);
			logger.LogTrace($"Cmp({d0},{d1}) {cmp}");

			//Tst
			d0 = BitConverter.SingleToUInt32Bits(1.0f);
			cmp = (int)CallLVO(libraryBase, -48, stackPtr, d0);
			logger.LogTrace($"Tst({d0:X8}) {cmp}");
			d0 = BitConverter.SingleToUInt32Bits(-1.0f);
			cmp = (int)CallLVO(libraryBase, -48, stackPtr, d0);
			logger.LogTrace($"Tst({d0:X8}) {cmp}");
			d0 = BitConverter.SingleToUInt32Bits(0.0f);
			cmp = (int)CallLVO(libraryBase, -48, stackPtr, d0);
			logger.LogTrace($"Tst({d0:X8}) {cmp}");

			//Abs
			d0 = BitConverter.SingleToUInt32Bits(-1.0f);
			rv = CallLVO(libraryBase, -54, stackPtr, d0);
			logger.LogTrace($"Abs({d0:X8}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Neg
			d0 = BitConverter.SingleToUInt32Bits(1.0f);
			rv = CallLVO(libraryBase, -60, stackPtr, d0);
			logger.LogTrace($"Neg({d0:X8}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");
		
			//Add
			d0 = BitConverter.SingleToUInt32Bits(1.0f);
			d1 = BitConverter.SingleToUInt32Bits(7.0f);
			rv = CallLVO(libraryBase, -66, stackPtr, d0, d1);
			logger.LogTrace($"Add({d0:X8},{d1:X8}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Sub
			d0 = BitConverter.SingleToUInt32Bits(1.0f);
			d1 = BitConverter.SingleToUInt32Bits(7.0f);
			rv = CallLVO(libraryBase, -72, stackPtr, d0, d1);
			logger.LogTrace($"Sub({d0:X8},{d1:X8}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Mul
			d0 = BitConverter.SingleToUInt32Bits(2.0f);
			d1 = BitConverter.SingleToUInt32Bits(7.0f);
			rv = CallLVO(libraryBase, -78, stackPtr, d0, d1);
			logger.LogTrace($"Mul({d0:X8},{d1:X8}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Div
			d0 = BitConverter.SingleToUInt32Bits(1.0f);
			d1 = BitConverter.SingleToUInt32Bits(7.0f);
			rv = CallLVO(libraryBase, -84, stackPtr, d0, d1);
			logger.LogTrace($"Div({d0:X8},{d1:X8}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Floor
			d0 = BitConverter.SingleToUInt32Bits(1.9f);
			rv = CallLVO(libraryBase, -90, stackPtr, d0);
			logger.LogTrace($"Floor({d0:X8}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Ceil
			d0 = BitConverter.SingleToUInt32Bits(1.9f);
			rv = CallLVO(libraryBase, -96, stackPtr, d0);
			logger.LogTrace($"Ceil({d0:X8}) {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");
		}

		[Test]
		public void TestMathIEEESingTrans()
		{
			const string libName = "mathieeesingtrans.library";

			uint libraryBase = LoadLibrary(0x10000, libName, out uint stackPtr);

			logger.LogTrace($"loaded {libName} at {libraryBase:X8}");

			//_LVOIEEESPAtan equ     -30
			//_LVOIEEESPSin equ     -36
			//_LVOIEEESPCos equ     -42
			//_LVOIEEESPTan equ     -48
			//_LVOIEEESPSincos equ     -54
			//_LVOIEEESPSinh equ     -60
			//_LVOIEEESPCosh equ     -66
			//_LVOIEEESPTanh equ     -72
			//_LVOIEEESPExp equ     -78
			//_LVOIEEESPLog equ     -84
			//_LVOIEEESPPow equ     -90
			//_LVOIEEESPSqrt equ     -96
			//_LVOIEEESPTieee equ     -102
			//_LVOIEEESPFieee equ     -108
			//_LVOIEEESPAsin equ     -114
			//_LVOIEEESPAcos equ     -120
			//_LVOIEEESPLog10 equ     -126

			uint d0, d1;
			uint rv;

			//Atan
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -30, stackPtr, d0);
			logger.LogTrace($"Atan({d0:X8}) 1.516 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Sin
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -36, stackPtr, d0);
			logger.LogTrace($"Sin({d0:X8}) -0.493 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Cos
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -42, stackPtr, d0);
			logger.LogTrace($"Cos({d0:X8}) 0.870 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Tan
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -48, stackPtr, d0);
			logger.LogTrace($"Tan({d0:X8}) -0.566 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//SinCos
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -54, stackPtr, d0, 0, stackPtr-4);
			logger.LogTrace($"SinCos({d0:X8}) -0.493,0.870 {rv:X8},{memory.UnsafeRead32(stackPtr-4):X8} {BitConverter.UInt32BitsToSingle(rv)},{BitConverter.UInt32BitsToSingle(memory.UnsafeRead32(stackPtr - 4))}");

			//Sinh
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -60, stackPtr, d0);
			logger.LogTrace($"Sinh({d0:X8}) 45861329 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Cosh
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -66, stackPtr, d0);
			logger.LogTrace($"Cosh({d0:X8}) 45861329 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Tanh
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -72, stackPtr, d0);
			logger.LogTrace($"Tanh({d0:X8}) 0.999 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Exp
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -78, stackPtr, d0);
			logger.LogTrace($"Exp({d0:X8}) 91722658 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Log
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -84, stackPtr, d0);
			logger.LogTrace($"Log({d0:X8}) 2.908 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Pow
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			d1 = BitConverter.SingleToUInt32Bits(2.0f);
			rv = CallLVO(libraryBase, -90, stackPtr, d0, d1);
			logger.LogTrace($"Pow({d0:X8},{d1:X8}) 336.14 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Sqrt
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -96, stackPtr, d0);
			logger.LogTrace($"Sqrt({d0:X8}) 4.282 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Tieee
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -102, stackPtr, d0);
			logger.LogTrace($"Tieee({d0:X8}) 18.334 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Fieee
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -108, stackPtr, d0);
			logger.LogTrace($"Fieee({d0:X8}) 18.334 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Asin
			d0 = BitConverter.SingleToUInt32Bits(0.1833428f);
			rv = CallLVO(libraryBase, -114, stackPtr, d0);
			logger.LogTrace($"Asin({d0:X8}) 0.184 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Acos
			d0 = BitConverter.SingleToUInt32Bits(0.1833428f);
			rv = CallLVO(libraryBase, -120, stackPtr, d0);
			logger.LogTrace($"Acos({d0:X8}) 1.386 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");

			//Log10
			d0 = BitConverter.SingleToUInt32Bits(18.33428f);
			rv = CallLVO(libraryBase, -126, stackPtr, d0);
			logger.LogTrace($"Log10({d0:X8}) 1.263 {rv:X8} {BitConverter.UInt32BitsToSingle(rv)}");
		}

		[Test]
		public void TestMathIEEEDoubTrans()
		{
			const string libName = "mathieeedoubtrans.library";

			uint libraryBase = LoadLibrary(0x10000, libName, out uint stackPtr);

			logger.LogTrace($"loaded {libName} at {libraryBase:X8}"); 
		}

		[Test]
		public void TestMathIEEEDoubBas()
		{
			const string libName = "mathieeedoubbas.library";

			uint libraryBase = LoadLibrary(0x10000, libName, out uint stackPtr);

			logger.LogTrace($"loaded {libName} at {libraryBase:X8}");
		}

		[Test]
		public void TestMPEGA020FPU()
		{
			const string libName = "MPEGA020FPU.library";

			uint libraryBase = LoadLibrary(0x10000, libName, out uint stackPtr);

			logger.LogTrace($"loaded {libName} at {libraryBase:X8}");

			Call(libraryBase, stackPtr, 0, 0);
		}
	}	
}
