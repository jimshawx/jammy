using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Custom;
using RunAmiga.Tests;
using RunAmiga.Types;

namespace RunAmiga
{
	public class Program
	{
		public static IServiceProvider ServiceProvider { get; private set; }

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
			ServiceProvider = serviceCollection
				.AddLogging()
				.AddSingleton<IMachine, Machine>()
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
				.AddSingleton<ICPU>(x =>
					new MusashiCPU(
						x.GetRequiredService<IInterrupt>(),
						x.GetRequiredService<IMemoryMapper>(),
						new BreakpointCollection(),
						x.GetRequiredService<ILoggerFactory>().CreateLogger<MusashiCPU>()))
				.AddSingleton<IDebugger, Debugger>()
				.AddSingleton<IChips, Chips>()
				.AddSingleton<IMemory>(x =>
					new Memory("M",
						x.GetRequiredService<ILoggerFactory>().CreateLogger<Memory>()))
				.AddSingleton<IMemoryMapper, MemoryMapper>()
				.BuildServiceProvider();

			//var test = new CPUTest();
			//test.FuzzCPU();

			var machine = ServiceProvider.GetRequiredService<IMachine>();

			var logger = ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
			logger.LogDebug("This is a log message");
			logger.LogError("This is a log message");
			logger.LogTrace("This is a log message");

			var form = new RunAmiga(machine);
			form.Init();
			Application.Run(form);
		}
	}
}
