using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
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
		DRQ=8,
		DSC=16,
		DWF=32,
		DRDY=64,
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
		private readonly ILogger logger;
		private readonly MemoryRange memoryRange = new MemoryRange(0xda0000, 0x20000);

		public IDEController(IInterrupt interrupt, ILogger<IDEController> logger)
		{
			this.interrupt = interrupt;
			this.logger = logger;
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
		trying to do something...

RunAmiga.Core.Custom.IDEController: Trace: IDE Controller Write 00DA2018 @00F88238 00000000 Byte
RunAmiga.Core.Custom.IDEController: Trace: IDE Controller Read 00DA2010 @00F8823C Byte
RunAmiga.Core.Custom.IDEController: Trace: IDE Controller Read 00DA3018 @00F88240 Byte
RunAmiga.Core.Custom.IDEController: Trace: IDE Controller Read 00DA9000 @00FC120C Byte
RunAmiga.Core.Custom.IDEController: Trace: IDE Controller Read 00DA9000 @00FC120C Byte
RunAmiga.Core.Custom.IDEController: Trace: IDE Controller Read 00DA9000 @00FC120C Byte
		 */

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
		private byte gayleINTENA;
		private byte gayleINTREQ;
		private byte gayleConfig;

		private byte ideStatus = 0;
		private byte driveHead = 0;
		private byte cylinderLow = 0;
		private byte cylingderHigh = 0;

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(insaddr, 0);

			uint value = 0;
			switch (address)
			{
				case Gayle.Status: value = gayleStatus; break;
				case Gayle.INTENA: value = gayleINTENA; break;
				case Gayle.INTREQ: value = gayleINTREQ; break;
				case Gayle.Config: value = gayleConfig; break;

				case IDE.AltStatus_DevControl: value = ideStatus; break;
				case IDE.Status_Command: value = ideStatus; Clr(); break;

				case IDE.DriveHead: value = driveHead; break;
				case IDE.CylinderLow: value = cylinderLow; break;
				case IDE.CylinderHigh: value = cylingderHigh; break;
			}

			logger.LogTrace($"IDE Controller R {GetName(address)} {address:X8} @{insaddr:X8} {value:X8} {size}");

			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(insaddr,0);

			logger.LogTrace($"IDE Controller W {GetName(address)} {address:X8} @{insaddr:X8} {value:X8} {size}");

			switch (address)
			{
				case Gayle.Status: gayleStatus = (byte)value; break;
				case Gayle.INTENA: gayleINTENA = (byte)value; break;
				case Gayle.INTREQ: gayleINTREQ = (byte)value; break;
				case Gayle.Config: gayleConfig = (byte)value; break;
				case IDE.DriveHead: Ack(); driveHead = (byte)value; break;
				case IDE.CylinderLow: Ack(); cylinderLow = (byte)value; break;
				case IDE.CylinderHigh: Ack(); cylingderHigh = (byte)value; break;
				case IDE.AltStatus_DevControl: break;

				case IDE.Status_Command: Command((byte)value); break;
			}
		}

		private void BsyInterrupt()
		{
			interrupt.AssertInterrupt((gayleINTENA & 1) == 0 ? Interrupt.TBE : Interrupt.BLIT);
		}

		private void BvdInterrupt()
		{
			interrupt.AssertInterrupt((gayleINTENA & 2) == 0 ? Interrupt.TBE : Interrupt.BLIT);
		}

		private void Ack()
		{
			gayleINTREQ |= 0x81;//flag gayle interrupt
			BsyInterrupt();
			ideStatus &= (byte)~IDE_STATUS.BSY;
			ideStatus |= (byte)IDE_STATUS.DRDY;
			ideStatus |= (byte)IDE_STATUS.DRQ;//always ready
		}

		private void Clr()
		{
			//clear IDE interrupt
			//todo:
			//clear Gayle interrupt
			gayleINTREQ &= 0x7e;
		}

		private void Command(byte value)
		{
			string cmd;
			switch (value)
			{
				case 0x10: cmd = "Recalibrate"; Ack(); break;
				case 0x48: cmd = "???"; Ack(); break;
				default: cmd = "Unknown"; break;
			}

			logger.LogTrace($"IDE Command {cmd} {value}");

		}

	}
}