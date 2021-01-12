using RunAmiga.Types;
using System;
using System.Diagnostics;
using System.IO;

namespace RunAmiga
{
	public class Memory : IMemoryMappedDevice, IEmulate
	{
		private readonly byte[] memory;
		private const uint memoryMask = 0x00ffffff;

		private readonly Debugger debugger;
		private readonly string id;

		public Memory(Debugger debugger, string id)
		{
			this.debugger = debugger;
			this.id = id;
			this.memory = new byte[16 * 1024 * 1024];
		}

		public bool IsMapped(uint address)
		{
			return true;
		}

		public uint read32(uint address)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			return ((uint)memory[address & memoryMask] << 24) +
				((uint)memory[(address + 1) & memoryMask] << 16) +
				((uint)memory[(address + 2) & memoryMask] << 8) +
				(uint)memory[(address + 3) & memoryMask];
		}

		public ushort read16(uint address)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			return (ushort)(
				((ushort)memory[address & memoryMask] << 8) +
				(ushort)memory[(address + 1) & memoryMask]);
		}

		public byte read8(uint address)
		{
			return memory[address & memoryMask];
		}

		public void write32(uint address, uint value)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			byte b0, b1, b2, b3;
			b0 = (byte)(value >> 24);
			b1 = (byte)(value >> 16);
			b2 = (byte)(value >> 8);
			b3 = (byte)(value);
			memory[address & memoryMask] = b0;
			memory[(address + 1) & memoryMask] = b1;
			memory[(address + 2) & memoryMask] = b2;
			memory[(address + 3) & memoryMask] = b3;
		}

		public void write16(uint address, ushort value)
		{
			if ((address & 1) != 0)
				throw new MemoryAlignmentException(address);

			byte b0, b1;
			b0 = (byte)(value >> 8);
			b1 = (byte)(value);
			memory[address & memoryMask] = b0;
			memory[(address + 1) & memoryMask] = b1;
		}

		public void write8(uint address, byte value)
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
			//if ((address >= 0xc014cd && address <= 0xC014ff && address != 0xc014f4) || (address == 0xc014f4 && (insaddr == 0xfc1798 || insaddr == 0)))
			//{
			//	if (size == Size.Long)
			//	{
			//		Write(insaddr, address + 2, (ushort)value, Size.Word);
			//		Write(insaddr, address,   (ushort)(value>>16), Size.Word);
			//		return;
			//	}
			//	Trace.WriteLine($"[LOG{id}] a:{address:X8} v:{value:X8} pc:{insaddr:X8} s:{size}");
			//}

			if (size == Size.Byte) { write8(address, (byte)value); return; }
			if (size == Size.Word) { write16(address, (ushort)value); return; }
			if (size == Size.Long) { write32(address, value); return; }
			throw new UnknownInstructionSizeException(insaddr, 0);
		}

		public byte[] GetMemoryArray()
		{
			return memory;
		}

		public void Clear()
		{
			Array.Clear(memory, 0, memory.Length);
		}

		public byte Read8(uint address)
		{
			if (address >= 0x1000000)
			{
				//Trace.WriteLine($"Memory Read Byte from {address:X8}");
				return 0;
			}
			return memory[address];
		}

		public ushort Read16(uint address)
		{
			if (address >= 0xfffffe)
			{
				Trace.WriteLine($"Memory Read Word from ${address:X8}");
				return 0;
			}
			if ((address & 1) != 0)
			{
				Trace.WriteLine($"Memory Read Unaligned Word from ${address:X8}");
				return 0;
			}
			return (ushort)(((ushort)memory[address] << 8) +
							(ushort)memory[(address + 1)]);
		}

		public uint Read32(uint address)
		{
			if (address >= 0xfffffc)
			{
				Trace.WriteLine($"Memory Read Int from ${address:X8}");
				return 0;
			}
			if ((address & 1) != 0)
			{
				Trace.WriteLine($"Memory Read Unaligned Int from ${address:X8}");
				return 0;
			}
			return ((uint)memory[address] << 24) +
					((uint)memory[(address + 1)] << 16) +
					((uint)memory[(address + 2)] << 8) +
					(uint)memory[(address + 3)];
		}

		public void Emulate(ulong ns)
		{

		}

		public void BulkWrite(int dst, byte[] src, int length)
		{
			Array.Copy(src, 0, memory, dst, length);
		}

		public void Reset()
		{
			Array.Clear(memory, 0, memory.Length);

			byte[] rom = File.ReadAllBytes("../../../../kick12.rom");
			Debug.Assert(rom.Length == 256 * 1024);

			BulkWrite(0xfc0000, rom, 256 * 1024);
			BulkWrite(0, rom, 256 * 1024);

			//byte[] rom = File.ReadAllBytes("../../../../kick13.rom");
			//Debug.Assert(rom.Length == 256 * 1024);

			//BulkWrite(0xfc0000, rom, 256 * 1024);
			//BulkWrite(0, rom, 256 * 1024);

			//byte[] rom = File.ReadAllBytes("../../../../kick31.rom");
			//Debug.Assert(rom.Length == 512 * 1024);

			//BulkWrite(0xf80000, rom, 512 * 1024);
			//BulkWrite(0, rom, 512 * 1024);
		}
	}
}
