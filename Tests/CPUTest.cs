using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RunAmiga.Options;
using RunAmiga.Types;

namespace RunAmiga.Tests
{
	[TestClass]
	public class CPUTest
	{
		private enum CPUType
		{
			Musashi,
			CSharp,
		}

		private class CPUTestRig
		{
			private readonly Memory memory;
			private readonly ICPU cpu;
			private readonly IEmulate emulate;
			private readonly Disassembler disassembler;

			public CPUTestRig(CPUType type)
			{
				var labeller = new Labeller();
				var breakpoints = new BreakpointCollection();

				memory = new Memory($"CPUTest_{type}");
				
				var disassembly = new Disassembly(memory.GetMemoryArray(), breakpoints);
				var tracer = new Tracer(disassembly, labeller);
				var memoryMapper = new MemoryMapper(new List<IMemoryMappedDevice> { memory });
				var interrupt = new Interrupt();
				
				if (type == CPUType.Musashi)
					cpu = new MusashiCPU(interrupt, memoryMapper, breakpoints);
				else
					cpu = new CPU(interrupt, memoryMapper, breakpoints, tracer);

				emulate = cpu as IEmulate;

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
		
		[TestMethod]
		public void FuzzCPU()
		{
			var cpu0 = new CPUTestRig(CPUType.Musashi);
			var cpu1 = new CPUTestRig(CPUType.CSharp);
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
						Logger.WriteLine($"FAIL {ins:X4} {cpu0.Disassemble(pc)}");
						Logger.WriteLine(string.Join('\n', r0.CompareSummary(r1)));
					}
					else
					{
						Logger.WriteLine($"PASS {ins:X4} {cpu0.Disassemble(pc)}");
					}
				}
				catch (Exception ex)
				{
					Logger.WriteLine($"FAIL {ins:X4} {cpu0.Disassemble(pc)}");
					Logger.WriteLine(ex.ToString());
				}
			}
		}
	}
}
