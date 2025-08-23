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

		private HashSet<uint> unmappedReads = new HashSet<uint>();

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

			switch (address)
			{
				case ChipRegs.VPOSR:
				case ChipRegs.VHPOSR:
					return agnus.Read(insaddr, address);
				case ChipRegs.CLXDAT:
				case ChipRegs.STREQU:
				case ChipRegs.STRHOR:
				case ChipRegs.STRLONG:
				case ChipRegs.STRVBL:
					return denise.Read(insaddr, address);
				case ChipRegs.BLTDDAT:
					return blitter.Read(insaddr, address);
				case ChipRegs.COPJMP1:
				case ChipRegs.COPJMP2:
					return copper.Read(insaddr, address);
				case ChipRegs.DSKDATR:
				case ChipRegs.DSKBYTR:
					return diskDrives.Read(insaddr, address);
				case ChipRegs.JOY0DAT:
				case ChipRegs.JOY1DAT:
				case ChipRegs.POTGOR:
				case ChipRegs.POT0DAT:
				case ChipRegs.POT1DAT:
					return mouse.Read(insaddr, address);
				case ChipRegs.ADKCONR:
					return (ushort)(audio.Read(insaddr, address) | diskDrives.Read(insaddr, address));
				case ChipRegs.DMACONR:
					return dma.Read(insaddr, address);
				case ChipRegs.INTENAR:
					return (uint)(intena & 0x7fff);
				case ChipRegs.INTREQR:
					return (uint)(intreq & 0x7fff);
				case ChipRegs.LISAID:
					logger.LogTrace($"LISAID Check @{insaddr:X8}");
					return lisaid;
				case ChipRegs.SERDATR:
					return serial.Read(insaddr, address);
			}

			if ((originalAddress >> 12) == 0xdff)
			{
				if (unmappedReads.Add(insaddr))
					logger.LogTrace($"R UNMAPPED {ChipRegs.Name(address)} {originalAddress:X8} @{insaddr:X8}");
			}
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

			switch (address)
			{
				case ChipRegs.DMACON:
					dma.Write(insaddr, address, (ushort)value);
					break;
				case ChipRegs.DMACONR:
					break;
				case ChipRegs.INTENA:
					{
						ushort prevIntena = intena;

						if ((value & 0x8000) != 0)
							intena |= (ushort)value;
						else
							intena &= (ushort)~value;

						if (((prevIntena ^ intena) & 0x0020) != 0)
							logger.LogTrace($"VERTB {((intena & 0x0020) != 0 ? "on" : "off")} @ {insaddr:X8}");

						audio.WriteINTENA((ushort)(intena & 0x7fff));

						interrupt.SetPaulaInterruptLevel((uint)intreq & 0x7fff, (uint)intena & 0x7fff);
						break;
					}

				case ChipRegs.INTENAR:
					break;
				case ChipRegs.INTREQ:
					if ((value & 0x8000) != 0)
						intreq |= (ushort)value;
					else
						intreq &= (ushort)~value;

					//if ((value & 0x0002) != 0)
					//	logger.LogTrace($"DSKBLK set {((intreq & 0x0002) != 0 ? "on" : "off")} @ {insaddr:X8}");

					audio.WriteINTREQ((ushort)(intreq & 0x7fff));
					serial.WriteINTREQ((ushort)(intreq & 0x7fff));

					interrupt.SetPaulaInterruptLevel((uint)intreq & 0x7fff, (uint)intena & 0x7fff);
					break;
				case ChipRegs.INTREQR:
					break;
				case ChipRegs.LISAID when settings.ChipSet == ChipSet.AGA:
					break;
				case ChipRegs.VPOSR:
				case ChipRegs.VHPOSR:
					break;
				case >= ChipRegs.DDFSTRT and <= ChipRegs.DDFSTOP:
				case >= ChipRegs.BPL1PTH and <= ChipRegs.BPL8PTL:
				case ChipRegs.BPL1MOD:
				case ChipRegs.BPL2MOD:
				case >= ChipRegs.BPL1DAT and <= ChipRegs.SPR7DATB:
				case ChipRegs.VPOSW:
				case ChipRegs.VHPOSW:
				case ChipRegs.VTOTAL:
				case ChipRegs.VBSTRT:
				case ChipRegs.VBSTOP:
				case ChipRegs.HTOTAL:
				case ChipRegs.HBSTRT:
				case ChipRegs.HBSTOP:
				case ChipRegs.VSSTRT:
				case ChipRegs.VSSTOP:
				case ChipRegs.HSSTRT:
				case ChipRegs.HSSTOP:
				case ChipRegs.HCENTER:
				case ChipRegs.BEAMCON0:
					agnus.Write(insaddr, address, (ushort)value);
					break;
				case ChipRegs.DIWSTRT:
				case ChipRegs.DIWSTOP:
				case ChipRegs.DIWHIGH:
				case ChipRegs.BPLCON0:
				case ChipRegs.FMODE:
					agnus.Write(insaddr, address, (ushort)value);
					denise.Write(insaddr, address, (ushort)value);
					break;
				case ChipRegs.BPLCON1:
				case ChipRegs.BPLCON2:
				case ChipRegs.BPLCON3:
				case ChipRegs.BPLCON4:
				case ChipRegs.CLXCON:
				case ChipRegs.CLXCON2:
				case >= ChipRegs.COLOR00 and <= ChipRegs.COLOR31:
				case ChipRegs.STREQU:
				case ChipRegs.STRHOR:
				case ChipRegs.STRLONG:
				case ChipRegs.STRVBL:
					denise.Write(insaddr, address, (ushort)value);
					break;
				case >= ChipRegs.COP1LCH and <= ChipRegs.COPINS:
				case ChipRegs.COPCON:
					copper.Write(insaddr, address, (ushort)value);
					break;
				case >= ChipRegs.BLTCON0 and <= ChipRegs.BLTADAT:
				case ChipRegs.BLTDDAT:
					blitter.Write(insaddr, address, (ushort)value);
					break;
				case ChipRegs.ADKCON:
					diskDrives.Write(insaddr, address, (ushort)value);
					audio.Write(insaddr, address, (ushort)value);
					break;
				case ChipRegs.ADKCONR:
					break;
				case ChipRegs.DSKSYNC:
				case ChipRegs.DSKPTH:
				case ChipRegs.DSKPTL:
				case ChipRegs.DSKLEN:
				case ChipRegs.DSKDAT:
					diskDrives.Write(insaddr, address, (ushort)value);
					break;
				case ChipRegs.JOY0DAT:
				case ChipRegs.JOY1DAT:
				case ChipRegs.POTGO:
				case ChipRegs.POTGOR:
				case ChipRegs.POT0DAT:
				case ChipRegs.POT1DAT:
				case ChipRegs.JOYTEST:
					mouse.Write(insaddr, address, (ushort)value);
					break;
				case >= ChipRegs.AUD0LCH and <= ChipRegs.AUD3DAT:
					audio.Write(insaddr, address, (ushort)value);
					break;
				case ChipRegs.NO_OP:
					break;
				case ChipRegs.SERDAT:
				case ChipRegs.SERDATR:
				case ChipRegs.SERPER:
					serial.Write(insaddr, address, (ushort)value);
					break;
				default:
					if (originalAddress >> 12 == 0xdff)
						logger.LogTrace($"W UNMAPPED {ChipRegs.Name(address)} {originalAddress:X8} {value:X4} {value.ToBin()} @{insaddr:X8}");
					break;
			}
		}

		public void WriteWide(uint address, ulong value)
		{
			uint originalAddress = address;
			address |= 0xdf0000;
			address &= 0xdffffe;

			if ((address >= ChipRegs.BPL1DAT && address <= ChipRegs.BPL8DAT) ||
				(address >= ChipRegs.SPR0DATA && address <= ChipRegs.SPR7DATB))
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
