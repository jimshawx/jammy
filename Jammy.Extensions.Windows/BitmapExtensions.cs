using System.Drawing.Imaging;
using System.Runtime.InteropServices;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Extensions.Windows
{
	public static class BitmapExtensions
	{
		public static MemoryStream ToBmp(this byte[] memory, int width)
		{
			int byteWidth = width / 8;
			int height = memory.Length / byteWidth;

			var bmp = new Bitmap(width, height, PixelFormat.Format1bppIndexed);
			var locked = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
			IntPtr ptr = locked.Scan0;
			for (int j = 0; j < height * byteWidth; j += byteWidth)
			{
				Marshal.Copy(memory, j, ptr, byteWidth);
				ptr += locked.Stride;
			}
			bmp.UnlockBits(locked);
			var ms = new MemoryStream();
			bmp.Save(ms, ImageFormat.Bmp);
			
			return ms;
		}

		public static byte[] FromBmp(this Stream ms)
		{
			var bmp = new Bitmap(ms);

			int byteWidth = bmp.Width / 8;
			byte[] memory = new byte[byteWidth * bmp.Height];

			var locked = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
			IntPtr ptr = locked.Scan0;
			for (int j = 0; j < locked.Height * byteWidth; j += byteWidth)
			{
				Marshal.Copy(ptr, memory, j, byteWidth);
				ptr += locked.Stride;
			}
			bmp.UnlockBits(locked);
			return memory;
		}
	}
}
