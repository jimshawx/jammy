using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IDE
{
	//SCSI Controller on the A3000
	public class A3000SCSIController : IA3000SCSIController
	{
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;

		public A3000SCSIController(IInterrupt interrupt, ILogger<A3000SCSIController> logger)
		{
			this.interrupt = interrupt;
			this.logger = logger;
		}

		//A3000 and A3000T has Commodore's Super DMAC (SDMAC) in front of a WD33C93A SCSI chip.
		//https://datasheet4u.com/datasheets/Western-Digital/WD33C93A/501412

		private int sasrRegIndex;
		private byte[] sasrReg = new byte[256];
		private byte isr;

		private IMemoryMappedDevice sdmacRam = new MemoryUtility(0x100);

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint origAddress = address;

			uint value = 0;
			address &= 0xff;
			switch (address)
			{
				case 0x41:
				case 0x49:
					value = isr != 0 ? 0x80u : 0u;//status
					break;
				case 0x43:
					value = ScsiRead();
					break;
				case 0x1f:
					value = (uint)(isr | 1); break;
				default:
					value = sdmacRam.Read(0, address, size);
					break;
			}
			if (!(address == 0x1f && value == 1))
				logger.LogTrace($"SDMAC R {origAddress:X8} @{insaddr:X8} {size} -> {value:X8} {value}");
			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			uint origAddress = address;

			address &= 0xff;
			switch (address&0xff)
			{
				case 0x41:
				case 0x49:
					sasrRegIndex = (byte)value & 0x1f;
					break;
				case 0x43:
					ScsiWrite((byte)value);
					break;
				default:
					sdmacRam.Write(0, address, value, size);
					if (address == 0x0A || address == 0x0B)// CNTR (word at $0A covers $0B)
						Interrupt();
					break;
			}
			logger.LogTrace($"SDMAC W {origAddress:X8} @{insaddr:X8} {value:X8} {size}");

		}

		private byte ScsiRead()
		{
			byte value = sasrReg[sasrRegIndex];
			
			if (sasrRegIndex == 0x17)
			{
				isr = 0;
				value = 0x42;//timeout
			}
			AutoIncrement();
			logger.LogTrace($"SASR R {sasrRegIndex:X2} {value:X2}");
			return value;
		}

		private void ScsiWrite(byte value)
		{
			sasrReg[sasrRegIndex] = value;
			if (sasrRegIndex == 0x18)
			{
				isr = 0x50;
				Interrupt();
			}
			AutoIncrement();
			logger.LogTrace($"SASR W {sasrRegIndex:X2} {value:X2}");
		}

		private void AutoIncrement()
		{
			if (sasrRegIndex != 0x18 && sasrRegIndex != 0x19 && sasrRegIndex != 0x1f)
				sasrRegIndex = (sasrRegIndex + 1) & 0x1f;
		}

		private void Interrupt()
		{
			bool inten = (sdmacRam.Read(0, 0x0B, Size.Byte) & 0x04) != 0;
			if (inten && isr != 0)
				interrupt.AssertInterrupt(Types.Interrupt.PORTS);
		}

		public void Reset()
		{
		}
	}
}