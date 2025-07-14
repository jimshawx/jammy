using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay.Overlays
{
	public interface IOverlayRenderer
	{
		void Render();
	}

	public class BaseOverlay
	{
		protected readonly INativeOverlay nativeOverlay;
		protected readonly ILogger logger;

		public BaseOverlay(INativeOverlay nativeOverlay, ILogger logger)
		{
			this.nativeOverlay = nativeOverlay;
			this.logger = logger;
		}
		public int[] screen => nativeOverlay.Screen;
		public int screenWidth => nativeOverlay.SCREEN_WIDTH;
		public int screenHeight => nativeOverlay.SCREEN_HEIGHT;
	}

	public interface IDiskLightOverlay : IOverlayRenderer
	{
		bool PowerLight { set; }
		bool DiskLight { set; }
	}

	public interface ITicksOverlay : IOverlayRenderer
	{
	}

	public interface ICpuUsageOverlay : IOverlayRenderer
	{
	}

}
