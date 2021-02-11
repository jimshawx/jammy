using System;
using System.Windows.Forms;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class Chips : IEmulate, IMemoryMappedDevice
	{
		private readonly Interrupt interrupt;
		private readonly DiskDrives diskDrives;
		private ushort[] regs = new ushort[32768];

		private readonly Copper copper;
		private readonly Blitter blitter;
		private readonly Mouse mouse;
		private readonly Keyboard keyboard;
		private readonly Audio audio;

		public Chips(Debugger debugger, IMemoryMappedDevice memory, Interrupt interrupt, DiskDrives diskDrives, Mouse mouse, Keyboard keyboard)
		{
			this.interrupt = interrupt;
			this.diskDrives = diskDrives;
			this.mouse = mouse;
			this.keyboard = keyboard;
			blitter = new Blitter(this, memory, interrupt);
			copper = new Copper(memory, this, interrupt);
			audio = new Audio(memory);
		}

		public void Emulate(ulong cycles)
		{
			copper.Emulate(cycles);
			blitter.Emulate(cycles);
			diskDrives.Emulate(cycles);
			mouse.Emulate(cycles);
			keyboard.Emulate(cycles);
		}

		public void Reset()
		{
			copper.Reset();
			blitter.Reset();
			diskDrives.Reset();
			mouse.Reset();
			keyboard.Reset();

			regs[REG(ChipRegs.LISAID)] = 0x00f8;//LISA (0x00fc ECS Denise 8373) (OCD Denise just returns last value on bus).
			regs[REG(ChipRegs.LISAID)] = 0x0000;
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
			if (size == Size.Byte)
			{
				uint r0 = Read(insaddr, address&~1u, Size.Word);
				if ((address & 1)!=0) return (byte) r0;
				return r0 >> 8;
			}

			if (size == Size.Long)
			{
				//Logger.WriteLine($"Custom read from long {address:X8}");
				uint r0 = Read(insaddr, address, Size.Word);
				uint r1 = Read(insaddr, address + 2, Size.Word);
				return (r0 << 16) | r1;
			}

			int reg = REG(address);

			if ((address >= ChipRegs.COP1LCH && address <= ChipRegs.DDFSTOP) ||
				(address >= ChipRegs.BPL1PTH && address <= ChipRegs.COLOR31)||
				address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR || address == ChipRegs.VPOSW || address == ChipRegs.VHPOSW
				|| address == ChipRegs.VBSTRT || address == ChipRegs.VBSTOP || address == ChipRegs.VTOTAL || address == ChipRegs.DIWHIGH
				|| address == ChipRegs.FMODE)
			{
				regs[reg] = copper.Read(insaddr, address);
			}
			else if (address >= ChipRegs.BLTCON0 && address < ChipRegs.SPRHDAT || address == ChipRegs.BLTDDAT)
			{
				regs[reg] = blitter.Read(insaddr, address);
			}
			else if (address == ChipRegs.DSKSYNC || address == ChipRegs.DSKDATR || address == ChipRegs.DSKBYTR
			         || address == ChipRegs.DSKPTH || address == ChipRegs.DSKPTL || address == ChipRegs.DSKLEN || address == ChipRegs.DSKDAT 
			         )//|| address == ChipRegs.ADKCON || address == ChipRegs.ADKCONR)//these last two shared with audio
			{
				regs[reg] = diskDrives.Read(insaddr, address);
			}
			else if (address == ChipRegs.JOY0DAT || address == ChipRegs.JOY1DAT || address == ChipRegs.POTGO || address == ChipRegs.POTGOR
			         || address == ChipRegs.POT0DAT || address == ChipRegs.POT1DAT || address == ChipRegs.JOYTEST)
			{
				regs[reg] = mouse.Read(insaddr, address);
			}
			else if (address >= ChipRegs.AUD0LCH && address <= ChipRegs.AUD3DAT)
			{
				regs[reg] = audio.Read(insaddr, address);
			}
			else if (address == ChipRegs.DMACON || address == ChipRegs.INTENA || address == ChipRegs.INTREQ || address == ChipRegs.ADKCON ||
			                     address == ChipRegs.DMACONR || address == ChipRegs.INTENAR || address == ChipRegs.INTREQR || address == ChipRegs.ADKCONR
			                     || address == ChipRegs.LISAID || address == ChipRegs.NO_OP)
			{

			}
			else
			{
				Logger.WriteLine($"R {ChipRegs.Name(address)} #{regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} {regs[reg]} @{insaddr:X8}");
			}

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

				//Logger.WriteLine($"Custom write to byte {address:X8}");
				if ((address & 1) != 0)
					Write(insaddr, address & ~1u, value, Size.Word);
				else
					Write(insaddr, address, value << 8, Size.Word);
				return;
			}

			if ((address & 1) != 0)
				throw new InstructionAlignmentException(insaddr, address, 0);

			if (size == Size.Long)
			{
				//Logger.WriteLine($"Custom write to long {address:X8}");
				Write(insaddr, address, value >> 16, Size.Word);
				Write(insaddr, address + 2, value, Size.Word);
				return;
			}

			int reg = REG(address);

			if (address == ChipRegs.DMACON)
			{
				if ((value & 0x8000) != 0)
					regs[reg] |= (ushort)value;
				else
					regs[reg] &= (ushort)~value;
				//Logger.WriteLine($"DMACON {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} @{insaddr:X8}");
				
				//if ((regs[reg] & 0x4000) != 0) Logger.Write("BBUSY ");
				//if ((regs[reg] & 0x2000) != 0) Logger.Write("EXTER ");
				//if ((regs[reg] & 0x1000) != 0) Logger.Write("BZERO ");
				//if ((regs[reg] & 0x0800) != 0) Logger.Write("unused ");
				//if ((regs[reg] & 0x0400) != 0) Logger.Write("unused ");
				//if ((regs[reg] & 0x0200) != 0) Logger.Write("BLTPRI ");
				//if ((regs[reg] & 0x0100) != 0) Logger.Write("DMAEN ");
				//if ((regs[reg] & 0x0080) != 0) Logger.Write("BPLEN ");
				//if ((regs[reg] & 0x0040) != 0) Logger.Write("COPEN ");
				//if ((regs[reg] & 0x0020) != 0) Logger.Write("BLTEN ");
				//if ((regs[reg] & 0x0010) != 0) Logger.Write("DSKEN ");
				//if ((regs[reg] & 0x0008) != 0) Logger.Write("AUD3EN ");
				//if ((regs[reg] & 0x0004) != 0) Logger.Write("AUD2EN ");
				//if ((regs[reg] & 0x0002) != 0) Logger.Write("AUD1EN ");
				//if ((regs[reg] & 0x0001) != 0) Logger.Write("AUD0EN ");
				//if ((regs[reg] & 0x7fff) != 0) Logger.WriteLine("");

				regs[REG(ChipRegs.DMACONR)] = regs[reg];
			}
			else if (address == ChipRegs.INTENA)
			{
				//Logger.WriteLine($"INTENA {Convert.ToString(value, 2).PadLeft(16, '0')} @{insaddr:X8}");
				if ((value & 0x8000) != 0)
					regs[reg] |= (ushort)value;
				else
					regs[reg] &= (ushort)~value;
				//Logger.WriteLine($"    -> {Convert.ToString(regs[reg],2).PadLeft(16,'0')} {regs[reg]:X4}");

				//if ((regs[reg] & 0x4000) != 0) Logger.Write("INTEN ");
				//if ((regs[reg] & 0x2000) != 0) Logger.Write("EXTER ");
				//if ((regs[reg] & 0x1000) != 0) Logger.Write("DSKSYN ");
				//if ((regs[reg] & 0x0800) != 0) Logger.Write("RBF ");
				//if ((regs[reg] & 0x0400) != 0) Logger.Write("AUD3 ");
				//if ((regs[reg] & 0x0200) != 0) Logger.Write("AUD2 ");
				//if ((regs[reg] & 0x0100) != 0) Logger.Write("AUD1 ");
				//if ((regs[reg] & 0x0080) != 0) Logger.Write("AUD0 ");
				//if ((regs[reg] & 0x0040) != 0) Logger.Write("BLIT ");
				//if ((regs[reg] & 0x0020) != 0) Logger.Write("VERTB ");
				//if ((regs[reg] & 0x0010) != 0) Logger.Write("COPER ");
				//if ((regs[reg] & 0x0008) != 0) Logger.Write("PORTS ");
				//if ((regs[reg] & 0x0004) != 0) Logger.Write("SOFT ");
				//if ((regs[reg] & 0x0002) != 0) Logger.Write("DSKBLK ");
				//if ((regs[reg] & 0x0001) != 0) Logger.Write("TBE ");
				//if ((regs[reg] & 0x7fff) != 0) Logger.WriteLine("");

				regs[REG(ChipRegs.INTENAR)] = regs[reg];
			}
			else if (address == ChipRegs.INTREQ)
			{
				if ((value & 0x8000) != 0)
				{
					regs[reg] |= (ushort)value;
					//Logger.WriteLine($"INTREQ {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')}");
				}
				else
				{
					regs[reg] &= (ushort)~value;
				}
				
				interrupt.SetCPUInterruptLevel(regs[reg]);

				//Logger.WriteLine($"INTREQ {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')}");

				//if ((regs[reg] & 0x4000) != 0) Logger.Write("INTEN ");
				//if ((regs[reg] & 0x2000) != 0) Logger.Write("EXTER ");
				//if ((regs[reg] & 0x1000) != 0) Logger.Write("DSKSYN ");
				//if ((regs[reg] & 0x0800) != 0) Logger.Write("RBF ");
				//if ((regs[reg] & 0x0400) != 0) Logger.Write("AUD3 ");
				//if ((regs[reg] & 0x0200) != 0) Logger.Write("AUD2 ");
				//if ((regs[reg] & 0x0100) != 0) Logger.Write("AUD1 ");
				//if ((regs[reg] & 0x0080) != 0) Logger.Write("AUD0 ");
				//if ((regs[reg] & 0x0040) != 0) Logger.Write("BLIT ");
				//if ((regs[reg] & 0x0020) != 0) Logger.Write("VERTB ");
				//if ((regs[reg] & 0x0010) != 0) Logger.Write("COPER ");
				//if ((regs[reg] & 0x0008) != 0) Logger.Write("PORTS ");
				//if ((regs[reg] & 0x0004) != 0) Logger.Write("SOFT ");
				//if ((regs[reg] & 0x0002) != 0) Logger.Write("DSKBLK ");
				//if ((regs[reg] & 0x0001) != 0) Logger.Write("TBE ");
				//if ((regs[reg] & 0x7fff) != 0) Logger.WriteLine("");

				regs[REG(ChipRegs.INTREQR)] = regs[reg];
			}
			else if (address == ChipRegs.ADKCON)
			{
				if ((value & 0x8000) != 0)
					regs[reg] |= (ushort) value;
				else
					regs[reg] &= (ushort) ~value;
				regs[REG(ChipRegs.ADKCONR)] = regs[reg];
			}
			else
			{
				regs[reg] = (ushort)value;
			}

			if ((address >= ChipRegs.COP1LCH && address <= ChipRegs.DDFSTOP) ||
			    (address >= ChipRegs.BPL1PTH && address <= ChipRegs.COLOR31) ||
			    address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR || address == ChipRegs.VPOSW || address == ChipRegs.VHPOSW
			    || address == ChipRegs.VBSTRT || address == ChipRegs.VBSTOP || address == ChipRegs.VTOTAL || address == ChipRegs.DIWHIGH
			    || address == ChipRegs.FMODE)
			{
				copper.Write(insaddr, address, (ushort)value);
			}
			else if (address >= ChipRegs.BLTCON0 && address < ChipRegs.SPRHDAT || address == ChipRegs.BLTDDAT)
			{
				blitter.Write(insaddr, address, (ushort)value);
			}
			else if (address == ChipRegs.DSKSYNC || address == ChipRegs.DSKDATR || address == ChipRegs.DSKBYTR
			         || address == ChipRegs.DSKPTH || address == ChipRegs.DSKPTL || address == ChipRegs.DSKLEN || address == ChipRegs.DSKDAT 
			         || address == ChipRegs.ADKCON || address == ChipRegs.ADKCONR)//these last two shared with audio
			{
				diskDrives.Write(insaddr, address, (ushort) value);
			}
			else if (address == ChipRegs.JOY0DAT || address == ChipRegs.JOY1DAT || address == ChipRegs.POTGO || address == ChipRegs.POTGOR
			         || address == ChipRegs.POT0DAT || address == ChipRegs.POT1DAT || address == ChipRegs.JOYTEST)
			{
				mouse.Write(insaddr, address, (ushort)value);
			}
			else if (address >= ChipRegs.AUD0LCH && address <= ChipRegs.AUD3DAT)
			{
				audio.Write(insaddr, address, (ushort)value);
			}
			else if (address == ChipRegs.DMACON || address == ChipRegs.INTENA || address == ChipRegs.INTREQ || address == ChipRegs.ADKCON ||
			         address == ChipRegs.DMACONR || address == ChipRegs.INTENAR || address == ChipRegs.INTREQR || address == ChipRegs.ADKCONR
			         || address == ChipRegs.LISAID || address == ChipRegs.NO_OP)
			{

			}
			else 
			{
				Logger.WriteLine($"W {ChipRegs.Name(address)} {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} @{insaddr:X8}");
			}
		}

		private void DebugInfo(uint insaddr, uint address, uint value, Size size)
		{
			Logger.WriteLine($"Custom Write {insaddr:X8} {address:X8} {value:X8} {size} {ChipRegs.Name(address)} {ChipRegs.Description(address)}");

			if (address == ChipRegs.BPLCON0)
			{
				if ((value & 2) != 0) Logger.Write("ESRY ");
				if ((value & 4) != 0) Logger.Write("LACE ");
				if ((value & 8) != 0) Logger.Write("LPEN ");
				if ((value & 256) != 0) Logger.Write("GAUD ");
				if ((value & 512) != 0) Logger.Write("COLOR_ON ");
				if ((value & 1024) != 0) Logger.Write("DBLPF ");
				if ((value & 2048) != 0) Logger.Write("HOMOD ");
				Logger.Write($"{(value >> 12) & 7}BPP ");
				if ((value & 32768) != 0) Logger.Write("HIRES ");
				Logger.WriteLine("");
			}

			if (address == ChipRegs.SERPER)
			{
				if ((value & 0x8000) != 0) Logger.WriteLine("9bit"); else Logger.WriteLine("8bit");
				Logger.WriteLine($"Baud {value & 0x7fff} = {1000000.0 / (((value & 0x7fff) + 1) * 0.27936)} NTSC");
				Logger.WriteLine($"Baud {value & 0x7fff} = {1000000.0 / (((value & 0x7fff) + 1) * 0.28194)} PAL");
			}
		}
	}
}
