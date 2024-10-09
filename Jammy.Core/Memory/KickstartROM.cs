using System;
using System.Collections.Generic;
using System.IO;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public class KickstartROM : Memory, IKickstartROM
	{
		private readonly IMemoryManager memoryManager;
		private readonly ILogger logger;
		private string path;
		private string name;

		private readonly MemoryRange mirrorRange;

		public KickstartROM(IMemoryManager memoryManager, IOptions<EmulationSettings> settings, ILogger<KickstartROM> logger)
		{
			this.memoryManager = memoryManager;
			this.logger = logger;
			//switch (settings.Value.KickStartDisassembly)
			//{
			//	case "1.2": path = "kick12.rom"; name = "Kickstart 1.2"; break;
			//	case "1.3": path = "kick13.rom"; name = "Kickstart 1.3"; break;
			//	case "2.04": path = "kick204.rom"; name = "Kickstart 2.04"; break;
			//	case "2.05": path = "kick205.rom"; name = "Kickstart 2.05"; break;
			//	case "3.1": path = "kick31.rom"; name = "Kickstart 3.1"; break;
			//	default:
			//		path = settings.Value.KickStart;
			//		name = Path.GetFileName(settings.Value.KickStart);
			//		break;
			//}
			path = settings.Value.KickStart;
			name = Path.GetFileName(settings.Value.KickStart);

			string fullName = Path.Combine("../../../../roms", path);

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
					memoryRange = new MemoryRange(0xf80000, 0x2000);
					mirrorRange = new MemoryRange(0, 0x2000);
				}
				else
				{
					throw new ArgumentOutOfRangeException();
				}

				addressMask = (uint)(memoryRange.Length - 1);
			}
			catch
			{
				logger.LogTrace($"Kickstart Load Failed");

				memoryRange = new MemoryRange(0, 0);
				mirrorRange = new MemoryRange(0, 0);
				addressMask = 0;
			}
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
