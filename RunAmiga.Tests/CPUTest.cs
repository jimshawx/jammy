using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

namespace RunAmiga.Tests
{
	[TestFixture]
	public class CPUTest
	{
		private ServiceProvider serviceProvider1;
		private ServiceProvider serviceProvider2;

		[OneTimeSetUp]
		public void CPUTestInit()
		{
			var serviceCollection = new ServiceCollection();
			serviceProvider1 = serviceCollection
				.AddLogging(x=>x.AddDebug())
				.AddSingleton<IInterrupt, Interrupt>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<ILogger<MusashiCPU>>(x=>new NullLogger<MusashiCPU>())
				.AddSingleton<IMusashiCPU, MusashiCPU>()
				.AddSingleton<IMemoryMapper>(x=> new MemoryMapper(new List<IMemoryMappedDevice>{ x.GetRequiredService<IMemory>() }))
				.AddSingleton<IMemory, Memory>()
				.BuildServiceProvider();

			serviceProvider2 = serviceCollection
				.AddLogging(x=>x.AddDebug())
				.AddSingleton<IInterrupt, Interrupt>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<ILogger<CPU>>(x => new NullLogger<CPU>())
				.AddSingleton<IDisassembly, Disassembly>()
				.AddSingleton<IAnalyser,Analyser>()
				.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<ITracer, Tracer>()
				.AddSingleton<IOptions<EmulationSettings>>(x=>new OptionsWrapper<EmulationSettings>(new EmulationSettings()))
				.AddSingleton<ICSharpCPU, CPU>()
				
				.AddSingleton<IMemoryMapper>(x => new MemoryMapper(new List<IMemoryMappedDevice> { x.GetRequiredService<IMemory>() }))
				.AddSingleton<IMemory, Memory>()
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
				for (uint i = 0; i < 16*1024*1024; i+=4)
					memory.Write(0,i, (uint)(r.Next()*2)&0xfffffffe, Size.Long);

				uint trapSentinel;
				memory.Write(0, 0, 0x800000, Size.Long);//sp
				memory.Write(0, 4, 0x10004, Size.Long);//pc loaded with 0x10004 at boot
				memory.Write(0, 0x10004, 0x4e72,Size.Word);//4e72 = stop

				trapSentinel= 0xABAD0008;
				for (uint i = 8; i < 0x1000; i+=4)
					memory.Write(0, i, trapSentinel+4, Size.Long);

				trapSentinel = 0xDEAD0000;
				for (uint i = 0x19*4; i <= 0x1f*4; i+=4)
					memory.Write(0, i, trapSentinel+=4,Size.Long);

				disassembler = new Disassembler.Disassembler();
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
				memory.Write(0, address, value, Size.Word);
			}

			public void Emulate()
			{
				cpu.Emulate(1);
			}

			public string Disassemble(uint address)
			{
				var roMemory = new ReadOnlySpan<byte>(memory.GetMemoryArray());
				var dasm = disassembler.Disassemble(address, roMemory.Slice((int)address, 20));
				return dasm.ToString(new DisassemblyOptions{IncludeBytes = true});
			}

			public void Reset()
			{
				cpu.Reset();
			}
		}
		
		[Test]
		public void FuzzCPU()
		{
			ServiceProviderFactory.ServiceProvider = serviceProvider1;
			var cpu0 = new CPUTestRig((ICPU)serviceProvider1.GetRequiredService<IMusashiCPU>(),
				serviceProvider1.GetRequiredService<IMemory>());

			ServiceProviderFactory.ServiceProvider = serviceProvider2;
			var cpu1 = new CPUTestRig((ICPU)serviceProvider2.GetRequiredService<ICSharpCPU>(),
				serviceProvider2.GetRequiredService<IMemory>());

			cpu0.Reset();
			cpu1.Reset();

			var regs = new Regs();

			var r = new Random(0x02011964);
			bool allSuccessful = true;
			for (int i = 0; i < 100; i++)
			{
				uint pc = (uint)(r.Next() * 2) & 0x007ffffe;

				regs.PC = pc;
				regs.SR = 0x2700;//(ushort)(0x2700 + r.Next(0x100));
				cpu0.SetRegs(regs);
				cpu1.SetRegs(regs);

				ushort ins;
				do
				{
					ins = (ushort)r.Next();
				} while ((ins >> 12) == 0b1010 || (ins >> 12) == 0b1111);

				cpu0.Write(pc, ins);
				cpu1.Write(pc, ins);

				try
				{
					cpu0.Emulate();
					cpu1.Emulate();

					var r0 = cpu0.GetRegs();
					var r1 = cpu1.GetRegs();

					Assert.IsFalse(r0.Compare(r1), "Test #{0} {1}\n{2}", i+1, cpu0.Disassemble(pc), string.Join(Environment.NewLine, r0.CompareSummary(r1)));

					TestContext.WriteLine($"PASS {ins:X4} {cpu0.Disassemble(pc)}");
				}
				catch (AssertionException)
				{
					allSuccessful = false;
				}
			}
			Assert.IsTrue(allSuccessful, "Some instructions failed the test");
		}

		[Test]
		public void TestSpecific()
		{
			var instructions = new []{ new ushort[] { 0xe3d8 } };//lsl.w     #1,(a0)+

			ServiceProviderFactory.ServiceProvider = serviceProvider1;
			var cpu0 = new CPUTestRig((ICPU)serviceProvider1.GetRequiredService<IMusashiCPU>(),
				serviceProvider1.GetRequiredService<IMemory>());

			ServiceProviderFactory.ServiceProvider = serviceProvider2;
			var cpu1 = new CPUTestRig((ICPU)serviceProvider2.GetRequiredService<ICSharpCPU>(),
				serviceProvider2.GetRequiredService<IMemory>());

			cpu0.Emulate();
			cpu1.Reset();

			foreach (var instruction in instructions)
			{
				uint address = 0x10000;
				foreach (var word in instruction)
				{
					cpu0.Write(address, word);
					cpu1.Write(address, word);
					address += 2;
				}

				cpu0.SetPC(0x10000);
				cpu1.SetPC(0x10000);

				cpu0.Emulate();
				cpu1.Emulate();

				var r0 = cpu0.GetRegs();
				var r1 = cpu1.GetRegs();

				Assert.IsFalse(r0.Compare(r1), string.Join(Environment.NewLine, r0.CompareSummary(r1)));
			}
		}
	}
}
