using RunAmiga.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace RunAmiga
{
	public partial class Form1 : Form
	{
		private readonly CPU cpu;
		private readonly Machine machine;

		public Form1(Machine machine)
		{
			InitializeComponent();

			this.machine = machine;

			machine.Init();

			cpu = machine.GetCPU();

			UpdateDisplay();

			var disasm =
				cpu.DisassembleTxt(
					new List<Tuple<uint, uint>>
					{
						new Tuple<uint, uint> (0x000000, 0x4000),
						new Tuple<uint, uint> (0xc00000, 0x4000),
						new Tuple<uint, uint> (0xfc0000, 0x4000)
					});
			txtDisassembly.Text = disasm;

			UpdateDisplay();

			Machine.SetEmulationMode(EmulationMode.Stopped);
			machine.Start();
		}

		Thread uiUpdateThread;
		public void Init()
		{
			uiUpdateThread = new Thread(UIUpdateThread);
			uiUpdateThread.Start();
		}

		private void UpdateDisplay()
		{
			UpdateRegs();
			UpdateMem();
			UpdatePowerLight();
			UpdateDiskLight();
			UpdateColours();
		}

		private void UpdateRegs()
		{
			Machine.LockEmulation();
			var regs = cpu.GetRegs();
			Machine.UnlockEmulation();

			lbRegisters.Items.Clear();
			lbRegisters.Items.AddRange(regs.Items().ToArray());
		}

		private void UpdateMem()
		{
			Machine.LockEmulation();
			var memory = cpu.GetMemory();
			Machine.UnlockEmulation();

			txtMemory.Text = memory.ToString();
		}

		private void SetSelection()
		{
			uint pc = cpu.GetRegs().PC;
			int line = cpu.GetAddressLine(pc);
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
			machine.GetCPU().Reset();

			SetSelection();
			UpdateDisplay();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
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
			machine.GetCPU().BreakAtNextPC();
			Machine.SetEmulationMode(EmulationMode.Running, true);
			Machine.UnlockEmulation();

			Machine.WaitEmulationMode(EmulationMode.Stopped);

			SetSelection();
			UpdateDisplay();
		}

		private void UIUpdateThread(object o)
		{
			for (; ; )
			{ 
				this.Invoke((Action)delegate() { UpdatePowerLight(); } );
				Thread.Sleep(100);
			}
		}

		private void UpdatePowerLight()
		{
			bool power = UI.PowerLight;
			picPower.BackColor = power?Color.Red:Color.DarkRed;
		}

		private void UpdateDiskLight()
		{
			bool disk = UI.DiskLight;
			picDisk.BackColor = disk? Color.Green : Color.DarkGreen;
		}

		private void UpdateColours()
		{
			var colours = new uint[256];
			UI.GetColours(colours);
		}
	}
}
