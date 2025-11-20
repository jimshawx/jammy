/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay
{
	public interface INativeOverlay
	{
		void Init(int[] screen, int screenWidth, int screenHeight);
		void WriteText(int x, int y, int colour, string txt);
		void TextScale(int s);
		int[] Screen {get;}
		int SCREEN_WIDTH { get; }
		int SCREEN_HEIGHT { get; }
	}

	public partial class NativeOverlay : INativeOverlay
	{
		private int[] screen;
		private int width;
		private int height;
		
		public void Init(int[] screen, int width, int height)
		{
			this.screen = screen;
			this.width = width;
			this.height = height;
		}

		public int SCREEN_WIDTH => width;
		public int SCREEN_HEIGHT => height;
		public int[] Screen => screen;

		private int sx = 2, sy = 2;

		public void TextScale(int s)
		{
			sx = sy = s;
		}

		public void WriteText(int x, int y, int colour, string txt)
		{
			bool skipUntilNewline = false;
			int ox = x;
			foreach (var c in txt)
			{
				if (c == '\n')
				{
					x = ox;
					y += (6 * sy);
					skipUntilNewline = false;
				}

				if (skipUntilNewline)
					continue;

				if (x >= width)
				{
					skipUntilNewline = true;
					continue;
				};

				if (y >= height) return;
					
				if (c < 31 || c > 127)
					continue;


				var letter = chars[c];

				for (int yy = y; yy < Math.Min(height, y + 5 * sy); yy++)
				{
					for (int xx = x; xx < Math.Min(width, x + 3 * sx); xx++)
					{
						if (letter[(yy - y) / sy][(xx - x) / sx] == 1)
							screen[xx + yy * width] ^= colour;
					}
				}

				x += 4 * sx;
			}
		}
	}
}
