using System.Collections.Generic;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

//From winaue.cpp
//https://github.com/tonioni/WinUAE/blob/master/akiko.cpp

/*
	B80000-B80003: $C0CACAFE (Read-only identifier)

	B80004.L: INTREQ (RO)
	B80008.L: INTENA (R/W)

	 31 $80000000 = Subcode interrupt (One subcode buffer filled and B0018.B has changed)
	 30 $40000000 = Drive has received all command bytes and executed the command (PIO only)
	 29 $20000000 = Drive has status data pending (PIO only)
	 28 $10000000 = Drive command DMA transmit complete (DMA only)
	 27 $08000000 = Drive status DMA receive complete (DMA only)
	 26 $04000000 = Drive data DMA complete
	 25 $02000000 = DMA overflow (lost data)?

	 INTREQ is read-only, each interrupt bit has different clearing method (see below).

	B80010.L: DMA data base address (R/W. Must be 64k aligned)

	B80014.L: Command/Status/Subcode DMA base. (R/W. Must be 1024 byte aligned)

	 Base + 0x000: 256 byte circular command DMA buffer. (Memory to drive)
	 Base + 0x100: Subcode DMA buffer (Doublebuffered, 2x128 byte buffers)
	 Base + 0x200: 256 byte circular status DMA buffer. (Drive to memory)

	B00018.B READ = Subcode DMA offset (ROM only checks if non-zero: second buffer in use)
	B00018.B WRITE = Clear subcode interrupt (bit 31)

	B0001D.B READ = Transmit DMA circular buffer current position.
	B0001D.B WRITE = Transmit DMA circular buffer end position.

	 If written value is different than current: transmit DMA starts and sends command bytes to drive until value matches end position.
	 Clears also transmit interrupt (bit 28)
	
	B0001E.B READ = Receive DMA circular buffer current position.
	B0001F.B WRITE = Receive DMA circular buffer end position.

	 If written value is different than current: receive DMA fills DMA buffer if drive has response data remaining, until value matches end position.
	 Clears also Receive interrupt (bit 27)

	B80020.W WRITE = DMA transfer block enable
	
	 Each bit marks position in DMA data address.
	 Bit 0 = DMA base address + 0
	 Bit 1 = DMA base address + 0x1000
	 ..
	 Bit 14 = DMA base address + 0xe000
	 Bit 15 = DMA base address + 0xf000

	 When writing, if bit is one, matching register bit gets set, if bit is zero, nothing happens, it is not possible to clear already set bits.
	 All one bit blocks (CD sectors) are transferred one by one. Bit 15 is always checked and processed first, then 14 and so on..
	 Interrupt is generated after each transferred block and matching register bit is cleared.

	 Writing to this register also clears INTREQ bit 26. Writing zero will only clear interrupt.
	 If CONFIG data transfer DMA enable is not active: register gets cleared and writes are ignored.

	 Structure of each block:

	 0-2: zeroed
	 3: low 5 bits of sector number
	 4 to 2352: 2348 bytes raw sector data (with first 4 bytes skipped)
	 0xc00: 146 bytes of CD error correction data?
	 The rest is unused(?).

	B80020.W READ = Read current DMA transfer status.

	B80024.L: CONFIG (R/W)

	 31 $80000000 = Subcode DMA enable
	 30 $40000000 = Command write (to CD) DMA enable
	 29 $20000000 = Status read (from CD) DMA enable
	 28 $10000000 = Memory access mode?
	 27 $08000000 = Data transfer DMA enable
	 26 $04000000 = CD interface enable?
	 25 $02000000 = CD data mode?
	 24 $01000000 = CD data mode?
	 23 $00800000 = Akiko internal CIA faked vsync rate (0=50Hz,1=60Hz)
	 00-22 = unused

	B80028.B WRITE = PIO write (If CONFIG bit 30 off). Clears also interrupt bit 30.
	B80028.B READ = PIO read (If CONFIG bit 29 off). Clears also interrupt bit 29 if no data available anymore.

	B80030.B NVRAM I2C IO. Bit 7 = SCL, bit 6 = SDA
	B80032.B NVRAM I2C DIRECTION. Bit 7 = SCL direction, bit 6 = SDA direction)

	B80038.L C2P

	Commands:

	1 = STOP

	 Size: 1 byte
	 Returns status response

	2 = PAUSE

	 Size: 1 byte
	 Returns status response

	3 = UNPAUSE

	 Size: 1 byte
	 Returns status response
	
	4 = PLAY/READ

	 Size: 12 bytes
	 Response: 2 bytes

	5 = LED (2 bytes)

	 Size: 2 bytes. Bit 7 set in second byte = response wanted.
	 Response: no response or 2 bytes. Second byte non-zero: led is currently lit.

	6 = SUBCODE

	 Size: 1 byte
	 Response: 15 bytes

	7 = INFO

	 Size: 1 byte
	 Response: 20 bytes (status and firmware version)

	Common status response: 2 bytes
	Status second byte bit 7 = Error, bit 3 = Playing, bit 0 = Door closed.

	First byte of command is combined 4 bit counter and command code.
	Command response's first byte is same as command.
	Counter byte can be used to match command with response.
	Command and response bytes have checksum byte appended.

*/

namespace Jammy.Core.Custom
{
	public class Akiko : IAkiko
	{
		private readonly EmulationSettings settings;
		private readonly ILogger logger;

		public readonly byte[] registers = new byte[64];

		public Akiko(IOptions<EmulationSettings> settings, ILogger<Akiko> logger)
		{
			this.settings = settings.Value;
			this.logger = logger;

			//Akiko is identified by 0xC0CACAFE at register 0
			registers[0] = 0xC0;
			registers[1] = 0xCA;
			registers[2] = 0xCA;
			registers[3] = 0xFE;
		}

		public bool IsMapped(uint address)
		{
			if (settings.ChipSet != ChipSet.CD32) return false;
			return mappedRange.Contains(address);
		}

		//Akiko registers are mapped between $b80000 to $b87fff and registers repeat after every 64 bytes
		private readonly MemoryRange mappedRange = new MemoryRange(0xb80000, 0x8000);

		public List<MemoryRange> MappedRange()
		{
			if (settings.ChipSet != ChipSet.CD32) return new List<MemoryRange>();
			return new List<MemoryRange> {mappedRange};
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			logger.LogTrace($"Akiko Read {address:X8} @{insaddr:X8} {size}");

			address &= 63;
			switch (size)
			{
				case Size.Byte : return registers[address];
				case Size.Word : return (uint)(registers[address]<<8) | (uint)registers[address+1];
				case Size.Long: return (uint)(registers[address]<<24) | (uint)(registers[address+1]<<16) | (uint)(registers[address+2]<<8) | registers[address+3];
			}

			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Akiko Write {address:X8} @{insaddr:X8} {value:X8} {size}");
			switch (size)
			{
				case Size.Byte:
					registers[address] = (byte)value;
					return;
				case Size.Word:
					registers[address] = (byte)(value>>8);
					registers[address + 1] = (byte)value;
					return;
				case Size.Long:
					registers[address] = (byte)(value >> 24);
					registers[address + 1] = (byte)(value>>16);
					registers[address + 2] = (byte)(value >> 8);
					registers[address + 3] = (byte)value;
					return;
			}
		}
	}
}