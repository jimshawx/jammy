using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Core.Memory
{
	public class MotherboardRAM : Memory, IMotherboardRAM
	{
		//A3000/A4000 Motherboard Fast RAM
		//Up to 16MB mapped from 0x07000000-0x07ffffff
		//or 64MB mapped from    0x04000000-0x07ffffff, not available on actual motherboards
		public MotherboardRAM(IOptions<EmulationSettings> settings, ILogger<MotherboardRAM> logger)
		{
			if (settings.Value.MotherboardMemory != 0.0)
			{
				if (settings.Value.MotherboardMemory > 64.0f) settings.Value.MotherboardMemory = 64.0f;
				uint memorySize = (uint)(settings.Value.MotherboardMemory * 1024 * 1024);
				if (settings.Value.MotherboardMemory > 16.0f)
					memoryRange = new MemoryRange(0x04000000, memorySize);
				else
					memoryRange = new MemoryRange(0x07000000, memorySize);
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
