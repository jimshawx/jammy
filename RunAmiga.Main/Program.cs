using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Core;
using RunAmiga.Core.CPU.CSharp;
using RunAmiga.Core.CPU.CSharp.MC68020;
using RunAmiga.Core.CPU.Musashi;
using RunAmiga.Core.CPU.Musashi.MC68020;
using RunAmiga.Core.CPU.Musashi.MC68030;
using RunAmiga.Core.Custom;
using RunAmiga.Core.Custom.Audio;
using RunAmiga.Core.Custom.CIA;
using RunAmiga.Core.Custom.IO;
using RunAmiga.Core.Floppy;
using RunAmiga.Core.IDE;
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
using RunAmiga.UI.Settings;

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

			var set = new Settings();
			Application.Run(set);
			if (!set.ConfigOK) return;

			var appConfig = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appSettings.json", false)
				.Build();

			var emuConfig = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("emulationSettings.json", false)
				.Build();

			var settings = new EmulationSettings();
			emuConfig.Bind("Emulation", settings);

			var serviceCollection = new ServiceCollection();
			var services = serviceCollection
				.AddSingleton<IConfigurationRoot>(appConfig)
				.AddLogging(x =>
				{
					x.AddConfiguration(appConfig.GetSection("Logging"));
					//x.AddDebug();
					//x.AddSQLite();
					x.AddDebugAsync();
					//x.AddDebugAsyncRTF();
				})
				.AddSingleton<IAmiga, Amiga>()
				.AddSingleton<IBattClock, BattClock>()
				.AddSingleton<IMotherboard, Motherboard>()
				.AddSingleton<IBlitter, Blitter>()
				.AddSingleton<ICIAAOdd, CIAAOdd>()
				.AddSingleton<ICIABEven, CIABEven>()
				.AddSingleton<ICIAMemory, CIAMemory>()
				.AddSingleton<ICopper, Copper>()
				.AddSingleton<IDiskDrives, DiskDrives>()
				.AddSingleton<IKeyboard, Keyboard>()
				.AddSingleton<IMouse, Mouse>()
				.AddSingleton<ISerial, Serial>()
				.AddSingleton<IZorro2, Zorro2>()
				.AddSingleton<IZorro3, Zorro3>()
				.AddSingleton<IChipRAM, ChipRAM>()
				.AddSingleton<ITrapdoorRAM, TrapdoorRAM>()
				.AddSingleton<IKickstartROM, KickstartROM>()
				.AddSingleton<IMotherboardRAM, MotherboardRAM>()
				.AddSingleton<ICPUSlotRAM, CPUSlotRAM>()
				.AddSingleton<IZorroConfigurator, ZorroConfigurator>()
				.AddSingleton<IUnmappedMemory, UnmappedMemory>()
				.AddSingleton<IInterrupt, Interrupt>()
				.AddSingleton<IBreakpointCollection, BreakpointCollection>()
				.AddSingleton<IDebugger, Debugger.Debugger>()
				.AddSingleton<IChips, Chips>()
				.AddSingleton<IAkiko, Akiko>()
				.AddSingleton<MemoryMapper>()
				.AddSingleton<IMemoryMapper>(x => x.GetRequiredService<MemoryMapper>())
				.AddSingleton<IDebugMemoryMapper>(x => x.GetRequiredService<MemoryMapper>())
				.AddSingleton<IMemoryManager, MemoryManager>()
				.AddSingleton<IEmulationWindow, EmulationWindow>()
				.AddSingleton<IEmulation, Emulation>()
				.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
				.AddSingleton<IDiskAnalysis, DiskAnalysis>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<IDisassembly, Disassembly>()
				.AddSingleton<IAnalysis, Analysis>()
				.AddSingleton<IAnalyser, Analyser>()
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("Amiga"))
				.AddSingleton<RunAmiga>()
				.Configure<EmulationSettings>(o => emuConfig.Bind("Emulation", o));

			//configure Audio
			if (settings.Audio == AudioDriver.XAudio2)
				services.AddSingleton<IAudio, AudioV2>();
			else
				services.AddSingleton<IAudio, Audio>();

			//configure CPU
			if (settings.CPU == CPUType.Musashi)
			{
				if (settings.Sku == CPUSku.MC68EC020)
					services.AddSingleton<ICPU, Musashi68EC020CPU>();
				else if (settings.Sku == CPUSku.MC68030)
					services.AddSingleton<ICPU, Musashi68030CPU>();
				else
					services.AddSingleton<ICPU, MusashiCPU>();
			}
			else
			{
				if (settings.Sku == CPUSku.MC68EC020)
					services.AddSingleton<ICPU, CPU68EC020>();
				else
					services.AddSingleton<ICPU, CPU>();
			}

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

			//configure Hard Disk Controller
			if (settings.DiskController == DiskController.A600_A1200)
			{
				services.AddSingleton<IDiskController, A1200IDEController>();
			}
			else if (settings.DiskController == DiskController.A3000)
			{
				services.AddSingleton<IDiskController, A3000DiskController>()
						.AddSingleton<ISCSIController, SCSIController>();
			}
			else if (settings.DiskController == DiskController.A4000)
			{
				services.AddSingleton<IDiskController, A4000DiskController>()
						.AddSingleton<IA4000IDEController, A4000IDEController>()
						.AddSingleton<ISCSIController, SCSIController>();
			}
			else
			{
				services.AddSingleton<IDiskController, NullDiskController>();
			}

			var serviceProvider = services.BuildServiceProvider();

			ServiceProviderFactory.ServiceProvider = serviceProvider;

			var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
			logger.LogTrace("Application Starting Up!");

			var blitter = serviceProvider.GetRequiredService<IBlitter>();
			blitter.SetLineMode(2);

			var form = serviceProvider.GetRequiredService<RunAmiga>();
			Application.Run(form);
		}
	}
}
