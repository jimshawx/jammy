using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace RunAmiga.Core
{
	public interface IEmulationWindow
	{
		Form GetForm();
		bool IsCaptured { get; }
	}

	public class EmulationWindow : IEmulationWindow
	{
		private readonly ILogger logger;
		private readonly Form emulation;

		public EmulationWindow(ILogger<EmulationWindow> logger)
		{
			this.logger = logger;
			this.emulation = new Form { Name = "Emulation" };
			emulation.MouseClick += Emulation_MouseClick;
			emulation.KeyPress += Emulation_KeyPress;
		}

		public Form GetForm()
		{
			return emulation;
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
	}
}
