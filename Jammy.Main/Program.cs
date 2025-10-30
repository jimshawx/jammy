using Jammy.Core;
using Jammy.Core.Audio.Windows;
using Jammy.Core.CPU.CSharp;
using Jammy.Core.CPU.Moira;
using Jammy.Core.CPU.Musashi;
using Jammy.Core.CPU.Musashi.CSharp;
using Jammy.Core.CPU.Musashi.MC68020;
using Jammy.Core.CPU.Musashi.MC68030;
using Jammy.Core.Custom;
using Jammy.Core.Custom.Audio;
using Jammy.Core.Custom.CIA;
using Jammy.Core.Custom.Denise;
using Jammy.Core.Custom.IO;
using Jammy.Core.Debug;
using Jammy.Core.Floppy;
using Jammy.Core.IDE;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.IO.Windows;
using Jammy.Core.Memory;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Debugger;
using Jammy.Debugger.Interceptors;
using Jammy.Disassembler;
using Jammy.Disassembler.Analysers;
using Jammy.Disassembler.TypeMapper;
using Jammy.Graph;
using Jammy.Interface;
using Jammy.NativeOverlay;
using Jammy.UI.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Parky.Logging;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Windows.Forms;
using Jammy.NativeOverlay.Overlays;
using Jammy.Core.Floppy.IPF;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main
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
					//x.AddOutputDebugString();
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
				.AddSingleton<IEmulationWindow, Core.EmulationWindow.DX.EmulationWindow>()
				//.AddSingleton<IEmulationWindow, Core.EmulationWindow.DIB.EmulationWindow>()
				//.AddSingleton<IEmulationWindow, Core.EmulationWindow.Window.EmulationWindow>()
				.AddSingleton<IEmulation, Emulation>()
				.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
				.AddSingleton<IDiskAnalysis, DiskAnalysis>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<IDisassembly, Disassembly>()
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
				//.AddSingleton<ILVOInterceptorAction, AllocMemLogger>()
				.AddSingleton<ILVOInterceptorAction, OpenLibraryLogger>()
				//.AddSingleton<ILVOInterceptorAction, OldOpenLibraryLogger>()
				//.AddSingleton<ILVOInterceptorAction, OpenResourceLogger>()
				.AddSingleton<ILVOInterceptorAction, MakeLibraryLogger>()
				.AddSingleton<ILVOInterceptorAction, OpenDeviceLogger>()
				.AddSingleton<ILVOInterceptorAction, OpenScreenTagListLogger>()
				.AddSingleton<ILVOInterceptorAction, OpenScreenLogger>()
				.AddSingleton<ILVOInterceptorAction, LoadRGB32Logger>()
				.AddSingleton<ILVOInterceptorAction, LoadRGB4Logger>()
				.AddSingleton<ILVOInterceptorAction, GetRGB32Logger>()
				.AddSingleton<ILVOInterceptorAction, GetRGB4Logger>()
				.AddSingleton<ILVOInterceptorAction, MrgCopLogger>()
				.AddSingleton<IOpenFileTracker, OpenFileTracker>()
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
				.AddSingleton<IGraph, Graph.Graph>()
				.AddSingleton<IFlowAnalyser, FlowAnalyser>()
				.AddSingleton<IPersistenceManager, PersistenceManager>()
				.AddSingleton<IIPF, IPF>()
				.AddSingleton<IDiskLoader, DiskLoader>()
				.AddSingleton<IDiskFormat, Rp9Format>()
				.AddSingleton<IDiskFormat, ZippedADFFormat>()
				.AddSingleton<IDiskFormat, GZipADZFormat>()
				.AddSingleton<IDiskFormat, DMSFormat>()
				.AddSingleton<IDiskFormat, IPFFormat>()
				.AddSingleton<IDiskFormat, RawADFFormat>()
				.Configure<EmulationSettings>(o => emuConfig.Bind("Emulation", o));

			//configure Blitter
			services.AddSingleton<IBlitter, Blitter>();

			//configure Audio
			if (settings.Audio == AudioDriver.XAudio2)
				services.AddSingleton<IAudio, AudioVortice>();
			else
				services.AddSingleton<IAudio, Audio>();

			//services.AddSingleton<IAudio, NullAudio>();

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
			else if (settings.CPU == CPUType.MusashiCSharp)
			{
				services.AddSingleton<ICPU, CPUWrapperMusashi>();
			}
			else if (settings.CPU == CPUType.Moira)
			{
				services.AddSingleton<ICPU, MoiraCPU>();
			}
			else
			{
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

			if (settings.ChipSet == ChipSet.OCS || settings.ChipSet == ChipSet.ECS)
			{
				if (Avx2.IsSupported)
					services.AddSingleton<IBpldatPix, BpldatPix32AVX2>();
				else
					services.AddSingleton<IBpldatPix, BpldatPix32>();
			}
			else
			{
				//if (Avx2.IsSupported)
				//	services.AddSingleton<IBpldatPix, BpldatPix64AVX2>();
				//else
					services.AddSingleton<IBpldatPix, BpldatPix64>();
			}

			//set up the list of IStatePersisters
			var types = services.Where(x=>x.ImplementationType != null &&
				x.ImplementationType.GetInterfaces().Contains(typeof(IStatePersister))).ToList();
			foreach (var x in types)
				services.AddSingleton(y => (IStatePersister)y.GetRequiredService(x.ServiceType));
			
			var serviceProvider = services.BuildServiceProvider();

			var audio = serviceProvider.GetRequiredService<IAudio>();
			var dma = serviceProvider.GetRequiredService<IDMA>();
			var memoryMapper = serviceProvider.GetRequiredService<MemoryMapper>();
			var chipsetDebugger = serviceProvider.GetRequiredService<IChipsetDebugger>();
			var chips = serviceProvider.GetRequiredService<IChips>();
			var chipRAM = serviceProvider.GetRequiredService<IChipRAM>();
			dma.Init(audio, memoryMapper, chipRAM);
			chipsetDebugger.Init(chips);
			var ciab = serviceProvider.GetRequiredService<ICIABEven>();
			serviceProvider.GetRequiredService<IAgnus>().Init(dma);
			serviceProvider.GetRequiredService<ICopper>().Init(dma);
			serviceProvider.GetRequiredService<IDiskDrives>().Init(dma, ciab, chipRAM);

			//hack to start IPF
			var ipf = serviceProvider.GetRequiredService<IIPF>();

			var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
			logger.LogTrace("Application Starting Up!");

			var form = serviceProvider.GetRequiredService<Jammy>();
			Application.Run(form);
		}
	}
}
