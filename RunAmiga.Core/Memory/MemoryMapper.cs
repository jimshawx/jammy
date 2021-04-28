using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{
	public class MemoryMapper : IMemoryMapper, IDebugMemoryMapper
	{
		private readonly IChipRAM chipRAM;
		private readonly IKickstartROM kickstartROM;
		private readonly IMemoryManager memoryManager;
		private IMemoryInterceptor interceptor;

		public MemoryMapper(
			IMemoryManager memoryManager, IZorroConfigurator zorroConfigurator,

			ICIAMemory ciaMemory, IChips custom, IBattClock battClock,
			IZorro expansion, IChipRAM chipRAM, ITrapdoorRAM trapdoorRAM, IUnmappedMemory unmappedMemory,
			IKickstartROM kickstartROM, IIDEController ideController, ISCSIController scsiController,
			IAkiko akiko, IMotherboard motherboard,
			ILogger<MemoryMapper> logger)
		{
			this.chipRAM = chipRAM;
			this.kickstartROM = kickstartROM;
			this.memoryManager = memoryManager;
			_ = zorroConfigurator;

			var devices = new List<IMemoryMappedDevice>
			{
				unmappedMemory,
				ciaMemory,
				custom,
				chipRAM,
				trapdoorRAM,
				kickstartROM,
				battClock,
				motherboard,
				expansion,
				ideController,
				scsiController,
				akiko
			};

			memoryManager.AddDevices(devices);

			CopyKickstart();
		}

		public MemoryMapper(IMemoryManager memoryManager, IMemoryMappedDevice memory)
		{
			this.memoryManager = memoryManager;
			memoryManager.AddDevice(memory);
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
			uint len = (uint)(kickstartROM.MappedRange().Length/4);
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
			uint value = memoryManager.MappedDevice[address].Read(insaddr, address, size);
			if (interceptor != null) interceptor.Read(insaddr, address, value, size);
			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (interceptor != null) interceptor.Write(insaddr, address, value, size);
			memoryManager.MappedDevice[address].Write(insaddr, address, value, size);
		}

		public uint Fetch(uint insaddr, uint address, Size size)
		{
			uint value = memoryManager.MappedDevice[address].Read(insaddr, address, size);
			if (interceptor != null) interceptor.Fetch(insaddr, address, value, size);
			return value;
		}

		// IDebuggableMemoryMapper

		//todo: these should NOT be using the emulation's Read/Write methods
		public byte UnsafeRead8(uint address)
		{
			return (byte)memoryManager.DebugMappedDevice[address].Read(0, address, Size.Byte);
		}

		public ushort UnsafeRead16(uint address)
		{
			return (ushort)memoryManager.DebugMappedDevice[address].Read(0, address, Size.Word);
		}

		public uint UnsafeRead32(uint address)
		{
			return memoryManager.DebugMappedDevice[address].Read(0, address, Size.Long);
		}

		public void UnsafeWrite32(uint address, uint value)
		{
			memoryManager.DebugMappedDevice[address].Write(0, address, value, Size.Long);
		}

		public void UnsafeWrite16(uint address, ushort value)
		{
			memoryManager.DebugMappedDevice[address].Write(0, address, value, Size.Word);
		}

		public void UnsafeWrite8(uint address, byte value)
		{
			memoryManager.DebugMappedDevice[address].Write(0, address, value, Size.Byte);
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

		public IEnumerable<byte> GetEnumerable(int start, long length)
		{
			for (long i = start; i < Math.Min(start + length, memoryRange.Length); i++)
				if (memoryManager.DebugMappedDevice[(uint)i] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead8((uint)i);
		}

		public IEnumerable<byte> GetEnumerable(int start)
		{
			for (long i = start; i < memoryRange.Length; i++)
			{
				if (memoryManager.DebugMappedDevice[(uint)i] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead8((uint)i);
			}
		}

		public IEnumerable<uint> AsULong(int start)
		{
			for (long i = start; i < memoryRange.Length; i += 4)
				if (memoryManager.DebugMappedDevice[(uint)i] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead32((uint)i);
		}

		public IEnumerable<ushort> AsUWord(int start)
		{
			for (long i = start; i < memoryRange.Length; i += 2)
				if (memoryManager.DebugMappedDevice[(uint)i]  is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead16((uint)i);
		}

		public int Length => (int)memoryRange.Length;

		public List<BulkMemoryRange> GetBulkRanges()
		{
			return memoryManager.MappedDevice.BulkReadableDevices()
				.Select(x => x.ReadBulk())
				.ToList();
		}
	}
}
