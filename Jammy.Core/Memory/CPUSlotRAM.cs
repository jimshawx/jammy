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
	public class CPUSlotRAM : Memory, ICPUSlotRAM
	{
		//A3000/A4000 CPU Slot Fast RAM
		//Up to 128MB mapped from 0x08000000-0x07ffffff

		public CPUSlotRAM(IOptions<EmulationSettings> settings, ILogger<CPUSlotRAM> logger)
		{
			if (settings.Value.CPUSlotMemory != 0.0)
			{
				if (settings.Value.CPUSlotMemory > 128.0f) settings.Value.CPUSlotMemory = 128.0f;

				uint memorySize = (uint)(settings.Value.CPUSlotMemory * 1024 * 1024);
				memoryRange = new MemoryRange(0x08000000, memorySize);
				memory = new byte[memorySize];
				addressMask = memorySize - 1;
			}
			else
			{
				memoryRange = new MemoryRange(0, 0);
				memory = new byte[0];
				addressMask = 0;
			}
		}
	}
}
