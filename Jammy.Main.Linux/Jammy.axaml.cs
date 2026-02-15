using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Rendering;
using Jammy.Core;
using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Debugger;
using Jammy.Disassembler;
using Jammy.Interface;
using Jammy.Plugins.Interface;
using Jammy.Types;
using Jammy.Types.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReactiveUI;
using System.Reactive;
using System.Web;

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
		private readonly IDisassemblyRanges disassemblyRanges;
		private readonly IPluginManager pluginManager;
		private readonly List<AddressRange> memoryDumpRanges = new List<AddressRange>
				{
					new AddressRange(0x000000, 0x10000),
					new AddressRange(0xc00000, 0x10000),
					new AddressRange(0xf80000, 0x40000),
					new AddressRange(0xfc0000, 0x40000)
				};

		public Jammy()
		{
			InitializeComponent();
		}

		public Jammy(IEmulation emulation, IDisassembly disassembly, IDebugger debugger, IAnalysis analysis,
			IFlowAnalyser flowAnalyser, /*IGraph graph,*/ IChipsetDebugger chipsetDebugger, IObjectMapper objectMapper,
			IChipRAM chipRAM, /*ILogger<GfxScan> gfxLogger,*/ /*ILogger<StringScan> stringLogger,*/ IMemoryMapper memoryMapper,
			/*ILogger<DMAExplorer> dmaLogger,*/ IInstructionAnalysisDatabase instructionAnalysisDatabase,
			IDisassemblyRanges disassemblyRanges, IPluginManager pluginManager,
			ILogger<Jammy> logger, IOptions<EmulationSettings> options) : this()
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
			this.disassemblyRanges = disassemblyRanges;
			this.pluginManager = pluginManager;
			var renderer = ((IRenderRoot)this).Renderer;
			logger.LogTrace($"Using Avalonia Renderer: {renderer.GetType().FullName}");

			settings = options.Value;

			menuMemory_ItemClickedEvent = ReactiveCommand.Create<string>(menuMemory_ItemClicked);
			menuDisassembly_ItemClickedEvent = ReactiveCommand.Create<string>(menuDisassembly_ItemClicked);

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
			//pluginManager.Start();
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

		//private void UpdatePowerLight()
		//{
		//	bool power = UI.UI.PowerLight;
		//	picPower.BackColor = power ? Color.Red : Color.DarkRed;
		//}

		//private void UpdateDiskLight()
		//{
		//	bool disk = global::Jammy.UI.UI.DiskLight;
		//	picDisk.BackColor = disk ? Color.LightGreen : Color.DarkGreen;
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

		private void SetSelection()
		{
			uint pc = uiData.Regs.PC;

			txtDisassembly.ReallySuspendLayout();
			txtDisassembly.DeselectAll();

			int line = disassemblyView.GetAddressLine(pc & (uint)((1 << settings.AddressBits) - 1));

			//scroll the view to the line 5 lines before the PC
			txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, line - 5));
			txtDisassembly.ScrollToCaret();

			//find the line at the current pc, and the next line after that, and highlight it.
			int start = txtDisassembly.GetFirstCharIndexFromLine(line);

			if (start >= 0)
				txtDisassembly.Select(start, txtDisassembly.GetFirstCharIndexFromLine(line + 1) - start);
			else
				txtDisassembly.DeselectAll();

			txtDisassembly.ReallyResumeLayout();
			txtDisassembly.Refresh();
		}

		private void btnStep_Click(object sender, RoutedEventArgs e)
		{
			Amiga.SetEmulationMode(EmulationMode.Step);
		}

		private void btnStepOut_Click(object sender, RoutedEventArgs e) 
		{
			Amiga.SetEmulationMode(EmulationMode.StepOut);
		}

		private void btnStop_Click(object sender, RoutedEventArgs e) 
		{
			Amiga.SetEmulationMode(EmulationMode.Stopped);
		}

		private void btnGo_Click(object sender, RoutedEventArgs e)
		{
			txtDisassembly.DeselectAll();
			Amiga.SetEmulationMode(EmulationMode.Running);
		}

		private void btnReset_Click(object sender, RoutedEventArgs e)
		{
			Amiga.SetEmulationMode(EmulationMode.Stopped);

			Amiga.LockEmulation();
			emulation.Reset();
			Amiga.UnlockEmulation();

			FetchUI(FetchUIFlags.All);
			SetSelection();
			UpdateDisplay();
		}
		private void btnRefresh_Click(object sender, RoutedEventArgs e)
		{
			FetchUI(FetchUIFlags.All);
			SetSelection();
			UpdateDisplay();
		}

		private void btnStepOver_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			debugger.BreakAtNextPC();
			Amiga.SetEmulationMode(EmulationMode.Running, true);
			Amiga.UnlockEmulation();
		}

		private void btnDisassemble_Click(object sender, RoutedEventArgs e)
		{
			FetchUI(FetchUIFlags.All);
			UpdateDisassembly();
			SetSelection();
			UpdateDisplay();
		}

		public ReactiveCommand<string, Unit> menuMemory_ItemClickedEvent {  get; }

		private void menuMemory_ItemClicked(string e)
		{
			if (e == "menuMemoryGotoItem")
			{
				//var gotoForm = new GoTo();
				//var res = gotoForm.ShowDialog();
				//if (res == DialogResult.OK)
				//{
				//	uint address = gotoForm.GotoLocation;
				//}
			}
			else if (e == "menuMemoryFindItem")
			{
				//var findForm = new Find();
				//var res = findForm.ShowDialog();
				//if (res == DialogResult.OK)
				//{
				//	if (findForm.SearchText != null)
				//	{
				//		uint address = debugger.FindMemoryText(findForm.SearchText);
				//		if (address != 0)
				//			JumpToMemoryAddress(address);
				//	}
				//	if (findForm.SearchSeq != null)
				//	{
				//		uint address = debugger.FindMemory(findForm.SearchSeq);
				//		if (address != 0)
				//			JumpToMemoryAddress(address);
				//	}
				//}
			}
		}

		private void JumpToMemoryAddress(uint address)
		{
			//make sure the text is available in the memory dump
			memoryDumpRanges.Add(new AddressRange(address, 256));

			Amiga.LockEmulation();
			var memory = debugger.GetMemory();

			//make sure the text is available in the memory dump
			var memoryText = memory.GetString(memoryDumpRanges);
			memoryDumpView = new MemoryDumpView(memory, memoryText);

			int gotoLine = memory.AddressToLine(address);
			Amiga.UnlockEmulation();

			txtMemory.SuspendLayout();
			txtMemory.Text = memoryDumpView.Text;
			txtMemory.SelectionStart = txtMemory.GetFirstCharIndexFromLine(gotoLine);
			txtMemory.ScrollToCaret();
			txtMemory.Select(txtMemory.GetFirstCharIndexFromLine(gotoLine),
				txtMemory.GetFirstCharIndexFromLine(gotoLine + 1) - txtMemory.GetFirstCharIndexFromLine(gotoLine));
			txtMemory.Invalidate();
			txtMemory.ResumeLayout();
			txtMemory.Update();
		}

		private void btnCIAInt_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			if (cbCIA.Text == "TIMERA") debugger.CIAInt(ICRB.TIMERA);
			if (cbCIA.Text == "TIMERB") debugger.CIAInt(ICRB.TIMERB);
			if (cbCIA.Text == "TODALARM") debugger.CIAInt(ICRB.TODALARM);
			if (cbCIA.Text == "SERIAL") debugger.CIAInt(ICRB.SERIAL);
			if (cbCIA.Text == "FLAG") debugger.CIAInt(ICRB.FLAG);
			Amiga.UnlockEmulation();
		}

		private void btnIRQ_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			if (cbIRQ.Text == "EXTER") debugger.IRQ(Core.Types.Interrupt.EXTER);
			if (cbIRQ.Text == "DSKBLK") debugger.IRQ(Core.Types.Interrupt.DSKBLK);
			if (cbIRQ.Text == "PORTS") debugger.IRQ(Core.Types.Interrupt.PORTS);
			if (cbIRQ.Text == "BLIT") debugger.IRQ(Core.Types.Interrupt.BLIT);
			if (cbIRQ.Text == "COPPER") debugger.IRQ(Core.Types.Interrupt.COPPER);
			if (cbIRQ.Text == "DSKSYNC") debugger.IRQ(Core.Types.Interrupt.DSKSYNC);
			if (cbIRQ.Text == "AUD0") debugger.IRQ(Core.Types.Interrupt.AUD0);
			if (cbIRQ.Text == "AUD1") debugger.IRQ(Core.Types.Interrupt.AUD1);
			if (cbIRQ.Text == "AUD2") debugger.IRQ(Core.Types.Interrupt.AUD2);
			if (cbIRQ.Text == "AUD3") debugger.IRQ(Core.Types.Interrupt.AUD3);
			if (cbIRQ.Text == "VERTB") debugger.IRQ(Core.Types.Interrupt.VERTB);
			if (cbIRQ.Text == "SOFTINT") debugger.IRQ(Core.Types.Interrupt.SOFTINT);
			Amiga.UnlockEmulation();
		}

		private void btnINTENA_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			if (cbIRQ.Text == "EXTER") debugger.INTENA(Core.Types.Interrupt.EXTER);
			if (cbIRQ.Text == "DSKBLK") debugger.INTENA(Core.Types.Interrupt.DSKBLK);
			if (cbIRQ.Text == "PORTS") debugger.INTENA(Core.Types.Interrupt.PORTS);
			if (cbIRQ.Text == "BLIT") debugger.INTENA(Core.Types.Interrupt.BLIT);
			if (cbIRQ.Text == "COPPER") debugger.INTENA(Core.Types.Interrupt.COPPER);
			if (cbIRQ.Text == "DSKSYNC") debugger.INTENA(Core.Types.Interrupt.DSKSYNC);
			if (cbIRQ.Text == "AUD0") debugger.INTENA(Core.Types.Interrupt.AUD0);
			if (cbIRQ.Text == "AUD1") debugger.INTENA(Core.Types.Interrupt.AUD1);
			if (cbIRQ.Text == "AUD2") debugger.INTENA(Core.Types.Interrupt.AUD2);
			if (cbIRQ.Text == "AUD3") debugger.INTENA(Core.Types.Interrupt.AUD3);
			if (cbIRQ.Text == "VERTB") debugger.INTENA(Core.Types.Interrupt.VERTB);
			if (cbIRQ.Text == "SOFTINT") debugger.INTENA(Core.Types.Interrupt.SOFTINT);
			Amiga.UnlockEmulation();
		}

		private void btnINTDIS_Click(object sender, RoutedEventArgs e) 
		{
			Amiga.LockEmulation();
			if (cbIRQ.Text == "EXTER") debugger.INTDIS(Core.Types.Interrupt.EXTER);
			if (cbIRQ.Text == "DSKBLK") debugger.INTDIS(Core.Types.Interrupt.DSKBLK);
			if (cbIRQ.Text == "PORTS") debugger.INTDIS(Core.Types.Interrupt.PORTS);
			if (cbIRQ.Text == "BLIT") debugger.INTDIS(Core.Types.Interrupt.BLIT);
			if (cbIRQ.Text == "COPPER") debugger.INTDIS(Core.Types.Interrupt.COPPER);
			if (cbIRQ.Text == "DSKSYNC") debugger.INTDIS(Core.Types.Interrupt.DSKSYNC);
			if (cbIRQ.Text == "AUD0") debugger.INTDIS(Core.Types.Interrupt.AUD0);
			if (cbIRQ.Text == "AUD1") debugger.INTDIS(Core.Types.Interrupt.AUD1);
			if (cbIRQ.Text == "AUD2") debugger.INTDIS(Core.Types.Interrupt.AUD2);
			if (cbIRQ.Text == "AUD3") debugger.INTDIS(Core.Types.Interrupt.AUD3);
			if (cbIRQ.Text == "VERTB") debugger.INTDIS(Core.Types.Interrupt.VERTB);
			if (cbIRQ.Text == "SOFTINT") debugger.INTDIS(Core.Types.Interrupt.SOFTINT);
			Amiga.UnlockEmulation();
		}

		private int lastFound = -1;
		private string lastText = string.Empty;

		public ReactiveCommand<string, Unit> menuDisassembly_ItemClickedEvent { get; }

		private void menuDisassembly_ItemClicked(string e)
		{
			uint pc;
			//{
			//	var mouse = this.PointToClient(ctx.Location);
			//	logger.LogTrace($"ctx {mouse.X} {mouse.Y}");
			//	int c = txtDisassembly.GetCharIndexFromPosition(mouse);
			//	logger.LogTrace($"char {c}");
			//	int line = txtDisassembly.GetLineFromCharIndex(c) - 1;
			//	logger.LogTrace($"line {line}");
			//	pc = disassemblyView.GetLineAddress(line);
			//	logger.LogTrace($"PC {pc:X8}");
			//}

			if (e == "toolStripBreakpoint")
			{
				//logger.LogTrace($"BP {pc:X8}");
				//Amiga.LockEmulation();
				//debugger.ToggleBreakpoint(pc);
				//Amiga.UnlockEmulation();
			}
			else if (e == "toolStripSkip")
			{
				//logger.LogTrace($"SKIP {pc:X8}");
				//Amiga.LockEmulation();
				//debugger.SetPC(pc);
				//Amiga.UnlockEmulation();
			}
			else if (e == "toolStripGoto")
			{
				//var gotoForm = new GoTo();
				//var res = gotoForm.ShowDialog();
				//if (res == DialogResult.OK)
				//{
				//	uint address = gotoForm.GotoLocation;
				//	int gotoLine = disassemblyView.GetAddressLine(address);
				//	txtDisassembly.SuspendLayout();
				//	txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, gotoLine - 5));
				//	txtDisassembly.ScrollToCaret();
				//	txtDisassembly.Select(txtDisassembly.GetFirstCharIndexFromLine(gotoLine),
				//		txtDisassembly.GetFirstCharIndexFromLine(gotoLine + 1) - txtDisassembly.GetFirstCharIndexFromLine(gotoLine));
				//	txtDisassembly.Invalidate();
				//	txtDisassembly.ResumeLayout();
				//	txtDisassembly.Update();
				//}
			}
			else if (e == "toolStripFind")
			{
				//var findForm = new Find();
				//findForm.radioFindByte.Enabled
				//	= findForm.radioFindWord.Enabled
				//	= findForm.radioFindLong.Enabled = false;
				//findForm.radioFindText.Checked = true;
				//var res = findForm.ShowDialog();
				//if (res == DialogResult.OK)
				//{
				//	if (findForm.SearchText != null)
				//	{
				//		lastText = findForm.SearchText;
				//		lastFound = txtDisassembly.Find(findForm.SearchText, 0, RichTextBoxFinds.NoHighlight);
				//		if (lastFound != -1)
				//		{
				//			txtDisassembly.ReallySuspendLayout();
				//			txtDisassembly.DeselectAll();
				//			txtDisassembly.SelectionStart = lastFound;
				//			txtDisassembly.ScrollToCaret();
				//			txtDisassembly.ReallyResumeLayout();
				//			txtDisassembly.Refresh();
				//		}
				//	}
				//	return;
				//}
			}
			else if (e == "toolStripFindNext")
			{
				//if (lastFound == -1)
				//	return;
				//lastFound = txtDisassembly.Find(lastText, lastFound + 1, RichTextBoxFinds.NoHighlight);
				//for (int i = 0; i < 2; i++)
				//{
				//	if (lastFound != -1)
				//	{
				//		txtDisassembly.ReallySuspendLayout();
				//		txtDisassembly.DeselectAll();
				//		txtDisassembly.SelectionStart = lastFound;
				//		txtDisassembly.ScrollToCaret();
				//		txtDisassembly.ReallyResumeLayout();
				//		txtDisassembly.Refresh();
				//		return;
				//	}
				//	lastFound = txtDisassembly.Find(lastText, 0, RichTextBoxFinds.NoHighlight);
				//}
			}

			FetchUI(FetchUIFlags.All);
			//UpdateDisassembly();
			SetSelection();
			UpdateDisplay();
		}

		private void btnDumpTrace_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			debugger.WriteTrace();
			Amiga.UnlockEmulation();
		}

		private void btnIDEACK_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			debugger.IDEACK();
			Amiga.UnlockEmulation();
		}

		private int currentDrive = 0;
		private void btnChange_Click(object sender, RoutedEventArgs e)
		{
			StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions
				{
					FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("ADF Files") { Patterns = new[] { "*.adf", "*.zip", "*.adz", "*.rp9", "*.dms", "*.ipf" } } }
				}).ContinueWith((t) => {
					var openFileDialog1 = t.Result;
					if (openFileDialog1.Any())
					{
						Amiga.LockEmulation();
						debugger.ChangeDisk(currentDrive, HttpUtility.UrlDecode(openFileDialog1.First().Path.AbsolutePath));
						Amiga.UnlockEmulation();
					}
				});
		}

		private void btnInsertDisk_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			debugger.InsertDisk(currentDrive);
			Amiga.UnlockEmulation();
		}

		private void btnRemoveDisk_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			debugger.RemoveDisk(currentDrive);
			Amiga.UnlockEmulation();
		}

		private void btnGfxScan_Click(object sender, RoutedEventArgs e) { }

		private void btnStringScan_Click(object sender, RoutedEventArgs e) { }

		private void btnClearBBUSY_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			debugger.ClearBBUSY();
			Amiga.UnlockEmulation();
		}

		private void btnCribSheet_Click(object sender, RoutedEventArgs e) { }

		private void btnReadyDisk_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			debugger.ReadyDisk();
			Amiga.UnlockEmulation();
		}

		private void btnAnalyseFlow_Click(object sender, RoutedEventArgs e) { }

		private void btnDMAExplorer_Click(object sender, RoutedEventArgs e) { }

		private void btnGenDisassemblies_Click(object sender, RoutedEventArgs e)
		{
			Amiga.LockEmulation();
			debugger.GenerateDisassemblies();
			Amiga.UnlockEmulation();
		}
	}

	public static class JammyAvaloniaExtensions
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
			txtBox.CaretIndex = txtBox.SelectionStart;
		}

		public static int GetFirstCharIndexFromLine(this TextBox txtBox, int line)
		{
			return 0;
		}

		public static void DeselectAll(this TextBox txtBox)
		{
			txtBox.SelectionStart = -1;
			txtBox.SelectionEnd = -1;
		}

		public static void ReallySuspendLayout(this TextBox txtBox)
		{
		}

		public static void ReallyResumeLayout(this TextBox txtBox)
		{
		}

		public static void SuspendLayout(this TextBox txtBox)
		{
		}

		public static void ResumeLayout(this TextBox txtBox)
		{
		}

		public static void Refresh(this TextBox txtBox)
		{
		}

		public static void Invalidate(this TextBox txtBox)
		{
		}

		public static void Update(this TextBox txtBox)
		{
		}

		public static void Select(this TextBox txtBox, int start, int length)
		{
			txtBox.SelectionStart = start;
			txtBox.SelectionEnd = start + length;
		}
	}
}
