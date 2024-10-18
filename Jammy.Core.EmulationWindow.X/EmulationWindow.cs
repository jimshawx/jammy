using Jammy.Core.Interface.Interfaces;
using Jammy.NativeOverlay;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.EmulationWindow.X
{
	public class EmulationWindow : IEmulationWindow, IDisposable
	{
		private readonly INativeOverlay nativeOverlay;
		private readonly ILogger logger;
		// Form emulation;
		private int[] screen;
		public EmulationWindow(INativeOverlay nativeOverlay, ILogger<EmulationWindow> logger)
		{
			this.nativeOverlay = nativeOverlay;
			this.logger = logger;


		}

		public void Dispose()
		{
			//emulation.Close();
		}
		public bool PowerLight { private get; set; }
		public bool DiskLight { private get; set; }

		public void Blit(int[] screen)
		{
			RenderTicks();
			RenderLights();


		}

		public bool IsCaptured { get; private set; } = false;
		private int screenWidth;
		private int screenHeight;

		private int displayHz;

		public void SetPicture(int width, int height)
		{
		}

		private DateTime lastTick = DateTime.Now;
		private float[] fpsarr = new float[128];
		private int fpsarrpos = 0;
		private void RenderTicks()
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

		private void RenderLights()
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

		public Types.Types.Point RecentreMouse()
		{
			//put the cursor back in the middle of the emulation window

			int x = 0;
			int y = 0;
			return new Types.Types.Point { X = x, Y = y };
		}

		public void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp)
		{

		}

		public bool IsActive()
		{
			return IsCaptured;
		}

		public int[] GetFramebuffer()
		{
			return screen;
		}
	}
}