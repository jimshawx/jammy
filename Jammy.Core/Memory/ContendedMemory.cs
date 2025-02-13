using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;

namespace Jammy.Core.Memory
{
	public abstract class ContendedMemory : Memory, IContendedMemoryMappedDevice
	{
		private readonly IDMA dma;
		private ulong contendedReads = 0;
		private ulong contendedWrites = 0;

		protected abstract CPUTarget target { get; }

		public ContendedMemory(IDMA dma)
		{
			this.dma = dma;
		}

		public new uint Read(uint insaddr, uint address, Size size)
		{
			uint v = 0;
			
			if (size == Size.Long)
			{
				contendedReads++;
				dma.ReadCPU(target, address, Size.Word);
				v = dma.ChipsetSync() << 16;
				size = Size.Word;
				address += 2;
			}

			contendedReads++;
			dma.ReadCPU(target, address, size);
			v |= dma.ChipsetSync();

			return v;
		}

		public new void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size == Size.Long)
			{
				contendedWrites++;
				dma.WriteCPU(target, address, (ushort)(value >> 16), Size.Word);
				dma.ChipsetSync();
				size = Size.Word;
				address += 2;
			}
			contendedWrites++;
			dma.WriteCPU(target, address, (ushort)value, size);
			dma.ChipsetSync();
		}
	}
}
