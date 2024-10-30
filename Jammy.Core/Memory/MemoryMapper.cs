using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Jammy.Core.Persistence;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public class MemoryMapper : IMemoryMapper, IDebugMemoryMapper, IStatePersister
	{
		private readonly IMemoryManager memoryManager;
		private IMemoryInterceptor interceptor;

		public MemoryMapper(
			IMemoryManager memoryManager, IZorroConfigurator zorroConfigurator,

			ICIAMemory ciaMemory, IBattClock battClock,
			IZorro2 zorro2, IZorro3 zorro3, IAgnus agnus, IUnmappedMemory unmappedMemory,
			IKickstartROM kickstartROM, IDiskController diskController,
			IAkiko akiko, IMotherboard motherboard, IMotherboardRAM motherboardRAM, ICPUSlotRAM cpuSlotRAM,
			ILogger<MemoryMapper> logger)
		{
			this.memoryManager = memoryManager;
			_ = zorroConfigurator;

			var devices = new List<IMemoryMappedDevice>
			{
				unmappedMemory,
				ciaMemory,
				agnus,
				kickstartROM,
				battClock,
				motherboard,
				zorro2,
				zorro3,
				diskController,
				akiko,
				motherboardRAM,
				cpuSlotRAM
			};

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

		private string Pack(byte[] mem)
		{
			using (var unzipped = new MemoryStream(mem))
			{
				using (var zipped = new MemoryStream())
				{
					using (var gz = new GZipStream(zipped, CompressionLevel.SmallestSize))
					{
						unzipped.CopyTo(gz);
					}
					return Convert.ToBase64String(zipped.ToArray());
				}
			}
		}

		private byte[] Unpack(string mem)
		{
			using (var zipped = new MemoryStream(Convert.FromBase64String(mem)))
			{
				using (var unzipped = new MemoryStream())
				{
					using (var gz = new GZipStream(zipped, CompressionMode.Decompress))
					{ 
						gz.CopyTo(unzipped);
					}
					return unzipped.ToArray();
				}
			}
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
						Content = Pack(m.Memory),
					});
				jb["id"]="RAM";
				obj.Add(jb);
			}
		}

		public void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "RAM")) return;
			
			var mem = obj.ToObject<PersistMemory>();
			var bytes = Unpack(mem.Content);
			for (ulong address = 0; address < mem.Length; address++)
				UnsafeWrite8((uint)(address+mem.Start), bytes[address]);
		}
	}
}
