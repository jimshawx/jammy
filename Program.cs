using System;
using System.Windows.Forms;
using RunAmiga.Tests;

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

			//var test = new CPUTest();
			//test.FuzzCPU();

			var machine = new Machine();
			var form = new RunAmiga(machine);
			form.Init();
			Application.Run(form);
		}
	}
}
