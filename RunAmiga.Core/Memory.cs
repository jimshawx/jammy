using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interfaces;
using RunAmiga.Core.Types;

namespace RunAmiga.Core
{
	public class Memory : IMemory
	{
		private readonly byte[] memory;
		private const uint memoryMask = 0x00ffffff;

		private readonly string id;
		private readonly ILogger<Memory> logger;

		public Memory(string id, ILogger<Memory> logger)
		{
			this.id = id;
			this.logger = logger;
			this.memory = new byte[16 * 1024 * 1024];

			var ks = new Kickstart("../../../../kick12.rom", "Kickstart 1.2");
			SetKickstart(ks);
			//var kickstart = new Kickstart("../../../../kick13.rom", "Kickstart 1.3");
			//var kickstart = new Kickstart("../../../../kick204.rom", "Kickstart 2.04");
			//var kickstart = new Kickstart("../../../../kick31.rom", "Kickstart 3.1");

			Reset();
		}

		public bool IsMapped(uint address)
		{
			return true;
		}

		private uint read32(uint address)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			return ((uint)memory[address & memoryMask] << 24) +
				((uint)memory[(address + 1) & memoryMask] << 16) +
				((uint)memory[(address + 2) & memoryMask] << 8) +
				(uint)memory[(address + 3) & memoryMask];
		}

		private ushort read16(uint address)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			return (ushort)(
				((ushort)memory[address & memoryMask] << 8) +
				(ushort)memory[(address + 1) & memoryMask]);
		}

		private byte read8(uint address)
		{
			return memory[address & memoryMask];
		}

		private void write32(uint address, uint value)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			memory[address & memoryMask] = (byte)(value >> 24);
			memory[(address + 1) & memoryMask] = (byte)(value >> 16);
			memory[(address + 2) & memoryMask] = (byte)(value >> 8);
			memory[(address + 3) & memoryMask] = (byte)value;
		}

		private void write16(uint address, ushort value)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			memory[address & memoryMask] = (byte)(value >> 8);
			memory[(address + 1) & memoryMask] = (byte)value;
		}

		private void write8(uint address, byte value)
		{
			memory[address & memoryMask] = value;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size == Size.Byte) return read8(address);
			if (size == Size.Word) return read16(address);
			if (size == Size.Long) return read32(address);
			throw new UnknownInstructionSizeException(insaddr, 0);
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size == Size.Byte) { write8(address, (byte)value); return; }
			if (size == Size.Word) { write16(address, (ushort)value); return; }
			if (size == Size.Long) { write32(address, value); return; }
			throw new UnknownInstructionSizeException(insaddr, 0);
		}

		public byte[] GetMemoryArray()
		{
			return memory;
		}

		public byte Read8(uint address)
		{
			if (address >= 0x1000000)
			{
				//logger.LogTrace($"Memory Read Byte from {address:X8}");
				return 0;
			}
			return memory[address];
		}

		public ushort Read16(uint address)
		{
			if (address >= 0xfffffe)
			{
				logger.LogTrace($"Memory Read Word from ${address:X8}");
				return 0;
			}
			if ((address & 1) != 0)
			{
				logger.LogTrace($"Memory Read Unaligned Word from ${address:X8}");
				return 0;
			}
			return (ushort)(((ushort)memory[address] << 8) +
							(ushort)memory[(address + 1)]);
		}

		public uint Read32(uint address)
		{
			if (address >= 0xfffffc)
			{
				logger.LogTrace($"Memory Read Int from ${address:X8}");
				return 0;
			}
			if ((address & 1) != 0)
			{
				logger.LogTrace($"Memory Read Unaligned Int from ${address:X8}");
				return 0;
			}
			return ((uint)memory[address] << 24) +
					((uint)memory[(address + 1)] << 16) +
					((uint)memory[(address + 2)] << 8) +
					(uint)memory[(address + 3)];
		}

		public void Emulate(ulong cycles)
		{

		}

		public void BulkWrite(uint dst, byte[] src, int length)
		{
			Array.Copy(src, 0, memory, dst, length);
		}

		private Kickstart kickstart;

		public void SetKickstart(Kickstart kickstart)
		{
			this.kickstart = kickstart;

			BulkWrite(kickstart.Origin, kickstart.ROM, kickstart.ROM.Length);
			BulkWrite(0, kickstart.ROM, kickstart.ROM.Length);
		}

		public void Reset()
		{
			Array.Clear(memory, 0, memory.Length);

			if (kickstart != null)
			{
				BulkWrite(kickstart.Origin, kickstart.ROM, kickstart.ROM.Length);
				BulkWrite(0, kickstart.ROM, kickstart.ROM.Length);
			}
		}

		public uint FindSequence(byte[] bytes)
		{
			for (int i = 0xfc0000; i < memory.Length - bytes.Length; i++)
			{
				if (bytes.SequenceEqual(memory.Skip(i).Take(bytes.Length)))
					return (uint)i;
			}

			return 0;
		}
	}
}
