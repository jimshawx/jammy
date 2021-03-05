using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace RunAmiga.Core
{
	public interface IEmulationWindow
	{
		bool IsCaptured { get; }
		void SetPicture(int screenWidth, int screenHeight);
		void Blit(int[] screen);
		Point RecentreMouse();
		void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp);
	}

	public class EmulationWindow : IEmulationWindow
	{
		private readonly ILogger logger;
		private Form emulation;

		public EmulationWindow(ILogger<EmulationWindow> logger)
		{
			this.logger = logger;

			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				emulation = new Form {Name = "Emulation"};

				if (emulation.Handle == IntPtr.Zero)
					throw new ApplicationException();

				ss.Release();

				emulation.MouseClick += Emulation_MouseClick;
				emulation.KeyPress += Emulation_KeyPress;
				emulation.Show();

				Application.Run(emulation);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		public bool IsCaptured { get; private set; } = false;

		private void Capture(string where)
		{
			if (!IsCaptured)
			{
				logger.LogTrace($"Capture {where}");
				IsCaptured = true;
				Cursor.Hide();
				Cursor.Clip = emulation.RectangleToScreen(emulation.ClientRectangle);
			}
		}

		private void Release(string where)
		{
			logger.LogTrace($"Release {where}");
			IsCaptured = false;
			Cursor.Show();
			Cursor.Clip = new Rectangle(0, 0, 0, 0);
		}

		private void Emulation_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				if (Control.MouseButtons == (MouseButtons.Left | MouseButtons.Right))
					Release("Click");
				else
					Capture("Click");
			}
		}

		private void Emulation_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == 0x1B)
				Release("KeyPress");
		}

		private Bitmap bitmap;
		private PictureBox picture;
		private int screenWidth;
		private int screenHeight;

		public void SetPicture(int width, int height)
		{
			if (emulation.IsDisposed) return;

			emulation.Invoke((Action)delegate
			{
				screenWidth = width;
				screenHeight = height;

				emulation.ClientSize = new Size(screenWidth, screenHeight);
				bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppRgb);
				picture = new PictureBox {Image = bitmap, ClientSize = new Size(screenWidth, screenHeight), Enabled = false};

				//try to scale the box
				//picture.SizeMode = PictureBoxSizeMode.StretchImage;
				//int scaledHeight = (SCREEN_HEIGHT * 6) / 5;
				//emulation.ClientSize = new System.Drawing.Size(SCREEN_WIDTH, scaledHeight);
				//picture.ClientSize = new System.Drawing.Size(SCREEN_WIDTH, scaledHeight);

				emulation.Controls.Add(picture);
				emulation.Show();
			});
		}

		public void Blit(int[] screen)
		{
			if (emulation.IsDisposed) return;

			emulation.Invoke((Action)delegate
			{
				var bitmapData = bitmap.LockBits(new Rectangle(0, 0, screenWidth, screenHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
				Marshal.Copy(screen, 0, bitmapData.Scan0, screen.Length);
				bitmap.UnlockBits(bitmapData);
				picture.Image = bitmap;
				emulation.Invalidate();
			});
		}

		public Point RecentreMouse()
		{
			var centre = new Point(0, 0);

			if (!emulation.IsDisposed)
			{
				emulation.Invoke((Action)delegate()
				{
					//put the cursor back in the middle of the emulation window
					var emuRect = emulation.RectangleToScreen(emulation.ClientRectangle);
					centre = new Point(emuRect.X + emuRect.Width / 2, emuRect.Y + emuRect.Height / 2);
					Cursor.Position = centre;
				});
			}

			return centre;
		}

		public void SetKeyHandlers(Action<int> addKeyDown, Action<int> addKeyUp)
		{
			emulation.KeyDown += (sender, e) => addKeyDown(e.KeyValue);
			emulation.KeyUp += (sender, e) => addKeyUp(e.KeyValue);
		}
	}
}