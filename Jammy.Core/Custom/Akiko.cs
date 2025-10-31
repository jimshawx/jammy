using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
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
		private enum INTREQ : uint
		{
			BIT_31 = 1u << 31,//$80000000 = Subcode interrupt (One subcode buffer filled and B0018.B has changed)
			BIT_30 = 1u << 30,//$40000000 = Drive has received all command bytes and executed the command(PIO only)
			BIT_29 = 1u << 29,//$20000000 = Drive has status data pending(PIO only)
			BIT_28 = 1u << 28,//$10000000 = Drive command DMA transmit complete(DMA only)
			BIT_27 = 1u << 27,//$08000000 = Drive status DMA receive complete(DMA only)
			BIT_26 = 1u << 26,//$04000000 = Drive data DMA complete
			BIT_25 = 1u << 25,//$02000000 = DMA overflow(lost data)?
		}

		private readonly string[] regNames = {
			"AKIKOID3",
			"AKIKOID2",
			"AKIKOID1",
			"AKIKOID0",
			"INTREQ3",
			"INTREQ2",
			"INTREQ1",
			"INTREQ0",
			"INTENA3",
			"INTENA2",
			"INTENA1",
			"INTENA0",
			"0c",
			"0d",
			"0e",
			"0f",
			"DMADATA3",
			"DMADATA2",
			"DMADATA1",
			"DMADATA0",
			"DMACMD3",
			"DMACMD2",
			"DMACMD1",
			"DMACMD0",
			"18",
			"TXDMAPOS(R)/END(W)",
			"RXDMAPOS(R)/END(W)",
			"1b",
			"1c",
			"TXDMAPOS(R)/END(W)",
			"1e",
			"RXDMAPOS(R)/END(W)",
			"DMAENA1",
			"DMAENA0",
			"22",
			"23",
			"CONFIG3",
			"CONFIG2",
			"CONFIG1",
			"CONFIG0",
			"PIO",
			"29",
			"2a",
			"2b",
			"2c",
			"2d",
			"2e",
			"2f",
			"NVRAM3",
			"NVRAM2",
			"NVRAM1",
			"NVRAM0",
			"34",
			"35",
			"36",
			"37",
			"C2P3",
			"C2P2",
			"C2P1",
			"C2P0",
			"3c",
			"3d",
			"3e",
			"3f",
			};

		private readonly EmulationSettings settings;
		private readonly ICDDrive cddrive;
		private IMemoryMapper memory;
		private readonly ILogger logger;

		public Akiko(ICDDrive cddrive, IOptions<EmulationSettings> settings, ILogger<Akiko> logger)
		{
			this.settings = settings.Value;
			this.cddrive = cddrive;
			
			this.logger = logger;
		}

		public void Init(IMemoryMapper memory)
		{
			this.memory = memory;
		}

		public bool IsMapped(uint address)
		{
			return mappedRange.Contains(address);
		}

		//Akiko registers are mapped between $b80000 to $b87fff and registers repeat after every 64 bytes
		//private readonly MemoryRange mappedRange = new MemoryRange(0xb80000, 0x8000);
		//hack - needs to be >= 64k for MemoryManager to work correctly
		private readonly MemoryRange mappedRange = new MemoryRange(0xb80000, 0x10000);

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {mappedRange};
		}

		private bool[] rmsg = new bool[64];
		private bool[] wmsg = new bool[64];

		private uint intreq = 0;
		private uint intena = 0;
		private uint dmadata = 0;
		private uint dmacmd = 0;
		private byte txDMAend = 0;
		private byte rxDMAend = 0;
		private byte txDMApos = 0;
		private byte rxDMApos = 0;
		private uint config = 0;
		private uint nvram = 0;
		private ushort dmaena = 0;
		private byte piorw = 0;

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint origaddress = address;
			uint v = 0;

			address &= 63;
			//rmsg[address] = true;
			switch (size)
			{
				case Size.Byte : 
					switch (address)
					{
						//AKIKO ID
						case 0x0: v = 0xC0; break;
						case 0x1: v = 0xCA; break;
						case 0x2: v = 0xCA; break;
						case 0x3: v = 0xFE; break;

						//INTREQ
						case 0x4: v = intreq >> 24; break;
						case 0x5: v = (intreq >> 16) & 0xff; break;
						case 0x6: v = (intreq >> 8) & 0xff; break;
						case 0x7: v = intreq & 0xff; break;

						//INTENA
						case 0x8: v = intena >> 24; break;
						case 0x9: v = (intena >> 16) & 0xff; break;
						case 0xA: v = (intena >> 8) & 0xff; break;
						case 0xB: v = intena & 0xff; break;

						//DMADATA
						case 0x10: v = dmadata >> 24; break;
						case 0x11: v = (dmadata >> 16) & 0xff; break;
						case 0x12: v = (dmadata >> 8) & 0xff; break;
						case 0x13: v = dmadata & 0xff; break;

						//DMACMD
						case 0x14: v = dmacmd >> 24; break;
						case 0x15: v = (dmacmd >> 16) & 0xff; break;
						case 0x16: v = (dmacmd >> 8) & 0xff; break;
						case 0x17: v = dmacmd & 0xff; break;

						case 0x1d: case 0x19: v = txDMApos; break;
						case 0x1f: case 0x1A: v = rxDMApos; break;

						//DMAENABLE
						case 0x20: v = (uint)(dmaena >> 8); break;
						case 0x21: v = dmaena &= 0xff; break;

						//CONFIG
						case 0x24: v = config >> 24; break;
						case 0x25: v = (config >> 16) & 0xff; break;
						case 0x26: v = (config >> 8) & 0xff; break;
						case 0x27: v = config & 0xff; break;

						//PIO
						case 0x28: v = piorw; break;

						//NVRAM
						case 0x30: v = nvram >> 24; break;
						case 0x31: v = (nvram >> 16) & 0xff; break;
						case 0x32: v = (nvram >> 8) & 0xff; break;
						case 0x33: v = nvram & 0xff; break;

						//C2P
						case 0x38: IncC2PRead(); v = planar[planarRead] >> 24; break;
						case 0x39: IncC2PRead(); v = (planar[planarRead] >> 16) & 0xff; break;
						case 0x3A: IncC2PRead(); v = (planar[planarRead] >> 8) & 0xff; break;
						case 0x3B: IncC2PRead(); v = planar[planarRead] & 0xff; break;

						default:
							throw new NotImplementedException($"Akiko Read not implemented for address {address:X8} size {size}");
					}
					break;
				case Size.Word:
					switch (address)
					{
						case 0x38: IncC2PRead(); v = planar[planarRead] >> 16; break;
						case 0x3A: IncC2PRead(); v = planar[planarRead] & 0xffff; break;
						default:
							v = (Read(insaddr, address, Size.Byte) <<8) | 
									Read(insaddr, address + 1, Size.Byte); break;
					}
					break;
				case Size.Long:
					switch (address)
					{
						case 0x38: IncC2PRead(); v = planar[planarRead]; break;
						default:
							v = (Read(insaddr, address, Size.Byte) <<24) |
									(Read(insaddr, address + 1, Size.Byte) <<16) | 
									(Read(insaddr, address + 2, Size.Byte) <<8) |
									Read(insaddr, address + 3, Size.Byte); break;
					}
					break;
			}

			if (origaddress >= 0xb80000 && !rmsg[address])
				logger.LogTrace($"R {regNames[address],-18} {origaddress:X8} {size} {v:X8} {v.ToBin()} @{insaddr:X8}");

			return v;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (address >= 0xb80000 && !wmsg[address & 63])
				logger.LogTrace($"W {regNames[address&63],-18} {address:X8} {size} {value:X8} {value.ToBin()} @{insaddr:X8}");

			address &= 63;
			//wmsg[address] = true;
			if (address < 4) return;//readonly Akiko ID
			if (address < 8) return;//readonly INTREQ
			switch (size)
			{
				case Size.Byte:
					switch (address)
					{
						//INTENA
						case 0x8: intena &= 0x00ffffff; intena |= value << 24; break;
						case 0x9: intena &= 0xff00ffff; intena |= value << 16; break;
						case 0xA: intena &= 0xffff00ff; intena |= value << 8; break;
						case 0xB: intena &= 0xffffff00; intena |= value; break;

						//DMADATA
						case 0x10: dmadata &= 0x00ffffff; dmadata |= value << 24; break;
						case 0x11: dmadata &= 0xff00ffff; dmadata |= value << 16; break;
						case 0x12: dmadata &= 0xffff00ff; dmadata |= value << 8; break;
						case 0x13: dmadata &= 0xffffff00; dmadata |= value; break;

						//DMACMD
						case 0x14: dmacmd &= 0x00ffffff; dmacmd |= value << 24; break;
						case 0x15: dmacmd &= 0xff00ffff; dmacmd |= value << 16; break;
						case 0x16: dmacmd &= 0xffff00ff; dmacmd |= value << 8; break;
						case 0x17: dmacmd &= 0xffffff00; dmacmd |= value; break;

						//
						case 0x18: intreq &= (uint)~INTREQ.BIT_31; break;

						case 0x1d: txDMAend = (byte)value; cddrive.SendCommand(TxCmd()); intreq &= (uint)~INTREQ.BIT_28; break;
						case 0x1f: rxDMAend = (byte)value; intreq &= (uint)~INTREQ.BIT_27; break;

						//DMAENABLE
						case 0x20: dmaena &= 0x00ff; dmaena |= (ushort)(value << 8); intreq &= (uint)~INTREQ.BIT_26; break;
						case 0x21: dmaena &= 0xff00; dmaena |= (ushort)value; intreq &= (uint)~INTREQ.BIT_26; break;

						//CONFIG
						case 0x24: config &= 0x00ffffff; config |= value << 24; break;
						case 0x25: config &= 0xff00ffff; config |= value << 16; break;
						case 0x26: config &= 0xffff00ff; config |= value << 8; break;
						case 0x27: config &= 0xffffff00; config |= value; break;
						
						//PIO
						case 0x28: piorw = (byte)value; break;

						//NVRAM
						case 0x30: nvram &= 0x00ffffff; nvram |= value << 24; break;
						case 0x31: nvram &= 0xff00ffff; nvram |= value << 16; break;
						case 0x32: nvram &= 0xffff00ff; nvram |= value << 8; break;
						case 0x33: nvram &= 0xffffff00; nvram |= value; break;

						//C2P
						case 0x38: chunky[chunkyWrite] &= 0x00ffffff; chunky[chunkyWrite] |= value << 24; IncC2PWrite(); break;
						case 0x39: chunky[chunkyWrite] &= 0xff00ffff; chunky[chunkyWrite] |= value << 16; break;
						case 0x3A: chunky[chunkyWrite] &= 0xffff00ff; chunky[chunkyWrite] |= value << 8; break;
						case 0x3B: chunky[chunkyWrite] = 0; break;//byte write to low byte clears the register

						default:
							throw new NotImplementedException($"Akiko Write not implemented for address {address:X8} size {size} value {value:X8}");
					}
					break;
				case Size.Word:
					switch (address)
					{
						case 0x38: chunky[chunkyWrite] &= 0x0000ffff; chunky[chunkyWrite] |= value << 16; IncC2PWrite(); break;
						case 0x3A: chunky[chunkyWrite] &= 0xffff0000; chunky[chunkyWrite] |= value; break;
						default:
							Write(insaddr, address, (byte)(value >> 8), Size.Byte);
							Write(insaddr, address+1, (byte)value, Size.Byte);
							break;
					}
					break;
				case Size.Long:
					switch (address)
					{
						case 0x38: chunky[chunkyWrite] = value; IncC2PWrite(); break;
						default:
							Write(insaddr, address, (byte)(value >> 24), Size.Byte);
							Write(insaddr, address + 1, (byte)(value >> 16), Size.Byte);
							Write(insaddr, address + 2, (byte)(value >> 8), Size.Byte);
							Write(insaddr, address + 3, (byte)value, Size.Byte);
							break;
					}
					break;
			}
		}

		private byte[] TxCmd()
		{
			var cmd = new byte[txDMAend];
			for (uint i = 0; i < txDMAend; i++)
				cmd[i] = (byte)memory.Read(0, dmacmd + i +512,Size.Byte);

			var sb = new StringBuilder();
			sb.AppendLine();
			sb.AppendLine("TX");
			for (uint j = 0; j < 8; j++)
			{ 
				for (uint i = 0; i < 32; i++)
				{
					sb.Append($"{memory.Read(0, dmacmd+i+j*32, Size.Byte):X2} ");
				}
				sb.AppendLine();
			}
			sb.AppendLine("SUB0");
			for (uint j = 0; j < 4; j++)
			{
				for (uint i = 0; i < 32; i++)
				{
					sb.Append($"{memory.Read(0, dmacmd + i + j * 32+256, Size.Byte):X2} ");
				}
				sb.AppendLine();
			}
			sb.AppendLine("SUB1");
			for (uint j = 0; j < 4; j++)
			{
				for (uint i = 0; i < 32; i++)
				{
					sb.Append($"{memory.Read(0, dmacmd + i + j * 32+384, Size.Byte):X2} ");
				}
				sb.AppendLine();
			}
			sb.AppendLine("RX");
			for (uint j = 0; j < 8; j++)
			{
				for (uint i = 0; i < 32; i++)
				{
					sb.Append($"{memory.Read(0, dmacmd + i + j * 32+512, Size.Byte):X2} ");
				}
				sb.AppendLine();
			}
			logger.LogTrace($"{sb.ToString()}");

			return cmd;
		}


		private readonly uint[] chunky = new uint[8];
		private readonly uint[] planar = new uint[8];
		private int chunkyWrite = 0;
		private int planarRead = -1;

		private void IncC2PWrite()
		{
			chunkyWrite++;
			chunkyWrite &= 7;
			planarRead = -1;//after any write, there must be a conversion before read
		}

		private void IncC2PRead()
		{
			if (planarRead == -1)
				planarConvert();
			planarRead++;
			planarRead &= 7;
			chunkyWrite = 0;//after any read, write starts from 0 again
		}

		private void planarConvert()
		{
			logger.LogTrace("C");
			for (int i = 0; i < 8; i++)
				logger.LogTrace($"{chunky[i].ToBin()}");

			for (int j = 7; j >= 0; j--)
			{
				for (int b = 0; b < 4; b++)
				{
					for (int i = 0; i < 8; i++)
					{
						planar[i] >>= 1;
						planar[i] |= (chunky[j] & 1) << 31;
						chunky[j] >>= 1;
					}
				}
			}

			//alternative
			//for (int k = 0; k < 256; k++)
			//{
			//	int i = k&7;
			//	int j = (k>>5)^7;

			//	planar[i] >>= 1;
			//	planar[i] |= (chunky[j] & 1) << 31;
			//	chunky[j] >>= 1;
			//}

			logger.LogTrace("to P");
			for (int i = 0; i < 8; i++)
				logger.LogTrace($"{planar[i].ToBin()}");
		}
	}

	/*
	 * // 0x18 mirrored to 0x10, 0x14 and 0x1c
	 * // 0x19 mirrored to 0x11, 0x15 and 0x1d
	 * // 0x1a mirrored to 0x12, 0x16 and 0x1e
	 * // 0x1b mirrored to 0x13, 0x17 and 0x1f
	 * 
	 * case 0x0c:
		case 0x0d:
		case 0x0e:
		case 0x0f:
			// read only duplicate of intena
	 * CD32 boot sequence
Jammy.Core.Custom.Akiko: Trace: W RXDMAPOS(R)/END(W) 00B8001F Byte 00000000 00000000000000000000000000000000 @00E57A24
Jammy.Core.Custom.Akiko: Trace: W TXDMAPOS(R)/END(W) 00B8001D Byte 00000000 00000000000000000000000000000000 @00E57A2A
Jammy.Core.Custom.Akiko: Trace: W DMADATA3			 00B80010 Long 00C20000 00000000110000100000000000000000 @00E57AD4
Jammy.Core.Custom.Akiko: Trace: W DMACMD3			 00B80014 Long 00CFFC00 00000000110011111111110000000000 @00E57B02
Jammy.Core.Custom.Akiko: Trace: R TXDMAPOS(R)/END(W) 00B80019 Byte 00000000 00000000000000000000000000000000 @00E57BAC
Jammy.Core.Custom.Akiko: Trace: R CONFIG3			 00B80024 Long 00000000 00000000000000000000000000000000 @00E57BB2
Jammy.Core.Custom.Akiko: Trace: W CONFIG3			 00B80024 Long 79000000 01111001000000000000000000000000 @00E57BC2
Jammy.Core.Custom.Akiko: Trace: R RXDMAPOS(R)/END(W) 00B8001A Byte 00000000 00000000000000000000000000000000 @00E57BC6
Jammy.Core.Custom.Akiko: Trace: W RXDMAPOS(R)/END(W) 00B8001F Byte 00000001 00000000000000000000000000000001 @00E57BD2
Jammy.Core.Custom.Akiko: Trace: W TXDMAPOS(R)/END(W) 00B8001D Byte 00000000 00000000000000000000000000000000 @00E57BE0
Jammy.Core.Custom.Akiko: Trace: W INTENA3			 00B80008 Long 18000000 00011000000000000000000000000000 @00E57BE6
Jammy.Core.Custom.Akiko: Trace: W TXDMAPOS(R)/END(W) 00B8001D Byte 00000003 00000000000000000000000000000011 @00E593B0
Jammy.Core.Custom.Akiko: Trace: R INTENA3			 00B80008 Long 18000000 00011000000000000000000000000000 @00E593DA
Jammy.Core.Custom.Akiko: Trace: W INTENA3			 00B80008 Long 18000000 00011000000000000000000000000000 @00E593DA
Jammy.Core.Custom.Akiko: Trace: W TXDMAPOS(R)/END(W) 00B8001D Byte 00000005 00000000000000000000000000000101 @00E593B0
Jammy.Core.Custom.Akiko: Trace: R INTENA3			 00B80008 Long 18000000 00011000000000000000000000000000 @00E593DA
Jammy.Core.Custom.Akiko: Trace: W INTENA3			 00B80008 Long 18000000 00011000000000000000000000000000 @00E593DA
	*/

}