using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{

	public class TrapdoorRAM : Memory, ITrapdoorRAM
	{
		//AKA Slow-fast RAM
		//Up to 1.75MB mapped from 0xC00000-0xDC0000
		//detected by looking for mirrors of custom registers

		public TrapdoorRAM(IOptions<EmulationSettings> settings, ILogger<TrapdoorRAM> logger)
		{
			if (settings.Value.TrapdoorMemory != 0.0)
			{
				uint trapdoorSize = (uint)(settings.Value.TrapdoorMemory * 1024 * 1024);
				memoryRange = new MemoryRange(0xC00000, trapdoorSize);
				memory = new byte[trapdoorSize];
				addressMask = trapdoorSize - 1;
			}
			else
			{
				memoryRange = new MemoryRange(0, 0);
			}
		}
	}
}