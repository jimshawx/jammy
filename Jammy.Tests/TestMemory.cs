using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Tests
{
	public interface ITestMemory
	{
		public byte[] GetMemoryArray();
	}

	public class TestMemory : Memory, ITestMemory, IDebugMemoryMapper, IMemoryMapper
	{
		private readonly ILogger<TestMemory> logger;
		private readonly IMachineIdentifier machineIdentifier;

		public TestMemory(ILogger<TestMemory> logger, IOptions<EmulationSettings> settings, IMachineIdentifier machineIdentifier)
		{
			this.logger = logger;
			this.machineIdentifier = machineIdentifier;

			memory = new byte[1ul<<settings.Value.AddressBits];
			memoryRange = new MemoryRange(0x0, (uint)memory.Length);
			addressMask = (uint)(memory.Length - 1);
		}

		public byte[] GetMemoryArray()
		{
			return memory;
		}

		public void Reset()
		{
			
		}

		public new uint Read(uint insaddr, uint address, Size size)
		{
			uint value = base.Read(insaddr, address, size);

			//if (size == Size.Long) logger.LogTrace($"{machineIdentifier.Id} R32 {address:X8} {value:X8}");
			//else if (size == Size.Word) logger.LogTrace($"{machineIdentifier.Id} R16 {address:X8} {value:X4}");
			//else if (size == Size.Byte) logger.LogTrace($"{machineIdentifier.Id} R8 {address:X8} {value:X2}");

			return value;
		}

		public new void Write(uint insaddr, uint address, uint value, Size size)
		{
			base.Write(insaddr, address, value, size);

			//if (size == Size.Long) logger.LogTrace($"{machineIdentifier.Id} W32 {address:X8} {value:X8}");
			//else if (size == Size.Word) logger.LogTrace($"{machineIdentifier.Id} W16 {address:X8} {value:X4}");
			//else if (size == Size.Byte) logger.LogTrace($"{machineIdentifier.Id} W8 {address:X8} {value:X2}");
		}

		public uint Fetch(uint insaddr, uint address, Size size)
		{
			return Read(insaddr, address, size);
		}

		public byte UnsafeRead8(uint address) { return (byte)base.Read(0, address, Size.Byte); }
		public ushort UnsafeRead16(uint address) { return (ushort)base.Read(0, address, Size.Word); }
		public uint UnsafeRead32(uint address) { return base.Read(0, address, Size.Long); }
		public uint UnsafeRead(uint address, Size size)
		{
			if (size == Size.Byte) return UnsafeRead8(address);
			if (size == Size.Word) return UnsafeRead16(address);
			return UnsafeRead32(address);
		}

		public void UnsafeWrite8(uint address, byte value) { base.Write(0, address, value, Size.Byte); }
		public void UnsafeWrite16(uint address, ushort value) { base.Write(0, address, value, Size.Word); }
		public void UnsafeWrite32(uint address, uint value) { base.Write(0, address, value, Size.Long); }

		public uint FindSequence(byte[] bytes)
		{
			for (int i = 0; i < memory.Length - bytes.Length; i++)
			{
				if (bytes.SequenceEqual(memory.Skip(i).Take(bytes.Length)))
					return (uint)i;
			}

			return 0;
		}

		public IEnumerable<byte> GetEnumerable(uint start, ulong length)
		{
			return memory[(int)start..(int)Math.Min((ulong)memory.Length, start+length)];
		}

		public IEnumerable<byte> GetEnumerable(uint start)
		{
			return memory[(int)start..];
		}

		public IEnumerable<uint> AsULong(uint start)
		{
			return memory[(int)start..].AsULong();
		}

		public IEnumerable<ushort> AsUWord(uint start)
		{
			return memory[(int)start..].AsUWord();
		}

		public ulong Length => (ulong)memory.Length;

		public void AddMemoryIntercept(IMemoryInterceptor interceptor)
		{
		}

		public List<BulkMemoryRange> GetBulkRanges()
		{
			return new List<BulkMemoryRange> {new BulkMemoryRange {Memory = memory, Start = 0}};
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
	}
}
