using System;
using System.Collections.Generic;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IDE
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
		public const uint Data = 0x1f0;
		public const uint Error_Feature = 0x1f1;
		public const uint SectorCount = 0x1f2;
		public const uint SectorNumber = 0x1f3;
		public const uint CylinderLow = 0x1f4;
		public const uint CylinderHigh = 0x1f5;
		public const uint DriveHead = 0x1f6; //aka. DeviceHead
		public const uint Status_Command =  0x1f7;
		public const uint AltStatus_DevControl = 0x3f6;
	}

	[Flags]
	public enum IDE_STATUS : byte
	{
		ERR = 1,
		IDX = 2,
		CORR = 4,
		DRQ = 8,//data request ready
		DSC = 16,//drive seek complete
		DWF = 32,
		DRDY = 64,//drive ready
		BSY = 128
	}

	//IDE Controller on the A600, A1200 and A4000
	public abstract class IDEController : IIDEController, IDisposable
	{
		private readonly IInterrupt interrupt;
		private readonly EmulationSettings settings;
		private readonly ILogger logger;
		private readonly List<IHardDrive> hardDrives = new List<IHardDrive>();

		public IDEController(IEnumerable<IHardDrive> hardDrives, IInterrupt interrupt, IOptions<EmulationSettings> settings, ILogger<IDEController> logger)
		{
			this.interrupt = interrupt;
			this.settings = settings.Value;
			this.logger = logger;
			this.hardDrives.AddRange(hardDrives);
		}

		public void Reset()
		{
		}

		private readonly Dictionary<uint, string> registerNames = new Dictionary<uint, string>
		{
			{Gayle.Status, "Gayle.Status"},
			{Gayle.INTREQ, "Gayle.INTREQ"},
			{Gayle.INTENA, "Gayle.INTENA"},
			{Gayle.Config, "Gayle.Config"},

			{A4000.INTREQ, "A4000.INTREQ" },

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

		private byte gayleStatus;
		private GAYLE_INTENA gayleINTENA;
		private GAYLE_INTENA gayleINTREQ;
		private byte gayleConfig;

		private IHardDrive currentDrive;

		//registers
		private byte driveHead;
		private byte cylinderLow;
		private byte cylinderHigh;
		private byte sectorNumber;

		private byte sectorCount;
		private byte errorFeature;
		private IDE_STATUS ideStatus = IDE_STATUS.DRDY;

		//28 bit address LBA
		private uint LbaAddress
		{
			get
			{
				uint address = 0;
				address = (address & 0xf0ffffff) | (uint)((driveHead << 24) & 0x0f000000);
				address = (address & 0xff00ffff) | (uint)((cylinderHigh << 16) & 0x00ff0000);
				address = (address & 0xffff00ff) | (uint)((cylinderLow << 8) & 0x0000ff00);
				address = (address & 0xffffff00) | sectorNumber;
				return address;
			}
		}

		//28 bit address CHS
		private uint ChsAddress
		{
			get
			{
				int HPC = currentDrive.Heads;//always 16
				int SPT = currentDrive.Sectors;//always 63
				int C = cylinderLow + (cylinderHigh << 8);
				int H = driveHead & 0xf;
				int S = Math.Max((byte)1, sectorNumber);
				return (uint)((C * HPC + H) * SPT + (S - 1));
			}
		}

		private bool IsLBA()
		{
			return ((driveHead >> 6) & 1) != 0;
		}

		private uint CurrentAddress()
		{
			return IsLBA() ? LbaAddress : ChsAddress;
		}

		private void IncrementAddress(int count = 1)
		{
			if (count == 0) count = 256;
			while (count-- > 0)
			{
				if (IsLBA())
				{
					int nextSector = sectorNumber + 1;
					int nextCylinderLow = cylinderLow + (nextSector >> 8);
					int nextCylinderHigh = cylinderHigh + (nextCylinderLow >> 8);
					int nextHead = (driveHead & 0xf) + (nextCylinderHigh >> 8);

					sectorNumber = (byte)nextSector;
					cylinderLow = (byte)nextCylinderLow;
					cylinderHigh = (byte)nextCylinderHigh;
					driveHead = (byte)((driveHead & 0xf0) | (nextHead & 0x0f));
				}
				else
				{
					int nextSector = sectorNumber + 1;
					int nextHead = (driveHead & 0x0f) + (nextSector >> 6);
					int nextCylinderLow = cylinderLow + (nextHead >> 4);
					int nextCylinderHigh = cylinderHigh + (nextCylinderLow >> 8);

					sectorNumber = (byte)(nextSector == 64 ? 1 : nextSector);
					driveHead = (byte)((driveHead & 0xf0) | (nextHead & 0x0f));
					cylinderLow = (byte)nextCylinderLow;
					cylinderHigh = (byte)nextCylinderHigh;
				}
			}
		}

		public abstract uint Read(uint insaddr, uint address, Size size);

		public uint ReadATA(uint insaddr, uint address, Size size)
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

			//if (address != IDE.Data && address != Gayle.INTREQ && address != IDE.Status_Command)
			//	logger.LogTrace($"IDE Controller R {GetName(address)} {value:X2} status: {IdeStatus} drive: {(DriveHead >> 4) & 1} {address:X8} @{insaddr:X8} {size} ");

			return value;
		}

		public abstract void Write(uint insaddr, uint address, uint value, Size size);

		public void WriteATA(uint insaddr, uint address, uint value, Size size)
		{
			//if (address != IDE.Data) logger.LogTrace($"IDE Controller W {GetName(address)} {value:X2} status: {IdeStatus} drive: {(DriveHead >> 4) & 1} {address:X8} @{insaddr:X8} {size}");

			switch (address)
			{
				case Gayle.Status: gayleStatus = (byte)value; break;
				case Gayle.INTENA: gayleINTENA = (GAYLE_INTENA)value; UpdateInterrupt(); break;
				case Gayle.INTREQ: gayleINTREQ &= (GAYLE_INTENA)value; UpdateInterrupt(); break;
				case Gayle.Config: gayleConfig = (byte)value; break;

				case IDE.AltStatus_DevControl: DevControl((byte)value); break;
				case IDE.Status_Command: Command((byte)value); break;
				case IDE.Error_Feature: Feature((byte)value); break;
				case IDE.SectorCount: sectorCount = (byte)value; break;

				case IDE.DriveHead:
					currentDrive = hardDrives[(int)((value >> 4) & 1)];
					driveHead = (byte)value;
					break;
				case IDE.CylinderLow: cylinderLow = (byte)value; break;
				case IDE.CylinderHigh: cylinderHigh = (byte)value; break;
				case IDE.SectorNumber: sectorNumber = (byte)value; break;

				case IDE.Data: WriteDataWord((ushort)value); break;
			}

			//if (address == IDE.CylinderLow || address == IDE.CylinderHigh || address == IDE.SectorNumber || address == IDE.DriveHead)
			//	logger.LogTrace($"W {GetName(address)} {value:X2} {ChsAddress:X8}");
		}

		private void DevControl(byte value)
		{
			logger.LogTrace($"IDE DevControl {value}");
		}

		private void Feature(byte value)
		{
			logger.LogTrace($"IDE Feature {value}");
		}

		private const int INT_2 = 2;
		private const int INT_3 = 3;
		private const int INT_6 = 6;

		private void UpdateInterrupt()
		{
			uint intreq = 0;

			//logger.LogTrace($"INTENA {gayleINTENA}");
			//logger.LogTrace($"INTREQ {gayleINTREQ}");

			GAYLE_INTENA masked = gayleINTENA & gayleINTREQ & (GAYLE_INTENA)0xfc;
			if ((masked & GAYLE_INTENA.IRQ) != 0)
			{
				intreq |= 1 << INT_3;

				int bvd = (gayleINTENA & GAYLE_INTENA.CCSTATUS_BVDIRQ) != 0 ? INT_6 : INT_2;
				int bsy = (gayleINTENA & GAYLE_INTENA.BERR_BUSYIRQ) != 0 ? INT_6 : INT_2;

				if ((masked & GAYLE_INTENA.CC) != 0) intreq |= 1 << INT_6;
				if ((masked & GAYLE_INTENA.BVD2) != 0) intreq |= (uint)(1 << bvd);
				if ((masked & GAYLE_INTENA.BVD1) != 0) intreq |= (uint)(1 << bvd);
				if ((masked & GAYLE_INTENA.WR) != 0) intreq |= (uint)(1 << bsy);
				if ((masked & GAYLE_INTENA.BSY) != 0) intreq |= (uint)(1 << bsy);
			}

			interrupt.SetGayleInterruptLevel(intreq);
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

			//flag the BSY bit change interrupt
			//gayleINTREQ |= GAYLE_INTENA.BSY | GAYLE_INTENA.IRQ;
			gayleINTREQ |= GAYLE_INTENA.IRQ;
			//flag which drive it was
			//gayleINTREQ |= (GAYLE_INTENA)currentDrive.DriveIRQBit;

			UpdateInterrupt();
		}

		public void DebugAck()
		{
			var thisDrive = currentDrive;
			currentDrive = hardDrives[0];
			ClearBusy();
			currentDrive = hardDrives[1];
			ClearBusy();
			currentDrive = thisDrive;
		}

		private void Ack()
		{
			ClearBusy();
		}

		private class Transfer
		{
			public int WordCount { get; set; }
			public int SectorCount { get; set; }
			public TransferDirection Direction { get; }

			public Transfer(byte sectorCount, TransferDirection hostWrite)
			{
				this.SectorCount = sectorCount;
				if (this.SectorCount == 0) this.SectorCount = 256;
				Direction = hostWrite;
			}

			public enum TransferDirection
			{
				HostRead,
				HostWrite,
			}
		}

		private void NextWord()
		{
			currentTransfer.WordCount--;
			if (currentTransfer.WordCount <= 0)
			{
				if (currentTransfer.Direction == Transfer.TransferDirection.HostRead)
				{
					NextSector();
				}
				else if (currentTransfer.Direction == Transfer.TransferDirection.HostWrite)
				{
					Ack();
					NextSector();
				}
			}
		}

		private void NextSector()
		{
			if (currentTransfer.SectorCount <= 0)
			{
				//end of sectors

				if (currentTransfer.Direction == Transfer.TransferDirection.HostRead)
				{

				}
				else if (currentTransfer.Direction == Transfer.TransferDirection.HostWrite)
				{
					ideStatus &= ~IDE_STATUS.DRQ;
					currentDrive.SyncDisk();
				}
				IncrementAddress();
				currentTransfer = null;
			}
			else
			{
				IncrementAddress();

				ideStatus |= IDE_STATUS.DRQ;

				//next sector
				if (currentTransfer.Direction == Transfer.TransferDirection.HostRead)
				{
					//trigger an interrupt, the data is read and available
					Ack();
				}
				else if (currentTransfer.Direction == Transfer.TransferDirection.HostWrite)
				{
					//reset the write counter so an interrupt is generated when the last word is written
				}
				currentTransfer.WordCount = 256;
				currentTransfer.SectorCount--;
			}

		}

		private Transfer currentTransfer = null;

		private void Clr()
		{
			gayleINTREQ = 0;
			UpdateInterrupt();
		}

		private void Command(byte value)
		{
			if (currentDrive == null)
			{
				errorFeature |= 1 << 2;//ABRT
				ideStatus = IDE_STATUS.ERR;
				Ack();
				return;
			}

			ideStatus = IDE_STATUS.DRDY;

			string cmd;
			switch (value)
			{
				case var v when (v >= 0x10 && v <= 0x1f):
					cmd = "Recalibrate";
					cylinderLow = cylinderHigh = 0;
					ideStatus |= IDE_STATUS.DSC;
					Ack();
					break;

				case 0xEC:
					cmd = "Identify Drive";
					currentTransfer = new Transfer(1, Transfer.TransferDirection.HostRead);
					currentDrive.BeginRead(currentDrive.GetDriveId());
					NextSector();
					break;

				case 0x91:
					cmd = "Initialise Drive Parameters";
					currentDrive.ConfiguredParamsSectorsPerTrack = sectorCount;
					currentDrive.ConfiguredParamsHeads = (byte)((driveHead & 0xf) + 1);
					logger.LogTrace($"Drive Parameters spt: {currentDrive.ConfiguredParamsSectorsPerTrack} h: {currentDrive.ConfiguredParamsHeads}");
					Ack();
					break;

				case 0x20:
				case 0x21:
					cmd = "Read Sector(s)";
					currentTransfer = new Transfer(sectorCount, Transfer.TransferDirection.HostRead);
					//logger.LogTrace($"READ drv: {currentDrive.DiskNumber} cnt: {currentTransfer.SectorCount} LBA: {LbaAddress:X8} CHS: {ChsAddress:X8} lba: {(driveHead >> 6) & 1}");
					currentDrive.BeginRead(CurrentAddress(), sectorCount);
					NextSector();
					break;

				case 0x30:
				case 0x31:
					cmd = "Write Sector(s)";
					currentTransfer = new Transfer(sectorCount, Transfer.TransferDirection.HostWrite);
					//logger.LogTrace($"WRITE drv: {currentDrive.DiskNumber} cnt: {sectorCount} LBA: {LbaAddress:X8} CHS: {ChsAddress:X8} lba: {(driveHead >> 6) & 1}");
					currentDrive.BeginWrite(CurrentAddress());
					NextSector();
					break;

				case 0x40:
				case 0x41:
					cmd = "Verify Sector(s)";
					IncrementAddress(sectorCount);
					Ack();//all good :)
					break;

				case var v when (v >= 0x70 && v <= 0x7f):
					cmd = "Seek";
					ideStatus |= IDE_STATUS.DSC;
					Ack();
					break;

				//other mandatory commands not supported (do nothing)
				case 0x50:
					cmd = "Format Track";
					Ack();
					break;

				case 0x90:
					cmd = "Execute Drive Diagnostic";
					Ack();//all good here :)s
					break;

				case 0x22:
				case 0x23:
					cmd = "Read Long";
					Ack();
					break;

				case 0x32:
				case 0x33:
					cmd = "Write Long";
					Ack();
					break;

				//other non-mandatory commands not supported
				default:
					cmd = $"unknown command {value:X2} {value}";
					errorFeature = 1 << 2;//ABRT
					ideStatus |= IDE_STATUS.ERR;
					Ack();
					break;
			}

			//logger.LogTrace($"IDE Command {cmd} ${value:X2} {value} drive: {currentDrive.DiskNumber}");
		}

		private ushort ReadDataWord()
		{
			ushort v = currentDrive.Read();
			//logger.LogTrace($"R {v:X4}");
			NextWord();
			return v;
		}

		private void WriteDataWord(ushort data)
		{
			currentDrive.Write(data);
			//logger.LogTrace($"W {data:X4}");
			NextWord();
		}

		public void Dispose()
		{
			hardDrives[0].Dispose();
			hardDrives[1].Dispose();
		}
	}

	public class NullDiskController : IDiskController
	{
		private readonly MemoryRange emptyRange = new MemoryRange(0,0);

		public bool IsMapped(uint address) { return false; }
		public List<MemoryRange> MappedRange() { return new List<MemoryRange> {emptyRange}; }
		public uint Read(uint insaddr, uint address, Size size) { return 0; }
		public void Write(uint insaddr, uint address, uint value, Size size) { }
		public void Reset() { }
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
}