using System.Runtime.InteropServices;

namespace Jammy.Extensions.Windows
{
	public static class DarkMode
	{

		private static void ApplyDarkMode(Control ctl)
		{
			ctl.ForeColor = Color.FromArgb(ctl.ForeColor.ToArgb() ^ 0xffffff);
			ctl.BackColor = Color.FromArgb(ctl.BackColor.ToArgb() ^ 0xffffff);
			SetWindowTheme(ctl.Handle, "DarkMode_Explorer", null);

			if (ctl is Button)
			{
				var btn = (Button)ctl;
				btn.FlatStyle = FlatStyle.Flat;
				btn.ForeColor = Color.White;
				btn.BackColor = Color.Black;
				btn.FlatAppearance.BorderColor = Color.Gray;
				btn.FlatAppearance.MouseOverBackColor = Color.Gray;
				btn.FlatAppearance.MouseDownBackColor = Color.DarkGray;
			}
			else if (ctl is RadioButton)
			{
				var btn = (RadioButton)ctl;
				btn.FlatStyle = FlatStyle.Flat;
				btn.ForeColor = Color.White;
				btn.BackColor = Color.Black;
			}
			else if (ctl is SplitContainer)
			{
				var spt = (SplitContainer)ctl;
			}
			else if (ctl is TabControl)
			{
				var tab = (TabControl)ctl;
			}

			foreach (var c in ctl.Controls.Cast<Control>())
				ApplyDarkMode(c);
		}

		// Windows 10+ dark mode attribute
		private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

		[DllImport("dwmapi.dll")]
		private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		[DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

		public static void Apply(Form form)
		{
			int useDark = 1;
			if (Environment.OSVersion.Version.Major >= 10)
				DwmSetWindowAttribute(form.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
			ApplyDarkMode(form);
		}
	}
}
