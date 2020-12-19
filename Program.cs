using System;
using System.Windows.Forms;

namespace runamiga
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
			machine.Init();

			Application.Run(new Form1());
		}
	}
}
