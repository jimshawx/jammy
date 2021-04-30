using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;

namespace RunAmiga.Main
{
	public class GfxScan
	{
		private readonly ILogger logger;
		private readonly IChipRAM chipRAM;
		private Form emulation;
		private readonly int maxMemory;

		public GfxScan(ILogger<GfxScan> logger, IChipRAM chipRAM)
		{
			this.logger = logger;
			this.chipRAM = chipRAM;
			maxMemory = ((IBulkMemoryRead)chipRAM).ReadBulk().First().Memory.Length;
			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				emulation = new Form {Name = "GfxScan", Text = "Gfx Scan", ControlBox = false, FormBorderStyle = FormBorderStyle.FixedSingle, MinimizeBox = true, MaximizeBox = true};

				if (emulation.Handle == IntPtr.Zero)
					throw new ApplicationException();

				ss.Release();

				SetPicture(320,900);

				emulation.Show();

				Application.Run(emulation);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		private Bitmap bitmap;
		private PictureBox picture;
		private HScrollBar slider;
		private int screenWidth;
		private int screenHeight;
		private int startAddress;

		public void SetPicture(int width, int height)
		{
			if (emulation.IsDisposed) return;

			emulation.Invoke((Action)delegate
			{
				screenWidth = width;
				screenHeight = height;

				var btn0 = new Button();
				btn0.Text = "Wider";
				btn0.Click += btnWiderClick;
				btn0.Location = new Point(0, 0);
				var btn1 = new Button();
				btn1.Text = "Narrower";
				btn1.Click += btnNarrowerClick;
				btn1.Location = new Point(btn0.Width, 0);
				slider = new HScrollBar();
				slider.Minimum = 0;
				slider.Maximum = maxMemory;
				slider.Value = 0;
				slider.ValueChanged += sliderChanged;
				slider.Location = new Point(btn0.Width + btn1.Width, 0);
				slider.Width = screenWidth - btn0.Width - btn1.Width;
				emulation.Controls.Add(btn0);
				emulation.Controls.Add(btn1);
				emulation.Controls.Add(slider);

				emulation.ClientSize = new Size(screenWidth, screenHeight+btn0.Height);

				bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppRgb);
				picture = new PictureBox {Image = bitmap, ClientSize = new Size(screenWidth, screenHeight), Enabled = false, Location = new Point(0,btn0.Height)};
				emulation.Controls.Add(picture);

				Redraw();

				emulation.Show();
			});
		}

		private void sliderChanged(object sender, EventArgs e)
		{
			startAddress = slider.Value;
			startAddress &= ~1;
			Redraw();
		}

		private void btnWiderClick(object sender, EventArgs e)
		{
			screenWidth += 16;
			bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppRgb);
			picture.Width = screenWidth;
			emulation.ClientSize = new Size(screenWidth, emulation.ClientSize.Height);
			Redraw();
		}

		private void btnNarrowerClick(object sender, EventArgs e)
		{
			screenWidth -= 16;
			if (screenWidth < 16)
				screenWidth = 16;
			bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppRgb);
			picture.Width = screenWidth;
			emulation.ClientSize = new Size(screenWidth, emulation.ClientSize.Height);
			Redraw();
		}

		private void Redraw()
		{
			int[] screen = new int[screenWidth * screenHeight];

			var bulk = ((IBulkMemoryRead)chipRAM).ReadBulk().First();

			int d=0;
			for (int i = startAddress; i < Math.Min(bulk.EndAddress, startAddress+ (screenWidth*screenHeight)/16); i += 2)
			{
				ushort b = (ushort)((bulk.Memory[i]<<8)+bulk.Memory[i + 1]);
				for (int bit = 0x8000; bit > 0; bit >>= 1)
					screen[d++] = (b &bit)!=0?0x0:0xffffff;
			}

			Blit(screen);
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
				picture.Refresh();
			});
		}
	}
}