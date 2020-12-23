using System;
using System.Windows.Forms;

namespace RunAmiga
{
	static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var machine = new Machine();
			var form = new Form1(machine);
			form.Init();
			Application.Run(form);
		}
	}
}
