using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jammy.Core;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Disassembler.TypeMapper;
using Jammy.Interface;
using Jammy.Main.Dialogs;
using Jammy.Types.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main
{
	public partial class Jammy : Form
	{
		private readonly IEmulation emulation;
		private IDisassemblyView disassemblyView;
		private readonly IDisassembly disassembly;
		private readonly IDebugger debugger;
		private readonly ILogger logger;
		private readonly EmulationSettings settings;
		private readonly DisassemblyOptions disassemblyOptions;

		public Jammy(IEmulation emulation, IDisassembly disassembly, IDebugger debugger,
			ILogger<Jammy> logger, IOptions<EmulationSettings> options)
		{
			if (this.Handle == IntPtr.Zero)
				throw new ApplicationException("Can't create Handle");

			this.emulation = emulation;
			this.disassembly = disassembly;
			this.debugger = debugger;
			this.logger = logger;

			InitializeComponent();

			addressFollowBox.SelectedIndex = 0;
			cbTypes.SelectedIndex = 0;

			settings = options.Value;

			disassemblyOptions = new DisassemblyOptions {IncludeBytes = true, IncludeBreakpoints = true, IncludeComments = true, Full32BitAddress = settings.AddressBits>24};

			emulation.Start();

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
						this.Invoke((Action)delegate()
						{
							//if (UI.UI.IsDirty)
							{
								SetSelection();
								UpdateDisplay();
							}
						});
					}

					//Task.Delay(500).Wait(uiUpdateTokenSource.Token);
				}
			}, uiUpdateTokenSource.Token, TaskCreationOptions.LongRunning);
			uiUpdateTask.Start();
		}

		private void UpdateDisassembly()
		{
			Amiga.LockEmulation();

			//prime the disassembly with a decent starting point
			var ranges = new List<Tuple<uint, uint>>
			{
				new Tuple<uint, uint>(0x000000, 0x1000),
				new Tuple<uint, uint>(0xfc0000, 0x40000),
			};
			if (settings.TrapdoorMemory != 0.0)
				ranges.Add(new Tuple<uint, uint>(0xc00000, 0x1000));
			if (debugger.KickstartSize() == 512 * 1024)
				ranges.Add(new Tuple<uint, uint>(0xf80000, 0x40000));
			if (debugger.KickstartSize() == 0x2000)
				ranges.Add(new Tuple<uint, uint>(0xf80000, 0x2000));

			disassembly.Clear();
			var disasm = disassembly.DisassembleTxt(
				ranges,
				disassemblyOptions);

			Amiga.UnlockEmulation();

			//this is the new view
			//disassemblyView = disassembly.DisassemblyView(0, 0, 100, disassemblyOptions);
			disassemblyView = disassembly.FullDisassemblyView(disassemblyOptions);
			txtDisassembly.Text = disassemblyView.Text;
		}

		private void UpdateDisplay()
		{
			UpdateRegs();
			//UpdateMem();
			UpdatePowerLight();
			UpdateDiskLight();
			//UpdateExecBase();
			UI.UI.IsDirty = false;
		}

		private void UpdateRegs()
		{
			Amiga.LockEmulation();
			var regs = debugger.GetRegs();
			var chipRegs = debugger.GetChipRegs();
			Amiga.UnlockEmulation();

			lbRegisters.Items.Clear();
			lbRegisters.Items.AddRange(regs.Items().Cast<object>().ToArray());

			lbCustom.Items.Clear();
			{
				//string hdr =
				//	"   D           S\n" +
				//	"   S       C  DO\n" +
				//	" IEK      VOP SF\n" +
				//	" NXS AAAABEPO KT\n" +
				//	"NTTYRUUUULRPRTBI\n" +
				//	"MEENBDDDDITETBLN\n" +
				//	"INTCF3210TBRSEKT";

				//lbCustom.Items.AddRange(hdr.Split('\n'));
				//lbCustom.Items.Add("INTENA W:DFF09A R:DFF01C");
				//lbCustom.Items.Add($"{Convert.ToString(intena, 2).PadLeft(16, '0')}");
				//lbCustom.Items.Add("INTREQ W:DFF09C R:DFF01E");
				//lbCustom.Items.Add($"{Convert.ToString(intreq, 2).PadLeft(16, '0')}");
			}

			{
				lbCustom.Items.Add($"SR: {(regs.SR >> 8) & 7} IRQ: {debugger.GetInterruptLevel()}");
				lbCustom.Items.Add("INTENA W:DFF09A R:DFF01C");
				lbCustom.Items.Add("INTREQ W:DFF09C R:DFF01E");
				lbCustom.Items.Add("        ENA REQ");
				string[] names = new String [16] {"NMI", "INTEN", "EXTEN", "DSKSYNC", "RBF", "AUD3", "AUD2", "AUD1", "AUD0", "BLIT", "VERTB", "COPPER", "PORTS", "SOFTINT", "DSKBLK", "TBE"};
				for (int i = 0; i < 16; i++)
				{
					int bit = 1 << (i ^ 15);
					lbCustom.Items.Add($"{names[i],8} {((chipRegs.intena & bit) != 0 ? 1 : 0)}   {((chipRegs.intreq & bit) != 0 ? 1 : 0)}");
				}
			}

			//*
			// * 			SETCLR = 0x8000,
			//			BBUSY = 0x4000,
			//			BZERO = 0x2000,
			//			unused0 = 0x1000,
			//			unused1 = 0x0800,
			//			BLTPRI = 0x0400,
			//			DMAEN = 0x0200,
			//			BPLEN = 0x00100,
			//			COPEN = 0x0080,
			//			BLTEN = 0x0040,
			//			SPREN = 0x0020,
			//			DSKEN = 0x0010,
			//			AUD3EN = 0x0008,
			//			AUD2EN = 0x0004,
			//			AUD1EN = 0x0002,
			//			AUD0EN = 0x0001,
			// */
			//{
			//	string hdr =
			//		"S    B      AAAA\n" +
			//		"EBB  LDBCBSDUUUU\n" +
			//		"TBZ  TMPOLPSDDDD\n" +
			//		"CUE  PALPTRK3210\n" +
			//		"LSR  REEEEEEEEEE\n" +
			//		"RYO  INNNNNNNNNN";
			//	lbCustom.Items.AddRange(hdr.Split('\n'));
			//	lbCustom.Items.Add("DMACON W:DFF096 R:DFF002");
			//	lbCustom.Items.Add($"{Convert.ToString(chipRegs.dmacon, 2).PadLeft(16, '0')}");
			//}
			{
				lbCustom.Items.Add("DMACON W:DFF096 R:DFF002");
				lbCustom.Items.Add("        ENA");
				string[] names = new string[16] {"SETCLR", "BBUSY", "BZERO", "x", "x", "BLTPRI", "DMAEN", "BPLEN", "COPEN", "BLTEN", "SPREN", "DSKEN", "AUD3EN", "AUD2EN", "AUD1EN", "AUD0EN"};
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

		private void UpdateMem()
		{
			Amiga.LockEmulation();
			var memory = debugger.GetMemory();
			var regs = debugger.GetRegs();
			Amiga.UnlockEmulation();

			{
				var mem = new List<Tuple<uint, uint>>();
				long sp = (long)regs.SP;
				long ssp = (long)regs.SSP;
				int cnt = 32;
				while (cnt-- > 0)
				{
					uint spv = memory.Read32((uint)sp);
					uint sspv = memory.Read32((uint)ssp);
					mem.Add(new Tuple<uint, uint>(spv, sspv));
					sp += 4;
					ssp += 4;
				}

				lbCallStack.Items.Clear();
				lbCallStack.Items.Add("   SP       SSP   ");
				lbCallStack.Items.AddRange(mem.Select(x => $"{x.Item1:X8}  {x.Item2:X8}").Cast<object>().ToArray());
			}

			{
				txtMemory.Text = memory.ToString();

				if (addressFollowBox.SelectedIndex != 0)
				{
					uint address = ValueFromRegName(regs, (string)addressFollowBox.SelectedItem);
					var line = memory.AddressToLine(address);
					if (line != 0)
					{
						txtMemory.SelectionStart = txtMemory.GetFirstCharIndexFromLine(line);
						txtMemory.ScrollToCaret();
					}
					UpdateExecBase();
				}
			}
		}

		private void SetSelection()
		{
			Amiga.LockEmulation();
			uint pc = debugger.GetRegs().PC;
			Amiga.UnlockEmulation();

			//int line = disassembly.GetAddressLine(pc);
			//if (line == 0) return;

			//txtDisassembly.SuspendLayout();
			txtDisassembly.ReallySuspendLayout();
			txtDisassembly.DeselectAll();

			//disassemblyView = disassembly.DisassemblyView(pc, 10, 100, disassemblyOptions);
			int line = disassemblyView.GetAddressLine(pc);
			txtDisassembly.Text = disassemblyView.Text;

			txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, line - 5));
			txtDisassembly.ScrollToCaret();
			int start = txtDisassembly.GetFirstCharIndexFromLine(line);
			if (start >=0)
				txtDisassembly.Select(start, txtDisassembly.GetFirstCharIndexFromLine(line + 1) - start);
			else
				txtDisassembly.DeselectAll();
			//txtDisassembly.ResumeLayout();
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
			UpdateDisassembly();
			SetSelection();
			UpdateDisplay();
		}

		private void addressFollowBox_SelectionChangeCommitted(object sender, EventArgs e)
		{
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

						Amiga.LockEmulation();
						var memory = debugger.GetMemory();
						int gotoLine = memory.AddressToLine(address);
						Amiga.UnlockEmulation();

						txtMemory.SuspendLayout();
						txtMemory.SelectionStart = txtMemory.GetFirstCharIndexFromLine(Math.Max(0, gotoLine - 5));
						txtMemory.ScrollToCaret();
						txtMemory.Select(txtMemory.GetFirstCharIndexFromLine(gotoLine),
							txtMemory.GetFirstCharIndexFromLine(gotoLine + 1) - txtMemory.GetFirstCharIndexFromLine(gotoLine));
						txtMemory.Invalidate();
						txtMemory.ResumeLayout();
						txtMemory.Update();
					}
				}
			}

			UpdateDisassembly();

			SetSelection();
			UpdateDisplay();
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
			if (cbIRQ.Text == "EXTER") debugger.IRQ(Interrupt.EXTER);
			if (cbIRQ.Text == "DSKBLK") debugger.IRQ(Interrupt.DSKBLK);
			if (cbIRQ.Text == "PORTS") debugger.IRQ(Interrupt.PORTS);
			if (cbIRQ.Text == "BLIT") debugger.IRQ(Interrupt.BLIT);
			if (cbIRQ.Text == "COPPER") debugger.IRQ(Interrupt.COPPER);
			if (cbIRQ.Text == "DSKSYNC") debugger.IRQ(Interrupt.DSKSYNC);
			if (cbIRQ.Text == "AUD0") debugger.IRQ(Interrupt.AUD0);
			if (cbIRQ.Text == "AUD1") debugger.IRQ(Interrupt.AUD1);
			if (cbIRQ.Text == "AUD2") debugger.IRQ(Interrupt.AUD2);
			if (cbIRQ.Text == "AUD3") debugger.IRQ(Interrupt.AUD3);
			Amiga.UnlockEmulation();
		}

		private void btnINTENA_Click(object sender, EventArgs e)
		{
			Amiga.LockEmulation();
			if (cbIRQ.Text == "EXTER") debugger.INTENA(Interrupt.EXTER);
			if (cbIRQ.Text == "DSKBLK") debugger.INTENA(Interrupt.DSKBLK);
			if (cbIRQ.Text == "PORTS") debugger.INTENA(Interrupt.PORTS);
			if (cbIRQ.Text == "BLIT") debugger.INTENA(Interrupt.BLIT);
			if (cbIRQ.Text == "COPPER") debugger.INTENA(Interrupt.COPPER);
			if (cbIRQ.Text == "DSKSYNC") debugger.INTENA(Interrupt.DSKSYNC);
			if (cbIRQ.Text == "AUD0") debugger.INTENA(Interrupt.AUD0);
			if (cbIRQ.Text == "AUD1") debugger.INTENA(Interrupt.AUD1);
			if (cbIRQ.Text == "AUD2") debugger.INTENA(Interrupt.AUD2);
			if (cbIRQ.Text == "AUD3") debugger.INTENA(Interrupt.AUD3);
			Amiga.UnlockEmulation();
		}

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

			//UpdateDisassembly();

			SetSelection();
			UpdateDisplay();
		}

		private void UpdateExecBase()
		{
			if (cbTypes.SelectedIndex != 0 && addressFollowBox.SelectedIndex != 0)
			{
				string typeName = (string)cbTypes.SelectedItem;

				Amiga.LockEmulation();

				var regs = debugger.GetRegs();

				uint address = ValueFromRegName(regs, (string)addressFollowBox.SelectedItem);

				var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "Jammy.Disassembler");
				if (assembly != null)
				{
					var type = assembly.GetTypes().SingleOrDefault(x => x.Name == typeName);
					if (type != null)
					{
						object tp = Activator.CreateInstance(type);
						if (tp != null)
						{
							Amiga.LockEmulation();
							txtExecBase.Text = ObjectMapper.MapObject(tp, address);

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
			debugger.IDEACK();
		}

		private int currentDrive = 0;
		private void btnChange_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				if (ofd.ShowDialog() == DialogResult.OK)
					debugger.ChangeDisk(currentDrive, ofd.FileName);
			}
		}

		private void btnInsertDisk_Click(object sender, EventArgs e)
		{
			debugger.InsertDisk(currentDrive);
		}

		private void btnRemoveDisk_Click(object sender, EventArgs e)
		{
			debugger.RemoveDisk(currentDrive);
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
			var gfxScan = new GfxScan(
					ServiceProviderFactory.ServiceProvider.GetRequiredService<ILogger<GfxScan>>(),
					ServiceProviderFactory.ServiceProvider.GetRequiredService<IChipRAM>()
				);
		}

		private void btnClearBBUSY_Click(object sender, EventArgs e)
		{
			debugger.ClearBBUSY();
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
	}
}
