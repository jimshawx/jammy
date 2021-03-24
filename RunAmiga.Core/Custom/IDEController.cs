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

	//IDE Controller on the A600 and A1200
	public class IDEController : IIDEController
	{
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

		private readonly ILogger logger;
		private readonly MemoryRange memoryRange = new MemoryRange(0xda0000, 0x20000);

		public IDEController(ILogger<IDEController> logger)
		{
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

		private ushort gayleStatus;
		private ushort gayleINTENA;
		private ushort gayleINTREQ;
		private ushort gayleConfig;

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint value = 0;
			switch (address)
			{
				case Gayle.Status: value = gayleStatus; break;
				case Gayle.INTENA: value = gayleINTENA; break;
				case Gayle.INTREQ: value = gayleINTREQ; break;
				case Gayle.Config: value = gayleConfig; break;
			}

			logger.LogTrace($"IDE Controller Read {address:X8} @{insaddr:X8} {value>>8:X2}{size}");

			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			switch (address)
			{
				case Gayle.Status: gayleStatus = (ushort)value; break;
				case Gayle.INTENA: gayleINTENA = (ushort)value; break;
				case Gayle.INTREQ: gayleINTREQ = (ushort)value; break;
				case Gayle.Config: gayleConfig = (ushort)value; break;
			}

			logger.LogTrace($"IDE Controller Write {address:X8} @{insaddr:X8} {value:X8} {size}");
		}
	}
}