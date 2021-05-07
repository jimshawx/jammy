using System;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Core.Memory
{

	public class ChipRAM : Memory, IChipRAM
	{
		//Up to 2MB Mapped from 0x0 to 0x00200000
		//Detected by writing 0 to location 0x00000000 and then writing signature long every 4KB
		//until address 0 is overwritten caused by incomplete address decoding causing a wrap

		public ChipRAM(IOptions<EmulationSettings> settings, ILogger<ChipRAM> logger)
		{
			uint chipSize = (uint)(Math.Max(settings.Value.ChipMemory, 0.5) * 1024 * 1024);
			
			//chip RAM is just mirrored across the first 2MB
			memory = new byte[chipSize];
			addressMask = chipSize - 1;
			memoryRange = new MemoryRange(0, 0x200000);
		}
	}
}