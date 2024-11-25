using Jammy.Core.Debug;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Jammy.Main
{
	public class DMAExplorer
	{
		private Form form;
		private PictureBox pic;
		private const int WW = 10;
		private const int HH = 5;
		private const int NX = 228;
		private const int NY = 313;
		private ILogger logger;

		private DMAEntry[] dbg;

		public DMAExplorer(IChipsetDebugger debugger, ILogger<DMAExplorer> logger)
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
				pic = new PictureBox { Dock = DockStyle.Fill };
				pic.Controls.Add(textBox);
				pic.Paint += Repaint;
				pic.MouseMove += MouseMove;
				form.Controls.Add(pic);

				if (form.Handle == IntPtr.Zero)
					throw new ApplicationException();

				ss.Release();

				form.Show();

				new Thread(() =>
				{
					try
					{
						for (;;)
						{
							form.Invoke(() => form.Refresh());
							Thread.Sleep(33);
						}
					}
					catch (ObjectDisposedException) { /* normal failure mode when closing */ }
					catch (Exception ex) {
						/* don't care, but something bad happened */
						logger.LogTrace(ex.ToString());
						}
				}).Start();

				Application.Run(form);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		private int selectX=-1, selectY=-1;
		private Control textBox = new TextBox { Multiline = true, ReadOnly = true, Width = 200, Height = 200, Enabled = false, Font = new Font(FontFamily.GenericMonospace, 6), Visible = false };
		private StringBuilder sb = new StringBuilder();
		private void MouseMove(object sender, MouseEventArgs e)
		{
			var loc = e.Location;//form.PointToClient(e.Location);
			selectX = loc.X / WW;
			selectY = loc.Y / HH;
			
			if (selectX < 0 || selectY < 0) return;
			if (selectX >= NX) return;
			if (selectY >= NY) return;

			ref var d = ref dbg[selectX+NX*selectY];
			sb.Clear();
			sb.AppendLine($"   Type: {d.Type}");
			if (d.Type != DMAActivityType.None && d.Type != DMAActivityType.CPU)
			{
				if (d.Type == DMAActivityType.WriteReg)
					sb.AppendLine($"    Reg: {ChipRegs.Name(d.ChipReg)} {d.ChipReg:X6}");

				if (d.Type != DMAActivityType.Consume)
				{ 
					sb.AppendLine($"Address: {d.Address:X8}");
					sb.AppendLine($"   Size: {d.Size}");
				}
				sb.AppendLine($"    Pri: {d.Priority}");

				if (d.Type != DMAActivityType.Consume)
				{
					if (d.Size == Core.Types.Types.Size.Long)
						sb.AppendLine($"  Value: {d.Value:X8} {d.Value.ToBin(32)} {d.Value}");
					if (d.Size == Core.Types.Types.Size.Word)
						sb.AppendLine($"  Value: {d.Value:X4} {d.Value.ToBin(16)} {(short)d.Value}");
					if (d.Size == Core.Types.Types.Size.Byte)
						sb.AppendLine($"  Value: {d.Value:X2} {d.Value.ToBin(8)} {(sbyte)d.Value}");
				}
			}

			textBox.Text = sb.ToString();
			textBox.Left = selectX * WW;
			textBox.Top = selectY * HH;
			textBox.Visible = true;
		}

		private readonly Brush [] dmacols =
		{
			new SolidBrush(Color.Black),//None
			new SolidBrush(Color.Red),//Read
			new SolidBrush(Color.White),//Write
			new SolidBrush(Color.Pink),//WriteReg
			new SolidBrush(Color.Teal),//Consume
			new SolidBrush(Color.Green)//CPU
		};

		private readonly Brush[] pricols =
		{
			new SolidBrush(Color.Cyan),//AUD0EN
			new SolidBrush(Color.Cyan),//AUD1EN
			new SolidBrush(Color.Cyan),//AUD2EN
			new SolidBrush(Color.Cyan),//AUD3EN
			new SolidBrush(Color.Teal),//DSKEN
			new SolidBrush(Color.Green),//SPREN
			new SolidBrush(Color.Blue),//BLTEN
			new SolidBrush(Color.Red),//COPEN
			new SolidBrush(Color.Orange),//BPLEN
			new SolidBrush(Color.DarkGoldenrod),//DMAEN aka Refresh
		};

		private readonly Brush black = new SolidBrush(Color.Black);
		private readonly Brush white = new SolidBrush(Color.White);
		private readonly Brush grey = new SolidBrush(Color.Gray);

		private void Repaint(object sender, PaintEventArgs e)
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
					//e.Graphics.FillRectangle(dmacols[(int)d.Type], r);
					if (d.Type == DMAActivityType.None)
						e.Graphics.FillRectangle(black, r);
					//else if (d.Type == DMAActivityType.Consume)
					//	e.Graphics.FillRectangle(grey, r);
					else if (d.Type == DMAActivityType.CPU)
						e.Graphics.FillRectangle(white, r);
					else
						e.Graphics.FillRectangle(pricols[(int)Math.Log2((int)d.Priority)], r);
				}
			}
			if (selectX < 0 || selectY < 0) return;
		}
	}
}
