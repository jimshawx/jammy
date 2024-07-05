using System;
using System.Collections.Generic;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class Chips : IChips
	{
		private readonly IInterrupt interrupt;
		private readonly IDiskDrives diskDrives;
		private readonly ushort[] regs = new ushort[32768];

		private ICopper copper;
		private IBlitter blitter;
		private readonly IMouse mouse;
		private readonly ISerial serial;
		private readonly EmulationSettings settings;
		private readonly ILogger logger;
		private IAudio audio;

		public Chips(IInterrupt interrupt, IDiskDrives diskDrives, IMouse mouse, ISerial serial,
			IOptions<EmulationSettings> settings, IEmulationWindow emulationWindow, ILogger<Chips> logger)
		{
			this.interrupt = interrupt;
			this.diskDrives = diskDrives;
			this.mouse = mouse;
			this.serial = serial;
			this.settings = settings.Value;
			this.logger = logger;

			emulationWindow.SetKeyHandlers(dbug_Keydown, dbug_Keyup);
		}

		private bool blitterDebugging = false;
		private void dbug_Keydown(int obj)
		{
			if (obj == (int)VK.VK_F2)
			{
				blitterDebugging ^= true;
				logger.LogTrace($"Blitter Debugging {(blitterDebugging?"enabled":"disabled")}");
				blitter.Logging(blitterDebugging);
				copper.Dumping(blitterDebugging);
			}
		}

		private void dbug_Keyup(int obj){}

		public void Init(IBlitter blitter, ICopper copper, IAudio audio)
		{
			this.blitter = blitter;
			this.copper = copper;
			this.audio = audio;
		}

		public void Reset()
		{
			//http://eab.abime.net/showthread.php?t=72300
			switch (settings.ChipSet)
			{
				case ChipSet.OCS:
					//OCS, setting this means KS3.1 sees OCS and gets copper list right for this
					//LISAID doesn't exist on OCS (OCS Denise 8362 just returns last value on bus).
					regs[REG(ChipRegs.LISAID)] = 0xffff;
					break;
				case ChipSet.ECS:
					regs[REG(ChipRegs.LISAID)] = 0x00fc;//LISA (0x00fc ECS Denise 8373) 
					break;
				case ChipSet.AGA:
					regs[REG(ChipRegs.LISAID)] = 0x00f8;//Lisa returns 0xF8, upper byte is 0 for A1200, non-zero for A4000
					break;
			}
		}

		readonly MemoryRange memoryRange = new MemoryRange(0xc00000, 0x200000);

		public bool IsMapped(uint address)
		{
			return (address >> 21) == 6;
		}

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange};
		}

		private int REG(uint address)
		{
			return (int)(address & 0x0000fffe) >> 1;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint originalAddress = address;
			address |= 0xdf0000;
			address &= 0xdffffe;

			if (size == Size.Byte)
			{
				uint r0 = Read(insaddr, address, Size.Word);
				byte r =(byte)(((originalAddress & 1)!=0)?r0:(r0>>8));
				//if ((originalAddress & 1) != 0) logger.LogTrace($"Read from odd address {originalAddress:X8} {r0:X4} {r:X2} {ChipRegs.Name(address)}");
				return r;
			}

			if (size == Size.Long)
			{
				uint r0 = Read(insaddr, address, Size.Word);
				uint r1 = Read(insaddr, address + 2, Size.Word);
				return (r0 << 16) | r1;
			}

			int reg = REG(address);

			if (address == 0xdf9000 || address == 0xdfa000)
			{
				logger.LogTrace($"R Out Of Range {address:X8} {originalAddress:X8}");
				return 0;
			}

			if ((address >= ChipRegs.COP1LCH && address <= ChipRegs.DDFSTOP) ||
				(address >= ChipRegs.BPL1PTH && address <= ChipRegs.COLOR31)||
				address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR || address == ChipRegs.VPOSW || address == ChipRegs.VHPOSW
				|| address == ChipRegs.VBSTRT || address == ChipRegs.VBSTOP || address == ChipRegs.VTOTAL || address == ChipRegs.DIWHIGH
				|| address == ChipRegs.VSSTRT || address == ChipRegs.VSSTOP
				|| address == ChipRegs.FMODE || address == ChipRegs.COPCON
				|| address == ChipRegs.CLXDAT)
			{
				regs[reg] = copper.Read(insaddr, address);
			}
			else if (address >= ChipRegs.BLTCON0 && address < ChipRegs.SPRHDAT || address == ChipRegs.BLTDDAT)
			{
				regs[reg] = blitter.Read(insaddr, address);
			}
			else if (address == ChipRegs.DSKSYNC || address == ChipRegs.DSKDATR || address == ChipRegs.DSKBYTR
			         || address == ChipRegs.DSKPTH || address == ChipRegs.DSKPTL || address == ChipRegs.DSKLEN || address == ChipRegs.DSKDAT)
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
			else if (address == ChipRegs.ADKCON) { /* can't read here */ }
			else if (address == ChipRegs.ADKCONR)
			{
				regs[reg] = (ushort)(audio.Read(insaddr, address)|diskDrives.Read(insaddr, address));
			}
			else if (address == ChipRegs.DMACON || address == ChipRegs.INTENA || address == ChipRegs.INTREQ || 
			         address == ChipRegs.DMACONR || address == ChipRegs.INTENAR || address == ChipRegs.INTREQR 
			         /*|| address == ChipRegs.LISAID*/ || address == ChipRegs.NO_OP)
			{

			}
			else if (address == ChipRegs.SERDATR || address == ChipRegs.SERDAT || address == ChipRegs.SERPER)
			{
				regs[reg] = serial.Read(insaddr, address);
			}
			else
			{
				logger.LogTrace($"R {ChipRegs.Name(address)} {originalAddress:X8} #{regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} {regs[reg]} @{insaddr:X8}");
			}

			return (uint)regs[reg];
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			uint originalAddress = address;
			address |= 0xdf0000;
			address &= 0xdffffe;

			if (size == Size.Byte)
			{
				//if ((originalAddress & 1) != 0) logger.LogTrace($"Write to odd address {originalAddress:X8},{value:X2} {ChipRegs.Name(address)}");
				value &= 0xff;
				value |= value<<8;
				Write(insaddr, address, value, Size.Word);
				return;

				/*
	If AGA (or maybe any 68020+ hardware?)
	- if odd address: 00xx is written to even address
	- if even address: xxxx is written. (duplicated)

	If "custom byte write bug":
	- if odd address: 00xx is written to even address.
	- if even address: xx00 is written.
				*/

				//logger.LogTrace($"Custom write to byte {originalAddress:X8}");
				if ((originalAddress & 1) != 0)
					Write(insaddr, address, value, Size.Word);
				else
					Write(insaddr, address, value << 8, Size.Word);
				return;
			}


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

				//if ((regs[reg] & 0x4000) != 0) logger.LogTrace("BBUSY ");
				//if ((regs[reg] & 0x2000) != 0) logger.LogTrace("BZERO ");
				//if ((regs[reg] & 0x1000) != 0) logger.LogTrace("unused ");
				//if ((regs[reg] & 0x0800) != 0) logger.LogTrace("unused ");
				//if ((regs[reg] & 0x0400) != 0) logger.LogTrace("BLTPRI ");
				//if ((regs[reg] & 0x0200) != 0) logger.LogTrace("DMAEN "); else logger.LogTrace("~DMAEN ");
				//if ((regs[reg] & 0x0100) != 0) logger.LogTrace("BPLEN ");
				//if ((regs[reg] & 0x0080) != 0) logger.LogTrace("COPEN "); else logger.LogTrace("~COPEN ");
				//if ((regs[reg] & 0x0040) != 0) logger.LogTrace("BLTEN ");
				//if ((regs[reg] & 0x0020) != 0) logger.LogTrace("SPREN ");
				//if ((regs[reg] & 0x0010) != 0) logger.LogTrace("DSKEN ");
				//if ((regs[reg] & 0x0008) != 0) logger.LogTrace("AUD3EN ");
				//if ((regs[reg] & 0x0004) != 0) logger.LogTrace("AUD2EN ");
				//if ((regs[reg] & 0x0002) != 0) logger.LogTrace("AUD1EN ");
				//if ((regs[reg] & 0x0001) != 0) logger.LogTrace("AUD0EN ");
				//if ((regs[reg] & 0x7fff) != 0) logger.LogTrace("");

				//if ((value & (int)(ChipRegs.DMA.SETCLR | ChipRegs.DMA.AUD3EN))== (int)(ChipRegs.DMA.SETCLR | ChipRegs.DMA.AUD3EN))
				//	logger.LogTrace("AUD3EN ON!");
				//else if ((value & (int)(ChipRegs.DMA.SETCLR | ChipRegs.DMA.AUD3EN)) == (int)ChipRegs.DMA.AUD3EN)
				//	logger.LogTrace("AUD3EN OFF!");

				audio.WriteDMACON((ushort)(regs[reg]&0x7fff));

				regs[REG(ChipRegs.DMACONR)] = regs[reg];
			}
			else if (address == ChipRegs.DMACONR) { /* can't write here */ }
			else if (address == ChipRegs.INTENA)
			{
				//logger.LogTrace($"INTENA {Convert.ToString(value, 2).PadLeft(16, '0')} @{insaddr:X8}");
				if ((value & 0x8000) != 0)
					regs[reg] |= (ushort)value;
				else
					regs[reg] &= (ushort)~value;
				//logger.LogTrace($"    -> {Convert.ToString(regs[reg],2).PadLeft(16,'0')} {regs[reg]:X4}");

				//if ((regs[reg] & 0x4000) != 0) logger.LogTrace("INTEN ");
				//if ((regs[reg] & 0x2000) != 0) logger.LogTrace("EXTER ");
				//if ((regs[reg] & 0x1000) != 0) logger.LogTrace("DSKSYN ");
				//if ((regs[reg] & 0x0800) != 0) logger.LogTrace("RBF ");
				//if ((regs[reg] & 0x0400) != 0) logger.LogTrace("AUD3 ");
				//if ((regs[reg] & 0x0200) != 0) logger.LogTrace("AUD2 ");
				//if ((regs[reg] & 0x0100) != 0) logger.LogTrace("AUD1 ");
				//if ((regs[reg] & 0x0080) != 0) logger.LogTrace("AUD0 ");
				//if ((regs[reg] & 0x0040) != 0) logger.LogTrace("BLIT ");
				//if ((regs[reg] & 0x0020) != 0) logger.LogTrace("VERTB ");
				//if ((regs[reg] & 0x0010) != 0) logger.LogTrace("COPER ");
				//if ((regs[reg] & 0x0008) != 0) logger.LogTrace("PORTS ");
				//if ((regs[reg] & 0x0004) != 0) logger.LogTrace("SOFT ");
				//if ((regs[reg] & 0x0002) != 0) logger.LogTrace("DSKBLK ");
				//if ((regs[reg] & 0x0001) != 0) logger.LogTrace("TBE ");
				//if ((regs[reg] & 0x7fff) != 0) logger.LogTrace("");
				audio.WriteINTENA((ushort)(regs[reg]&0x7fff));

				regs[REG(ChipRegs.INTENAR)] = (ushort)(regs[reg]&0x7fff);

				interrupt.SetPaulaInterruptLevel(regs[REG(ChipRegs.INTREQR)], regs[REG(ChipRegs.INTENAR)]);
			}
			else if (address == ChipRegs.INTENAR) { /* can't write here */}
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
				
				//logger.LogTrace($"INTREQ {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')}");

				//if ((regs[reg] & 0x4000) != 0) logger.LogTrace("INTEN ");
				//if ((regs[reg] & 0x2000) != 0) logger.LogTrace("EXTER ");
				//if ((regs[reg] & 0x1000) != 0) logger.LogTrace("DSKSYN ");
				//if ((regs[reg] & 0x0800) != 0) logger.LogTrace("RBF ");
				//if ((regs[reg] & 0x0400) != 0) logger.LogTrace("AUD3 ");
				//if ((regs[reg] & 0x0200) != 0) logger.LogTrace("AUD2 ");
				//if ((regs[reg] & 0x0100) != 0) logger.LogTrace("AUD1 ");
				//if ((regs[reg] & 0x0080) != 0) logger.LogTrace("AUD0 ");
				//if ((regs[reg] & 0x0040) != 0) logger.LogTrace("BLIT ");
				//if ((regs[reg] & 0x0020) != 0) logger.LogTrace("VERTB ");
				//if ((regs[reg] & 0x0010) != 0) logger.LogTrace("COPER ");
				//if ((regs[reg] & 0x0008) != 0) logger.LogTrace("PORTS ");
				//if ((regs[reg] & 0x0004) != 0) logger.LogTrace("SOFT ");
				//if ((regs[reg] & 0x0002) != 0) logger.LogTrace("DSKBLK ");
				//if ((regs[reg] & 0x0001) != 0) logger.LogTrace("TBE ");
				//if ((regs[reg] & 0x7fff) != 0) logger.LogTrace("");

				audio.WriteINTREQ((ushort)(regs[reg]&0x7fff));
				serial.WriteINTREQ((ushort)(regs[reg] & 0x7fff));

				regs[REG(ChipRegs.INTREQR)] = (ushort)(regs[reg]&0x7fff);

				interrupt.SetPaulaInterruptLevel(regs[REG(ChipRegs.INTREQR)], regs[REG(ChipRegs.INTENAR)]);
			}
			else if (address == ChipRegs.INTREQR) { /* can't write here */}

			else if (address == ChipRegs.LISAID && settings.ChipSet == ChipSet.AGA) { /* can't write here on AGA */}
			else
			{
				regs[reg] = (ushort)value;
			}

			if ((address >= ChipRegs.COP1LCH && address <= ChipRegs.DDFSTOP) ||
			    (address >= ChipRegs.BPL1PTH && address <= ChipRegs.COLOR31) ||
			    address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR || address == ChipRegs.VPOSW || address == ChipRegs.VHPOSW
			    || address == ChipRegs.VBSTRT || address == ChipRegs.VBSTOP || address == ChipRegs.VTOTAL || address == ChipRegs.DIWHIGH
			    || address == ChipRegs.VSSTRT || address == ChipRegs.VSSTOP
				|| address == ChipRegs.FMODE || address == ChipRegs.BEAMCON0 || address == ChipRegs.COPCON
				|| address == ChipRegs.CLXCON || address == ChipRegs.CLXCON2)
			{
				copper.Write(insaddr, address, (ushort)value);
			}
			else if (address >= ChipRegs.BLTCON0 && address < ChipRegs.SPRHDAT || address == ChipRegs.BLTDDAT)
			{
				blitter.Write(insaddr, address, (ushort)value);
			}
			else if (address == ChipRegs.ADKCON)
			{
				diskDrives.Write(insaddr, address, (ushort)value);
				audio.Write(insaddr, address, (ushort)value);
			}
			else if (address == ChipRegs.ADKCONR) { /* can't write here */ }
			else if (address == ChipRegs.DSKSYNC || address == ChipRegs.DSKDATR || address == ChipRegs.DSKBYTR
			         || address == ChipRegs.DSKPTH || address == ChipRegs.DSKPTL || address == ChipRegs.DSKLEN || address == ChipRegs.DSKDAT )
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

			else if (address == ChipRegs.DMACON || address == ChipRegs.INTENA || address == ChipRegs.INTREQ ||
			         address == ChipRegs.DMACONR || address == ChipRegs.INTENAR || address == ChipRegs.INTREQR ||
			         /*address == ChipRegs.LISAID  ||*/ address == ChipRegs.NO_OP)
			{

			}
			else if (address == ChipRegs.SERDAT || address == ChipRegs.SERDATR || address == ChipRegs.SERPER)
			{
				serial.Write(insaddr, address, (ushort)value);
			}
			else 
			{
				logger.LogTrace($"W {ChipRegs.Name(address)} {originalAddress:X8} {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} @{insaddr:X8}");
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
