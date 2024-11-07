using Jammy.Core.Debug;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Jammy.Main
{
	public class DMAExplorer
	{
		private Form form;
		private const int WW = 5;
		private const int HH = 5;
		private const int NX = 226;
		private const int NY = 313;

		//private IChipsetDebugger debugger;
		private ILogger logger;

		private DMAEntry[] dbg;

		public DMAExplorer(IChipsetDebugger debugger, ILogger logger)
		{
			//this.debugger = debugger;
			this.logger = logger;

			dbg = debugger.GetDMASummary();

			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				form = new Form { Name = "DMA", Text = "DMA", ControlBox = true, FormBorderStyle = FormBorderStyle.SizableToolWindow, MinimizeBox = true, MaximizeBox = true };
				form.ClientSize = new System.Drawing.Size(NX * WW, NY * HH);
				form.MaximumSize = form.Size;
				var pic = new PictureBox { Dock = DockStyle.Fill };
				pic.Paint += Form_Paint;
				form.Controls.Add(pic);

				form.MouseMove += MouseMove;

				if (form.Handle == IntPtr.Zero)
					throw new ApplicationException();

				ss.Release();

				form.Show();

				Application.Run(form);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		private void MouseMove(object sender, MouseEventArgs e)
		{
			
		}


		private readonly Brush [] dmacols =
		{
			new SolidBrush(Color.Black),//None
			new SolidBrush(Color.Red),//Read
			new SolidBrush(Color.White),//Write
			new SolidBrush(Color.Pink),//WriteReg
			new SolidBrush(Color.Teal),//Consume
			new SolidBrush(Color.Olive)//CPU
		};

		private void Form_Paint(object sender, PaintEventArgs e)
		{
			var r = new Rectangle();
			r.Width = WW;
			r.Height = HH;
			for (int y = 0; y < NY; y++)
			{
				r.Y = y*HH;
				for (int x = 0; x < NX; x++)
				{
					r.X = x*WW;
					var d = dbg[x+y*NX];
					e.Graphics.FillRectangle(dmacols[(int)d.Type], r);
				}
			}
		}
	}
}
