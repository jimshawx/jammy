using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class Chips : IChips
	{
		private readonly IInterrupt interrupt;
		private readonly IDiskDrives diskDrives;

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

		private uint lisaid;
		private ushort intena;
		private ushort intreq;

		public void Reset()
		{
			//http://eab.abime.net/showthread.php?t=72300
			switch (settings.ChipSet)
			{
				case ChipSet.OCS:
					//OCS, setting this means KS3.1 sees OCS and gets copper list right for this
					//LISAID doesn't exist on OCS (OCS Denise 8362 just returns last value on bus).
					lisaid = 0xffff;
					break;
				case ChipSet.ECS:
					lisaid = 0x00fc;//LISA (0x00fc ECS Denise 8373) 
					break;
				case ChipSet.AGA:
					lisaid = 0x00f8;//Lisa returns 0xF8, upper byte is 0 for A1200, non-zero for A4000
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

			int reg = REG(address);

			if (address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR)
			{
				return agnus.Read(insaddr, address);
			}
			else if (address == ChipRegs.CLXDAT
						|| address == ChipRegs.STREQU || address == ChipRegs.STRHOR
						|| address == ChipRegs.STRLONG || address == ChipRegs.STRVBL)
			{
				return denise.Read(insaddr, address);
			}
			else if (address == ChipRegs.BLTDDAT)
			{
				return blitter.Read(insaddr, address);
			}
			else if (address == ChipRegs.COPJMP1 || address == ChipRegs.COPJMP2)
			{
				return copper.Read(insaddr, address);
			}
			else if (address == ChipRegs.DSKDATR || address == ChipRegs.DSKBYTR)
			{
				return diskDrives.Read(insaddr, address);
			}
			else if (address == ChipRegs.JOY0DAT || address == ChipRegs.JOY1DAT ||
				        address == ChipRegs.POTGOR || address == ChipRegs.POT0DAT || address == ChipRegs.POT1DAT)
			{
				return mouse.Read(insaddr, address);
			}
			else if (address == ChipRegs.ADKCONR)
			{
				return (ushort)(audio.Read(insaddr, address) | diskDrives.Read(insaddr, address));
			}
			else if (address == ChipRegs.DMACONR)
			{
				return dma.Read(insaddr, address);
			}
			else if (address == ChipRegs.INTENAR)
			{
				return (uint)(intena & 0x7fff);
			}
			else if (address == ChipRegs.INTREQR)
			{
				return (uint)(intreq & 0x7fff);
			}
			else if (address == ChipRegs.LISAID)
			{
				logger.LogTrace($"LISAID Check @{insaddr:X8}");
				return lisaid;
			}
			else if (address == ChipRegs.SERDATR)
			{
				return serial.Read(insaddr, address);
			}
			if ((originalAddress >> 12)==0xdff)
				logger.LogTrace($"R UNMAPPED {ChipRegs.Name(address)} {originalAddress:X8} @{insaddr:X8}");
			//almost certainly, this is code using clr.x on a chip register.  you were TOLD not to do that!
			return 0;
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
				ushort prevIntena = intena;

				if ((value & 0x8000) != 0)
					intena |= (ushort)value;
				else
					intena &= (ushort)~value;

				if (((prevIntena ^ intena)&0x0020)!=0)
					logger.LogTrace($"VERTB {((intena&0x0020)!=0?"on":"off")} @ {insaddr:X8}");

				audio.WriteINTENA((ushort)(intena & 0x7fff));

				interrupt.SetPaulaInterruptLevel((uint)intreq & 0x7fff, (uint)intena & 0x7fff);
			}
			else if (address == ChipRegs.INTENAR)
			{
				/* can't write here */
			}
			else if (address == ChipRegs.INTREQ)
			{
				if ((value & 0x8000) != 0)
					intreq |= (ushort)value;
				else
					intreq &= (ushort)~value;

				audio.WriteINTREQ((ushort)(intreq & 0x7fff));
				serial.WriteINTREQ((ushort)(intreq & 0x7fff));

				interrupt.SetPaulaInterruptLevel((uint)intreq & 0x7fff, (uint)intena & 0x7fff);
			}
			else if (address == ChipRegs.INTREQR)
			{
				/* can't write here */
			}
			else if (address == ChipRegs.LISAID && settings.ChipSet == ChipSet.AGA)
			{
				/* can't write here on AGA */
			}
			else if (address == ChipRegs.VPOSR || address == ChipRegs.VHPOSR)
			{
				/* can't write here */
			}
			else if ((address >= ChipRegs.DDFSTRT && address <= ChipRegs.DDFSTOP) ||
				(address >= ChipRegs.BPL1PTH && address <= ChipRegs.BPL8PTL) ||
				address == ChipRegs.BPL1MOD || address == ChipRegs.BPL2MOD ||
				(address >= ChipRegs.BPL1DAT && address <= ChipRegs.SPR7DATB) ||
				address == ChipRegs.VPOSW || address == ChipRegs.VHPOSW ||
				address == ChipRegs.VTOTAL || address == ChipRegs.VBSTRT || address == ChipRegs.VBSTOP ||
				address == ChipRegs.HTOTAL || address == ChipRegs.HBSTRT || address == ChipRegs.HBSTOP ||
				address == ChipRegs.VSSTRT || address == ChipRegs.VSSTOP ||
				address == ChipRegs.HSSTRT || address == ChipRegs.HSSTOP || address == ChipRegs.HCENTER ||
				address == ChipRegs.BEAMCON0)
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
				        || (address >= ChipRegs.COLOR00 && address <= ChipRegs.COLOR31)
						|| address == ChipRegs.STREQU || address == ChipRegs.STRHOR
						|| address == ChipRegs.STRLONG || address == ChipRegs.STRVBL)
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
			else if (address == ChipRegs.DSKSYNC
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
				/* nothing happens */
			}
			else if (address == ChipRegs.SERDAT || address == ChipRegs.SERDATR || address == ChipRegs.SERPER)
			{
				serial.Write(insaddr, address, (ushort)value);
			}
			else
			{
				if ((originalAddress >> 12) == 0xdff)
					logger.LogTrace($"W UNMAPPED {ChipRegs.Name(address)} {originalAddress:X8} {value:X4} {value.ToBin()} @{insaddr:X8}");
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

		public uint ImmediateRead(uint insaddr, uint address, Size size)
		{
			return Read(insaddr, address, size);
		}

		public void ImmediateWrite(uint insaddr, uint address, uint value, Size size)
		{
			Write(insaddr, address, value, size);
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
			else if (address == ChipRegs.INTENA || address == ChipRegs.INTENAR)
			{
				return intena;
			}
			else if (address == ChipRegs.INTREQ || address == ChipRegs.INTREQR)
			{
				return intreq;
			}
			else if (address == ChipRegs.NO_OP)
			{
				return 0;
			}
			else if (address == ChipRegs.DMACONR || address == ChipRegs.DMACON)
			{
				return dma.DebugChipsetRead(address, size);
			}
			else if (address == ChipRegs.SERDATR || address == ChipRegs.SERDAT || address == ChipRegs.SERPER)
			{
				return serial.DebugChipsetRead(address, size);
			}

			logger.LogTrace($"DR {ChipRegs.Name(address)}");
			return 0;
		}

		public void Save(JArray obj)
		{
			var cr = new JObject();
			cr["id"] = "chipregs";
			var deets = ChipRegs.GetPersistanceDetails()
							.Where(x => x.Name != "COPJMP1" && x.Name != "COPJMP2")
							.Where(x => x.Name != "DMACON" && x.Name != "INTENA" && x.Name != "ADKCON" && x.Name != "INTREQ")
							.Where(x => x.Name != "BLTSIZE" && x.Name != "BLTSIZH")
							.Where(x => x.Name != "LISAID");
			foreach (var reg in deets)
				cr.Add(reg.Name, (ushort)DebugChipsetRead(reg.Address, Size.Word));
			obj.Add(cr);
		}

		public void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "chipregs")) return;
			obj.Remove("id");

			var deets = ChipRegs.GetPersistanceDetails().ToDictionary(x=>x.Name);
			foreach (var pair in obj)
			{
				//todo: really need to implement a DebugChipsetWrite(), since some of these
				//writes will just drop on the floor
				ushort value = ushort.Parse(pair.Value.ToString());
				if (pair.Key == "DMACONR")
					Write(0, ChipRegs.DMACON, (ushort)(value|0x8000), Size.Word);
				else if (pair.Key == "INTENAR")
					Write(0, ChipRegs.INTENA, (ushort)(value | 0x8000), Size.Word);
				else if (pair.Key == "INTREQR")
					Write(0, ChipRegs.INTREQ, (ushort)(value | 0x8000), Size.Word);
				else if (pair.Key == "ADKCONR")
					Write(0, ChipRegs.ADKCON, (ushort)(value | 0x8000), Size.Word);
				else
					Write(0, deets[pair.Key].Address, value, Size.Word);
			}
		}
	}
}
