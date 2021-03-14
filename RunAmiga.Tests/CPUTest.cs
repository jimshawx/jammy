using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accessibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using RunAmiga.Core;
using RunAmiga.Core.CPU.CSharp;
using RunAmiga.Core.CPU.Musashi;
using RunAmiga.Core.Interface;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Options;
using RunAmiga.Core.Types.Types;
using RunAmiga.Debugger;
using RunAmiga.Disassembler;
using RunAmiga.Disassembler.Analysers;
using RunAmiga.Extensions.Extensions;
using RunAmiga.Logger.DebugAsync;

namespace RunAmiga.Tests
{
	[TestFixture]
	public class CPUTest
	{
		private ServiceProvider serviceProvider0;
		private ServiceProvider serviceProvider1;

		[OneTimeSetUp]
		public void CPUTestInit()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.Build();

			serviceProvider0 = new ServiceCollection()
				.AddLogging(x=>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					//x.AddDebug();
					x.AddDebugAsync();
				})
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("Musashi"))
				.AddSingleton<IInterrupt, Interrupt>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<IMusashiCPU, MusashiCPU>()
				.AddSingleton<IMemoryMapper>(x=> new MemoryMapper(new List<IMemoryMappedDevice>{ x.GetRequiredService<IMemory>() }))
				.AddSingleton<IMemory, Memory>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation").Bind(o))
				.BuildServiceProvider();

			serviceProvider1 = new ServiceCollection()
				.AddLogging(x=>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					//x.AddDebug();
					x.AddDebugAsync();
				})
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("CSharp"))
				.AddSingleton<IInterrupt, Interrupt>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<IDisassembly, Disassembly>()
				.AddSingleton<IAnalyser,Analyser>()
				.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<ITracer, Tracer>()
				.AddSingleton<ICSharpCPU, CPU>()
				.AddSingleton<IMemoryMapper>(x => new MemoryMapper(new List<IMemoryMappedDevice> { x.GetRequiredService<IMemory>() }))
				.AddSingleton<IMemory, Memory>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation").Bind(o))
				.BuildServiceProvider();
		}

		private class CPUTestRig
		{
			private readonly IMemory memory;
			private readonly ICPU cpu;
			private readonly Disassembler.Disassembler disassembler;

			public CPUTestRig(ICPU cpu, IMemory memory)
			{
				this.memory = memory;
				this.cpu = cpu;

				var r = new Random(0x24061972);
				for (uint i = 0; i < memory.GetMemoryArray().Length; i+=4)
					memory.UnsafeWrite32(i, (uint)(r.Next()*2)&0xfffffffe);

				memory.UnsafeWrite32(0, 0x800000);//sp
				memory.UnsafeWrite32(4, 0x10004);//pc loaded with 0x10004 at boot
				memory.UnsafeWrite16(0x10004, 0x4e71);//4e71 = nop
				memory.UnsafeWrite16(0x10006, 0x4e71);//4e71 = nop

				uint trapSentinel;
				trapSentinel = 0xDEAD0000;
				for (uint i = 0x19*4; i <= 0x1f*4; i+=4)
					memory.UnsafeWrite32(i, trapSentinel+=4);

				disassembler = new Disassembler.Disassembler();
			}

			public void SetTraps()
			{
				uint trapSentinel;
				trapSentinel = 0xABAD0008;
				for (uint i = 8; i < 0x1000; i += 4)
					memory.UnsafeWrite32(i, trapSentinel + 4);
			}

			public void SetPC(uint pc)
			{
				cpu.SetPC(pc);
			}

			public void SetRegs(Regs regs)
			{
				cpu.SetRegs(regs);
			}

			public Regs GetRegs()
			{
				return cpu.GetRegs();
			}

			public void Write(uint address, ushort value)
			{
				memory.UnsafeWrite16(address, value);
			}
			
			public ushort Read(uint address)
			{
				return memory.UnsafeRead16(address);
			}

			public void Emulate()
			{
				cpu.Emulate(1);
			}

			public string Disassemble(uint address)
			{
				if (address >= memory.GetMemoryArray().Length) return "";
				var roMemory = new ReadOnlySpan<byte>(memory.GetMemoryArray());
				var dasm = disassembler.Disassemble(address, roMemory.Slice((int)address, 20));
				return dasm.ToString(new DisassemblyOptions{IncludeBytes = true});
			}

			public void Reset()
			{
				cpu.Reset();
			}

			public byte[] GetMemory()
			{
				return memory.GetMemoryArray();
			}
		}

		[Test(Description = "ALU0")]
		public void FuzzCPU0() { FuzzCPU(0x0000); }

		[Ignore("All Pass")]
		[Test(Description = "MOVE.B")]
		public void FuzzCPU1() { FuzzCPU(0x1000); }

		[Ignore("All Pass")]
		[Test(Description = "MOVE.W")]
		public void FuzzCPU2() { FuzzCPU(0x2000); }

		[Ignore("All Pass")]
		[Test(Description = "MOVE.L")]
		public void FuzzCPU3() { FuzzCPU(0x3000); }

		[Test(Description = "ALU1")]
		public void FuzzCPU4() { FuzzCPU(0x4000); }

		[Ignore("All Pass")]
		[Test(Description = "ADD/SUBQ,Scc,DBcc")]
		public void FuzzCPU5() { FuzzCPU(0x5000); }

		[Ignore("All Pass")]
		[Test(Description = "BRANCHES")]
		public void FuzzCPU6() { FuzzCPU(0x6000); }

		[Ignore("All Pass")]
		[Test(Description = "MOVEQ")]
		public void FuzzCPU7() { FuzzCPU(0x7000); }

		[Test(Description = "DIVU/S,SBCD,OR")]
		public void FuzzCPU8() { FuzzCPU(0x8000); }

		[Test(Description = "SUB/A/X")]
		public void FuzzCPU9() { FuzzCPU(0x9000); }

		[Ignore("Not Implemented")]
		[Test(Description = "Coprocessor")]
		public void FuzzCPUA() { FuzzCPU(0xA000); }

		[Ignore("All Pass")]
		[Test(Description = "EOR,CMP")]
		public void FuzzCPUB() { FuzzCPU(0xB000); }
		
		[Test(Description = "MULU/S,ABCD,EXG,AND")]
		public void FuzzCPUC() { FuzzCPU(0xC000); }
		
		[Test(Description = "ADD/A/X")]
		public void FuzzCPUD() { FuzzCPU(0xD000); }

		[Test(Description = "SHIFT/RORATE")]
		public void FuzzCPUE() { FuzzCPU(0xE000); }

		[Ignore("Not Implemented")]
		[Test(Description="FPU MC68881")]
		public void FuzzCPUF() { FuzzCPU(0xF000); }

		public void FuzzCPU(ushort prefix, int size=0x1000)
		{
			ServiceProviderFactory.ServiceProvider = serviceProvider0;
			var cpu0 = new CPUTestRig((ICPU)serviceProvider0.GetRequiredService<IMusashiCPU>(),
				serviceProvider0.GetRequiredService<IMemory>());

			ServiceProviderFactory.ServiceProvider = serviceProvider1;
			var cpu1 = new CPUTestRig((ICPU)serviceProvider1.GetRequiredService<ICSharpCPU>(),
				serviceProvider1.GetRequiredService<IMemory>());

			cpu0.Reset();
			cpu1.Reset();

			cpu0.Emulate();
			cpu1.Emulate();

			var regs = new Regs();

			var r = new Random(0x02011964);

			uint failcount = 0;
			for (int i = 0; i < size; i++)
			{
				uint pc = (uint)((r.Next() * 2) & ((cpu0.GetMemory().Length/2)-1) & 0xffffffc) + 0x10000;

				regs.PC = pc;
				regs.SR = 0x2700;//(ushort)(0x2700 + r.Next(0x100));
				cpu0.SetRegs(regs);
				cpu1.SetRegs(regs);

				//put the traps back
				cpu0.SetTraps();
				cpu1.SetTraps();
				
				ushort ins = (ushort)(i|prefix);

				cpu0.Write(pc, ins);
				cpu1.Write(pc, ins);

				try
				{
					cpu0.Emulate();
					cpu1.Emulate();

					var r0 = cpu0.GetRegs();
					var r1 = cpu1.GetRegs();

					if (r0.PC >> 16 == 0xABAD && r0.PC == r1.PC)
					{
						//emulation TRAPped, don't really care if the exception stack frame doesn't match (for now)
						bool memoriesMatch = cpu0.GetMemory().SequenceEqual(cpu1.GetMemory());
						if (!memoriesMatch)
						{
							TestContext.WriteLine($"ALERT memories don't match!\n{cpu0.GetMemory().DiffSummary(cpu1.GetMemory())}");
							Array.Copy(cpu0.GetMemory(), cpu1.GetMemory(), cpu0.GetMemory().Length);
						}
					}
					else
					{
						Assert.IsFalse(r0.Compare(r1), "Test #{0} {1}\n{2}", i + 1, cpu0.Disassemble(pc), string.Join(Environment.NewLine, r0.CompareSummary(r1)));
						Assert.IsTrue(cpu0.GetMemory().SequenceEqual(cpu1.GetMemory()), $"Test {i + 1} Memory Contents Differ!\n{cpu0.Disassemble(pc)}\n{cpu0.GetMemory().DiffSummary(cpu1.GetMemory())}");
					}

					TestContext.WriteLine($"PASS {ins:X4} {cpu0.Disassemble(pc)}");
				}
				catch (AssertionException)
				{
					TestContext.WriteLine($"FAIL {ins:X4} {cpu0.Disassemble(pc)}");
					Array.Copy(cpu0.GetMemory(), cpu1.GetMemory(), cpu0.GetMemory().Length);
					failcount++;
					Assert.Fail();
				}
			}
			Assert.AreEqual(0, failcount, "Some instructions failed the test");
		}
	}
}
