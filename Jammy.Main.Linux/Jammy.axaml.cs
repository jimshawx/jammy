using Avalonia.Controls;
using Avalonia.Interactivity;
using Jammy.Core;
using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Debugger;
using Jammy.Disassembler;
using Jammy.Interface;
using Jammy.Types;
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
		private readonly IObjectMapper objectMapper;
		private readonly IInstructionAnalysisDatabase instructionAnalysisDatabase;
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
			IFlowAnalyser flowAnalyser, /*IGraph graph,*/ IChipsetDebugger chipsetDebugger, IObjectMapper objectMapper,
			IChipRAM chipRAM, /*ILogger<GfxScan> gfxLogger,*/ /*ILogger<StringScan> stringLogger,*/ IMemoryMapper memoryMapper,
			/*ILogger<DMAExplorer> dmaLogger,*/ IInstructionAnalysisDatabase instructionAnalysisDatabase,
			ILogger<Jammy> logger, IOptions<EmulationSettings> options)
		{
			this.emulation = emulation;
			this.disassembly = disassembly;
			this.debugger = debugger;
			this.analysis = analysis;
			this.flowAnalyser = flowAnalyser;
			this.objectMapper = objectMapper;
			this.instructionAnalysisDatabase = instructionAnalysisDatabase;
			//this.graph = graph;
			this.logger = logger;

			InitializeComponent();

			settings = options.Value;

			disassemblyOptions = new DisassemblyOptions { IncludeBytes = true, IncludeBreakpoints = true, IncludeComments = true, Full32BitAddress = settings.AddressBits > 24 };

			//fill in the types we can use to map
			GetAmigaTypes();

			//prime the disassembly with a decent starting point
			disassemblyRanges.Add(new AddressRange(0x000000, 0x3000));//exec
			disassemblyRanges.Add(new AddressRange(0xfc0000, 0x40000));//roms
			if (settings.TrapdoorMemory != 0.0)
				disassemblyRanges.Add(new AddressRange(0xc00000, 0x1000));
			if (debugger.KickstartSize() == 512 * 1024)
				disassemblyRanges.Add(new AddressRange(0xf80000, 0x40000));
			if (debugger.KickstartSize() == 0x2000)
				disassemblyRanges.Add(new AddressRange(0xf80000, 0x2000));
			//hack - disassemble the cd32 extension rom
			if (settings.KickStart.EndsWith("amiga-os-310-cd32.rom"))
				disassemblyRanges.Add(new AddressRange(0xe00000, 0x80000));

			emulation.Start();

			FetchUI(FetchUIFlags.All);
			UpdateDisassembly();
			UpdateDisplay();

			//InitUIRefreshThread();
		}

		//private CancellationTokenSource uiUpdateTokenSource;
		//private Task uiUpdateTask;

		//private void InitUIRefreshThread()
		//{
		//	uiUpdateTokenSource = new CancellationTokenSource();

		//	uiUpdateTask = new Task(() =>
		//	{
		//		while (!uiUpdateTokenSource.IsCancellationRequested)
		//		{
		//			//this.Invoke((Action)UpdatePowerLight);

		//			if (UI.UI.IsDirty)
		//			{
		//				this.Invoke((Action)delegate ()
		//				{
		//					FetchUI(FetchUIFlags.All);
		//					SetSelection();
		//					UpdateDisplay();
		//				});
		//			}
		//		}
		//	}, uiUpdateTokenSource.Token, TaskCreationOptions.LongRunning);
		//	uiUpdateTask.Start();
		//}

		[Flags]
		private enum FetchUIFlags
		{
			Regs = 1,
			Memory = 2,
			ChipRegs = 4,

			All = Regs | Memory | ChipRegs,
		}

		private class FetchUIData
		{
			public Regs Regs { get; set; }
			//public MemoryDump Memory { get; set; }
			public ChipState ChipRegs { get; set; }
		}

		private readonly FetchUIData uiData = new FetchUIData();

		private void FetchUI(FetchUIFlags flags)
		{
			Amiga.LockEmulation();
			if ((flags & FetchUIFlags.Regs) != 0) uiData.Regs = debugger.GetRegs();
			//if ((flags & FetchUIFlags.Memory) != 0) uiData.Memory = debugger.GetMemory();
			if ((flags & FetchUIFlags.ChipRegs) != 0) uiData.ChipRegs = debugger.GetChipRegs();
			Amiga.UnlockEmulation();
		}

		private void UpdateDisassembly()
		{
			Amiga.LockEmulation();

			//add in an area of code disassembly around the current PC
			var regs = debugger.GetRegs();
			disassemblyRanges.Add(new AddressRange(regs.PC - 0x100, 0x1000));

			disassembly.Clear();
			var disasm = disassembly.DisassembleTxt(
				disassemblyRanges,
				disassemblyOptions);

			var memory = debugger.GetMemory();
			var memoryText = memory.GetString(memoryDumpRanges);

			Amiga.UnlockEmulation();

			//this is the new view
			//disassemblyView = disassembly.DisassemblyView(0, 0, 100, disassemblyOptions);
			disassemblyView = disassembly.FullDisassemblyView(disassemblyOptions);
			txtDisassembly.Text = disassemblyView.Text;

			memoryDumpView = new MemoryDumpView(memory, memoryText);
			txtMemory.Text = memoryDumpView.Text;

			UpdateMem();
		}

		private void UpdateDisplay()
		{
			UpdateRegs();
			UpdateMem();
			//UpdatePowerLight();
			//UpdateDiskLight();
			UpdateExecBase();
			UpdateCopper();
			UpdateVectors();
			UpdateClock();

			var debug = debugger.Analyse();
			instructionAnalysisDatabase.Add(debug);
			logger.LogTrace($"{string.Join(" ", debug.EffectiveAddresses.Select(x => $"{x.Ea:X8}"))}");

			UI.UI.IsDirty = false;
		}

		private void UpdateCopper()
		{
			txtCopper.Text = debugger.GetCopperDisassembly();
		}

		private void UpdateClock()
		{
			tbClock.Text = debugger.GetChipClock().ToString();
		}

		private void UpdateRegs()
		{
			var regs = uiData.Regs;
			var chipRegs = uiData.ChipRegs;

			lbRegisters.Items.Clear();
			lbRegisters.Items.AddRange(regs.Items().Cast<object>().ToArray());
			lbRegisters.SizeListBox(2);

			lbCustom.Items.Clear();

			{
				lbCustom.Items.Add($"SR: {(regs.SR >> 8) & 7} IRQ: {debugger.GetInterruptLevel()}");
				lbCustom.Items.Add("INTENA W:DFF09A R:DFF01C");
				lbCustom.Items.Add("INTREQ W:DFF09C R:DFF01E");
				lbCustom.Items.Add("        ENA REQ");
				string[] names = new String[16] { "NMI", "INTEN", "EXTEN", "DSKSYNC", "RBF", "AUD3", "AUD2", "AUD1", "AUD0", "BLIT", "VERTB", "COPPER", "PORTS", "SOFTINT", "DSKBLK", "TBE" };
				for (int i = 0; i < 16; i++)
				{
					int bit = 1 << (i ^ 15);
					lbCustom.Items.Add($"{names[i],8} {((chipRegs.intena & bit) != 0 ? 1 : 0)}   {((chipRegs.intreq & bit) != 0 ? 1 : 0)}");
				}
			}

			{
				lbCustom.Items.Add("DMACON W:DFF096 R:DFF002");
				lbCustom.Items.Add("        ENA");
				string[] names = new string[16] { "SETCLR", "BBUSY", "BZERO", "x", "x", "BLTPRI", "DMAEN", "BPLEN", "COPEN", "BLTEN", "SPREN", "DSKEN", "AUD3EN", "AUD2EN", "AUD1EN", "AUD0EN" };
				for (int i = 0; i < 16; i++)
				{
					int bit = 1 << (i ^ 15);
					lbCustom.Items.Add($"{names[i],8} {((chipRegs.dmacon & bit) != 0 ? 1 : 0)}");
				}
			}
		}

		private uint ValueFromRegName(Regs regs, string txt)
		{
			uint address = 0;
			switch ((string)addressFollowBox.SelectedItem)
			{
				case "A0": address = regs.A[0]; break;
				case "A1": address = regs.A[1]; break;
				case "A2": address = regs.A[2]; break;
				case "A3": address = regs.A[3]; break;
				case "A4": address = regs.A[4]; break;
				case "A5": address = regs.A[5]; break;
				case "A6": address = regs.A[6]; break;
				case "SP": address = regs.SP; break;
				case "SSP": address = regs.SSP; break;
				case "D0": address = regs.D[0]; break;
				case "D1": address = regs.D[1]; break;
				case "D2": address = regs.D[2]; break;
				case "D3": address = regs.D[3]; break;
				case "D4": address = regs.D[4]; break;
				case "D5": address = regs.D[5]; break;
				case "D6": address = regs.D[6]; break;
				case "D7": address = regs.D[7]; break;
				case "PC": address = regs.PC; break;
			}

			return address;
		}

		private readonly string[] intsrc = ["-", "TBE/DSKBLK/SOFTINT", "PORTS (CIAA)", "COPER/VERTB/BLIT", "AUDIO", "RBF/DSKSYNC", "EXTER (CIAB)/INTEN", "NMI"];
		private void UpdateMem()
		{
			//var memory = uiData.Memory;
			var regs = uiData.Regs;

			{
				//var mem = new List<Tuple<uint, uint>>();
				//long sp = (long)regs.SP;
				//long ssp = (long)regs.SSP;
				//int cnt = 32;
				//while (cnt-- > 0)
				//{
				//	uint spv = debugger.Read32((uint)sp);
				//	uint sspv = debugger.Read32((uint)ssp);
				//	mem.Add(new Tuple<uint, uint>(spv, sspv));
				//	sp += 4;
				//	ssp += 4;
				//}

				//lbCallStack.Items.Clear();
				//lbCallStack.Items.Add("   SP       SSP   ");
				//lbCallStack.Items.AddRange(mem.Select(x => $"{x.Item1:X8}  {x.Item2:X8}").Cast<object>().ToArray());

				lbCallStack.Items.Clear();

				uint sp = regs.SP;
				lbCallStack.Items.Add("   SP");
				for (uint i = 0; i < 15; i++)
					lbCallStack.Items.Add($"{debugger.Read32(sp + i * 4):X8}");

				uint ssp = regs.SSP;
				lbCallStack.Items.Add("   SSP");
				for (uint i = 0; i < 15; i++)
					lbCallStack.Items.Add($"{debugger.Read32(ssp + i * 4):X8}");

				lbCallStack.SizeListBox(2);
			}

			{
				//txtMemory.Text = memory.ToString();

				if (addressFollowBox.SelectedIndex != 0)
				{
					uint address = ValueFromRegName(regs, (string)addressFollowBox.SelectedItem);
					var line = memoryDumpView.AddressToLine(address);
					if (line != 0)
					{
						txtMemory.SelectionStart = txtMemory.GetFirstCharIndexFromLine(line);
						txtMemory.ScrollToCaret();
					}
					UpdateExecBase();
				}
			}
			{
				lbIntvec.Items.Clear();
				for (uint i = 1; i <= 7; i++)
				{
					lbIntvec.Items.Add($"{i} {debugger.Read32((i + 0x18) * 4):X8} {intsrc[i]}");
				}
			}
		}

		private void UpdateVectors()
		{
			txtVectors.Text = debugger.GetVectors().ToString();
		}

		private void GetAmigaTypes()
		{
			var types = AmigaTypes.AmigaType.GetAmigaTypes();
			cbTypes.Items.Clear();
			cbTypes.Items.Add("(None)");
			cbTypes.Items.AddRange(types.Keys.OrderBy(x => x).ToArray());
			cbTypes.SelectedIndex = 0;
		}

		private void UpdateExecBase()
		{
			if (cbTypes.SelectedIndex != 0 && addressFollowBox.SelectedIndex != 0)
			{
				string typeName = (string)cbTypes.SelectedItem;

				Amiga.LockEmulation();

				var regs = debugger.GetRegs();

				uint address = ValueFromRegName(regs, (string)addressFollowBox.SelectedItem);

				var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "Jammy.AmigaTypes");
				if (assembly != null)
				{
					var type = assembly.GetTypes().SingleOrDefault(x => x.Name == typeName);
					if (type != null)
					{
						object tp = Activator.CreateInstance(type);
						if (tp != null)
						{
							txtExecBase.Text = objectMapper.MapObject(tp, address);
						}
					}
				}

				Amiga.UnlockEmulation();
			}
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

	public static class AvaloniaExtensions
	{
		public static void AddRange(this ItemCollection itemColl, IEnumerable<object> itemEnum)
		{
			foreach (var item in itemEnum)
				itemColl.Add(item);
		}

		public static void SizeListBox(this ListBox lb, int extraLines)
		{
		}

		public static void ScrollToCaret(this TextBox txtBox)
		{
		}

		public static int GetFirstCharIndexFromLine(this TextBox txtBox, int line)
		{
			return 0;
		}
	}
}
