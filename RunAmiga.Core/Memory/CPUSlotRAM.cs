using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{
	public class CPUSlotRAM : Memory, ICPUSlotRAM
	{
		//A3000/A4000 CPU Slot Fast RAM
		//Up to 128MB mapped from 0x08000000-0x07ffffff

		public CPUSlotRAM(IOptions<EmulationSettings> settings, ILogger<MotherboardRAM> logger)
		{
			if (settings.Value.CPUSlotMemory != 0.0)
			{
				if (settings.Value.CPUSlotMemory > 128.0f) settings.Value.CPUSlotMemory = 128.0f;

				uint memorySize = (uint)(settings.Value.CPUSlotMemory * 1024 * 1024);
				memoryRange = new MemoryRange(0x08000000, memorySize);
				memory = new byte[memorySize];
				addressMask = memorySize - 1;
			}
		}
	}
}
