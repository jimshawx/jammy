using Jammy.Core.Interface.Interfaces;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay.Overlays
{
	public class DebugOverlay : BaseOverlay, IDebugOverlay
	{
		private readonly IChipsetDebugger chipsetDebugger;

		public DebugOverlay(IChipsetDebugger chipsetDebugger, ILogger<DebugOverlay> logger) : base(logger)
		{
			this.chipsetDebugger = chipsetDebugger;
		}

		public void Render()
		{
			DebugPalette();
			DebugText();
			DebugLocation();
		}

		private void DebugPalette()
		{
			var truecolour = chipsetDebugger.GetDebugPalette();

			int sx = 256;
			int sy = 5;

			int box = 5;
			for (int y = 0; y < 4; y++)
			{
				for (int x = 0; x < 64; x++)
				{
					for (int p = 0; p < box; p++)
					{
						for (int q = 0; q < box; q++)
						{
							screen[sx + x * box + q + (sy + (y * box) + p) * screenWidth] = (int)truecolour[x + y * 64];
						}
					}
				}
			}
		}

		private void DebugText()
		{
			string regmsg = chipsetDebugger.GetOverlayText();
			nativeOverlay.TextScale(2);
			const int bg = 0xffffff;
			const int fg = 0x000000;
			nativeOverlay.WriteText(0, 81, bg, regmsg);
			nativeOverlay.WriteText(2, 81, bg, regmsg);
			nativeOverlay.WriteText(1, 80, bg, regmsg);
			nativeOverlay.WriteText(1, 82, bg, regmsg);
			nativeOverlay.WriteText(1, 81, fg, regmsg);
		}

		private void DebugLocation()
		{
			int dbugLine = chipsetDebugger.GetDebugLocation();

			if (dbugLine < 0) return;
			if (dbugLine >= screenHeight / 2) return;

			for (int x = 0; x < screenWidth; x += 4)
				screen[x + dbugLine * screenWidth * 2] ^= 0xffffff;
		}
	}
}
