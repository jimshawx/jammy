using System;
using System.Drawing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using RunAmiga.Custom;
using RunAmiga.Interfaces;
using RunAmiga.Options;
using RunAmiga.Types;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Size = RunAmiga.Types.Size;

namespace RunAmiga.Tests
{
	[TestFixture]
	public class CPUTest
	{
		private ServiceProvider serviceProvider1;
		private ServiceProvider serviceProvider2;
		private ILogger logger;

		[OneTimeSetUp]
		public void CPUTestInit()
		{var serviceCollection = new ServiceCollection();
			serviceProvider1 = serviceCollection.AddLogging()
				.AddSingleton<IMachine, Machine>()
				.AddSingleton<IAudio, Audio>()
				.AddSingleton<IBattClock, BattClock>()
				.AddSingleton<IBlitter, Blitter>()
				.AddSingleton<ICIAAOdd, CIAAOdd>()
				.AddSingleton<ICIABEven, CIABEven>()
				.AddSingleton<ICopper, Copper>()
				.AddSingleton<IDiskDrives, DiskDrives>()
				.AddSingleton<IKeyboard, Keyboard>()
				.AddSingleton<IMouse, Mouse>()
				.AddSingleton<IInterrupt, Interrupt>()
				.AddSingleton<ICPU, MusashiCPU>()
				.AddSingleton<IMemoryMapper, MemoryMapper>()
				.AddSingleton<IMemory>(x=> new Memory("CPUTest_Musashi", serviceProvider1.GetRequiredService<ILoggerFactory>().CreateLogger<Memory>()))
				.BuildServiceProvider();

			serviceProvider2 = serviceCollection.AddLogging()
				.AddSingleton<IMachine, Machine>()
				.AddSingleton<IAudio, Audio>()
				.AddSingleton<IBattClock, BattClock>()
				.AddSingleton<IBlitter, Blitter>()
				.AddSingleton<ICIAAOdd, CIAAOdd>()
				.AddSingleton<ICIABEven, CIABEven>()
				.AddSingleton<ICopper, Copper>()
				.AddSingleton<IDiskDrives, DiskDrives>()
				.AddSingleton<IKeyboard, Keyboard>()
				.AddSingleton<IMouse, Mouse>()
				.AddSingleton<IInterrupt, Interrupt>()
				.AddSingleton<ICPU, CPU>()
				.AddSingleton<IMemoryMapper, MemoryMapper>()
				.AddSingleton<IMemory>(x => new Memory("CPUTest_CSharp", serviceProvider1.GetRequiredService<ILoggerFactory>().CreateLogger<Memory>()))
				.BuildServiceProvider();

			logger = serviceProvider1.GetRequiredService<ILoggerFactory>().CreateLogger<CPUTest>();
		}

		private class CPUTestRig
		{
			private readonly IMemory memory;
			private readonly ICPU cpu;
			private readonly IEmulate emulate;
			private readonly Disassembler disassembler;

			public CPUTestRig(ICPU cpu, IMemory memory)
			{
				this.memory = memory;
				this.cpu = cpu;
				emulate = (IEmulate)cpu;

				var r = new Random(0x24061972);
				for (uint i = 0; i < 16*1024*1024; i+=4)
					memory.Write(0,i, (uint)(r.Next()*2), Size.Long);

				disassembler = new Disassembler();
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
				emulate.Emulate(1);
			}

			public string Disassemble(uint address)
			{
				var roMemory = new ReadOnlySpan<byte>(memory.GetMemoryArray());
				var dasm = disassembler.Disassemble(address, roMemory.Slice((int)address, 20));
				return dasm.ToString(new DisassemblyOptions{IncludeBytes = true});
			}

			public void Reset()
			{
				emulate.Reset();
			}
		}
		
		[Test]
		public void FuzzCPU()
		{
			var cpu0 = new CPUTestRig((ICPU)serviceProvider1.GetRequiredService<IMusashiCPU>(), serviceProvider1.GetRequiredService<IMemory>());
			var cpu1 = new CPUTestRig((ICPU)serviceProvider2.GetRequiredService<ICSharpCPU>(), serviceProvider2.GetRequiredService<IMemory>());
			cpu0.Emulate();
			cpu1.Reset();

			var regs = new Regs();
			regs.SR = 0x2700;

			var r = new Random();
			for (int i = 0; i < 100; i++)
			{
				uint pc = (uint)(r.Next() * 2) & 0x007ffffe;

				regs.PC = pc;

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

					if (r0.Compare(r1))
					{
						logger.LogTrace($"FAIL {ins:X4} {cpu0.Disassemble(pc)}");
						logger.LogTrace(string.Join('\n', r0.CompareSummary(r1)));
					}
					else
					{
						logger.LogTrace($"PASS {ins:X4} {cpu0.Disassemble(pc)}");
					}
				}
				catch (Exception ex)
				{
					logger.LogTrace($"FAIL {ins:X4} {cpu0.Disassemble(pc)}");
					logger.LogTrace(ex.ToString());
				}
			}
		}
	}
}
