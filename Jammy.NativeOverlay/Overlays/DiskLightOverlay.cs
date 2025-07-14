using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay.Overlays
{
	public class DiskLightOverlay : BaseOverlay, IDiskLightOverlay
	{
		public DiskLightOverlay(INativeOverlay nativeOverlay, ILogger<DiskLightOverlay> logger) : base(nativeOverlay, logger)
		{
		}

		public bool PowerLight { private get; set; }
		public bool DiskLight { private get; set; }

		public void Render()
		{
			int sx = screenWidth - 100;
			int sy = 20;
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 24; x++)
				{
					screen[x + sx + (sy + y) * screenWidth] = PowerLight ? 0xff0000 : 0x7f0000;
					screen[x + sx + 32 + (sy + y) * screenWidth] = DiskLight ? 0x00ff00 : 0x007f00;
				}
			}
		}

	}
}
