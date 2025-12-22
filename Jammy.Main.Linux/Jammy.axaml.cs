using Avalonia.Controls;
using Avalonia.Interactivity;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Debugger;
using Jammy.Interface;
using Jammy.Types.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Main.Linux
{
	public partial class Jammy : Window
	{
		private readonly IEmulation emulation;
		private IDisassemblyView disassemblyView;
		private IMemoryDumpView memoryDumpView;
		private readonly IDisassembly disassembly;
		private readonly IDebugger debugger;
		private readonly IAnalysis analysis;
		private readonly IFlowAnalyser flowAnalyser;
		private readonly ILogger logger;
		private readonly EmulationSettings settings;
		private readonly DisassemblyOptions disassemblyOptions;

		private readonly List<AddressRange> disassemblyRanges = new List<AddressRange>();

		private readonly List<AddressRange> memoryDumpRanges = new List<AddressRange>
				{
					new AddressRange(0x000000, 0x10000),
					new AddressRange(0xc00000, 0x10000),
					new AddressRange(0xf80000, 0x40000),
					new AddressRange(0xfc0000, 0x40000)
				};

		public Jammy(IEmulation emulation, IDisassembly disassembly, IDebugger debugger, IAnalysis analysis,
			IFlowAnalyser flowAnalyser, /*IGraph graph,*/
			ILogger<Jammy> logger, IOptions<EmulationSettings> options)
		{
			this.emulation = emulation;
			this.disassembly = disassembly;
			this.debugger = debugger;
			this.analysis = analysis;
			this.flowAnalyser = flowAnalyser;
			//this.graph = graph;
			this.logger = logger;

			InitializeComponent();

			settings = options.Value;

			disassemblyOptions = new DisassemblyOptions { IncludeBytes = true, IncludeBreakpoints = true, IncludeComments = true, Full32BitAddress = settings.AddressBits > 24 };

			//prime the disassembly with a decent starting point
			disassemblyRanges.Add(new AddressRange(0x000000, 0x3000));//exec
			disassemblyRanges.Add(new AddressRange(0xfc0000, 0x40000));//roms
			if (settings.TrapdoorMemory != 0.0)
				disassemblyRanges.Add(new AddressRange(0xc00000, 0x1000));
			if (debugger.KickstartSize() == 512 * 1024)
				disassemblyRanges.Add(new AddressRange(0xf80000, 0x40000));
			if (debugger.KickstartSize() == 0x2000)
				disassemblyRanges.Add(new AddressRange(0xf80000, 0x2000));

			emulation.Start();

			// FetchUI(FetchUIFlags.All);
			// UpdateDisassembly();
			// UpdateDisplay();

			// InitUIRefreshThread();
		}

		private void btnGenDisassemblies_Click(object sender, RoutedEventArgs e) { }
		private void btnDMAExplorer_Click(object sender, RoutedEventArgs e) { }
		private void btnAnalyseFlow_Click(object sender, RoutedEventArgs e) { }
		private void btnStringScan_Click(object sender, RoutedEventArgs e) { }
		private void btnINTDIS_Click(object sender, RoutedEventArgs e) { }
		private void btnReadyDisk_Click(object sender, RoutedEventArgs e) { }
		private void btnCribSheet_Click(object sender, RoutedEventArgs e) { }
		private void btnClearBBUSY_Click(object sender, RoutedEventArgs e) { }
		private void btnGfxScan_Click(object sender, RoutedEventArgs e) { }
		private void btnChange_Click(object sender, RoutedEventArgs e) { }
		private void btnIDEACK_Click(object sender, RoutedEventArgs e) { }
		private void btnDumpTrace_Click(object sender, RoutedEventArgs e) { }
		private void btnINTENA_Click(object sender, RoutedEventArgs e) { }
		private void btnStepOut_Click(object sender, RoutedEventArgs e) { }
		private void btnIRQ_Click(object sender, RoutedEventArgs e) { }
		private void btnCIAInt_Click(object sender, RoutedEventArgs e) { }
		private void btnRemoveDisk_Click(object sender, RoutedEventArgs e) { }
		private void btnInsertDisk_Click(object sender, RoutedEventArgs e) { }
		private void btnDisassemble_Click(object sender, RoutedEventArgs e) { }
		private void btnStepOver_Click(object sender, RoutedEventArgs e) { }
		private void btnRefresh_Click(object sender, RoutedEventArgs e) { }
		private void btnReset_Click(object sender, RoutedEventArgs e) { }
		private void btnGo_Click(object sender, RoutedEventArgs e) { }
		private void btnStop_Click(object sender, RoutedEventArgs e) { }
		private void btnStep_Click(object sender, RoutedEventArgs e) { }
	}
}
