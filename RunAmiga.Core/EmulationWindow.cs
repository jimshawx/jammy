using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace RunAmiga.Core
{
	public interface IEmulationWindow
	{
		Form GetForm();
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

		private bool isCaptured = false;

		private void Capture(string where)
		{
			if (!isCaptured)
			{
				logger.LogTrace($"Capture {where}");
				isCaptured = true;
				Cursor.Hide();
				Cursor.Clip = emulation.RectangleToScreen(emulation.ClientRectangle);
			}
		}

		private void Release(string where)
		{
			//if (isCaptured)
			{
				logger.LogTrace($"Release {where}");
				isCaptured = false;
				Cursor.Show();
				Cursor.Clip = new Rectangle(0, 0, 0, 0);
			}
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
