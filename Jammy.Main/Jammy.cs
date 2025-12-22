using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jammy.Core;
using Jammy.Core.Custom.CIA;
using Jammy.Core.Custom;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Disassembler;
using Jammy.Disassembler.TypeMapper;
using Jammy.Extensions.Windows;
using Jammy.Interface;
using Jammy.Main.Dialogs;
using Jammy.Types;
using Jammy.Types.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vortice.Direct3D11;
using System.Runtime.InteropServices;
using Jammy.Core.Types.Types.Breakpoints;
using Jammy.Core.Memory;
using Jammy.Debugger;
using Jammy.Graph;
using Jammy.Core.Debug;
using Parky.FormToAvalonia;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main
{
	public partial class Jammy : Form
	{
		private readonly IEmulation emulation;
		private IDisassemblyView disassemblyView;
		private IMemoryDumpView memoryDumpView;
		private readonly IDisassembly disassembly;
		private readonly IDebugger debugger;
		private readonly IAnalysis analysis;
		private readonly IFlowAnalyser flowAnalyser;
		private readonly IGraph graph;
		private readonly IChipsetDebugger chipsetDebugger;
		private readonly IObjectMapper objectMapper;
		private readonly IChipRAM chipRAM;
		private readonly ILogger<GfxScan> gfxLogger;
		private readonly ILogger<StringScan> stringLogger;
		private readonly ILogger<DMAExplorer> dmaLogger;
		private readonly IInstructionAnalysisDatabase instructionAnalysisDatabase;
		private readonly IMemoryMapper memoryMapper;
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
			IFlowAnalyser flowAnalyser, IGraph graph, IChipsetDebugger chipsetDebugger, IObjectMapper objectMapper,
			IChipRAM chipRAM, ILogger<GfxScan> gfxLogger, ILogger<StringScan> stringLogger, IMemoryMapper memoryMapper,
			ILogger<DMAExplorer> dmaLogger, IInstructionAnalysisDatabase instructionAnalysisDatabase,
			ILogger<Jammy> logger, IOptions<EmulationSettings> options)
		{
			if (this.Handle == IntPtr.Zero)
				throw new ApplicationException("Can't create Handle");

			this.emulation = emulation;
			this.disassembly = disassembly;
			this.debugger = debugger;
			this.analysis = analysis;
			this.flowAnalyser = flowAnalyser;
			this.graph = graph;
			this.chipsetDebugger = chipsetDebugger;
			this.objectMapper = objectMapper;
			this.chipRAM = chipRAM;
			this.gfxLogger = gfxLogger;
			this.stringLogger = stringLogger;
			this.dmaLogger = dmaLogger;
			this.instructionAnalysisDatabase = instructionAnalysisDatabase;
			this.memoryMapper = memoryMapper;
			this.logger = logger;

			InitializeComponent();
			//DarkMode.Apply(this);
			//Av.Convert(this);

			addressFollowBox.SelectedIndex = 0;
			cbTypes.SelectedIndex = 0;

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

			InitUIRefreshThread();
		}

		private CancellationTokenSource uiUpdateTokenSource;
		private Task uiUpdateTask;

		private void InitUIRefreshThread()
		{
			uiUpdateTokenSource = new CancellationTokenSource();

			uiUpdateTask = new Task(() =>
			{
				while (!uiUpdateTokenSource.IsCancellationRequested)
				{
					//this.Invoke((Action)UpdatePowerLight);

					if (UI.UI.IsDirty)
					{
						this.Invoke((Action)delegate ()
						{
							FetchUI(FetchUIFlags.All);
							SetSelection();
							UpdateDisplay();
						});
					}
				}
			}, uiUpdateTokenSource.Token, TaskCreationOptions.LongRunning);
			uiUpdateTask.Start();
		}

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
			UpdatePowerLight();
			UpdateDiskLight();
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

		private void SetSelection()
		{
			uint pc = uiData.Regs.PC;

			//int line = disassembly.GetAddressLine(pc);
			//if (line == 0) return;

			txtDisassembly.ReallySuspendLayout();
			txtDisassembly.DeselectAll();

			//disassemblyView = disassembly.DisassemblyView(pc, 10, 100, disassemblyOptions);
			int line = disassemblyView.GetAddressLine(pc & (uint)((1<<settings.AddressBits)-1));

			//only need to do this is disassembly is actually updated
			//txtDisassembly.Text = disassemblyView.Text;

			//scroll the view to the line 5 lines before the PC
			txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, line - 5));
			txtDisassembly.ScrollToCaret();

			//find the line at the current pc, and the next line after that, and highlight it.
			int start = txtDisassembly.GetFirstCharIndexFromLine(line);

			/*
			if (start >= 0)
			{
				string pcs = $"{pc:X6}";
				string pct= txtDisassembly.Text.Substring(start, 6);
				if (pcs != pct)
				{
					logger.LogTrace($"PC {pc:X6} LINE {txtDisassembly.Text.Substring(start, 6)}");
					try { 
					for (int l = 0; l < 1000000; l+=1000)
					{
						//get the address 'p' at start of line 'l'
						int s = txtDisassembly.GetFirstCharIndexFromLine(l);
						string p = txtDisassembly.Text.Substring(s, 6);

						//get the line 'x' from address 'p' from the disassembly
						int x = disassemblyView.GetAddressLine(uint.Parse(p, NumberStyles.AllowHexSpecifier));

						//they don't match, let's look
						if (l != x)
							logger.LogTrace($"Line {l} != {x} @{p:X6} {p}");
					}
					}
					catch { }
				}
			}
			*/
			if (start >= 0)
				txtDisassembly.Select(start, txtDisassembly.GetFirstCharIndexFromLine(line + 1) - start);
			else
				txtDisassembly.DeselectAll();

			txtDisassembly.ReallyResumeLayout();
			txtDisassembly.Refresh();
		}

		private void btnStep_Click(object sender, EventArgs e)
		{
			Amiga.SetEmulationMode(EmulationMode.Step);
		}

		private void btnStepOut_Click(object sender, EventArgs e)
		{
			Amiga.SetEmulationMode(EmulationMode.StepOut);
		}

		private void btnStop_Click(object sender, EventArgs e)
		{
			Amiga.SetEmulationMode(EmulationMode.Stopped);
		}

		private void btnGo_Click(object sender, EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Amiga.SetEmulationMode(EmulationMode.Running);
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			Amiga.SetEmulationMode(EmulationMode.Stopped);

			Amiga.LockEmulation();
			emulation.Reset();
			Amiga.UnlockEmulation();

			FetchUI(FetchUIFlags.All);
			SetSelection();
			UpdateDisplay();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			//set the token to cancel the UI thread
			uiUpdateTokenSource.Cancel();

			//release the UI thread semaphore in case it's being awaited on
			//(UI may well get redrawn now)
			UI.UI.IsDirty = true;

			//ensure Invokes are run
			Application.DoEvents();

			//wait for the Task to complete
			uiUpdateTask.Wait();

			//neatly exit the emulation thread
			Amiga.SetEmulationMode(EmulationMode.Exit);
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			FetchUI(FetchUIFlags.All);
			SetSelection();
			UpdateDisplay();
		}

		private void btnStepOver_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			debugger.BreakAtNextPC();
			Amiga.SetEmulationMode(EmulationMode.Running, true);
			Amiga.UnlockEmulation();
		}

		private void UpdatePowerLight()
		{
			bool power = UI.UI.PowerLight;
			picPower.BackColor = power ? Color.Red : Color.DarkRed;
		}

		private void UpdateDiskLight()
		{
			bool disk = global::Jammy.UI.UI.DiskLight;
			picDisk.BackColor = disk ? Color.LightGreen : Color.DarkGreen;
		}

		private void btnDisassemble_Click(object sender, EventArgs e)
		{
			FetchUI(FetchUIFlags.All);
			UpdateDisassembly();
			SetSelection();
			UpdateDisplay();
		}

		private void addressFollowBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
			FetchUI(FetchUIFlags.All);
			UpdateDisplay();
		}

		private void menuMemory_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (!(sender is ContextMenuStrip))
				return;

			var ctx = (ContextMenuStrip)sender;

			if (e.ClickedItem == menuMemoryGotoItem)
			{
				var gotoForm = new GoTo();
				var res = gotoForm.ShowDialog();
				if (res == DialogResult.OK)
				{
					uint address = gotoForm.GotoLocation;
				}
			}
			else if (e.ClickedItem == menuMemoryFindItem)
			{
				var findForm = new Find();
				var res = findForm.ShowDialog();
				if (res == DialogResult.OK)
				{
					if (findForm.SearchText != null)
					{
						uint address = debugger.FindMemoryText(findForm.SearchText);
						if (address != 0)
							JumpToMemoryAddress(address);
					}
					if (findForm.SearchSeq != null)
					{
						uint address = debugger.FindMemory(findForm.SearchSeq);
						if (address != 0)
							JumpToMemoryAddress(address);
					}
				}
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

		private void btnCIAInt_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			if (cbCIA.Text == "TIMERA") debugger.CIAInt(ICRB.TIMERA);
			if (cbCIA.Text == "TIMERB") debugger.CIAInt(ICRB.TIMERB);
			if (cbCIA.Text == "TODALARM") debugger.CIAInt(ICRB.TODALARM);
			if (cbCIA.Text == "SERIAL") debugger.CIAInt(ICRB.SERIAL);
			if (cbCIA.Text == "FLAG") debugger.CIAInt(ICRB.FLAG);
			Amiga.UnlockEmulation();
		}

		private void btnIRQ_Click(object sender, EventArgs e)
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

		private void btnINTENA_Click(object sender, EventArgs e)
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

		private void btnINTDIS_Click(object sender, EventArgs e)
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

		private void menuDisassembly_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (!(sender is ContextMenuStrip))
				return;

			var ctx = (ContextMenuStrip)sender;

			uint pc;
			{
				var mouse = this.PointToClient(ctx.Location);
				logger.LogTrace($"ctx {mouse.X} {mouse.Y}");
				int c = txtDisassembly.GetCharIndexFromPosition(mouse);
				logger.LogTrace($"char {c}");
				int line = txtDisassembly.GetLineFromCharIndex(c) - 1;
				logger.LogTrace($"line {line}");
				pc = disassemblyView.GetLineAddress(line);
				logger.LogTrace($"PC {pc:X8}");
			}

			if (e.ClickedItem == toolStripBreakpoint)
			{
				logger.LogTrace($"BP {pc:X8}");
				Amiga.LockEmulation();
				debugger.ToggleBreakpoint(pc);
				Amiga.UnlockEmulation();
			}
			else if (e.ClickedItem == toolStripSkip)
			{
				logger.LogTrace($"SKIP {pc:X8}");
				Amiga.LockEmulation();
				debugger.SetPC(pc);
				Amiga.UnlockEmulation();
			}
			else if (e.ClickedItem == toolStripGoto)
			{
				var gotoForm = new GoTo();
				var res = gotoForm.ShowDialog();
				if (res == DialogResult.OK)
				{
					uint address = gotoForm.GotoLocation;
					int gotoLine = disassemblyView.GetAddressLine(address);
					txtDisassembly.SuspendLayout();
					txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, gotoLine - 5));
					txtDisassembly.ScrollToCaret();
					txtDisassembly.Select(txtDisassembly.GetFirstCharIndexFromLine(gotoLine),
						txtDisassembly.GetFirstCharIndexFromLine(gotoLine + 1) - txtDisassembly.GetFirstCharIndexFromLine(gotoLine));
					txtDisassembly.Invalidate();
					txtDisassembly.ResumeLayout();
					txtDisassembly.Update();
				}
			}
			else if (e.ClickedItem == toolStripFind)
			{
				var findForm = new Find();
				findForm.radioFindByte.Enabled
					= findForm.radioFindWord.Enabled
					= findForm.radioFindLong.Enabled = false;
				findForm.radioFindText.Checked = true;
				var res = findForm.ShowDialog();
				if (res == DialogResult.OK)
				{
					if (findForm.SearchText != null)
					{
						lastText = findForm.SearchText;
						lastFound = txtDisassembly.Find(findForm.SearchText, 0, RichTextBoxFinds.NoHighlight);
						if (lastFound != -1)
						{
							txtDisassembly.ReallySuspendLayout();
							txtDisassembly.DeselectAll();
							txtDisassembly.SelectionStart = lastFound;
							txtDisassembly.ScrollToCaret();
							txtDisassembly.ReallyResumeLayout();
							txtDisassembly.Refresh();
						}
					}
					return;
				}
			}
			else if (e.ClickedItem == toolStripFindNext)
			{
				if (lastFound == -1)
					return;
				lastFound = txtDisassembly.Find(lastText, lastFound + 1, RichTextBoxFinds.NoHighlight);
				for (int i = 0; i < 2; i++)
				{
					if (lastFound != -1)
					{
						txtDisassembly.ReallySuspendLayout();
						txtDisassembly.DeselectAll();
						txtDisassembly.SelectionStart = lastFound;
						txtDisassembly.ScrollToCaret();
						txtDisassembly.ReallyResumeLayout();
						txtDisassembly.Refresh();
						return;
					}
					lastFound = txtDisassembly.Find(lastText, 0, RichTextBoxFinds.NoHighlight);
				}
			}

			FetchUI(FetchUIFlags.All);
			//UpdateDisassembly();
			SetSelection();
			UpdateDisplay();
		}

		private void GetAmigaTypes()
		{
			var types = AmigaTypes.AmigaType.GetAmigaTypes();
			cbTypes.Items.Clear();
			cbTypes.Items.Add("(None)");
			cbTypes.Items.AddRange(types.Keys.OrderBy(x=>x).ToArray());
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

		private void cbTypes_SelectionChangeCommitted(object sender, EventArgs e)
		{
			UpdateExecBase();
		}

		private void btnDumpTrace_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			debugger.WriteTrace();
			Amiga.UnlockEmulation();
		}

		private void btnIDEACK_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			debugger.IDEACK();
			Amiga.UnlockEmulation();
		}

		private int currentDrive = 0;
		private void btnChange_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				if (ofd.ShowDialog() == DialogResult.OK)
				{
					Amiga.LockEmulation();
					debugger.ChangeDisk(currentDrive, ofd.FileName);
					Amiga.UnlockEmulation();
				}
			}
		}

		private void btnInsertDisk_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			debugger.InsertDisk(currentDrive);
			Amiga.UnlockEmulation();
		}

		private void btnRemoveDisk_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			debugger.RemoveDisk(currentDrive);
			Amiga.UnlockEmulation();
		}

		private void radioDFx_CheckedChanged(object sender, EventArgs e)
		{
			var button = (RadioButton)sender;

			radioDF0.CheckedChanged -= radioDFx_CheckedChanged;
			radioDF1.CheckedChanged -= radioDFx_CheckedChanged;
			radioDF2.CheckedChanged -= radioDFx_CheckedChanged;
			radioDF3.CheckedChanged -= radioDFx_CheckedChanged;

			radioDF0.Checked = false;
			radioDF1.Checked = false;
			radioDF2.Checked = false;
			radioDF3.Checked = false;
			button.Checked = true;

			if (button == radioDF0) currentDrive = 0;
			if (button == radioDF1) currentDrive = 1;
			if (button == radioDF2) currentDrive = 2;
			if (button == radioDF3) currentDrive = 3;

			radioDF0.CheckedChanged += radioDFx_CheckedChanged;
			radioDF1.CheckedChanged += radioDFx_CheckedChanged;
			radioDF2.CheckedChanged += radioDFx_CheckedChanged;
			radioDF3.CheckedChanged += radioDFx_CheckedChanged;
		}

		private void btnGfxScan_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			var gfxScan = new GfxScan(gfxLogger, chipRAM);
			Amiga.UnlockEmulation();
		}

		private void btnStringScan_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			var stringScan = new StringScan(stringLogger, memoryMapper);
			Amiga.UnlockEmulation();
		}

		private void btnClearBBUSY_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			debugger.ClearBBUSY();
			Amiga.UnlockEmulation();
		}

		private void lbCallStack_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			int index = this.lbCallStack.IndexFromPoint(e.Location);
			if (index == ListBox.NoMatches) return;
			string item = (string)lbCallStack.Items[index];

			//is it a hex number?
			if (!char.IsAsciiHexDigit(item[0])) return;

			uint sp = uint.Parse(item, NumberStyles.AllowHexSpecifier);
			if (sp != 0)
			{
				logger.LogTrace($"scrolling to {sp:X8}");

				{
					txtDisassembly.ReallySuspendLayout();
					txtDisassembly.DeselectAll();

					int line = disassemblyView.GetAddressLine(sp);

					//scroll the view to the line 5 lines before the PC
					txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, line - 5));
					txtDisassembly.ScrollToCaret();

					txtDisassembly.ReallyResumeLayout();
					txtDisassembly.Refresh();
				}

				{
					int line = memoryDumpView.AddressToLine(sp);
					txtMemory.SelectionStart = txtMemory.GetFirstCharIndexFromLine(line);
					txtMemory.ScrollToCaret();
				}
			}
		}

		private void lbRegisters_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			int index = this.lbRegisters.IndexFromPoint(e.Location);
			if (index == ListBox.NoMatches) return;

			string item = (string)lbRegisters.Items[index];
			//is it Dx Ax PC SP SSP? if so we can follow it
			if (!item.StartsWith("D") && !item.StartsWith("A") &&
				!item.StartsWith("PC") && !item.StartsWith("S")) return;

			uint sp = uint.Parse(item.Split([' ', '\t'])[1], NumberStyles.AllowHexSpecifier);
			if (sp != 0)
			{
				logger.LogTrace($"scrolling to {sp:X8}");
				{
					txtDisassembly.ReallySuspendLayout();
					txtDisassembly.DeselectAll();

					int line = disassemblyView.GetAddressLine(sp);

					//scroll the view to the line 5 lines before the PC
					txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, line - 5));
					txtDisassembly.ScrollToCaret();

					txtDisassembly.ReallyResumeLayout();
					txtDisassembly.Refresh();
				}

				{
					int line = memoryDumpView.AddressToLine(sp);
					txtMemory.SelectionStart = txtMemory.GetFirstCharIndexFromLine(line);
					txtMemory.ScrollToCaret();
				}
			}
		}

		public class MiniForm : Form
		{
			private const int SC_CLOSE = 0xF060;
			private const int SC_MAXIMIZE = 0xF030;
			private const int SC_MINIMIZE = 0xF020;
			private const int SC_RESTORE = 0xF120;

			private const int WM_SYSCOMMAND = 0x0112;

			protected override void WndProc(ref Message m)
			{
				if (m.Msg == WM_SYSCOMMAND)
				{
					if (m.WParam == new IntPtr(SC_MAXIMIZE)) OnMaximize(new EventArgs());
					if (m.WParam == new IntPtr(SC_MINIMIZE)) OnMinimize(new EventArgs());
					if (m.WParam == new IntPtr(SC_RESTORE)) OnRestore(new EventArgs());
					//if (m.WParam == new IntPtr(SC_CLOSE)) { OnClose(new EventArgs()); return; /* override close behaviour */ }
				}
				base.WndProc(ref m);
			}

			private static readonly object s_minimizeEvent = new();
			private static readonly object s_maximizeEvent = new();
			private static readonly object s_restoreEvent = new();
			private static readonly object s_closeEvent = new();

			public event EventHandler MinimizeEvent
			{
				add => Events.AddHandler(s_minimizeEvent, value);
				remove => Events.RemoveHandler(s_minimizeEvent, value);
			}
			public event EventHandler MaximizeEvent
			{
				add => Events.AddHandler(s_maximizeEvent, value);
				remove => Events.RemoveHandler(s_maximizeEvent, value);
			}
			public event EventHandler RestoreEvent
			{
				add => Events.AddHandler(s_restoreEvent, value);
				remove => Events.RemoveHandler(s_restoreEvent, value);
			}
			public event EventHandler CloseEvent
			{
				add => Events.AddHandler(s_closeEvent, value);
				remove => Events.RemoveHandler(s_closeEvent, value);
			}
			protected void OnMinimize(EventArgs e) { if (Events[s_minimizeEvent] is EventHandler eh) eh(this, e); }
			protected void OnMaximize(EventArgs e) { if (Events[s_maximizeEvent] is EventHandler eh) eh(this, e); }
			protected void OnClose(EventArgs e) { if (Events[s_closeEvent] is EventHandler eh) eh(this, e); }
			protected void OnRestore(EventArgs e) { if (Events[s_restoreEvent] is EventHandler eh) eh(this, e); }

			private const int CP_NOCLOSE_BUTTON = 0x200;
			protected override CreateParams CreateParams
			{
				get
				{
					var cp = base.CreateParams;
					cp.ClassStyle |= CP_NOCLOSE_BUTTON;
					return cp;
				}
			}
		}

		private MiniForm cribSheet;
		private void CreateCribSheet()
		{
			if (cribSheet != null)
			{
				//cribSheet.Activate();
				if (cribSheet.WindowState == FormWindowState.Minimized)
					cribSheet.WindowState = FormWindowState.Normal;
				return;
			}

			var a = CIAAOdd.GetCribSheet();
			var b = CIABEven.GetCribSheet();
			var c = ChipRegs.GetCribSheet();
			//var d = a.Concat(b).Concat(c);

			const int width = 848 * 2;
			const int height = 480 * 2;

			cribSheet = new MiniForm { Name = "CribSheet", Text = "CribSheet", ShowInTaskbar = true, ControlBox = true, FormBorderStyle = FormBorderStyle.Sizable, MinimizeBox = true, MaximizeBox = false };
			if (cribSheet.Handle == IntPtr.Zero)
				throw new ApplicationException();
			var pos = Cursor.Position;
			//cribSheet.Top = pos.Y + 1;
			cribSheet.Left = pos.X - (width / 2);
			cribSheet.Width = width;
			cribSheet.Height = height;

			var tb = new TextBox();
			tb.Multiline = true;
			tb.WordWrap = false;
			tb.ReadOnly = true;
			tb.Font = new Font(FontFamily.GenericMonospace, 7.5f);
			tb.ScrollBars = ScrollBars.Both;
			tb.BackColor = SystemColors.Window;
			//tb.Text = string.Join("\r\n", d);
			int col = (c.Count + 3) / 3;
			var cols = new List<List<string>>
			{
				c.Take(col).ToList(),
				c.Skip(col).Take(col).ToList(),
				c.Skip(col*2).Take(col).ToList(),
				a.Concat(new List<string>{""}).Concat(b).ToList()
			};
			int pad = a.Concat(b).Concat(c).Max(x => x.Length);
			var items = new string[cols.Count];
			var sb = new StringBuilder();
			for (int j = 0; j < cols.Max(x => x.Count); j++)
			{
				for (int i = 0; i < cols.Count; i++)
				{
					items[i] = cols[i].Count > j ? cols[i][j] : string.Empty;
				}
				sb.AppendLine(string.Join(" | ", items.Select(x => x.PadRight(pad))));
			}
			tb.Text = sb.ToString();

			tb.Width = cribSheet.ClientSize.Width;
			tb.Height = cribSheet.ClientSize.Height;
			tb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;

			cribSheet.Controls.Add(tb);

			//cribSheet.CloseEvent += (s, ev) => { cribSheet.Hide(); };
			//tb.KeyDown += (s, ev) => { if (ev.KeyValue == (int)Keys.Escape) cribSheet.Hide(); };

			cribSheet.Show();
			tb.DeselectAll();
		}

		private void btnCribSheet_Click(object sender, EventArgs e)
		{
			CreateCribSheet();
		}

		private void btnReadyDisk_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			debugger.ReadyDisk();
			Amiga.UnlockEmulation();
		}

		private void lbIntvec_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			int index = this.lbIntvec.IndexFromPoint(e.Location);
			if (index == ListBox.NoMatches) return;

			string item = (string)lbIntvec.Items[index];

			item = item.Split(" ")[1];
			uint sp = uint.Parse(item, NumberStyles.AllowHexSpecifier);
			if (sp != 0)
			{
				logger.LogTrace($"scrolling to {sp:X8}");

				{
					txtDisassembly.ReallySuspendLayout();
					txtDisassembly.DeselectAll();

					int line = disassemblyView.GetAddressLine(sp);

					//scroll the view to the line 5 lines before the PC
					txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, line - 5));
					txtDisassembly.ScrollToCaret();

					txtDisassembly.ReallyResumeLayout();
					txtDisassembly.Refresh();
				}

				{
					int line = memoryDumpView.AddressToLine(sp);
					txtMemory.SelectionStart = txtMemory.GetFirstCharIndexFromLine(line);
					txtMemory.ScrollToCaret();
				}
			}
		}

		private List<string> history = new List<string>();
		private int currentHistory = 0;

		private void AddHistory(string h)
		{
			history.Add(h);
			currentHistory = history.Count;
		}

		private void tbCommand_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				ProcessCommand(tbCommand.Text);
				tbCommand.Clear();
				tbCommand.PlaceholderText = ">";
				e.SuppressKeyPress = true;
			}

			if (e.KeyCode == Keys.Up)
			{
				if (currentHistory > 0)
				{
					currentHistory--;
					tbCommand.Text = history[currentHistory];
					tbCommand.SelectionStart = tbCommand.Text.Length;
				}
				e.SuppressKeyPress = true;
			}
			if (e.KeyCode == Keys.Down)
			{
				if (currentHistory < history.Count - 1)
				{
					currentHistory++;
					tbCommand.Text = history[currentHistory];
					tbCommand.SelectionStart = tbCommand.Text.Length;
				}
				else
				{
					tbCommand.Clear();
					tbCommand.PlaceholderText = ">";
				}
				e.SuppressKeyPress = true;
			}
		}

		private void ProcessCommand(string cmd)
		{
			string[] parm = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parm.Length == 0)
				return;

			uint A(int i) { string s = P(i); return uint.Parse(s, NumberStyles.HexNumber); }
			uint AD(int i, uint def) { string s = P(i); return string.IsNullOrEmpty(s) ? def : uint.Parse(s, NumberStyles.HexNumber); }
			uint? N(int i) { string s = P(i); return string.IsNullOrWhiteSpace(s) ? null : A(i); }
			Core.Types.Types.Size? S(int i)
			{
				string s = P(i);
				if (s.Length != 1) return null;
				if (char.ToLower(s[0]) == 'b') return Core.Types.Types.Size.Byte;
				if (char.ToLower(s[0]) == 'w') return Core.Types.Types.Size.Word;
				if (char.ToLower(s[0]) == 'l') return Core.Types.Types.Size.Long;
				return null;
			}
			string P(int i) { return (i < parm.Length) ? parm[i] : string.Empty; }
			string R(int i) { return (i < parm.Length) ? string.Join(' ', parm[i..]) : string.Empty; }
			MemType? M(int i)
			{
				string s = P(i);
				if (s.Length != 1) return null;
				if (char.ToLower(s[0]) == 'c') return MemType.Code;
				if (char.ToLower(s[0]) == 'b') return MemType.Byte;
				if (char.ToLower(s[0]) == 'w') return MemType.Word;
				if (char.ToLower(s[0]) == 'l') return MemType.Long;
				if (char.ToLower(s[0]) == 's') return MemType.Str;
				if (char.ToLower(s[0]) == 'u') return MemType.Unknown;
				return null;
			}

			Amiga.LockEmulation();
			bool refresh = false;
			var regs = debugger.GetRegs();

			try
			{
				switch (P(0))
				{
					case "b":
						debugger.AddBreakpoint(AD(1, regs.PC));
						break;

					case "bw":
						debugger.AddBreakpoint(A(1), BreakpointType.Write, 0, S(2) ?? Core.Types.Types.Size.Word);
						break;
					case "br":
						debugger.AddBreakpoint(A(1), BreakpointType.Read, 0, S(2) ?? Core.Types.Types.Size.Word);
						break;
					case "brw":
						debugger.AddBreakpoint(A(1), BreakpointType.ReadOrWrite, 0, S(2) ?? Core.Types.Types.Size.Word);
						break;
					case "bl":
						debugger.DumpBreakpoints();
						break;

					case "bc":
						debugger.RemoveBreakpoint(AD(1, regs.PC));
						break;

					case "t":
						debugger.ToggleBreakpoint(AD(1, regs.PC));
						break;

					case "d":
						disassemblyRanges.Add(new AddressRange(A(1), N(2) ?? 0x1000));
						refresh = true;
						break;

					case "m":
						memoryDumpRanges.Add(new AddressRange(A(1), N(2) ?? 0x1000));
						refresh = true;
						break;

					case "w":
						debugger.DebugWrite(A(1), N(2) ?? 0, S(3) ?? Core.Types.Types.Size.Word);
						break;

					case "r":
						uint v = debugger.DebugRead(A(1), S(2) ?? Core.Types.Types.Size.Word);
						logger.LogTrace($"{v:X8} ({v})");
						break;

					case "a":
						for (uint i = 0; i < (N(3) ?? 1); i++)
							analysis.SetMemType(A(1) + i, M(2) ?? MemType.Code);
						refresh = true;
						break;

					case "c":
						analysis.AddComment(A(1), R(2));
						refresh = true;
						break;

					case "h":
						analysis.AddHeader(A(1), $"\t{R(2)}");
						refresh = true;
						break;

					case "g":
						Amiga.SetEmulationMode(EmulationMode.Running, true);
						break;
					case "so":
						Amiga.SetEmulationMode(EmulationMode.StepOut, true);
						break;
					case "s":
						Amiga.SetEmulationMode(EmulationMode.Step, true);
						break;
					case "x":
						Amiga.SetEmulationMode(EmulationMode.Stopped, true);
						break;

					case "?":
						logger.LogTrace("b address - breakpoint on execute at address");
						logger.LogTrace("bw address [size(W)] - breakpoint on write at address");
						logger.LogTrace("br address [size(W)] - breakpoint on read at address");
						logger.LogTrace("brw address [size(W)] - breakpoint on read/write at address");
						logger.LogTrace("bc address - remove breakpoint at address");
						logger.LogTrace("t address - toggle breakpoint at address");
						logger.LogTrace("bl - list all breakpoints");
						logger.LogTrace("d address [length(1000h)] - add an address range to the debugger");
						logger.LogTrace("m address [length(1000h)] - add an address range to the memory dump");
						logger.LogTrace("w address [value(0)] [size(W)] - write a value to memory");
						logger.LogTrace("r address [size(W)] - read a value from memory");
						logger.LogTrace("a address [type(C)] [length(1)] - set memory type C,B,W,L,S,U");
						logger.LogTrace("c address text - add a comment");
						logger.LogTrace("h address text - add a header");
						logger.LogTrace("g - emulation Go");
						logger.LogTrace("s - emulation Step");
						logger.LogTrace("so - emulation Step Out");
						logger.LogTrace("x - emulation Stop");
						break;
				}
				AddHistory(cmd);
			}
			catch
			{
				logger.LogTrace($"Can't execute \"{cmd}\"");
			}

			Amiga.UnlockEmulation();
			if (refresh)
				UpdateDisassembly();
		}

		private void btnAnalyseFlow_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			var regs = debugger.GetRegs();
			var trace = flowAnalyser.start_pc_trace(regs.PC);
			graph.GraphBranches(trace);
			Amiga.UnlockEmulation();
		}

		private void btnDMAExplorer_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			var x = new DMAExplorer(chipsetDebugger, dmaLogger);
			Amiga.UnlockEmulation();
		}

		private void btnGenDisassemblies_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			debugger.GenerateDisassemblies();
			Amiga.UnlockEmulation();
		}
	}

	public static class LayoutExtensions
	{
		private const int WM_SETREDRAW = 0xB;

		public static void ReallySuspendLayout(this Control c)
		{
			var msg = Message.Create(c.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
			NativeWindow.FromHandle(c.Handle).DefWndProc(ref msg);
		}

		public static void ReallyResumeLayout(this Control c)
		{
			var msg = Message.Create(c.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
			NativeWindow.FromHandle(c.Handle).DefWndProc(ref msg);
		}

		[DllImport("user32.dll")]
		private static extern int ShowWindow(IntPtr hWnd, uint Msg);

		private const uint SW_RESTORE = 0x09;

		public static void Restore(this Form form)
		{
			if (form.WindowState == FormWindowState.Minimized)
			{
				ShowWindow(form.Handle, SW_RESTORE);
			}
		}
	}
}
