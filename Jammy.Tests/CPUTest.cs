using Jammy.Core;
using Jammy.Core.CPU.CSharp;
using Jammy.Core.CPU.Musashi;
using Jammy.Core.CPU.Musashi.CSharp;
using Jammy.Core.CPU.Musashi.MC68020;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Debugger;
using Jammy.Disassembler;
using Jammy.Extensions.Extensions;
using Jammy.Interface;
using Jammy.Types.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Parky.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Tests
{

	[TestFixture]
	public class CPUTest
	{
		private CPUTestRig cpu0;
		private CPUTestRig cpu1;
		private ILogger logger;

		[OneTimeSetUp]
		public void CPUTestInit()
		{
			ServiceProvider serviceProvider;

			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.Build();

			var cpus = new List<ICPUTestRig>();

			var serviceCollection = new ServiceCollection()
				.AddLogging(x=>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					//x.AddDebug();
					x.AddDebugAsync();
				})
				.AddSingleton<IInterrupt, Core.Interrupt>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<ITracer, NullTracer>()
				.AddSingleton<TestMemory>()
				.AddSingleton<ICPUTestRig, CPUTestRig>()
				.AddSingleton<ITestMemory>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryMapper>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IDebugMemoryMapper>(x => x.GetRequiredService<TestMemory>())
				.AddSingleton<IMemoryMappedDevice>(x => x.GetRequiredService<TestMemory>());

			//68000
			//0
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("CS68000"))
				.AddSingleton<ICPU, CPU>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation000NoPrefetch").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			//1
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("Musashi68000"))
				.AddSingleton<ICPU, MusashiCPU>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation000NoPrefetch").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			//2
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("MusashiCS68000NoPrefetch"))
				.AddSingleton<ICPU, CPUWrapperMusashi>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation000NoPrefetch").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			//3
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("MusashiCS68000"))
				.AddSingleton<ICPU, CPUWrapperMusashi>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation000").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			//68EC020
			//4
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("Musashi68EC020"))
				.AddSingleton<ICPU, Musashi68EC020CPU>()
				.Configure<EmulationSettings>(o => configuration.GetSection("EmulationEC020").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			//5
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("MusashiCS68EC020"))
				.AddSingleton<ICPU, CPUWrapperMusashi>()
				.Configure<EmulationSettings>(o => configuration.GetSection("EmulationEC020").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			//68030
			//6
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("Musashi68030"))
				.AddSingleton<ICPU, Musashi68EC020CPU>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation030").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			//7
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("MusashiCS68030"))
				.AddSingleton<ICPU, CPUWrapperMusashi>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation030").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			//68040
			//8
			serviceCollection
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("MusashiCS68040"))
				.AddSingleton<ICPU, CPUWrapperMusashi>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation040").Bind(o));
			serviceProvider = serviceCollection.BuildServiceProvider();
			cpus.Add(serviceProvider.GetRequiredService<ICPUTestRig>());

			logger = serviceProvider.GetRequiredService<ILogger<CPUTest>>();

			//which CPUs are we going to test?
			cpu0 = (CPUTestRig)cpus[0];
			cpu1 = (CPUTestRig)cpus[1];

			cpu0.Reset();
			cpu1.Reset();

			cpu0.Emulate();
			cpu1.Emulate();
		}

		public interface ICPUTestRig {}

		private class CPUTestRig : ICPUTestRig
		{
			private readonly IDebugMemoryMapper memory;
			private readonly ICPU cpu;
			private readonly Jammy.Disassembler.Disassembler disassembler;
			private readonly ITestMemory testMemory;

			public CPUTestRig(ICPU cpu, IDebugMemoryMapper memory, ITestMemory testMemory)
			{
				this.memory = memory;
				this.testMemory = testMemory;
				this.cpu = cpu;

				var r = new Random(0x24061972);
				for (uint i = 0; i < memory.MappedRange().First().Length; i+=4)
					memory.UnsafeWrite32(i, (uint)(r.Next()*2)&0xfffffffe);

				memory.UnsafeWrite32(0, 0x800000);//sp
				memory.UnsafeWrite32(4, 0x10004);//pc loaded with 0x10004 at boot
				memory.UnsafeWrite16(0x10004, 0x4e71);//4e71 = nop
				memory.UnsafeWrite16(0x10006, 0x4e71);//4e71 = nop

				uint trapSentinel;
				trapSentinel = 0xDEAD0000;
				for (uint i = 0x19*4; i <= 0x1f*4; i+=4)
					memory.UnsafeWrite32(i, trapSentinel+=4);

				disassembler = new Jammy.Disassembler.Disassembler();

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
				cpu.Emulate();
			}

			public string Disassemble(uint address)
			{
				if (address + 20 > memory.Length)
					address -= address + 20 - (uint)memory.Length; 
				
				var dasm = disassembler.Disassemble(address, memory.GetEnumerable(address, 20));
				return dasm.ToString(new DisassemblyOptions{IncludeBytes = true});
			}

			public void Reset()
			{
				cpu.Reset();
			}

			public byte[] GetMemory()
			{
				return testMemory.GetMemoryArray();
			}
		}

		//[Ignore("All Pass")]
		[Test(Description = "ALU0")]
		public void FuzzCPU0() { FuzzCPU(0x0000); }

		//[Ignore("All Pass")]
		[Test(Description = "MOVE.B")]
		public void FuzzCPU1() { FuzzCPU(0x1000); }

		//[Ignore("All Pass")]
		[Test(Description = "MOVE.W")]
		public void FuzzCPU2() { FuzzCPU(0x2000); }

		//[Ignore("All Pass")]
		[Test(Description = "MOVE.L")]
		public void FuzzCPU3() { FuzzCPU(0x3000); }

		//[Ignore("All Pass")]
		[Test(Description = "ALU1")]
		public void FuzzCPU4() { FuzzCPU(0x4000); }

		//[Ignore("All Pass")]
		[Test(Description = "ADD/SUBQ,Scc,DBcc")]
		public void FuzzCPU5() { FuzzCPU(0x5000); }

		//[Ignore("All Pass")]
		[Test(Description = "BRANCHES")]
		public void FuzzCPU6() { FuzzCPU(0x6000); }

		//[Ignore("All Pass")]
		[Test(Description = "MOVEQ")]
		public void FuzzCPU7() { FuzzCPU(0x7000); }

		//[Ignore("All Pass")]
		[Test(Description = "DIVU/S,SBCD,OR")]
		public void FuzzCPU8() { FuzzCPU(0x8000); }

		//[Ignore("All Pass")]
		[Test(Description = "SUB/A/X")]
		public void FuzzCPU9() { FuzzCPU(0x9000); }

		[Ignore("Not Implemented COP")]
		[Test(Description = "Coprocessor")]
		public void FuzzCPUA() { FuzzCPU(0xA000); }

		//[Ignore("All Pass")]
		[Test(Description = "EOR,CMP")]
		public void FuzzCPUB() { FuzzCPU(0xB000); }

		//[Ignore("All Pass")]
		[Test(Description = "MULU/S,ABCD,EXG,AND")]
		public void FuzzCPUC() { FuzzCPU(0xC000); }

		//[Ignore("All Pass")]
		[Test(Description = "ADD/A/X")]
		public void FuzzCPUD() { FuzzCPU(0xD000); }

		//[Ignore("All Pass")]
		[Test(Description = "SHIFT/ROTATE")]
		public void FuzzCPUE() { FuzzCPU(0xE000); }

		[Ignore("Not Implemented MC6888x")]
		[Test(Description = "FPU MC68881")]
		public void FuzzCPUF() { FuzzCPU(0xF000); }

		[Ignore("")]
		[Test(Description = "More random instructions")]
		public void FuzzCPUMore()
		{
			var r = new Random(0x11071950);
			for (int j = 0; j < 100; j++)
			{
				TestContext.WriteLine($"Test Run #{j+1}");

				for (int i = 0; i < 16; i++)
				{
					TestContext.WriteLine($"Test Block #{i}:{j+1}");

					if (i == 15) continue;
					if (i == 10) continue;

					FuzzCPU((ushort)(i << 12), seed: r.Next());
				}
			}
		}

		public void FuzzCPU(ushort prefix, int size=0x1000, int seed=0x02011964)
		{
			var regs = new Regs();

			var r = new Random(seed);

			uint failcount = 0;
			for (int i = 0; i < size; i++)
			{
				uint pc = (uint)((r.Next() * 2) & ((cpu0.GetMemory().Length/2)-1) & 0xffffffc) + 0x10000;

				regs.PC = pc;
				regs.SR = (ushort)(0x0700 + r.Next(1<<5));

				for (int x = 0; x < 8; x++)
				{
					regs.D[x] = (uint)((r.Next() << 1) ^ r.Next());
					if (x < 7)
						regs.A[x] = (uint)((r.Next() << 1) ^ r.Next());
				}

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
						ClassicAssert.IsFalse(r0.Compare(r1), "Test #{0} {1}\n{2}", i + 1, cpu0.Disassemble(pc), string.Join(Environment.NewLine, r0.CompareSummary(r1)));
						ClassicAssert.IsTrue(cpu0.GetMemory().SequenceEqual(cpu1.GetMemory()), $"Test {i + 1} Memory Contents Differ!\n{cpu0.Disassemble(pc)}\n{cpu0.GetMemory().DiffSummary(cpu1.GetMemory())}");
					}

					TestContext.WriteLine($"PASS {ins:X4} {cpu0.Disassemble(pc)}");
				}
				catch (AssertionException)
				{
					TestContext.WriteLine($"FAIL {ins:X4} {cpu0.Disassemble(pc)}");
					Array.Copy(cpu0.GetMemory(), cpu1.GetMemory(), cpu0.GetMemory().Length);
					failcount++;
					break;
				}
			}
			ClassicAssert.AreEqual(0, failcount, "Some instructions failed the test");
		}
	}
}
