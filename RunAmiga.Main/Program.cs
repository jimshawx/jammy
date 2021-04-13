using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Core;
using RunAmiga.Core.CPU.CSharp;
using RunAmiga.Core.CPU.Musashi;
using RunAmiga.Core.Custom;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Memory;
using RunAmiga.Core.Types;
using RunAmiga.Debugger;
using RunAmiga.Disassembler;
using RunAmiga.Disassembler.Analysers;
using RunAmiga.Interface;
using RunAmiga.Logger.SQLite;
using RunAmiga.Logger.DebugAsync;
using RunAmiga.Logger.DebugAsyncRTF;

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

			var settings = new EmulationSettings();
			configuration.Bind("Emulation", settings);

			var serviceCollection = new ServiceCollection();
			var services = serviceCollection
				.AddSingleton<IConfigurationRoot>(configuration)
				.AddLogging(x =>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					//x.AddDebug();
					//x.AddSQLite();
					//x.AddDebugAsync();
					x.AddDebugAsyncRTF();
				})
				.AddSingleton<IMachine, Machine>()
				.AddSingleton<IBattClock, BattClock>()
				.AddSingleton<IBlitter, Blitter>()
				.AddSingleton<ICIAAOdd, CIAAOdd>()
				.AddSingleton<ICIABEven, CIABEven>()
				.AddSingleton<ICIAMemory, CIAMemory>()
				.AddSingleton<ICopper, Copper>()
				.AddSingleton<IDiskDrives, DiskDrives>()
				.AddSingleton<IKeyboard, Keyboard>()
				.AddSingleton<IMouse, Mouse>()
				.AddSingleton<ISerial, Serial>()
				.AddSingleton<IZorro, Zorro>()
				.AddSingleton<IChipRAM, ChipRAM>()
				.AddSingleton<ITrapdoorRAM, TrapdoorRAM>()
				.AddSingleton<IKickstartROM, KickstartROM>()
				.AddSingleton<IZorroConfigurator, ZorroConfigurator>()
				.AddSingleton<IUnmappedMemory, UnmappedMemory>()
				.AddSingleton<IInterrupt, Interrupt>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<IDebugger, Debugger.Debugger>()
				.AddSingleton<IChips, Chips>()
				.AddSingleton<IIDEController, IDEController>()
				.AddSingleton<ISCSIController, SCSIController>()
				.AddSingleton<IAkiko, Akiko>()
				.AddSingleton<MemoryMapper>()
				.AddSingleton<IMemoryMapper>(x => x.GetRequiredService<MemoryMapper>())
				.AddSingleton<IDebugMemoryMapper>(x => x.GetRequiredService<MemoryMapper>())
				.AddSingleton<IMemoryManager, MemoryManager>()
				.AddSingleton<IEmulationWindow, EmulationWindow>()
				.AddSingleton<IEmulation, Emulation>()
				.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<IDisassembly, Disassembly>()
				.AddSingleton<IAnalysis, Analysis>()
				.AddSingleton<IAnalyser, Analyser>()
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("Amiga"))
				.AddSingleton<RunAmiga>()
				.Configure<EmulationSettings>(o => configuration.Bind("Emulation", o));

			//configure Audio
			if (settings.Audio == AudioDriver.XAudio2)
				services.AddSingleton<IAudio, AudioV2>();
			else
				services.AddSingleton<IAudio, Audio>();

			//configure CPU
			if (settings.CPU == CPUType.Musashi)
				services.AddSingleton<ICPU, MusashiCPU>();
			else
				services.AddSingleton<ICPU, CPU>();

			//configure Tracing
			if (settings.Tracer == Feature.Enabled)
				services.AddSingleton<ITracer, Tracer>();
			else
				services.AddSingleton<ITracer, NullTracer>();

			//configure Serial Console
			if (settings.Console == SerialConsole.ANSI)
				services.AddSingleton<ISerialConsole, ANSIConsole>();
			else if (settings.Console == SerialConsole.Emulation)
				services.AddSingleton<ISerialConsole, EmulationConsole>();
			else
				services.AddSingleton<ISerialConsole, NullConsole>();

			var serviceProvider = services.BuildServiceProvider();

			ServiceProviderFactory.ServiceProvider = serviceProvider;

			var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
			logger.LogTrace("Application Starting Up!");

			var form = serviceProvider.GetRequiredService<RunAmiga>();
			Application.Run(form);
		}
	}
}
