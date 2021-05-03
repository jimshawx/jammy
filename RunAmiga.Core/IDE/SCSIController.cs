using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.IDE
{
	//SCSI/IDE Controller on the A3000 and A4000
	public class SCSIController : ISCSIController
	{
		private readonly ILogger logger;

		public SCSIController(ILogger<SCSIController> logger)
		{
			this.logger = logger;
		}

		//A4000 does this at boot 10x
		//$dd203A W 0
		//$dd2032 R
		//$dd203e R
		private byte reg_dd2032 = 0;
		public uint Read(uint insaddr, uint address, Size size)
		{
			logger.LogTrace($"SCSI Controller Read {address:X8} @{insaddr:X8} {size}");
			if (address == 0xdd203a) return 0;
			if (address == 0xdd203e) return 0b01111110;
			if (address == 0xdd2032) return reg_dd2032;
			if (address == 0xdd3020) return 0x8000;
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"SCSI Controller Write {address:X8} @{insaddr:X8} {value:X8} {size}");
			if (address == 0xdd2032) reg_dd2032 = (byte)value;
		}

		public void Reset()
		{
		}
	}
}