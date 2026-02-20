using System;
using System.IO;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{
	public class ExtendedKickstartROM : Memory, IExtendedKickstartROM
	{
		private readonly IMemoryManager memoryManager;
		private readonly ILogger logger;
		private string path;
		private string name;

		public ExtendedKickstartROM(IMemoryManager memoryManager, IOptions<EmulationSettings> settings, ILogger<ExtendedKickstartROM> logger)
		{
			this.memoryManager = memoryManager;
			this.logger = logger;

			if (string.IsNullOrEmpty(settings.Value.KickStart))
			{
				logger.LogTrace($"No Extended Kickstart ROM specified");

				memoryRange = new MemoryRange(0, 0);
				memory = Array.Empty<byte>();
				addressMask = 0;

				return;
			}

			path = settings.Value.KickStart;
			name = Path.GetFileName(settings.Value.KickStart);

			path = path.Replace(".", "-ext.");
			string fullName = Path.Combine("roms", path);

			try
			{
				logger.LogTrace($"Extended Kickstart Loading {path} {name}");

				memory = File.ReadAllBytes(fullName);
				if (memory.Length == 512 * 1024)
				{
					memoryRange = new MemoryRange(0xe00000, 0x80000);
				}
				
				else
				{
					logger.LogTrace($"Extended Kickstart ROM is not a standard size ({memory.Length:X8})");
					memoryRange = new MemoryRange(0, 0);
					memory = Array.Empty<byte>();
					addressMask = 0;
				}

				addressMask = (uint)(memoryRange.Length - 1);
			}
			catch
			{
				logger.LogTrace($"Extended Kickstart Load Failed");

				memoryRange = new MemoryRange(0, 0);
				memory = Array.Empty<byte>();
				addressMask = 0;
			}
		}

		public new void Write(uint insaddr, uint address, uint value, Size size)
		{
			logger.LogTrace($"Write to Extended ROM {address:X8} @{insaddr:X8} {value:X8} {size}");
		}
	}
}
