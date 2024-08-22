using System;
using System.IO;
using System.Windows.Forms;
using Jammy.Core;
using Jammy.Core.CPU.CSharp;
using Jammy.Core.CPU.CSharp.MC68020;
using Jammy.Core.CPU.Musashi;
using Jammy.Core.CPU.Musashi.MC68020;
using Jammy.Core.CPU.Musashi.MC68030;
using Jammy.Core.Custom;
using Jammy.Core.Custom.Audio;
using Jammy.Core.Custom.CIA;
using Jammy.Core.Custom.IO;
using Jammy.Core.Debug;
using Jammy.Core.Floppy;
using Jammy.Core.IDE;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Jammy.Debugger;
using Jammy.Disassembler;
using Jammy.Disassembler.Analysers;
using Jammy.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Jammy.UI.Settings;
using Parky.Logging;
using Jammy.Core.IO.Windows;
using Jammy.Core.EmulationWindow.GDI;
using Jammy.Core.EmulationWindow.DX;
using Jammy.Debugger.Interceptors;
using Jammy.NativeOverlay;
using Jammy.Core.CPU.Microcode;
using Jammy.Core.CPU.Musashi.CSharp;

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
				.AddSingleton<IMotherboardRAM, MotherboardRAM>()
				.AddSingleton<ICPUSlotRAM, CPUSlotRAM>()
				.AddSingleton<IZorroConfigurator, ZorroConfigurator>()
				.AddSingleton<IUnmappedMemory, UnmappedMemory>()
				.AddSingleton<IInterrupt, Interrupt>()
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
				//.AddSingleton<IEmulationWindow, Core.EmulationWindow.DX.EmulationWindow>()
				.AddSingleton<IEmulationWindow, Core.EmulationWindow.DIB.EmulationWindow>()
				.AddSingleton<IEmulation, Emulation>()
				.AddSingleton<IKickstartAnalysis, KickstartAnalysis>()
				.AddSingleton<IDiskAnalysis, DiskAnalysis>()
				.AddSingleton<ILabeller, Labeller>()
				.AddSingleton<IDisassembly, Disassembly>()
				.AddSingleton<IAnalysis, Analysis>()
				.AddSingleton<IAnalyser, Analyser>()
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
				.AddSingleton<IOpenFileTracker, OpenFileTracker>()
				.AddSingleton<ILibraryBases, LibraryBases>()
				.AddSingleton<INativeOverlay, NativeOverlay.NativeOverlay>()
				.AddSingleton<IChipsetDebugger, ChipsetDebugger>()
				.AddSingleton<IMachineIdentifier>(x => new MachineIdentifer("Amiga"))
				.AddSingleton<Jammy>()
				.Configure<EmulationSettings>(o => emuConfig.Bind("Emulation", o));

			//configure Blitter
			services.AddSingleton<IBlitter, Blitter>();

			//configure Audio
			if (settings.Audio == AudioDriver.XAudio2)
				services.AddSingleton<IAudio, AudioVortice>();
			else
				services.AddSingleton<IAudio, Audio>();

			settings.CPU = CPUType.MusashiCSharp;

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
			else
			{
				if (settings.Sku == CPUSku.MC68EC020)
					services.AddSingleton<ICPU, CPU68EC020>();
				else
					//services.AddSingleton<ICPU, CPU>();
					services.AddSingleton<ICPU, CPUWrapper>();
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

			var dma = serviceProvider.GetRequiredService<IDMA>();
			serviceProvider.GetRequiredService<IAgnus>().Init(dma);
			serviceProvider.GetRequiredService<ICopper>().Init(dma);
			serviceProvider.GetRequiredService<IBlitter>().Init(dma);
			
			serviceProvider.GetRequiredService<IChipsetClock>().Init(dma);

			ServiceProviderFactory.ServiceProvider = serviceProvider;

			var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
			logger.LogTrace("Application Starting Up!");

			var form = serviceProvider.GetRequiredService<Jammy>();
			Application.Run(form);
		}
	}
}
