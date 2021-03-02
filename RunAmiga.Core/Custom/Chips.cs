using System;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class Chips : IChips
	{
		private readonly IInterrupt interrupt;
		private readonly IDiskDrives diskDrives;
		private readonly ushort[] regs = new ushort[32768];

		private ICopper copper;
		private IBlitter blitter;
		private readonly IMouse mouse;
		private readonly IKeyboard keyboard;
		private readonly ILogger logger;
		private IAudio audio;

		public Chips(IInterrupt interrupt, IDiskDrives diskDrives, IMouse mouse, IKeyboard keyboard, ILogger<Chips> logger)
		{
			this.interrupt = interrupt;
			this.diskDrives = diskDrives;
			this.mouse = mouse;
			this.keyboard = keyboard;
			this.logger = logger;
		}

		public void Init(IBlitter blitter, ICopper copper, IAudio audio)
		{
			this.blitter = blitter;
			this.copper = copper;
			this.audio = audio;
		}

		public void Emulate(ulong cycles)
		{
			copper.Emulate(cycles);
			blitter.Emulate(cycles);
			diskDrives.Emulate(cycles);
			mouse.Emulate(cycles);
			keyboard.Emulate(cycles);
			audio.Emulate(cycles);
		}

		public void Reset()
		{
			copper.Reset();
			blitter.Reset();
			diskDrives.Reset();
			mouse.Reset();
			keyboard.Reset();
			audio.Reset();

			regs[REG(ChipRegs.LISAID)] = 0x00f8;//LISA (0x00fc ECS Denise 8373) (OCD Denise just returns last value on bus).
			regs[REG(ChipRegs.LISAID)] = 0x0000;
		}

		readonly MemoryRange memoryRange = new MemoryRange(0xdf0000, 0x10000);

		public bool IsMapped(uint address)
		{
			return (address >> 16) == 0xdf;
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
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
				//logger.LogTrace($"Custom read from long {address:X8}");
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
				logger.LogTrace($"R {ChipRegs.Name(address)} #{regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} {regs[reg]} @{insaddr:X8}");
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

				//logger.LogTrace($"Custom write to byte {address:X8}");
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
				//logger.LogTrace($"Custom write to long {address:X8}");
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
				//logger.LogTrace($"DMACON {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} @{insaddr:X8}");
				
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
				//if ((regs[reg] & 0x7fff) != 0) logger.LogTrace("");

				regs[REG(ChipRegs.DMACONR)] = regs[reg];
			}
			else if (address == ChipRegs.INTENA)
			{
				//logger.LogTrace($"INTENA {Convert.ToString(value, 2).PadLeft(16, '0')} @{insaddr:X8}");
				if ((value & 0x8000) != 0)
					regs[reg] |= (ushort)value;
				else
					regs[reg] &= (ushort)~value;
				//logger.LogTrace($"    -> {Convert.ToString(regs[reg],2).PadLeft(16,'0')} {regs[reg]:X4}");

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
				//if ((regs[reg] & 0x7fff) != 0) logger.LogTrace("");

				regs[REG(ChipRegs.INTENAR)] = regs[reg];
			}
			else if (address == ChipRegs.INTREQ)
			{
				if ((value & 0x8000) != 0)
				{
					regs[reg] |= (ushort)value;
					//logger.LogTrace($"INTREQ {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')}");
				}
				else
				{
					regs[reg] &= (ushort)~value;
				}
				
				interrupt.SetCPUInterruptLevel(regs[reg]);

				//logger.LogTrace($"INTREQ {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')}");

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
				//if ((regs[reg] & 0x7fff) != 0) logger.LogTrace("");

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
				logger.LogTrace($"W {ChipRegs.Name(address)} {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} @{insaddr:X8}");
			}
		}

		private void DebugInfo(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Custom Write {insaddr:X8} {address:X8} {value:X8} {size} {ChipRegs.Name(address)} {ChipRegs.Description(address)}");

			if (address == ChipRegs.BPLCON0)
			{
				var log = new StringBuilder();
				if ((value & 2) != 0) log.Append("ESRY ");
				if ((value & 4) != 0) log.Append("LACE ");
				if ((value & 8) != 0) log.Append("LPEN ");
				if ((value & 256) != 0) log.Append("GAUD ");
				if ((value & 512) != 0) log.Append("COLOR_ON ");
				if ((value & 1024) != 0) log.Append("DBLPF ");
				if ((value & 2048) != 0) log.Append("HOMOD ");
				log.Append($"{(value >> 12) & 7}BPP ");
				if ((value & 32768) != 0) log.Append("HIRES ");
				logger.LogTrace(log.ToString());
			}

			if (address == ChipRegs.SERPER)
			{
				if ((value & 0x8000) != 0) logger.LogTrace("9bit"); else logger.LogTrace("8bit");
				logger.LogTrace($"Baud {value & 0x7fff} = {1000000.0 / (((value & 0x7fff) + 1) * 0.27936)} NTSC");
				logger.LogTrace($"Baud {value & 0x7fff} = {1000000.0 / (((value & 0x7fff) + 1) * 0.28194)} PAL");
			}
		}
	}
}
