using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public class MemoryMapper : IMemoryMapper, IDebugMemoryMapper, IStatePersister
	{
		private readonly IMemoryManager memoryManager;
		private readonly ILogger logger;
		private readonly EmulationSettings settings;
		private IMemoryInterceptor interceptor;

		public MemoryMapper(
			IMemoryManager memoryManager, IZorroConfigurator zorroConfigurator,
			ICIAMemory ciaMemory, IBattClock battClock,
			IZorro2 zorro2, IZorro3 zorro3, IAgnus agnus, IUnmappedMemory unmappedMemory,
			IChipRAM chipRAM, ITrapdoorRAM trapdoorRAM,
 			IKickstartROM kickstartROM, IDiskController diskController,
			IExtendedKickstartROM extendedKickstartROM,
			IAkiko akiko, IMotherboard motherboard, IMotherboardRAM motherboardRAM, ICPUSlotRAM cpuSlotRAM,
			IChips chips,
			IOptions<EmulationSettings> settings,
			ILogger<MemoryMapper> logger)
		{
			this.memoryManager = memoryManager;
			this.logger = logger;
			_ = zorroConfigurator;
			this.settings = settings.Value;

			var devices = new List<IMemoryMappedDevice>
			{
				unmappedMemory,
				ciaMemory,

				chipRAM,
				kickstartROM,

				//agnus,
				chips,
				battClock,
				motherboard,
				zorro2,
				zorro3,
				diskController,
				motherboardRAM,
				cpuSlotRAM,
				trapdoorRAM,
				extendedKickstartROM
			};

			//if (settings.Value.Akiko.IsEnabled())
				devices.Add(akiko);

			memoryManager.AddDevices(devices);
		}

		public MemoryMapper(IMemoryManager memoryManager, IMemoryMappedDevice memory)
		{
			this.memoryManager = memoryManager;
			memoryManager.AddDevice(memory);
		}

		public void Reset()
		{
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

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange};
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			//if (address >> 16 == 0)
			//	logger.LogTrace($"*** Read From Address 0 - {memoryManager.MappedDevice[0]}");

			uint value = memoryManager.MappedDevice[address].Read(insaddr, address, size);
			if (interceptor != null) interceptor.Read(insaddr, address, value, size);
			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			//if (address >> 16 == 0xb8)
			//	logger.LogTrace($"*** Write to Address 0 - {memoryManager.MappedDevice[0]}");

			if (interceptor != null) interceptor.Write(insaddr, address, value, size);
			memoryManager.MappedDevice[address].Write(insaddr, address, value, size);
		}

		public uint ImmediateRead(uint insaddr, uint address, Size size)
		{
			//if (address >> 16 == 0)
			//	logger.LogTrace($"*** Read From Address 0 - {memoryManager.MappedDevice[0]}");

			//if (memoryManager.MappedDevice[address] is IContendedMemoryMappedDevice)
			//{ 
			uint value = ((IContendedMemoryMappedDevice)memoryManager.MappedDevice[address]).ImmediateRead(insaddr, address, size);
			if (interceptor != null) interceptor.Read(insaddr, address, value, size);
			return value;
			//}
			//return memoryManager.MappedDevice[address].Read(insaddr, address, size);
		}

		public void ImmediateWrite(uint insaddr, uint address, uint value, Size size)
		{
			//if (address >> 16 == 0xb8)
			//	logger.LogTrace($"*** Write to Address 0 - {memoryManager.MappedDevice[0]}");

			if (interceptor != null) interceptor.Write(insaddr, address, value, size);
			((IContendedMemoryMappedDevice)memoryManager.MappedDevice[address]).ImmediateWrite(insaddr, address, value, size);
		}

		public uint Fetch(uint insaddr, uint address, Size size)
		{
			//if (address>>16 == 0)
			//	logger.LogTrace($"*** Fetch From Address 0 - {memoryManager.MappedDevice[0]}");

			uint value = memoryManager.MappedDevice[address].Read(insaddr, address, size);
			if (interceptor != null) interceptor.Fetch(insaddr, address, value, size);
			return value;
		}

		// IDebuggableMemoryMapper

		public byte UnsafeRead8(uint address)
		{
			return (byte)((IDebuggableMemory)memoryManager.DebugMappedDevice[address]).DebugRead(address, Size.Byte);
		}

		public ushort UnsafeRead16(uint address)
		{
			return (ushort)((IDebuggableMemory)memoryManager.DebugMappedDevice[address]).DebugRead(address, Size.Word);
		}

		public uint UnsafeRead32(uint address)
		{
			return ((IDebuggableMemory)memoryManager.DebugMappedDevice[address]).DebugRead(address, Size.Long);
		}

		public uint UnsafeRead(uint address, Size size)
		{
			if (size == Size.Byte) return UnsafeRead8(address);
			if (size == Size.Word) return UnsafeRead16(address);
			return UnsafeRead32(address);
		}

		public void UnsafeWrite(uint address, uint value, Size size)
		{
			if (size == Size.Byte) UnsafeWrite8(address, (byte)value);
			if (size == Size.Word) UnsafeWrite16(address, (ushort)value);
			if (size == Size.Long) UnsafeWrite32(address, value);
		}

		public void UnsafeWrite32(uint address, uint value)
		{
			((IDebuggableMemory)memoryManager.DebugMappedDevice[address]).DebugWrite(address, value, Size.Long);
		}

		public void UnsafeWrite16(uint address, ushort value)
		{
			((IDebuggableMemory)memoryManager.DebugMappedDevice[address]).DebugWrite(address, value, Size.Word);
		}

		public void UnsafeWrite8(uint address, byte value)
		{
			((IDebuggableMemory)memoryManager.DebugMappedDevice[address]).DebugWrite(address, value, Size.Byte);
		}

		public uint FindSequence(byte[] bytes)
		{
			var ranges = GetBulkRanges();
			foreach (var range in GetBulkRanges())
			{
				if (range.Length < (uint)bytes.Length) continue;
				for (uint i = 0; i < range.Length - (uint)bytes.Length; i++)
				{
					if (MemoryExtensions.SequenceEqual(range.Memory.AsSpan((int)i, bytes.Length), bytes))
						return i + range.Start;
				}
			}
			return 0;
		}

		public IEnumerable<byte> GetEnumerable(uint start, ulong length)
		{
			for (ulong i = start; i < Math.Min(start + length, memoryRange.Length); i++)
				if (memoryManager.DebugMappedDevice[(uint)i] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead8((uint)i);
		}

		public IEnumerable<byte> GetEnumerable(uint start)
		{
			for (ulong i = start; i < memoryRange.Length; i++)
			{
				if (memoryManager.DebugMappedDevice[(uint)i] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead8((uint)i);
			}
		}

		public IEnumerable<uint> AsULong(uint start)
		{
			for (ulong i = start; i < memoryRange.Length; i += 4)
				if (memoryManager.DebugMappedDevice[(uint)i] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead32((uint)i);
		}

		public IEnumerable<ushort> AsUWord(uint start)
		{
			for (ulong i = start; i < memoryRange.Length; i += 2)
				if (memoryManager.DebugMappedDevice[(uint)i] is IUnmappedMemory)
					yield return 0;
				else
					yield return UnsafeRead16((uint)i);
		}

		public ulong Length => memoryRange.Length;

		public List<BulkMemoryRange> GetBulkRanges()
		{
			return memoryManager.MappedDevice.BulkReadableDevices()
				.SelectMany(x => x.ReadBulk())
				.Where(x => x.Length > 0)
				.ToList();
		}

		public List<BulkMemoryRange> GetPersistableRanges()
		{
			return memoryManager.MappedDevice.PersistableDevices()
				.SelectMany(x => ((IBulkMemoryRead)x).ReadBulk())
				.Where(x=>x.Length > 0)
				.ToList();
		}

		public string GetString(uint str)
		{
			var sb = new StringBuilder();
			for (; ; )
			{
				byte c = UnsafeRead8(str);
				if (c == 0)
					return sb.ToString();

				sb.Append(Convert.ToChar(c));
				str++;
			}
		}

		private class PersistMemory
		{
			public uint Start { get; set; }
			public ulong Length { get; set; }
			public string Content { get; set; }
		}

		public void Save(JArray obj)
		{
			foreach (var m in GetPersistableRanges())
			{
				var jb = JObject.FromObject(
					new PersistMemory
					{
						Start = m.Start,
						Length = m.Length,
						Content = PersistenceManager.Pack(m.Memory),
					});
				jb["id"]="RAM";
				obj.Add(jb);
			}
		}

		public void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "RAM")) return;
			
			var mem = obj.ToObject<PersistMemory>();
			var bytes = PersistenceManager.Unpack(mem.Content);
			for (ulong address = 0; address < mem.Length; address++)
				UnsafeWrite8((uint)(address+mem.Start), bytes[address]);
		}
	}
}
