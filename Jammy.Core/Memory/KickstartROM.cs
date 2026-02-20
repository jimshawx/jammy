using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public class KickstartROM : ContendedMemory, IKickstartROM
	{
		private readonly IMemoryManager memoryManager;
		private readonly ILogger logger;
		private string path;
		private string name;

		private readonly MemoryRange mirrorRange;

		protected override CPUTarget target => CPUTarget.KickROM;

		public KickstartROM(IDMA dma, IMemoryManager memoryManager, IOptions<EmulationSettings> settings, ILogger<KickstartROM> logger) : base(dma)
		{
			this.memoryManager = memoryManager;
			this.logger = logger;

			if (string.IsNullOrEmpty(settings.Value.KickStart))
			{
				logger.LogTrace($"No Kickstart ROM specified");

				memoryRange = new MemoryRange(0, 0);
				mirrorRange = new MemoryRange(0, 0);
				memory = Array.Empty<byte>();
				addressMask = 0;

				return;
			}

			path = settings.Value.KickStart;
			name = Path.GetFileName(settings.Value.KickStart);

			string fullName = Path.Combine("roms", path);

			try
			{
				logger.LogTrace($"Kickstart Loading {path} {name}");

				memory = File.ReadAllBytes(fullName);
				if (memory.Length == 512 * 1024)
				{
					memoryRange = new MemoryRange(0xf80000, 0x80000);
					mirrorRange = new MemoryRange(0, 0x80000);
				}
				else if (memory.Length == 256 * 1024)
				{
					memoryRange = new MemoryRange(0xfc0000, 0x40000);
					mirrorRange = new MemoryRange(0, 0x40000);
				}
				else if (memory.Length == 0x2000)
				{
					//this is for Test cases
					memoryRange = new MemoryRange(0xf80000, 0x2000);
					mirrorRange = new MemoryRange(0, 0x2000);
				}
				else
				{
					logger.LogTrace($"Kickstart ROM is not a standard size ({memory.Length:X8})");
					uint pot = BitOperations.RoundUpToPowerOf2((uint)memory.Length);
					if (pot > 512 * 1024)
						throw new ArgumentOutOfRangeException();
					logger.LogTrace($"Rounding ROM size up to {pot:X8} ({pot/1024}KB)");
					if (pot > 256 * 1024)
						memoryRange = new MemoryRange(0xf80000, pot);
					else
						memoryRange = new MemoryRange(0xfc0000, pot);
					mirrorRange = new MemoryRange(0, pot);
					Array.Resize(ref memory, (int)pot);
				}

				addressMask = (uint)(memoryRange.Length - 1);
			}
			catch
			{
				logger.LogTrace($"Kickstart Load Failed");

				memoryRange = new MemoryRange(0, 0);
				mirrorRange = new MemoryRange(0, 0);
				memory = Array.Empty<byte>();
				addressMask = 0;
			}
		}

		public bool IsPresent()
		{
			return memoryRange.Length != 0;
		}

		public new bool IsMapped(uint address)
		{
			return mirrorRange.Contains(address) || memoryRange.Contains(address);
		}

		public override List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange, mirrorRange};
		}

		private bool? wasMirrored = null;
		public void SetMirror(bool mirrored)
		{
			if (wasMirrored == mirrored) return;

			wasMirrored = mirrored;

			if (mirrored)
				mirrorRange.Length = memoryRange.Length;
			else
				mirrorRange.Length = 0;
			memoryManager.RefreshDevices();
		}

		public new void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Write to ROM {address:X8} @{insaddr:X8} {value:X8} {size}");
		}

		public override List<BulkMemoryRange> ReadBulk()
		{
			var ranges = base.ReadBulk();

			if (mirrorRange.Length != 0)
			{
				ranges.Add(new BulkMemoryRange
				{
					Start = mirrorRange.Start,
					Memory = (byte[])memory.Clone()
				});
			}
			return ranges;
		}
	}
}
