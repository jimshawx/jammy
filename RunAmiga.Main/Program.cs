using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Core;
using RunAmiga.Core.Custom;
using RunAmiga.Core.Interfaces;
using RunAmiga.Core.Types;

namespace RunAmiga.Main
{
	public class Program
	{

		[STAThread]
		static void Main()
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.Build();


			var serviceCollection = new ServiceCollection();
			var serviceProvider = serviceCollection
				.AddSingleton<IConfigurationRoot>(configuration)
				.AddLogging(x=>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					x.AddDebug();
				})
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
				.AddSingleton<IEmulation, Emulation>()
				.BuildServiceProvider();

			ServiceProviderFactory.ServiceProvider = serviceProvider;

			//var test = new CPUTest();
			//test.FuzzCPU();

			var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
			logger.LogTrace("Application Starting Up!");

			var emulation = serviceProvider.GetRequiredService<IEmulation>();

			var form = new RunAmiga(emulation);
			form.Init();
			Application.Run(form);
		}
	}
}
