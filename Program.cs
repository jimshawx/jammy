using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Custom;
using RunAmiga.Tests;

namespace RunAmiga
{
	public class Program
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
			
			var serviceCollection = new ServiceCollection();
			var serviceProvider = serviceCollection
				.AddLogging()
				.AddSingleton(serviceCollection)
				.AddSingleton<IMachine,Machine>()
				.AddSingleton<IAudio, Audio>()
				.AddSingleton<IBattClock, BattClock>()
				.AddSingleton<IBlitter, Blitter>()
				.AddSingleton<ICIAAOdd, CIAAOdd>()
				.AddSingleton<ICIABEven, CIABEven>()
				.AddSingleton<ICopper, Copper>()
				.AddSingleton<IDiskDrives, DiskDrives>()
				.AddSingleton<IKeyboard, Keyboard>()
				.AddSingleton<IMouse, Mouse>()
				.AddSingleton<IInterrupt, Interrupt>()
				.BuildServiceProvider();

			//var test = new CPUTest();
			//test.FuzzCPU();

			var machine = serviceProvider.GetService<IMachine>();

			var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();
			logger.LogDebug("This is a log message");
			logger.LogError("This is a log message");
			logger.LogTrace("This is a log message");

			var form = new RunAmiga(machine);
			form.Init();
			Application.Run(form);
		}
	}
}
