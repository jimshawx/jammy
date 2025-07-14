using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay.Overlays
{
	public class TicksOverlay : BaseOverlay, ITicksOverlay
	{
		public TicksOverlay(INativeOverlay nativeOverlay, ILogger<TicksOverlay> logger) : base(nativeOverlay, logger)
		{
		}

		public bool PowerLight { private get; set; }
		public bool DiskLight { private get; set; }

		private DateTime lastTick = DateTime.Now;
		private float[] fpsarr = new float[128];
		private int fpsarrpos = 0;
		private const int displayHz = 120;

		public void Render()
		{
			var now = DateTime.Now;
			TimeSpan dt = now - lastTick;
			lastTick = now;

			if (dt > TimeSpan.Zero && dt.Milliseconds <= 1000)
			{
				int so = 20 + 10 * screenWidth;
				int ss = 2;
				var fps = 1000.0f / dt.Milliseconds;
				fpsarr[fpsarrpos++] = fps;
				fpsarrpos &= fpsarr.Length - 1;
				var avefps = fpsarr.Sum() / fpsarr.Length;

				for (int i = 0; i <= displayHz * ss; i += 10 * ss)
				{
					for (int y = 0; y < 8 * ss; y++)
						screen[so + i + y * screenWidth] = 0xffffff;
				}

				for (int i = 0; i < fps * ss; i++)
				{
					for (int y = 0; y < 3 * ss; y++)
						screen[so + i + y * screenWidth] = 0xff0000;
				}

				for (int i = 0; i < avefps * ss; i++)
				{
					for (int y = 0; y < 3 * ss; y++)
						screen[so + i + (4 * ss + y) * screenWidth] = 0x0000ff;
				}
				nativeOverlay.WriteText(20 + (int)fps * ss + 4, 10, 0xffffff, $"{(int)fps}");
				nativeOverlay.WriteText(20 + (int)avefps * ss + 4, 10 + 4 * ss, 0xffffff, $"{(int)avefps}");
			}
		}
	}
}
