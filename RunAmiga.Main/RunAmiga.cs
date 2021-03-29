using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Enums;
using RunAmiga.Core.Types.Options;
using RunAmiga.Core.Types.Types;
using RunAmiga.Disassembler.TypeMapper;
using RunAmiga.Main.Dialogs;

namespace RunAmiga.Main
{
	public partial class RunAmiga : Form
	{
		private readonly IEmulation emulation;
		private readonly IDisassembly disassembly;
		private readonly IDebugger debugger;
		private readonly ILogger logger;
		private readonly EmulationSettings settings;

		public RunAmiga(IEmulation emulation, IDisassembly disassembly, IDebugger debugger,
			ILogger<RunAmiga> logger,IOptions<EmulationSettings> options)
		{
			if (this.Handle == IntPtr.Zero)
				throw new ApplicationException("RunAmiga can't create Handle");

			this.emulation = emulation;
			this.disassembly = disassembly;
			this.debugger = debugger;
			this.logger = logger;

			InitializeComponent();

			addressFollowBox.SelectedIndex = 0;
			cbTypes.SelectedIndex = 0;

			settings = options.Value;

			UpdateDisassembly();
			UpdateDisplay();

			InitUIRefreshThread();

			emulation.Start();
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
							if (UI.UI.IsDirty)
							{
								SetSelection();
								UpdateDisplay();
							}
						});
					}

					Task.Delay(500).Wait(uiUpdateTokenSource.Token);
				}
			}, uiUpdateTokenSource.Token, TaskCreationOptions.LongRunning);
			uiUpdateTask.Start();
		}

		private void UpdateDisassembly()
		{
			Machine.LockEmulation();

			var ranges = new List<Tuple<uint, uint>>
			{
				new Tuple<uint, uint>(0x000000, 0x400),
				new Tuple<uint, uint>(0xfc0000, 0x40000),
			};
			if (settings.TrapdoorMemory != 0.0)
				ranges.Add(new Tuple<uint, uint>(0xc00000, 0x1000));
			if (debugger.KickstartSize() == 512*1024)
				ranges.Add(new Tuple<uint, uint>(0xf80000, 0x40000));

			var restarts = new List<uint>();
			if (settings.KickStart == "1.2")
				restarts.AddRange(new List<uint>
				{
					0xC0937b, 0xfe490c, 0xfe4916, 0xfe4f70, 0xfe5388, 0xFE53E8, 0xFE5478, 0xFE57D0, 0xFE5BC2,
					0xFE5D4C, 0xFE6994, 0xfe6dec, 0xFE6332, 0xfe66d8, 0xFE93C2, 0xFE571C, 0xFC5170, 0xFE5A04, 0xfe61d0, 0x00FE6FD4,
					0xFE43CC, 0xFE4588, 0xFE46CC, 0xfe42ee, 0xFC3A40, 0xfc43f4, 0xfc4408, 0xfc441c, 0xfc4474, 0xfc44a4, 0xfc44d0,0xFE62D4
				}); 

			var disasm = disassembly.DisassembleTxt(
				ranges,
				restarts,
				new DisassemblyOptions {IncludeBytes = true, IncludeBreakpoints = true, IncludeComments = true});

			Machine.UnlockEmulation();
			txtDisassembly.Text = disasm;
		}

		private void UpdateDisplay()
		{
			UpdateRegs();
			//UpdateMem();
			UpdatePowerLight();
			UpdateDiskLight();
			UpdateColours();
			UpdateExecBase();
			UI.UI.IsDirty = false;
		}

		private void UpdateRegs()
		{
			Machine.LockEmulation();
			var regs = debugger.GetRegs();
			var chipRegs = debugger.GetChipRegs();
			Machine.UnlockEmulation();
			
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
				string[] names = new String [16] {"NMI", "INTEN", "EXTEN", "DSKSYNC", "RBF", "AUD3", "AUD2", "AUD1", "AUD0", "BLIT", "VERTB", "COPPER", "PORTS", "TBE", "DSKBLJ", "SOFTINT"};
				for (int i = 0; i < 16; i++)
				{
					int bit = 1 << (i ^ 15);
					lbCustom.Items.Add($"{names[i],8} {((chipRegs.intena & bit) != 0 ? 1 : 0)}   {((chipRegs.intreq & bit) != 0 ? 1 : 0)}");
				}
			}

			/*
			 * 			SETCLR = 0x8000,
						BBUSY = 0x4000,
						BZERO = 0x2000,
						unused0 = 0x1000,
						unused1 = 0x0800,
						BLTPRI = 0x0400,
						DMAEN = 0x0200,
						BPLEN = 0x00100,
						COPEN = 0x0080,
						BLTEN = 0x0040,
						SPREN = 0x0020,
						DSKEN = 0x0010,
						AUD3EN = 0x0008,
						AUD2EN = 0x0004,
						AUD1EN = 0x0002,
						AUD0EN = 0x0001,
			 */
			{
				string hdr =
					"S    B      AAAA\n" +
					"EBB  LDBCBSDUUUU\n" +
					"TBZ  TMPOLPSDDDD\n" +
					"CUE  PALPTRK3210\n" +
					"LSR  REEEEEEEEEE\n" +
					"RYO  INNNNNNNNNN";
				lbCustom.Items.AddRange(hdr.Split('\n'));
				lbCustom.Items.Add("DMACON W:DFF096 R:DFF002");
				lbCustom.Items.Add($"{Convert.ToString(chipRegs.dmacon, 2).PadLeft(16, '0')}");
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
			Machine.LockEmulation();
			var memory = debugger.GetMemory();
			var regs = debugger.GetRegs();
			Machine.UnlockEmulation();

			{
			var mem = new List<Tuple<uint, uint>>();
			long sp = (long)regs.SP;
			long ssp = (long)regs.SSP;
			int cnt = 32;
			while (sp > 0 && cnt-- > 0)
			{
				uint spv = 0xffffffff, sspv = 0xffffffff;
				if (sp >= 0) spv = memory.Read32((uint)sp);
				if (ssp >= 0) sspv = memory.Read32((uint)ssp);
				mem.Add(new Tuple<uint, uint>(spv, sspv));
				sp -= 4;
				ssp -= 4;
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
			Machine.LockEmulation();
			uint pc = debugger.GetRegs().PC;
			Machine.UnlockEmulation();

			int line = disassembly.GetAddressLine(pc);
			if (line == 0) return;

			txtDisassembly.SuspendLayout();
			txtDisassembly.SelectionStart = txtDisassembly.GetFirstCharIndexFromLine(Math.Max(0, line - 5));
			txtDisassembly.ScrollToCaret();
			txtDisassembly.Select(txtDisassembly.GetFirstCharIndexFromLine(line),
				txtDisassembly.GetFirstCharIndexFromLine(line + 1) - txtDisassembly.GetFirstCharIndexFromLine(line));
			txtDisassembly.Invalidate();
			txtDisassembly.ResumeLayout();
			txtDisassembly.Update();
		}

		private void btnStep_Click(object sender, EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.Step);
		}

		private void btnStepOut_Click(object sender, EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.StepOut);
		}

		private void btnStop_Click(object sender, EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.Stopped);
		}

		private void btnGo_Click(object sender, EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.Running);
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			Machine.SetEmulationMode(EmulationMode.Stopped);

			Machine.LockEmulation();
			emulation.Reset();
			Machine.UnlockEmulation();

			SetSelection();
			UpdateDisplay();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			Machine.SetEmulationMode(EmulationMode.Exit);

			UI.UI.IsDirty = false;

			uiUpdateTokenSource.Cancel();
			//uiUpdateTask.Wait(1000);
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			SetSelection();
			UpdateDisplay();
		}

		private void btnStepOver_Click(object sender, EventArgs e)
		{
			Machine.LockEmulation();
			debugger.BreakAtNextPC();
			Machine.SetEmulationMode(EmulationMode.Running, true);
			Machine.UnlockEmulation();
		}

		private void UpdatePowerLight()
		{
			bool power = UI.UI.PowerLight;
			picPower.BackColor = power ? Color.Red : Color.DarkRed;
		}

		private void UpdateDiskLight()
		{
			bool disk = UI.UI.DiskLight;
			picDisk.BackColor = disk ? Color.LightGreen : Color.DarkGreen;
		}

		private void UpdateColours()
		{
			var colours = new uint[256];
			UI.UI.GetColours(colours);

			for (int i = 0; i < colours.Length; i++)
				colours[i] |= 0xff000000;

			colour0.BackColor = Color.FromArgb((int)colours[0]);
			colour1.BackColor = Color.FromArgb((int)colours[1]);
			colour2.BackColor = Color.FromArgb((int)colours[2]);
			colour3.BackColor = Color.FromArgb((int)colours[3]);
			colour4.BackColor = Color.FromArgb((int)colours[4]);
			colour5.BackColor = Color.FromArgb((int)colours[5]);
			colour6.BackColor = Color.FromArgb((int)colours[6]);
			colour7.BackColor = Color.FromArgb((int)colours[7]);
			colour8.BackColor = Color.FromArgb((int)colours[8]);
			colour9.BackColor = Color.FromArgb((int)colours[9]);
			colour10.BackColor = Color.FromArgb((int)colours[10]);
			colour11.BackColor = Color.FromArgb((int)colours[11]);
			colour12.BackColor = Color.FromArgb((int)colours[12]);
			colour13.BackColor = Color.FromArgb((int)colours[13]);
			colour14.BackColor = Color.FromArgb((int)colours[14]);
			colour15.BackColor = Color.FromArgb((int)colours[15]);
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

						Machine.LockEmulation();
						var memory = debugger.GetMemory();
						int gotoLine = memory.AddressToLine(address);
						Machine.UnlockEmulation();

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

		private void btnInsertDisk_Click(object sender, EventArgs e)
		{
			debugger.InsertDisk();
		}

		private void btnRemoveDisk_Click(object sender, EventArgs e)
		{
			debugger.RemoveDisk();
		}

		private void btnCIAInt_Click(object sender, EventArgs e)
		{
			Machine.LockEmulation();
			if (cbCIA.Text == "TIMERA") debugger.CIAInt(ICRB.TIMERA);
			if (cbCIA.Text == "TIMERB") debugger.CIAInt(ICRB.TIMERB);
			if (cbCIA.Text == "TODALARM") debugger.CIAInt(ICRB.TODALARM);
			if (cbCIA.Text == "SERIAL") debugger.CIAInt(ICRB.SERIAL);
			if (cbCIA.Text == "FLAG") debugger.CIAInt(ICRB.FLAG);
			Machine.UnlockEmulation();
		}

		private void btnIRQ_Click(object sender, EventArgs e)
		{
			Machine.LockEmulation();
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
			Machine.UnlockEmulation();
		}

		private void btnINTENA_Click(object sender, EventArgs e)
		{
			Machine.LockEmulation();
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
			Machine.UnlockEmulation();
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
				pc = disassembly.GetLineAddress(line);
				logger.LogTrace($"PC {pc:X8}");
			}

			if (e.ClickedItem == toolStripBreakpoint)
			{
				logger.LogTrace($"BP {pc:X8}");
				Machine.LockEmulation();
				debugger.ToggleBreakpoint(pc);
				Machine.UnlockEmulation();
			}
			else if (e.ClickedItem == toolStripSkip)
			{
				logger.LogTrace($"SKIP {pc:X8}");
				Machine.LockEmulation();
				debugger.SetPC(pc);
				Machine.UnlockEmulation();
			}
			else if (e.ClickedItem == toolStripGoto)
			{
				var gotoForm = new GoTo();
				var res = gotoForm.ShowDialog();
				if (res == DialogResult.OK)
				{
					uint address = gotoForm.GotoLocation;
					int gotoLine = disassembly.GetAddressLine(address);
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

			UpdateDisassembly();

			SetSelection();
			UpdateDisplay();
		}

		private void UpdateExecBase()
		{
			if (cbTypes.SelectedIndex != 0 && addressFollowBox.SelectedIndex != 0)
			{
				string typeName = (string)cbTypes.SelectedItem;

				Machine.LockEmulation();

				var regs = debugger.GetRegs();

				uint address = ValueFromRegName(regs, (string)addressFollowBox.SelectedItem);

				var assembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(x => x.GetName().Name == "RunAmiga.Disassembler");
				if (assembly != null)
				{
					var type = assembly.GetTypes().SingleOrDefault(x => x.Name == typeName);
					if (type != null)
					{
						object tp = Activator.CreateInstance(type);
						if (tp != null)
						{
							Machine.LockEmulation();
							txtExecBase.Text = ObjectMapper.MapObject(tp, address);

						}
					}
				}

				Machine.UnlockEmulation();
			}
		}

		private void cbTypes_SelectionChangeCommitted(object sender, EventArgs e)
		{
			UpdateExecBase();
		}

		private void btnDumpTrace_Click(object sender, EventArgs e)
		{
			Machine.LockEmulation();
			debugger.WriteTrace();
			Machine.UnlockEmulation();
		}

		private void btnIDEACK_Click(object sender, EventArgs e)
		{
			debugger.IDEACK();
		}

		private void btnChange_Click(object sender, EventArgs e)
		{
			using (var ofd = new OpenFileDialog())
			{
				if (ofd.ShowDialog() == DialogResult.OK)
					debugger.ChangeDisk(ofd.FileName);
			}
		}
	}
}
