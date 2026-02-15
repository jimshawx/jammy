using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Jammy.Core;
using Jammy.Core.Audio.Linux;
using Jammy.Core.CDROM;
using Jammy.Core.CPU.CSharp;
using Jammy.Core.CPU.Musashi.CSharp;
using Jammy.Core.Custom;
using Jammy.Core.Custom.Audio;
using Jammy.Core.Custom.CIA;
using Jammy.Core.Custom.Denise;
using Jammy.Core.Custom.IO;
using Jammy.Core.Debug;
using Jammy.Core.Floppy;
using Jammy.Core.IDE;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.IO.Linux;
using Jammy.Core.Memory;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Database;
using Jammy.Database.CommentDao;
using Jammy.Database.Core;
using Jammy.Database.DatabaseDao;
using Jammy.Database.HeaderDao;
using Jammy.Database.LabelDao;
using Jammy.Database.MemTypeDao;
using Jammy.Debugger;
using Jammy.Debugger.Interceptors;
using Jammy.Disassembler;
using Jammy.Disassembler.Analysers;
using Jammy.Disassembler.TypeMapper;
using Jammy.Interface;
using Jammy.NativeOverlay;
using Jammy.NativeOverlay.Overlays;
using Jammy.Plugins;
using Jammy.Plugins.Interface;
using Jammy.Plugins.JavaScript.Jint;
using Jammy.Plugins.Lua;
using Jammy.Plugins.X11;
using Jammy.UI.Settings.Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parky.Logging;
using ReactiveUI.Avalonia;
using System.Runtime.Intrinsics.X86;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main.Linux;

public class JammyApplication : Application
{
	public override void Initialize()
	{
		Styles.Add(new SimpleTheme());
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{ 
			desktop.MainWindow = new Settings();
			desktop.MainWindow.Closed += (s, e) => 
			{
				desktop.MainWindow = Program.AppMain();
				desktop.MainWindow.Show();
			};
		}
		base.OnFrameworkInitializationCompleted();
	}
}

public class Program
{
	public static AppBuilder BuildAvaloniaApp() =>
		AppBuilder.Configure<JammyApplication>()
			.UsePlatformDetect()
			.UseSkia()
			// .LogToTrace(LogEventLevel.Verbose)
			.UseReactiveUI();

	static void Main(string[] args)
	{
		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	public static Window AppMain()
	{
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
				//x.AddDebugAsync();
				//x.AddDebugAsyncRTF();
				//x.AddOutputDebugString();
				x.AddTerminalAsync();
			})
			.AddSingleton<IAmiga, Amiga>()
			.AddSingleton<IBattClock, BattClock>()
			.AddSingleton<IMotherboard, Motherboard>()
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
			.AddSingleton<IExtendedKickstartROM, ExtendedKickstartROM>()
			.AddSingleton<IMotherboardRAM, MotherboardRAM>()
			.AddSingleton<ICPUSlotRAM, CPUSlotRAM>()
			.AddSingleton<IZorroConfigurator, ZorroConfigurator>()
			.AddSingleton<IUnmappedMemory, UnmappedMemory>()
			.AddSingleton<IInterrupt, Core.Interrupt>()
			.AddSingleton<IBreakpointCollection, BreakpointCollection>()
			.AddSingleton<IDebugger, Debugger.Debugger>()
			.AddSingleton<ICPUAnalyser, CPUAnalyser>()
			.AddSingleton<IChips, Chips>()
			.AddSingleton<IAkiko, Akiko>()
			.AddSingleton<IDenise, Denise>()
			.AddSingleton<IAgnus, Agnus>()
			.AddSingleton<IDMA, DMAController>()
			.AddSingleton<IChipsetClock, ChipsetClock>()
			.AddSingleton<IPSUClock, PSUClock>()
			.AddSingleton<ICPUClock, CPUClock>()
			.AddSingleton<MemoryMapper>()
			.AddSingleton<IMemoryMapper>(x => x.GetRequiredService<MemoryMapper>())
			.AddSingleton<IDebugMemoryMapper>(x => x.GetRequiredService<MemoryMapper>())
			.AddSingleton<IMemoryManager, MemoryManager>()
			//.AddSingleton<IEmulationWindow, Core.EmulationWindow.GDI.EmulationWindow>()
			//.AddSingleton<IEmulationWindow, Core.EmulationWindow.DX.EmulationWindow>()
			//.AddSingleton<IEmulationWindow, Core.EmulationWindow.DIB.EmulationWindow>()
			//.AddSingleton<IEmulationWindow, Core.EmulationWindow.Window.EmulationWindow>()

			.AddSingleton<IEmulationWindow, Core.EmulationWindow.X.EmulationWindow>()
			//.AddSingleton<IEmulationWindow, Core.EmulationWindow.Wayland.EmulationWindow>()

			.AddSingleton<IEmulation, Emulation>()
			.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
			.AddSingleton<IDiskAnalysis, DiskAnalysis>()
			.AddSingleton<ILabeller, Labeller>()
			.AddSingleton<IDisassemblyRanges, DisassemblyRanges>()
			.AddSingleton<IDisassembly, Disassembly>()
			.AddSingleton<IDisassembler, Disassembler.Disassembler>()
			.AddSingleton<IEADatabase, EADatabase>()
			.AddSingleton<IInstructionAnalysisDatabase, InstructionAnalysisDatabase>()
			.AddSingleton<IAnalysis, Analysis>()
			.AddSingleton<IAnalyser, Analyser>()
			.AddSingleton<IObjectMapper, ObjectMapper>()
			.AddSingleton<IReturnValueSnagger, ReturnValueSnagger>()
			.AddSingleton<ILVOInterceptors, LVOInterceptors>()
			//.AddSingleton<ILVOInterceptorAction, ReadLogger>()
			.AddSingleton<ILVOInterceptorAction, OpenLogger>()
			.AddSingleton<ILVOInterceptorAction, CloseLogger>()
			//.AddSingleton<ILVOInterceptorAction, LoadSegLogger>()
			//.AddSingleton<ILVOInterceptorAction, InternalLoadSegLogger>()
			.AddSingleton<ILVOInterceptorAction, AllocMemLogger>()
			.AddSingleton<ILVOInterceptorAction, FreeMemLogger>()
			.AddSingleton<IAllocatedMemoryTracker, AllocatedMemoryTracker>()
			.AddSingleton<ILVOInterceptorAction, OpenLibraryLogger>()
			//.AddSingleton<ILVOInterceptorAction, OldOpenLibraryLogger>()
			//.AddSingleton<ILVOInterceptorAction, OpenResourceLogger>()
			.AddSingleton<ILVOInterceptorAction, MakeLibraryLogger>()
			.AddSingleton<ILVOInterceptorAction, OpenDeviceLogger>()
			.AddSingleton<IOpenFileTracker, OpenFileTracker>()
			.AddSingleton<ILVOInterceptorCollection, LVOInterceptorCollection>()
			.AddSingleton<ILibraryBaseCollection, LibraryBaseCollection>()
			.AddSingleton<ILibraryBases, LibraryBases>()
			.AddSingleton<INativeOverlay, NativeOverlay.NativeOverlay>()
			.AddSingleton<IOverlayCollection, OverlayCollection>()
			.AddSingleton<DiskLightOverlay>()
			.AddSingleton<IDiskLightOverlay>(x => x.GetRequiredService<DiskLightOverlay>())
			.AddSingleton<IOverlayRenderer>(x => x.GetRequiredService<DiskLightOverlay>())
			.AddSingleton<IOverlayRenderer, TicksOverlay>()
			//.AddSingleton<IOverlayRenderer, CpuUsageOverlay>()
			.AddSingleton<IChipsetDebugger, ChipsetDebugger>()
			.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("Amiga"))
			.AddSingleton<Jammy>()
			//.AddSingleton<IGraph, Graph.Graph>()
			.AddSingleton<IFlowAnalyser, FlowAnalyser>()
			.AddSingleton<IPersistenceManager, PersistenceManager>()
			.AddSingleton<IDiskLoader, DiskLoader>()
			.AddSingleton<IDiskFormat, Rp9Format>()
			.AddSingleton<IDiskFormat, ZippedADFFormat>()
			.AddSingleton<IDiskFormat, GZipADZFormat>()
			.AddSingleton<IDiskFormat, DMSFormat>()
			.AddSingleton<IDiskFormat, RawADFFormat>()
			.AddSingleton<ICDDrive, CDDrive>()
			.Configure<EmulationSettings>(o => emuConfig.Bind("Emulation", o));

		//configure Blitter
		services.AddSingleton<IBlitter, Blitter>();

		//configure Audio
		if (settings.Audio == AudioDriver.XAudio2)
			services.AddSingleton<IAudio, AudioALSA>();
		else
			services.AddSingleton<IAudio, Audio>();

		//configure CPU
		if (settings.CPU == CPUType.Musashi || settings.CPU == CPUType.MusashiCSharp)
		{
			services.AddSingleton<ICPU, CPUWrapperMusashi>();
		}
		else
		{
			services.AddSingleton<ICPU, CPU>();
		}

		//configure Tracing
		if (settings.Tracer.IsEnabled())
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
		services.AddSingleton<IHardDriveLoader, HardDriveLoader>();
		services.AddSingleton<IHardDrive>(x => x.GetRequiredService<IHardDriveLoader>().DiskRead("simple_020.hdf", 1));
		services.AddSingleton<IHardDrive>(x => x.GetRequiredService<IHardDriveLoader>().DiskRead("dh0.hdf", 0));

		if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS)
		{
			if (Avx2.IsSupported)
				services.AddSingleton<IBpldatPix, BpldatPix32AVX2>();
			else
				services.AddSingleton<IBpldatPix, BpldatPix32>();
		}
		else
		{
			if (Avx2.IsSupported)
				services.AddSingleton<IBpldatPix, BpldatPix64AVX2>();
			else
				services.AddSingleton<IBpldatPix, BpldatPix64>();
		}

		//set up the list of IStatePersisters
		var types = services.Where(x => x.ImplementationType != null &&
			x.ImplementationType.GetInterfaces().Contains(typeof(IStatePersister))).ToList();
		foreach (var x in types)
			services.AddSingleton(y => (IStatePersister)y.GetRequiredService(x.ServiceType));

		//set up the database access
		services.AddSingleton<IDatabaseConnection>(x => new DatabaseConnection("testing.db"));
		services.AddSingleton<IUpgradeDatabase, UpgradeDatabase>();
		services.AddSingleton<IDataAccess, DataAccess>();
		services.AddSingleton<ILabelDao, LabelDao>();
		services.AddSingleton<ICommentDao, CommentDao>();
		services.AddSingleton<IDatabaseDao, DatabaseDao>();
		services.AddSingleton<IHeaderDao, HeaderDao>();
		services.AddSingleton<IMemTypeDao, MemTypeDao>();

		//plugins
		services.AddSingleton<IPluginWindowFactory, X11PluginWindowFactory>();
		services.AddSingleton<IPluginManager, PluginManager>();
		services.AddSingleton<IPluginEngine, LuaEngine>();
		services.AddSingleton<IPluginEngine, JavaScriptEngine>();

		var serviceProvider = services.BuildServiceProvider();

		//ensure the default database exists
		var databaseDao = serviceProvider.GetRequiredService<IDatabaseDao>();
		var database = databaseDao
			.Search(new DatabaseSearch { Name = "default" })
			.SingleOrDefault() ?? new Database.Types.Database { Name = "default" };
		databaseDao.SaveOrUpdate(database);

		var audio = serviceProvider.GetRequiredService<IAudio>();
		var dma = serviceProvider.GetRequiredService<IDMA>();
		var memoryMapper = serviceProvider.GetRequiredService<MemoryMapper>();
		var chipsetDebugger = serviceProvider.GetRequiredService<IChipsetDebugger>();
		var chips = serviceProvider.GetRequiredService<IChips>();
		var chipRAM = serviceProvider.GetRequiredService<IChipRAM>();
		var akiko = serviceProvider.GetRequiredService<IAkiko>();
		dma.Init(audio, memoryMapper, chipRAM);
		akiko.Init(memoryMapper);
		chipsetDebugger.Init(chips);

		var ciab = serviceProvider.GetRequiredService<ICIABEven>();
		serviceProvider.GetRequiredService<IAgnus>().Init(dma);
		serviceProvider.GetRequiredService<ICopper>().Init(dma);
		serviceProvider.GetRequiredService<IDiskDrives>().Init(dma, ciab, chipRAM);

		var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
		logger.LogTrace("Application Starting Up!");

		return serviceProvider.GetRequiredService<Jammy>();
	}
}

