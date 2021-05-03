using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.IDE
{
	//SCSI/IDE Controller on the A3000 and A4000
	public class SCSIController : ISCSIController
	{
		private readonly ILogger logger;
		private readonly MemoryRange memoryRange = new MemoryRange(0xdd0000, 0);

		public SCSIController(ILogger<SCSIController> logger)
		{
			this.logger = logger;
		}

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange};
		}

		//A4000 does this at boot 10x
		//$dd203A W 0
		//$dd2032 R
		//$dd203e R

		public uint Read(uint insaddr, uint address, Size size)
		{
			logger.LogTrace($"SCSI Controller Read {address:X8} @{insaddr:X8} {size}");
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"SCSI Controller Write {address:X8} @{insaddr:X8} {value:X8} {size}");
		}
	}
}