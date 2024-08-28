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
		private IAgnus agnus;
		private IDenise denise;
		private readonly IMouse mouse;
		private readonly ISerial serial;
		private IDMA dma;
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
		private bool blitterDumping = false;
		private void dbug_Keydown(int obj)
		{
			if (obj == (int)VK.VK_F2)
			{
				blitterDebugging ^= true;
				logger.LogTrace($"Blitter Debugging {(blitterDebugging?"enabled":"disabled")}");
				blitter.Logging(blitterDebugging);
				copper.Dumping(blitterDebugging);
			}

			if (obj == (int)VK.VK_F4)
			{
				blitterDumping ^= true;
				blitter.Dumping(blitterDumping);
			}
		}

		private void dbug_Keyup(int obj){}

		public void Init(IBlitter blitter, ICopper copper, IAudio audio, IAgnus agnus, IDenise denise, IDMA dma)
		{
			this.blitter = blitter;
			this.copper = copper;
			this.audio = audio;
			this.agnus = agnus;
			this.denise = denise;
			this.dma = dma;
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
			return (int)(address & 0x00000ffe) >> 1;
		}

		private object locker = new object();

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint originalAddress = address;
			address |= 0xdf0000;
			address &= 0xdffffe;

			if (size == Size.Byte)
			{
				uint r0 = Read(insaddr, address, Size.Word);
				byte r = (byte)(((originalAddress & 1) != 0) ? r0 : (r0 >> 8));
				//if ((originalAddress & 1) != 0) logger.LogTrace($"Read from odd address {originalAddress:X8} {r0:X4} {r:X2} {ChipRegs.Name(address)}");
				return r;
			}

			if (size == Size.Long)
			{
				uint r0 = Read(insaddr, address, Size.Word);
				uint r1 = Read(insaddr, address + 2, Size.Word);
				return (r0 << 16) | r1;
			}

			/*
			 these are the only chip registers that can be read from outside
			BLTDDAT->Blitter
			DMACONR->Chips
			VPOSR->Agnus
			VHPOSR->Agnus
			DSKDATR->disk drives
			JOY0DAT->mouse
			JOY1DAT->mouse
			CLXDAT->Denise
			ADKCONR->Audio,disk drives
			POT0DAT->mouse
			POT1DAT->mouse
			POTINP/POTGOR->mouse
			SERDATR->serial
			DSKBYTR->disk drives
			INTENAR->Chips
			INTREQR->Chips
			DENISEID/LISAID->Chips		
			 */

			//lock (locker)
			{
				int reg = REG(address);

				if (address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR)
				{
					regs[reg] = agnus.Read(insaddr, address);
				}
				else if (address == ChipRegs.CLXDAT)
				{
					regs[reg] = denise.Read(insaddr, address);
				}
				else if (address == ChipRegs.BLTDDAT)
				{
					regs[reg] = blitter.Read(insaddr, address);
				}
				else if (address == ChipRegs.DSKDATR || address == ChipRegs.DSKBYTR)
				{
					regs[reg] = diskDrives.Read(insaddr, address);
				}
				else if (address == ChipRegs.JOY0DAT || address == ChipRegs.JOY1DAT ||
				         address == ChipRegs.POTGOR || address == ChipRegs.POT0DAT || address == ChipRegs.POT1DAT)
				{
					regs[reg] = mouse.Read(insaddr, address);
				}
				else if (address == ChipRegs.ADKCONR)
				{
					regs[reg] = (ushort)(audio.Read(insaddr, address) | diskDrives.Read(insaddr, address));
				}
				else if (address == ChipRegs.DMACONR)
				{
					regs[reg] = dma.Read(insaddr, address);
				}
				else if (address == ChipRegs.INTENAR || address == ChipRegs.INTREQR
				         || address == ChipRegs.LISAID)
				{
					//here
				}
				else if (address == ChipRegs.SERDATR)
				{
					regs[reg] = serial.Read(insaddr, address);
				}
				else
				{
					//logger.LogTrace($"R {ChipRegs.Name(address)} {originalAddress:X8} #{regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} {regs[reg]} @{insaddr:X8}");
					//almost certainly, this is code using clr.x on a chip register.  you were TOLD not to do that!
					return 0;
				}

				return (uint)regs[reg];
			}
		
		}

		//private object locker = new object();

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			uint originalAddress = address;
			address |= 0xdf0000;
			address &= 0xdffffe;

			if (size == Size.Byte)
			{
				//if ((originalAddress & 1) != 0) logger.LogTrace($"Write to odd address {originalAddress:X8},{value:X2} {ChipRegs.Name(address)}");
				value &= 0xff;
				value |= value << 8;
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

			//lock (locker)
			{
				int reg = REG(address);

				if (address == ChipRegs.DMACON)
				{
					dma.Write(insaddr, address, (ushort)value);
				}
				else if (address == ChipRegs.DMACONR)
				{
					/* can't write here */
				}
				else if (address == ChipRegs.INTENA)
				{
					ushort prevIntena = regs[reg];

					//logger.LogTrace($"INTENA {value.ToBin(2) @{insaddr:X8}");
					if ((value & 0x8000) != 0)
						regs[reg] |= (ushort)value;
					else
						regs[reg] &= (ushort)~value;
					//logger.LogTrace($"    -> {regs[reg].ToBin()} {regs[reg]:X4}");

					if (((prevIntena ^ regs[reg])&0x0020)!=0)
						logger.LogTrace($"VERTB {((regs[reg]&0x0020)!=0?"on":"off")} @ {insaddr:X8}");

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
					audio.WriteINTENA((ushort)(regs[reg] & 0x7fff));

					regs[REG(ChipRegs.INTENAR)] = (ushort)(regs[reg] & 0x7fff);

					interrupt.SetPaulaInterruptLevel(regs[REG(ChipRegs.INTREQR)], regs[REG(ChipRegs.INTENAR)]);
				}
				else if (address == ChipRegs.INTENAR)
				{
					/* can't write here */
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

					audio.WriteINTREQ((ushort)(regs[reg] & 0x7fff));
					serial.WriteINTREQ((ushort)(regs[reg] & 0x7fff));

					regs[REG(ChipRegs.INTREQR)] = (ushort)(regs[reg] & 0x7fff);

					interrupt.SetPaulaInterruptLevel(regs[REG(ChipRegs.INTREQR)], regs[REG(ChipRegs.INTENAR)]);
				}
				else if (address == ChipRegs.INTREQR)
				{
					/* can't write here */
				}
				else if (address == ChipRegs.LISAID && settings.ChipSet == ChipSet.AGA)
				{
					/* can't write here on AGA */
				}
				else if ((address >= ChipRegs.DDFSTRT && address <= ChipRegs.DDFSTOP) ||
				    (address >= ChipRegs.BPL1PTH && address <= ChipRegs.BPL8PTL) ||
				    address == ChipRegs.BPL1MOD || address == ChipRegs.BPL2MOD ||
				    (address >= ChipRegs.BPL1DAT && address <= ChipRegs.SPR7DATB) ||
				    address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR || address == ChipRegs.VPOSW ||
				    address == ChipRegs.VHPOSW
				    || address == ChipRegs.VBSTRT || address == ChipRegs.VBSTOP || address == ChipRegs.VTOTAL
				    || address == ChipRegs.VSSTRT || address == ChipRegs.VSSTOP
				    || address == ChipRegs.BEAMCON0)
				{
					agnus.Write(insaddr, address, (ushort)value);
				}
				else if (address == ChipRegs.DIWSTRT || address == ChipRegs.DIWSTOP ||
				         address == ChipRegs.DIWHIGH || address == ChipRegs.BPLCON0 || address == ChipRegs.FMODE)
				{
					agnus.Write(insaddr, address, (ushort)value);
					denise.Write(insaddr, address, (ushort)value);
				}
				else if (address == ChipRegs.BPLCON1 || address == ChipRegs.BPLCON2 ||
				         address == ChipRegs.BPLCON3 || address == ChipRegs.BPLCON4 || address == ChipRegs.CLXCON ||
				         address == ChipRegs.CLXCON2
				         || (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31))
				{
					denise.Write(insaddr, address, (ushort)value);
				}
				else if ((address >= ChipRegs.COP1LCH && address <= ChipRegs.COPINS) || address == ChipRegs.COPCON)
				{
					copper.Write(insaddr, address, (ushort)value);
				}
				else if (address >= ChipRegs.BLTCON0 && address <= ChipRegs.BLTADAT || address == ChipRegs.BLTDDAT)
				{
					blitter.Write(insaddr, address, (ushort)value);
				}
				else if (address == ChipRegs.ADKCON)
				{
					diskDrives.Write(insaddr, address, (ushort)value);
					audio.Write(insaddr, address, (ushort)value);
				}
				else if (address == ChipRegs.ADKCONR)
				{
					/* can't write here */
				}
				else if (address == ChipRegs.DSKSYNC || address == ChipRegs.DSKDATR || address == ChipRegs.DSKBYTR
				         || address == ChipRegs.DSKPTH || address == ChipRegs.DSKPTL ||
				         address == ChipRegs.DSKLEN || address == ChipRegs.DSKDAT)
				{
					diskDrives.Write(insaddr, address, (ushort)value);
				}
				else if (address == ChipRegs.JOY0DAT || address == ChipRegs.JOY1DAT || address == ChipRegs.POTGO ||
				         address == ChipRegs.POTGOR
				         || address == ChipRegs.POT0DAT || address == ChipRegs.POT1DAT ||
				         address == ChipRegs.JOYTEST)
				{
					mouse.Write(insaddr, address, (ushort)value);
				}
				else if (address >= ChipRegs.AUD0LCH && address <= ChipRegs.AUD3DAT)
				{
					audio.Write(insaddr, address, (ushort)value);
				}
				else if (address == ChipRegs.NO_OP)
				{

				}
				else if (address == ChipRegs.SERDAT || address == ChipRegs.SERDATR || address == ChipRegs.SERPER)
				{
					serial.Write(insaddr, address, (ushort)value);
				}
				else
				{
					logger.LogTrace($"W {ChipRegs.Name(address)} {originalAddress:X8} {regs[reg]:X4} {Convert.ToString(regs[reg], 2).PadLeft(16, '0')} @{insaddr:X8}");
					regs[reg] = (ushort)value;
				}
			}
		}

		public void WriteWide(uint address, ulong value)
		{
			uint originalAddress = address;
			address |= 0xdf0000;
			address &= 0xdffffe;

			if (address >= ChipRegs.BPL1DAT && address <= ChipRegs.BPL8DAT)
				agnus.WriteWide(address, value);
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

		public uint DebugChipsetRead(uint address, Size size)
		{
			if (size == Size.Long)
				return (DebugChipsetRead(address, Size.Word) << 16) | DebugChipsetRead(address + 2, Size.Word);

			if (size == Size.Byte)
				throw new ArgumentOutOfRangeException();

			if ((address >= ChipRegs.DDFSTRT && address <= ChipRegs.DDFSTOP) ||
					(address >= ChipRegs.BPL1PTH && address <= ChipRegs.SPR7DATB) ||
					address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR || address == ChipRegs.VPOSW || address == ChipRegs.VHPOSW
					|| address == ChipRegs.VBSTRT || address == ChipRegs.VBSTOP || address == ChipRegs.VTOTAL || address == ChipRegs.DIWHIGH
					|| address == ChipRegs.VSSTRT || address == ChipRegs.VSSTOP
					|| address == ChipRegs.FMODE)
			{
				return agnus.DebugChipsetRead(address, size);
			}
			else if (address == ChipRegs.CLXCON || address == ChipRegs.CLXCON2 || address == ChipRegs.CLXDAT
					 || (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31)
					 || address == ChipRegs.DIWSTRT || address == ChipRegs.DIWSTOP)
			{
				return denise.DebugChipsetRead(address, size);
			}
			else if ((address >= ChipRegs.COP1LCH && address <= ChipRegs.COPINS) || address == ChipRegs.COPCON)
			{
				return copper.DebugChipsetRead(address, size);
			}
			else if (address >= ChipRegs.BLTCON0 && address < ChipRegs.SPRHDAT || address == ChipRegs.BLTDDAT)
			{
				return blitter.DebugChipsetRead(address, size);
			}
			else if (address == ChipRegs.DSKSYNC || address == ChipRegs.DSKDATR || address == ChipRegs.DSKBYTR
				 || address == ChipRegs.DSKPTH || address == ChipRegs.DSKPTL || address == ChipRegs.DSKLEN || address == ChipRegs.DSKDAT)
			{
				return diskDrives.DebugChipsetRead(address, size);
			}
			else if (address == ChipRegs.JOY0DAT || address == ChipRegs.JOY1DAT || address == ChipRegs.POTGO || address == ChipRegs.POTGOR
					 || address == ChipRegs.POT0DAT || address == ChipRegs.POT1DAT || address == ChipRegs.JOYTEST)
			{
				return mouse.DebugChipsetRead(address, size);
			}
			else if (address >= ChipRegs.AUD0LCH && address <= ChipRegs.AUD3DAT)
			{
				return audio.DebugChipsetRead(address, size);
			}
			else if (address == ChipRegs.ADKCON || address == ChipRegs.ADKCONR)
			{
				return (ushort)(audio.DebugChipsetRead(address, size) | diskDrives.DebugChipsetRead(address, size));
			}
			else if (address == ChipRegs.INTENA || address == ChipRegs.INTREQ ||
				 address == ChipRegs.INTENAR || address == ChipRegs.INTREQR
				 || address == ChipRegs.NO_OP)
			{
				int reg = REG(address);
				return regs[reg];
			}
			else if (address == ChipRegs.DMACONR || address == ChipRegs.DMACON)
			{
				return dma.DebugChipsetRead(address, size);
			}
			else if (address == ChipRegs.SERDATR || address == ChipRegs.SERDAT || address == ChipRegs.SERPER)
			{
				return serial.DebugChipsetRead(address, size);
			}
			else
			{
				logger.LogTrace($"DR {ChipRegs.Name(address)}");
			}

			return 0;
		}
	}
}
