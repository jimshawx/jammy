using System.Diagnostics;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class Custom : IEmulate, IMemoryMappedDevice
	{
		private ushort[] regs = new ushort[32768];

		private Copper copper;
		private Blitter blitter;

		public Custom(Debugger debugger, Memory memory)
		{
			blitter = new Blitter(this);
			copper = new Copper(memory, this);
		}

		public void Emulate(ulong ns)
		{
			copper.Emulate(ns);
			blitter.Emulate(ns);
		}

		public void Reset()
		{
			copper.Reset();
		}

		public bool IsMapped(uint address)
		{
			return (address >> 16) == 0xdf;
		}

		private int REG(uint address)
		{
			return (int)(address & 0x0000fffe) >> 1;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if ((address & 1) != 0)
				throw new InstructionAlignmentException(address, 0);

			if (size == Size.Byte)
				throw new InvalidCustomRegisterSizeException(insaddr, address, size);

			if (size == Size.Long)
			{
				//Trace.WriteLine($"Custom read from long {address:X8}");
				uint r0 = Read(insaddr, address, Size.Word);
				uint r1 = Read(insaddr, address + 2, Size.Word);
				return (r0 << 16) | r1;
			}

			int reg = REG(address);

			//Trace.WriteLine($"Custom Read {address:X8} {size} : #{regs[reg]:X4} {debug[address].Item1} {debug[address].Item2}");

			return (uint)regs[reg];
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size == Size.Byte)
			{
				/*
	If AGA (or maybe any 68020+ hardware?)
	- if odd address: 00xx is written to even address
	- if even address: xxxx is written. (duplicated)

	If "custom byte write bug":
	- if odd address: 00xx is written to even address.
	- if even address: xx00 is written.
				*/

				//Trace.WriteLine($"Custom write to byte {address:X8}");
				if ((address & 1) != 0)
					Write(insaddr, address & ~1u, value, Size.Word);
				else
					Write(insaddr, address, value << 8, Size.Word);
				return;
			}

			if ((address & 1) != 0)
				throw new InstructionAlignmentException(address, 0);

			if (size == Size.Long)
			{
				//Trace.WriteLine($"Custom write to long {address:X8}");
				Write(insaddr, address, value >> 16, Size.Word);
				Write(insaddr, address + 2, value, Size.Word);
				return;
			}

			DebugInfo(address, value, size);

			int reg = REG(address);

			if (address == CustomRegs.DMACON)
			{
				if ((value & 0x8000) != 0)
					regs[reg] |= (ushort)value;
				else
					regs[reg] &= (ushort)~value;
				regs[REG(CustomRegs.DMACONR)] = regs[reg];
			}
			else if (address == CustomRegs.INTENA)
			{
				if ((value & 0x8000) != 0)
					regs[reg] |= (ushort)value;
				else
					regs[reg] &= (ushort)~value;
				regs[REG(CustomRegs.INTENAR)] = regs[reg];
			}
			else if (address == CustomRegs.INTREQ)
			{
				if ((value & 0x8000) != 0)
					regs[reg] |= (ushort)value;
				else
					regs[reg] &= (ushort)~value;
				regs[REG(CustomRegs.INTREQR)] = regs[reg];
			}
			else if (address == CustomRegs.COPJMP1)
			{
				uint copadd = (uint)(regs[REG(CustomRegs.COP1LCH)] << 16) + regs[REG(CustomRegs.COP1LCL)];
				copper.SetCopperPC(copadd);
			}
			else if (address == CustomRegs.COPJMP2)
			{
				uint copadd = (uint)(regs[REG(CustomRegs.COP2LCH)] << 16) + regs[REG(CustomRegs.COP2LCL)];
				copper.SetCopperPC(copadd);
			}
			else
			{
				regs[reg] = (ushort)value;
			}

			//NB. BPLCON3 13..15 controls the palette bank on AGA
			if (address >= CustomRegs.COLOR00 && address <= CustomRegs.COLOR31)
			{
				uint bank = (Read(insaddr, CustomRegs.BPLCON3, Size.Word) & 0b111_00000_00000000) >> (13 - 5);
				UI.SetColour((int)(bank + ((address - CustomRegs.COLOR00) >> 1)), (ushort)value);
			}
			else if (address == CustomRegs.COP1LCL)
			{
				uint copadd = ((uint)regs[REG(CustomRegs.COP1LCH)] << 16) + regs[REG(CustomRegs.COP1LCL)];
				copper.ParseCopperList(copadd);
				copadd = ((uint)regs[REG(CustomRegs.COP2LCH)] << 16) + regs[REG(CustomRegs.COP2LCL)];
				copper.ParseCopperList(copadd);
			}
			else if (address == CustomRegs.COP2LCL)
			{
				uint copadd = ((uint)regs[REG(CustomRegs.COP1LCH)] << 16) + regs[REG(CustomRegs.COP1LCL)];
				copper.ParseCopperList(copadd);
				copadd = ((uint)regs[REG(CustomRegs.COP2LCH)] << 16) + regs[REG(CustomRegs.COP2LCL)];
				copper.ParseCopperList(copadd);
			}
			else if (address == CustomRegs.BLTCON0 || address == CustomRegs.BLTCON1 || address == CustomRegs.BLTSIZE)
			{
				blitter.Write(address, (ushort)value);
			}
		}



		private void DebugInfo(uint address, uint value, Size size)
		{
			Trace.WriteLine($"Custom Write {address:X8} {value:X8} {size} {CustomRegs.Name(address)} {CustomRegs.Description(address)}");

			if (address == CustomRegs.BPLCON0)
			{
				if ((value & 2) != 0) Trace.Write("ESRY ");
				if ((value & 4) != 0) Trace.Write("LACE ");
				if ((value & 8) != 0) Trace.Write("LPEN ");
				if ((value & 256) != 0) Trace.Write("GAUD ");
				if ((value & 512) != 0) Trace.Write("COLOR_ON ");
				if ((value & 1024) != 0) Trace.Write("DBLPF ");
				if ((value & 2048) != 0) Trace.Write("HOMOD ");
				Trace.Write($"{(value >> 12) & 7}BPP ");
				if ((value & 32768) != 0) Trace.Write("HIRES ");
				Trace.WriteLine("");
			}

			if (address == CustomRegs.SERPER)
			{
				if ((value & 0x8000) != 0) Trace.WriteLine("9bit"); else Trace.WriteLine("8bit");
				Trace.WriteLine($"Baud {value & 0x7fff} = {1000000.0 / (((value & 0x7fff) + 1) * 0.27936)} NTSC");
				Trace.WriteLine($"Baud {value & 0x7fff} = {1000000.0 / (((value & 0x7fff) + 1) * 0.28194)} PAL");
			}
		}
	}
}
