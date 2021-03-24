using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Memory
{
	public class KickstartROM : Memory, IKickstartROM
	{
		private readonly ILogger logger;
		private string path;
		private string name;

		private readonly MemoryRange mirrorRange;
		private bool isMirrored = false;

		public KickstartROM(IOptions<EmulationSettings> settings, ILogger<KickstartROM> logger)
		{
			this.logger = logger;
			switch (settings.Value.KickStart)
			{
				case "1.2": path = "kick12.rom"; name = "Kickstart 1.2"; break;
				case "1.3": path = "kick13.rom"; name = "Kickstart 1.3"; break;
				case "2.04": path = "kick204.rom"; name = "Kickstart 2.04"; break;
				case "2.05": path = "kick205.rom"; name = "Kickstart 2.05"; break;
				case "3.1": path = "kick31.rom"; name = "Kickstart 3.1"; break;
			}

			string fullName = Path.Combine("../../../../", path);

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
				else
				{
					throw new ArgumentOutOfRangeException();
				}
				addressMask = memoryRange.Length - 1;
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

		public void SetMirror(bool mirrored)
		{
			if (mirrored)
				mirrorRange.Length = memoryRange.Length;
			else
				mirrorRange.Length = 0;
		}

		public new void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Write to ROM {address:X8} @{insaddr:X8} {value:X8} {size}");
		}
	}
}
