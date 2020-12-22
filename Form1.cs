using RunAmiga.Types;
using System;
using System.Diagnostics;
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

			UpdateRegisters();

			var disasm = cpu.DisassembleTxt(0xfc0000);
			txtDisassembly.Text = disasm;

			SetSelection();
			Machine.SetEmulationMode(EmulationMode.Stopped);
			machine.Start();
		}

		private void UpdateRegisters()
		{
			UpdateRegs();
			UpdateMem();
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

			SetSelection();
			UpdateRegisters();
		}

		private void btnStop_Click(object sender, System.EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.Stopped);

			SetSelection();
			UpdateRegisters();
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
			UpdateRegisters();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			Machine.SetEmulationMode(EmulationMode.Exit);
		}
	}
}
