using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Tests
{
	public interface ITestMemory
	{
		public byte[] GetMemoryArray();
	}

	public class TestMemory : IMemoryMappedDevice, ITestMemory
	{
		private readonly byte[] memory;
		private uint memoryMask;
		
		private readonly ILogger logger;
		private readonly IMachineIdentifier machineIdentifier;
		private readonly EmulationSettings settings;
		private readonly MemoryRange memoryRange;

		public TestMemory(ILogger<TestMemory> logger, IOptions<EmulationSettings> settings, IMachineIdentifier machineIdentifier)
		{
			this.logger = logger;
			this.machineIdentifier = machineIdentifier;
			this.settings = settings.Value;
			this.memory = new byte[settings.Value.MemorySize];
			memoryRange = new MemoryRange(0x0, (uint)memory.Length);
			memoryMask = (uint)(memory.Length - 1);

			//if (!string.IsNullOrEmpty(settings.Value.KickStart))
			//{
			//	Kickstart ks = null;
			//	switch (settings.Value.KickStart)
			//	{
			//		case "1.2":  ks = new Kickstart("../../../../kick12.rom", "Kickstart 1.2"); break;
			//		case "1.3":  ks = new Kickstart("../../../../kick13.rom", "Kickstart 1.3"); break;
			//		case "2.04": ks = new Kickstart("../../../../kick204.rom", "Kickstart 2.04"); break;
			//		case "3.1":  ks = new Kickstart("../../../../kick31.rom", "Kickstart 3.1"); break;
			//	}
			//	if (ks != null)
			//		SetKickstart(ks);
			//}

			//Reset();
		}


		public bool IsMapped(uint address)
		{
			return true;
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		private uint read32(uint address)
		{
			if (settings.AlignmentExceptions)
			{
				if ((address & 1) != 0)
					throw new MemoryAlignmentException(address);
			}

			uint value = (uint)((memory[address & memoryMask] << 24) +
								(memory[(address + 1) & memoryMask] << 16) +
								(memory[(address + 2) & memoryMask] << 8) +
								memory[(address + 3) & memoryMask]);

			//logger.LogTrace($"{machineIdentifier.Id} R32 {address:X8} {value:X8}");
			
			return value;
		}

		private ushort read16(uint address)
		{
			if (settings.AlignmentExceptions)
			{
				if ((address & 1) != 0)
					throw new MemoryAlignmentException(address);
			}

			ushort value = (ushort)((memory[address & memoryMask] << 8) + 
									memory[(address + 1) & memoryMask]);

			//logger.LogTrace($"{machineIdentifier.Id} R16 {address:X8} {value:X4}");
			
			return value;
		}

		private byte read8(uint address)
		{
			byte value = memory[address & memoryMask];

			//logger.LogTrace($"{machineIdentifier.Id} R8 {address:X8} {value:X2}");
			
			return value;
		}

		private void write32(uint address, uint value)
		{
			//logger.LogTrace($"{machineIdentifier.Id} W32 {address:X8} {value:X8}");

			if (settings.AlignmentExceptions)
			{
				if ((address & 1) != 0)
					throw new MemoryAlignmentException(address);
			}

			memory[address & memoryMask] = (byte)(value >> 24);
			memory[(address + 1) & memoryMask] = (byte)(value >> 16);
			memory[(address + 2) & memoryMask] = (byte)(value >> 8);
			memory[(address + 3) & memoryMask] = (byte)value;
		}

		private void write16(uint address, ushort value)
		{
			//logger.LogTrace($"{machineIdentifier.Id} W16 {address:X8} {value:X4}");

			if (settings.AlignmentExceptions)
			{
				if ((address & 1) != 0)
					throw new MemoryAlignmentException(address);
			}

			memory[address & memoryMask] = (byte)(value >> 8);
			memory[(address + 1) & memoryMask] = (byte)value;
		}

		private void write8(uint address, byte value)
		{
			//logger.LogTrace($"{machineIdentifier.Id} W8 {address:X8} {value:X2}");

			memory[address & memoryMask] = value;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size == Size.Word) return read16(address);
			if (size == Size.Byte) return read8(address);
			if (size == Size.Long) return read32(address);
			throw new UnknownInstructionSizeException(insaddr, 0);
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size == Size.Word) { write16(address, (ushort)value); return; }
			if (size == Size.Byte) { write8(address, (byte)value); return; }
			if (size == Size.Long) { write32(address, value); return; }
			throw new UnknownInstructionSizeException(insaddr, 0);
		}

		public byte[] GetMemoryArray()
		{
			return memory;
		}

		public byte UnsafeRead8(uint address)
		{
			address &= memoryMask;
			return memory[address];
		}

		public ushort UnsafeRead16(uint address)
		{
			address &= memoryMask;
			return (ushort)((memory[address] << 8) +
							memory[address + 1]);
		}

		public uint UnsafeRead32(uint address)
		{
			address &= memoryMask;
			return (uint)((memory[address] << 24) +
					(memory[address + 1] << 16) +
					(memory[address + 2] << 8) +
					memory[address + 3]);
		}

		public void UnsafeWrite32(uint address, uint value)
		{
			memory[address & memoryMask] = (byte)(value >> 24);
			memory[(address + 1) & memoryMask] = (byte)(value >> 16);
			memory[(address + 2) & memoryMask] = (byte)(value >> 8);
			memory[(address + 3) & memoryMask] = (byte)value;
		}

		public void UnsafeWrite16(uint address, ushort value)
		{
			memory[address & memoryMask] = (byte)(value >> 8);
			memory[(address + 1) & memoryMask] = (byte)value;
		}

		public void UnsafeWrite8(uint address, byte value)
		{
			memory[address & memoryMask] = value;
		}

		public uint FindSequence(byte[] bytes)
		{
			for (int i = 0; i < memory.Length - bytes.Length; i++)
			{
				if (bytes.SequenceEqual(memory.Skip(i).Take(bytes.Length)))
					return (uint)i;
			}

			return 0;
		}
	}
}
