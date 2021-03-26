using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class Gayle
	{
		public const uint Status = 0xda8000;
		public const uint INTREQ = 0xda9000;
		public const uint INTENA = 0xdaa000;
		public const uint Config = 0xdab000;
	}

	public class IDE 
	{
		public const uint Data = 0xda2000;//1f0
		public const uint Error_Feature = 0xda2004; //1f1
		public const uint SectorCount = 0xda2008;//1f2
		public const uint SectorNumber = 0xda200c;//1f3
		public const uint CylinderLow = 0xda2010;//1f4
		public const uint CylinderHigh = 0xda2014;//1f5
		public const uint DriveHead = 0xda2018;//1f6 //aka. DeviceHead
		public const uint Status_Command = 0xda201c; //1f7
		public const uint AltStatus_DevControl = 0xda3018; //3f7
	}

	[Flags]
	public enum IDE_STATUS : byte 
	{
		ERR=1,
		IDX=2,
		CORR=4,
		DRQ=8,//data request ready
		DSC=16,//drive seek complete
		DWF=32,
		DRDY=64,//drive ready
		BSY=128
	}

	//IDE Controller on the A600 and A1200
	public class IDEController : IIDEController
	{
		private readonly Dictionary<uint, string> registerNames = new Dictionary<uint, string>
		{
			{Gayle.Status, "Gayle.Status"},
			{Gayle.INTREQ, "Gayle.INTREQ"},
			{Gayle.INTENA, "Gayle.INTENA"},
			{Gayle.Config, "Gayle.Config"},

			{IDE.Data, "IDE.DATA"}, //1f0
			{IDE.Error_Feature, "IDE.Error_Feature"}, //1f1
			{IDE.SectorCount, "IDE.SectorCount"}, //1f2
			{IDE.SectorNumber, "IDE.SectorNumber"}, //1f3
			{IDE.CylinderLow, "IDE.CylinderLow"}, //1f4
			{IDE.CylinderHigh, "IDE.CylinderHigh"}, //1f5
			{IDE.DriveHead, "IDE.DriveHead"}, //1f6 //aka. DeviceHead
			{IDE.Status_Command, "IDE.Status_Command"}, //1f7
			{IDE.AltStatus_DevControl, "IDE.AltStatus_DevControl"} //3f7
		};

		private string GetName(uint address)
		{
			if (registerNames.TryGetValue(address, out var m))
				return m;
			return $"UNKNOWN_{address:X8}";
		}

		/*
http://eab.abime.net/showthread.php?t=23924

0xda0000	// Data
0xda0006	// Error | Feature
0xda000a	// Sector Count
0xda000e	// Sector Number
0xda0012	// Cylinder Low
0xda0016	// Cylinder High
0xda001a	// Device / Head
0xda001e	// Status | Command
0xda101a	// Control

0xda8000	// Gayle Status
0xda9000	// Gayle INTREQ
0xdaa000	// Gayle INTENA
0xdab000	// Gayle Config

0xda9000 Gayle INTREQ:
0x80 IDE
0X02 IDE1ACK (Slave)
0x01 IDE0ACK (Master)

If a Interrupt (Level 2) occurs and it is caused by an IDE Device Gayle INTREQ IDE bit 7 is set.
I'm not sure if, the corresponding IDExACK will be set. When done with interrupt handling these bits will be set to 0 by the device driver.

0xdaa000 Gayle INTENA:
0x80 IDE

Setting bit 7 of Gayle INTENA enables ATA Interrupts.

As usual for Amiga adresses are not fully decoded. Kickstart uses the following adresses for IDE.
The above mentioned adresses are used by Linux. If you want the project to be compatible with Linux you should implement a similiar incomplete decoding.

0xda2000 Data
0xda2004 Error | Feature
0xda2008 SectorCount
0xda200c SectorNumber
0xda2010 CylinderLow
0xda2014 CylinderHigh
0xda2018 Device/Head
0xda201c Status | Command
0xda3018 Control


0xde1000's MSB should actually be interpreted as a 8 bit serial shift register, which reads 0xd0.
Fat Garys (A3000,A4000) have the very same mechanism at 0xde1002 this register is called GaryID (see: http://www.thule.no/haynie/research/...ocs/a3000p.pdf).


The "Gayle-check" is:

write 00h to 0xde1000
read byte 0xde1000 with bit 7 set
read byte 0xde1000 with bit 7 set
read byte 0xde1000 with bit 7 cleared
read byte 0xde1000 with bit 7 set

D1: 0, A1: $DE1000
 FC0C22  7403                moveq.l   #3,d2
 FC0C24  1281                move.b    d1,(a1)
 FC0C26  1011                move.b    (a1),d0
 FC0C28  E308                lsl.b     #1,d0
 FC0C2A  D301                addx.b    d1,d1
 FC0C2C  51CA FFF8           dbra      d2,#$FC0C26(pc)
 FC0C30  0C01 000D           cmpi.b    #$D,d1

		 */

		/*
$DA3018 ; (read) alt status, doesn't clear int
$DA3018 ; (write) device control - int/reset

[table]

; auxiliary status register bit definitions
BITDEF WDC,INT,7 an interrupt is pending
BITDEF WDC,LCI,6 last command ignored
BITDEF WDC,BSY,5 chip is busy with a level 2 command
BITDEF WDC,CIP,4 command in progress
BITDEF WDC,PE,1 a parity error was detected
BITDEF WDC,DBR,0 data buffer ready during programmed I/O

; control register bit definitions
BITDEF WDC,DMA,7 DMA mode is enabled for data transfers
BITDEF WDC,WDB,6 direct buffer access for data transfers
BITDEF WDC,HA,1 halt on attention (target mode only)
BITDEF WDC,HPE,0 halt on parity error enable

; source ID register control bits
BITDEF WDC,ER,7 enable reselection
BITDEF WDC,ES,6 enable selection (target or multiple initiators only)
BITDEF WDC,SIV,3 source ID valid

; command register control bits
BITDEF WDC,SBT,7 enable single byte transfer mode

		 */
		private readonly IInterrupt interrupt;
		private readonly IChips custom;
		private readonly ILogger logger;
		private readonly MemoryRange memoryRange = new MemoryRange(0xda0000, 0x20000);

		public IDEController(IInterrupt interrupt, IChips custom, ILogger<IDEController> logger)
		{
			this.interrupt = interrupt;
			this.custom = custom;
			this.logger = logger;

			InitDriveId();
		}

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		/*
		DA .... .... ........ = IDE
		   x000 .... ........ = CS1, 8bit
		   x001 .... ........ = CS2, 8bit
		   x010 .... ........ = CS1, 16bit
		   x011 .... ........ = CS2, 16bit
		   x10x .... ........ = None, 8bit
		   x11x .... ........ = None, 16bit

0	->	DA2018 - device control DA0018
	<-	DA2010 - cylinder low
	<-	DA3018 - control
	<-	DA9000 - INTREQ

		 */

		private byte gayleStatus;
		private GAYLE_INTENA gayleINTENA;
		private GAYLE_INTENA gayleINTREQ;
		private byte gayleConfig;

		private IDE_STATUS ideStatus = IDE_STATUS.DRDY;
		private byte driveHead = 0;
		private byte cylinderLow = 0;
		private byte cylinderHigh = 0;
		private byte sectorNumber = 0;
		private byte sectorCount = 0;
		private byte errorFeature = 0;

		private uint lbaAddress = 0;//28 bit address, 

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint value = 0;
			switch (address)
			{
				case Gayle.Status: value = gayleStatus; break;
				case Gayle.INTENA: value = (byte)gayleINTENA; break;
				case Gayle.INTREQ: value = (byte)gayleINTREQ; break;
				case Gayle.Config: value = gayleConfig; break;

				case IDE.AltStatus_DevControl: value = (byte)ideStatus; break;
				case IDE.Status_Command: value = (byte)ideStatus; Clr(); break;
				case IDE.Error_Feature: value = errorFeature; break;

				case IDE.DriveHead: value = driveHead; break;
				case IDE.CylinderLow: value = cylinderLow; break;
				case IDE.CylinderHigh: value = cylinderHigh; break;
				case IDE.SectorNumber: value = sectorNumber; break;
				case IDE.SectorCount: value = sectorCount; break;

				case IDE.Data: value = ReadDataWord(); break;
			}

			if (address != IDE.Data) logger.LogTrace($"IDE Controller R {GetName(address)} {value:X2} {ideStatus} {address:X8} @{insaddr:X8} {size} ");

			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (address != IDE.Data) logger.LogTrace($"IDE Controller W {GetName(address)} {value:X2} {address:X8} @{insaddr:X8} {size}");

			switch (address)
			{
				case Gayle.Status: gayleStatus = (byte)value; break;
				case Gayle.INTENA: gayleINTENA = (GAYLE_INTENA)value; UpdateInterrupt(); break;
				case Gayle.INTREQ: gayleINTREQ &= (GAYLE_INTENA)value; UpdateInterrupt(); break;
				case Gayle.Config: gayleConfig = (byte)value; break;

				case IDE.AltStatus_DevControl: DevControl((byte)value); break;
				case IDE.Status_Command: Command((byte)value); break;
				case IDE.Error_Feature: Feature((byte)value); break;

				case IDE.DriveHead:
					driveHead = (byte)value;
					logger.LogTrace($"drive: {(driveHead>>4)&1} lba: {(driveHead>>6)&1}");
					lbaAddress = (lbaAddress & 0xf0ffffff) | ((value << 25) & 0x0f000000);
					break;
				case IDE.CylinderLow:
					cylinderLow = (byte)value;
					lbaAddress = (lbaAddress & 0xffff00ff) | ((value << 8) & 0x0000ff00);
					break;
				case IDE.CylinderHigh:
					cylinderHigh = (byte)value;
					lbaAddress = (lbaAddress & 0xff00ffff) | ((value << 16) & 0x00ff0000);
					break;
				case IDE.SectorNumber: 
					sectorNumber = (byte)value;
					lbaAddress = (lbaAddress & 0xffffff00) | (byte)value;
					break;
					
				case IDE.SectorCount: sectorCount = (byte)value; break;

				case IDE.Data: WriteDataWord((ushort)value); break;
			}
		}

		private void DevControl(byte value)
		{
			logger.LogTrace($"IDE DevControl {value}");
		}

		private void Feature(byte value)
		{
			logger.LogTrace($"IDE Feature {value}");
		}

		/*
		public const uint NMI = 15;
		public const uint INTEN = 14;
		public const uint EXTER = 13;
		public const uint DSKSYNC = 12;
		public const uint RBF = 11;
		public const uint AUD3 = 10;
		public const uint AUD2 = 9;
		public const uint AUD1 = 8;
		public const uint AUD0 = 7;
		public const uint BLIT = 6;
		public const uint VERTB = 5;
		public const uint COPPER = 4;
		public const uint PORTS = 3;
		public const uint TBE = 2;
		public const uint DSKBLK = 1;
		public const uint SOFTINT = 0;

		public static uint[] priority = new uint[]{ 1,1,1,2,3,3,3,4,4,4,4,5,5,6,6,7};
		 */

		//something that generates a CPU level interrupt
		private const int INT_2 = (int)Interrupt.PORTS;
		private const int INT_3 = (int)Interrupt.COPPER;
		private const int INT_6 = (int)Interrupt.EXTER;

		private void UpdateInterrupt()
		{
			uint intreq = 0;

			logger.LogTrace($"INTENA {(GAYLE_INTENA)gayleINTENA}");
			logger.LogTrace($"INTREQ {(GAYLE_INTENA)gayleINTREQ}");

			byte masked = (byte)(gayleINTENA & gayleINTREQ);
			if ((masked & (byte)GAYLE_INTENA.IRQ) != 0)
			{
				intreq |= 1 << INT_3;

				int bvd = (gayleINTENA & GAYLE_INTENA.CCSTATUS_BVDIRQ) != 0 ? INT_6 : INT_2;
				int bsy = (gayleINTENA & GAYLE_INTENA.BERR_BUSYIRQ) != 0 ? INT_6: INT_2;

				if ((masked & (byte)GAYLE_INTENA.CC) != 0) intreq |= 1 << INT_6;
				if ((masked & (byte)GAYLE_INTENA.BVD2) != 0) intreq |= (uint)(1 << bvd);
				if ((masked & (byte)GAYLE_INTENA.BVD1) != 0) intreq |= (uint)(1 << bvd);
				if ((masked & (byte)GAYLE_INTENA.WR) != 0) intreq |= (uint)(1 << bsy);
				if ((masked & (byte)GAYLE_INTENA.BSY) != 0) intreq |= (uint)(1 << bsy);

				custom.Write(0, ChipRegs.INTREQ, 0x8000|intreq, Size.Word);
			}
		}

		[Flags]
		public enum GAYLE_INTENA : byte
		{
			BERR_BUSYIRQ = 1,
			CCSTATUS_BVDIRQ = 2,
			BSY = 4,//busy changed
			WR = 8,
			BVD1 = 16,
			BVD2 = 32,
			CC = 64,
			IRQ = 128
		}

		private void ClearBusy()
		{
			ideStatus &= ~IDE_STATUS.BSY;
			ideStatus |= IDE_STATUS.DRDY;//drive is always ready

			gayleINTREQ |= GAYLE_INTENA.BSY | GAYLE_INTENA.IRQ;
			gayleINTREQ |= (GAYLE_INTENA)1;//Master
			//gayleINTREQ |= (GAYLE_INTENA)2;//Slave

			UpdateInterrupt();
		}

		public void Ack()
		{
			ClearBusy();
		}

		private void Clr()
		{
			gayleINTREQ = 0;
			UpdateInterrupt();
		}

		private void InitDriveId()
		{
			//cylinders * heads * sectors = number of blocks
			//cylinders * heads * sectors * 512 = size of disk
			//blocks per track = sectors
			//tracks per cylinder = heads
			//blocks per cylinder = sectors * heads
			//cylinders * blocks per cylinder = total number of blocks on the drive

			//let's say we want a 10MB Hard Disk

			uint sectorSize = 512;//common standard for sector size
			uint bytes = 10 * 1024 * 1024; // 10,485,760 bytes
			uint blocks = bytes / sectorSize; // 20480

			//uint heads = 16;//these are 4 bits in ATA spec for head number
			//uint sectors = 256;//there are 8 bits for sector number
			//uint cylindes = 65536;//these are 16 bits for cylinder number

			uint heads = 16;//maximum number of heads

			uint sectorCylinders = blocks / heads;//1280 = sectors * cylinders
			uint sectors = 64;//let's pick 64 sectors
			uint cylinders = sectorCylinders / sectors;

			//configuration
			driveId[0] = 1 << 6;//fixed drive

			//geometry
			driveId[1] = (ushort)cylinders;
			driveId[3] = (ushort)heads;
			driveId[4] = (ushort)(sectors * sectorSize);//sectors = blocks per track
			driveId[5] = (ushort)sectorSize;
			driveId[6] = (ushort)sectors;
			
			//vendor
			driveId[7] = 'J';
			driveId[8] = 'I';
			driveId[9] = 'M';

			//serial number
			driveId[10] = '3';
			driveId[11] = '.';
			driveId[12] = '1';
			driveId[13] = '4';
			driveId[14] = '1';
			driveId[15] = '5';
			driveId[16] = '9';
			driveId[17] = '2';
			driveId[18] = '6';
			driveId[19] = '5';

			//firmware revision
			driveId[23] = 24;
			driveId[24] = 06;
			driveId[26] = 19;
			driveId[27] = 72;

			//model number
			driveId[27] = 'J';
			driveId[28] = 'I';
			driveId[29] = 'M';
			driveId[30] = 'S';
			driveId[31] = 'H';
			driveId[32] = 'D';
			driveId[33] = 'J';
			driveId[34] = 'I';
			driveId[35] = 'M';
			driveId[36] = 'S';
			driveId[37] = 'H';
			driveId[38] = 'D';
			driveId[39] = 'J';
			driveId[40] = 'I';
			driveId[41] = 'M';
			driveId[42] = 'S';
			driveId[43] = 'H';
			driveId[44] = 'D';
			driveId[45] = 'J';
			driveId[46] = 'I';

			//supports LBA
			driveId[49] = 1 << 9;

			//LBA number of sectors
			uint total = cylinders * heads * sectors;
			driveId[60] = (ushort)total;
			driveId[61] = (ushort)(total >> 16);

			//it's little-endian, need to swap to big-endian for Amiga
			for (int i = 0; i < driveId.Length; i++)
				driveId[i] = (ushort)((driveId[i] >> 8) | (driveId[i] << 8));
		}

		//drive identification
		private readonly ushort[] driveId = new ushort[256]
		{
			0,// 0 general configuration
			0,// 1 number of cylinders
			0,// 2 reserved
			0,// 3 number of heads
			0,// 4 number of unformatted bytes per track
			0,// 5 number of unformatted bytes per sector
			0,// 6 number of sectors per track
			0,0,0,//7-9 vendor unique
			0,0,0,0,0,0,0,0,0,0,//10-19 serial number ASCII
			0,//20 buffer type
			0,//21 buffer size in 512 bytes increments (0 - not specified)
			0,//22 # of ECC byte available on read/write long commands (0 - not specified)
			0,0,0,0,//23-26 firmware revision
			0,0,0,0,0,0,0,0,0,0,//27-46 model number
			0,0,0,0,0,0,0,0,0,0,
			0,//47 bits 15-8 vendor unique, 7-0 00 = read/write multiple commands not implemented, xx = maximum # of sectors that can be transferred per interrupt
			0,//48 0 - cannot perform double-word IO
			0,//49 capabilities 15-10 reserved, 9 - LBA supported 1/0, 8 - DMA supported 1/0, 7-0 vendor unique
			0,//50 reserved
			0,//51 15-8 PIO data transfer cycle timing mode, 7-0 vendor unique
			0,//52 15-8 DMA data transfer cycle timing mode, 7-0 vendor unique
			0,//53 15-1 reserved, 0 - words 48-58 are valid 1/0
			0,//54 number of current cylinders
			0,//55 number of current heads
			0,//56 number of sectors per track
			0,0,//57-58 current capacity in sectors
			0,//59 15-9 reserved, 8 - multiple sector setting is valid 1/0, 7-0 xx = current setting for maximum number of settings transferred per interrupt
			0,0,//60-61 total number of addressable sectors (LBA mode only)
			0,//62 15-8 single word DMA transfer mode active, 7-0 single word DMA transfer modes supported
			0,//63 15-8 multiword DMA transfer mode active, 7-0 multiword DMA transfer modes supported
			
			0,0,0,0,0,0,0,0,//64-127 reserved
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,

			0,0,0,0,0,0,0,0,//128-159 vendor specific
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,

			0,0,0,0,0,0,0,0,//160-255 reserved
			0,0,0,0,0,0,0,0,//168
			0,0,0,0,0,0,0,0,//176
			0,0,0,0,0,0,0,0,//184
			0,0,0,0,0,0,0,0,//192
			0,0,0,0,0,0,0,0,//200
			0,0,0,0,0,0,0,0,//208
			0,0,0,0,0,0,0,0,//216
			0,0,0,0,0,0,0,0,//224
			0,0,0,0,0,0,0,0,//232
			0,0,0,0,0,0,0,0,//240
			0,0,0,0,0,0,0,0,//248
		};

		//drive geometry
		private byte sectorsPerTrack;
		private byte heads;

		private void Command(byte value)
		{
			ideStatus = IDE_STATUS.DRDY;

			string cmd;
			switch (value)
			{
				case 0x10:
					cmd = "Recalibrate";
					cylinderLow = cylinderHigh = 0;
					ideStatus |= IDE_STATUS.DSC;
					Ack();
					break;
				
				case 0xEC:
					cmd = "Identify Drive";
					ideStatus |= IDE_STATUS.DRQ;
					dataSource = driveId.AsEnumerable().GetEnumerator();
					Ack();
					break;

				case 0x91:
					cmd = "Initialise Drive Parameters";
					sectorsPerTrack = sectorCount;
					heads = driveHead;
					logger.LogTrace($"Drive Parameters sph: {sectorsPerTrack} h: {heads}");
					Ack();
					break;

				case 0x20:
					cmd = "Read Sector(s)";
					logger.LogTrace($"sector count {sectorCount} {lbaAddress}");
					ideStatus |= IDE_STATUS.DRQ;
					dataSource = new ushort[sectorCount * 256].AsEnumerable().GetEnumerator();
					Ack();
					break;

				default:
					cmd = "Unknown";
					break;
			}

			logger.LogTrace($"IDE Command {cmd} ${value:X2} {value}");
		}

		private IEnumerator<ushort> dataSource = null;

		private ushort ReadDataWord()
		{
			ushort v = 0xffff;
			if (dataSource != null)
			{
				if (!dataSource.MoveNext())
				{
					dataSource.Dispose();
					dataSource = null;
				}
				else
				{
					v = dataSource.Current;
				}
			}

			logger.LogTrace($"R {v:X4}");
			return v;
		}

		private void WriteDataWord(ushort data)
		{
			logger.LogTrace($"W {data:X4}");
		}

	}
}