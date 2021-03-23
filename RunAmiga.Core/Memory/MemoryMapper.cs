using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{
	public class MemoryMapper : IMemoryMapper
	{
		private readonly IChipRAM chipRAM;
		private readonly IKickstartROM kickstartROM;
		private readonly ILogger logger;
		private IMemoryInterceptor interceptor;
		private readonly List<IMemoryMappedDevice> devices = new List<IMemoryMappedDevice>();

		private readonly IMemoryMappedDevice [] mappedDevice = new IMemoryMappedDevice[0x100];

		private readonly uint memoryMask;

		public MemoryMapper(ICIAMemory ciaMemory, IChips custom, IBattClock battClock,
			IZorro expansion, IChipRAM chipRAM, ITrapdoorRAM trapdoorRAM, IUnmappedMemory unmappedMemory,
			IKickstartROM kickstartROM, IIDEController ideController,
			ILogger<MemoryMapper> logger, IOptions<EmulationSettings> settings)
		{
			this.chipRAM = chipRAM;
			this.kickstartROM = kickstartROM;
			this.logger = logger;

			memoryMask = (uint)(settings.Value.MemorySize - 1);

			devices.Add(unmappedMemory);
			devices.Add(ciaMemory);
			devices.Add(custom);
			devices.Add(chipRAM);
			devices.Add(trapdoorRAM);
			devices.Add(kickstartROM);
			devices.Add(battClock);
			devices.Add(expansion);
			devices.Add(ideController);

			CopyKickstart();
			
			BuildMappedDevices();
		}

		public MemoryMapper(List<IMemoryMappedDevice> memoryDevices, IOptions<EmulationSettings> settings)
		{
			memoryMask = (uint)(settings.Value.MemorySize - 1);
			devices.AddRange(memoryDevices);
			BuildMappedDevices();
		}

		public void Emulate(ulong cycles)
		{
		}

		public void Reset()
		{
			CopyKickstart();
		}

		private void CopyKickstart()
		{
			//hack: this is a hack to put the kickstart at 0x0 at reset time
			//todo: should be looking at CIA-A PRA OVL bit (0) and update the mappings
			uint src = kickstartROM.MappedRange().Start;
			uint dst = chipRAM.MappedRange().Start;
			uint len = kickstartROM.MappedRange().Length/4;
			while (len-- != 0)
			{
				uint v = kickstartROM.Read(0, src, Size.Long);
				chipRAM.Write(0, dst, v, Size.Long);
				src += 4;
				dst += 4;
			}
		}


		public void AddMemoryIntercept(IMemoryInterceptor interceptor)
		{
			this.interceptor = interceptor;
		}

		private void BuildMappedDevices()
		{
			foreach (var dev in devices.Select(x => new { device = x, range = x.MappedRange()}) )
			{
				uint start = dev.range.Start>>16;
				uint end = (dev.range.Start + dev.range.Length)>>16;
				for (uint i = start; i < end; i++)
				{
					if (dev.device.IsMapped(i << 16))
						mappedDevice[i] = dev.device;
				}
			}
		}

		readonly MemoryRange memoryRange = new MemoryRange(0x0, 0x1000000);

		public bool IsMapped(uint address)
		{
			return true;
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			address &= memoryMask;
			uint value = mappedDevice[address >> 16].Read(insaddr, address, size);
			if (interceptor != null) interceptor.Read(insaddr, address, value, size);
			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			address &= memoryMask;
			if (interceptor != null) interceptor.Write(insaddr, address, value, size);
			mappedDevice[address>>16].Write(insaddr, address, value, size);
		}
	}
}
