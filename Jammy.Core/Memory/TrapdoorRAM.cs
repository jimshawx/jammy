using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
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
				if (settings.Value.TrapdoorMemory > 1.75) settings.Value.TrapdoorMemory = 1.75f;

				uint trapdoorSize = (uint)(settings.Value.TrapdoorMemory * 1024 * 1024);
				memoryRange = new MemoryRange(0xC00000, trapdoorSize);
				memory = new byte[trapdoorSize];
				addressMask = trapdoorSize - 1;
			}
			else
			{
				//allow for chip register shadows to fill the space
				memoryRange = new MemoryRange(0, 0);
				memory = new byte[0];
				addressMask = 0;
			}
		}
	}
}
