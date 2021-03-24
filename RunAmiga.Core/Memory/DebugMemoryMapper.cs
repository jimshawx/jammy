using System;
using System.Collections.Generic;
using System.Linq;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{
	public class DebugMemoryMapper : IDebugMemoryMapper
	{
		private readonly List<IMemoryMappedDevice> memories = new List<IMemoryMappedDevice>();
		private readonly IMemoryMappedDevice[] mappedDevice = new IMemoryMappedDevice[0x100];
		private readonly MemoryRange memoryRange = new MemoryRange(0, 16 * 1024 * 1024);
		private const uint memoryMask = 0x00ffffff;

		public DebugMemoryMapper(IChipRAM chipRAM, ITrapdoorRAM trapdoorRAM, IZorroRAM zorroRAM, IKickstartROM kickstartROM, IUnmappedMemory unmappedMemory)
		{
			this.memories.Add(unmappedMemory);
			this.memories.Add(chipRAM);
			this.memories.Add(trapdoorRAM);
			this.memories.Add(kickstartROM);
			this.memories.Add(zorroRAM);

			BuildMappedDevices();
		}

		public DebugMemoryMapper(IMemoryMappedDevice memory)
		{
			this.memories.Add(memory);
			BuildMappedDevices();
		}

		private void BuildMappedDevices()
		{
			foreach (var dev in memories.Select(x => new { device = x, range = x.MappedRange() }))
			{
				uint start = dev.range.Start >> 16;
				uint end = (dev.range.Start + dev.range.Length) >> 16;
				for (uint i = start; i < end; i++)
				{
					if (dev.device.IsMapped(i << 16))
						mappedDevice[i] = dev.device;
				}
			}
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		public byte UnsafeRead8(uint address)
		{
			address &= memoryMask;
			return (byte)mappedDevice[address >> 16].Read(0, address, Size.Byte);
		}

		public ushort UnsafeRead16(uint address)
		{
			address &= memoryMask;
			return (ushort)mappedDevice[address >> 16].Read(0, address, Size.Word);
		}

		public uint UnsafeRead32(uint address)
		{
			address &= memoryMask;
			return mappedDevice[address >> 16].Read(0, address, Size.Long);
		}

		public void UnsafeWrite32(uint address, uint value)
		{
			address &= memoryMask;
			mappedDevice[address >> 16].Write(0, address, value, Size.Long);
		}

		public void UnsafeWrite16(uint address, ushort value)
		{
			address &= memoryMask;
			mappedDevice[address >> 16].Write(0, address, value, Size.Word);
		}

		public void UnsafeWrite8(uint address, byte value)
		{
			address &= memoryMask;
			mappedDevice[address >> 16].Write(0, address, value, Size.Byte);
		}

		public uint FindSequence(byte[] bytes)
		{
			//todo: expensive!
			byte[] find = GetEnumerable(0).ToArray();
			for (int i = 0; i < Length - bytes.Length; i++)
			{
				if (bytes.SequenceEqual(find.Skip(i).Take(bytes.Length)))
					return (uint)i;
			}

			return 0;
		}

		public IEnumerable<byte> GetEnumerable(int start, int length)
		{
			for (int i = start; i < Math.Min(start+length, memoryRange.Length); i++)
				if (mappedDevice[i >> 16] is IUnmappedMemory)
					yield return 0;
				else 
					yield return UnsafeRead8((uint)i);
		}

		public IEnumerable<byte> GetEnumerable(int start)
		{
			for (int i = start; i < memoryRange.Length; i++)
			{
				if (mappedDevice[i >> 16] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead8((uint)i);
			}
		}

		public IEnumerable<uint> AsULong(int start)
		{
			for (uint i = (uint)start; i < memoryRange.Length; i += 4)
				if (mappedDevice[i >> 16] is IUnmappedMemory)
					yield return 0;
				else 
					yield return UnsafeRead32(i);
		}

		public IEnumerable<ushort> AsUWord(int start)
		{
			for (uint i = (uint)start; i < memoryRange.Length; i += 2)
				if (mappedDevice[i >> 16] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead16(i);
		}

		public int Length => (int)memoryRange.Length;
	}
}