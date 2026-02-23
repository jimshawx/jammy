using Jammy.Core.Interface.Interfaces;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay.Overlays
{
	public class DiskLightOverlay : BaseOverlay, IDiskLightOverlay
	{
		private readonly IDriveLights driveLights;

		public DiskLightOverlay(INativeOverlay nativeOverlay, IDriveLights driveLights,
			ILogger<DiskLightOverlay> logger) : base(nativeOverlay, logger)
		{
			this.driveLights = driveLights;
		}

		public void Render()
		{
			int sx = screenWidth - 100;
			int sy = 20;
			for (int y = 0; y < 8; y++)
			{
				for (int x = 0; x < 24; x++)
				{
					screen[x + sx + (sy + y) * screenWidth] = driveLights.PowerLight ? 0xff0000 : 0x7f0000;
					screen[x + sx + 32 + (sy + y) * screenWidth] = driveLights.DiskLight ? 0x00ff00 : 0x007f00;
				}
			}
		}

	}
}
