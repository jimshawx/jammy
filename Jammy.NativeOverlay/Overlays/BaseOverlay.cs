using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay.Overlays
{
	public interface IOverlayRenderer
	{
		void Render();
		void SetNativeOverlay(INativeOverlay nativeOverlay);
	}

	public class BaseOverlay
	{
		protected INativeOverlay nativeOverlay;
		protected readonly ILogger logger;

		public BaseOverlay(ILogger logger)
		{
			this.logger = logger;
		}

		public void SetNativeOverlay(INativeOverlay nativeOverlay)
		{
			this.nativeOverlay = nativeOverlay;
		}

		public int[] screen => nativeOverlay.Screen;
		public int screenWidth => nativeOverlay.SCREEN_WIDTH;
		public int screenHeight => nativeOverlay.SCREEN_HEIGHT;
	}

	public interface IDiskLightOverlay : IOverlayRenderer {}

	public interface ITicksOverlay : IOverlayRenderer {}

	public interface ICpuUsageOverlay : IOverlayRenderer {}

	public interface IDebugOverlay : IOverlayRenderer { }

}
