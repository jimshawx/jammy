using RunAmiga.Types;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using RunAmiga.Dialogs;
using RunAmiga.Options;

namespace RunAmiga
{
	public partial class RunAmiga : Form
	{
		private readonly Machine machine;
		private readonly Debugger debugger;

		public RunAmiga(Machine machine)
		{
			InitializeComponent();

			addressFollowBox.SelectedIndex = 0;

			this.machine = machine;
			this.debugger = machine.GetDebugger();

			UpdateDisplay();

			Machine.SetEmulationMode(EmulationMode.Stopped);
			machine.Start();
		}

		Thread uiUpdateThread, uiUpdateThread2;
		public void Init()
		{
			UpdateDisassembly();
			UpdateDisplay();

			while (this.Handle == IntPtr.Zero)
				Application.DoEvents();

			uiUpdateThread = new Thread(UIUpdateThread);
			uiUpdateThread.Start();

			uiUpdateThread2 = new Thread(UIUpdateThread2);
			uiUpdateThread2.Start();
		}

		private void UpdateDisassembly()
		{
			Machine.LockEmulation();

			string dmp;
			//dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>> {new Tuple<uint, uint>(0xfe88d6, 0xfe8e18 - 0xfe88d6 + 1)}, new DisassemblyOptions{IncludeBytes = false, CommentPad = true});
			//File.WriteAllText("boot_disassembly.txt", dmp);

			//dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>>
			//{
			//	new Tuple<uint, uint>(0xFEa734, 0xFEB460 - 0xFEa734 + 1),
			//	//new Tuple<uint, uint>(0xFE9930, 0xFEB460 - 0xFE9930 + 1),
			//}, new DisassemblyOptions{ IncludeBytes = false, CommentPad = true});
			//File.WriteAllText("trackdisk_disassembly.txt", dmp);

			dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>>
			{
				new Tuple<uint, uint>(0xfe489a , 0xFE889E  - 0xfe489a + 1),
			}, new DisassemblyOptions { IncludeBytes = false, CommentPad = true });
			File.WriteAllText("keymap.resource_disassembly.txt", dmp);

			var disasm = debugger.DisassembleTxt(
					new List<Tuple<uint, uint>>
					{
						new Tuple<uint, uint> (0x000000, 0x400),
						new Tuple<uint, uint> (0xc00000, 0x1000),
						new Tuple<uint, uint> (0xf80000, 0x40000),
						new Tuple<uint, uint> (0xfc0000, 0x0900),
						new Tuple<uint, uint> (0xfc0900, 0x4000),
						new Tuple<uint, uint> (0xfc4900, 0x1f000),
						new Tuple<uint, uint> (0xfe52a4, 0x0144),
						new Tuple<uint, uint> (0xfe53e8, 0x6000)
					}, new DisassemblyOptions{ IncludeBytes = true, IncludeBreakpoints = true, IncludeComments = true});

			Machine.UnlockEmulation();
			txtDisassembly.Text = disasm;
		}

		private void UpdateDisplay()
		{
			UpdateRegs();
			UpdateMem();
			UpdatePowerLight();
			UpdateDiskLight();
			UpdateColours();
			UpdateExecBase();
		}

		private void UpdateExecBase()
		{
			Machine.LockEmulation();
			var execBaseTxt = debugger.UpdateExecBase();
			Machine.UnlockEmulation();
			this.txtExecBase.Text = execBaseTxt;
		}

		private void UpdateRegs()
		{
			Machine.LockEmulation();
			var regs = debugger.GetRegs();
			Machine.UnlockEmulation();

			lbRegisters.Items.Clear();
			lbRegisters.Items.AddRange(regs.Items().ToArray());
		}

		private void UpdateMem()
		{
			Machine.LockEmulation();
			var memory = debugger.GetMemory();
			var regs = debugger.GetRegs();
			Machine.UnlockEmulation();

			txtMemory.Text = memory.ToString();

			if (addressFollowBox.SelectedIndex != 0)
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
				var line = memory.AddressToLine(address);
				if (line != 0)
				{
					txtMemory.SelectionStart = txtMemory.GetFirstCharIndexFromLine(line);
					txtMemory.ScrollToCaret();
				} 
			}

		}

		private void SetSelection()
		{
			uint pc = debugger.GetRegs().PC;
			int line = debugger.GetAddressLine(pc);
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

		private void btnStep_Click(object sender, System.EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.Step);

			Machine.WaitEmulationMode(EmulationMode.Stopped);

			SetSelection();
			UpdateDisplay();
		}

		private void btnStop_Click(object sender, System.EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.Stopped);

			SetSelection();
			UpdateDisplay();
		}

		private void btnGo_Click(object sender, System.EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.Running);
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			Machine.SetEmulationMode(EmulationMode.Stopped);
			machine.Reset();

			SetSelection();
			UpdateDisplay();
		}

		bool exiting = false;
		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			exiting = true;
			while (exiting)
			{
				Thread.Yield();
				Application.DoEvents();
			}
			Machine.SetEmulationMode(EmulationMode.Exit);
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

			Machine.WaitEmulationMode(EmulationMode.Stopped);

			SetSelection();
			UpdateDisplay();
		}

		private void UIUpdateThread2(object o)
		{
			while (!exiting)
			{
				Application.DoEvents();
				Thread.Sleep(500);
			}
		}

		private void UIUpdateThread(object o)
		{
			while (!exiting)
			{
				this.Invoke((Action)delegate () { UpdatePowerLight(); });

				if (UI.IsDirty)
				{
					this.Invoke((Action)delegate () { SetSelection(); UpdateDisplay(); });
					UI.IsDirty = false;
				}

				Thread.Sleep(500);
			}
		}

		private void UpdatePowerLight()
		{
			bool power = UI.PowerLight;
			picPower.BackColor = power ? Color.Red : Color.DarkRed;
		}

		private void UpdateDiskLight()
		{
			bool disk = UI.DiskLight;
			picDisk.BackColor = disk ? Color.LightGreen : Color.DarkGreen;
		}

		private void UpdateColours()
		{
			var colours = new uint[256];
			UI.GetColours(colours);

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

		private void menuDisassembly_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (!(sender is ContextMenuStrip))
				return;

			var ctx = (ContextMenuStrip)sender;

			uint pc;
			{
				var mouse = this.PointToClient(ctx.Location);
				Logger.WriteLine($"ctx {mouse.X} {mouse.Y}");
				int c = txtDisassembly.GetCharIndexFromPosition(mouse);
				Logger.WriteLine($"char {c}");
				int line = txtDisassembly.GetLineFromCharIndex(c) - 1;
				Logger.WriteLine($"line {line}");
				pc = debugger.GetLineAddress(line);
				Logger.WriteLine($"PC {pc:X8}");
			}

			if (e.ClickedItem == toolStripBreakpoint)
			{
				Logger.WriteLine($"BP {pc:X8}");
				Machine.LockEmulation();
				debugger.ToggleBreakpoint(pc);
				Machine.UnlockEmulation();
			}
			else if (e.ClickedItem == toolStripSkip)
			{
				Logger.WriteLine($"SKIP {pc:X8}");
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
					int gotoLine = debugger.GetAddressLine(address);
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
	}
}
