using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RunAmiga.Core;
using RunAmiga.Core.Interface;
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
		private readonly IDebugger debugger;

		private readonly IDisassembly disassembly;
		private readonly ILogger logger;

		private EmulationSettings settings;

		public RunAmiga(IEmulation emulation)
		{
			if (this.Handle == IntPtr.Zero)
				throw new ApplicationException("RG");

			this.emulation = emulation;
			InitializeComponent();

			addressFollowBox.SelectedIndex = 0;
			cbTypes.SelectedIndex = 0;

			logger = ServiceProviderFactory.ServiceProvider.GetRequiredService<ILogger<RunAmiga>>();
			settings = ServiceProviderFactory.ServiceProvider.GetRequiredService<IOptions<EmulationSettings>>().Value;

			this.debugger = emulation.GetDebugger();
			this.disassembly = debugger.GetDisassembly();

			UpdateDisplay();

			Machine.SetEmulationMode(EmulationMode.Stopped);
			emulation.Start();
		}

		private CancellationTokenSource uiUpdateTokenSource;
		private Task uiUpdateTask;

		public void Init()
		{
			UpdateDisassembly();
			UpdateDisplay();

			uiUpdateTokenSource = new CancellationTokenSource();

			uiUpdateTask = new Task(() =>
			{
				while (!uiUpdateTokenSource.IsCancellationRequested)
				{
					this.Invoke((Action)UpdatePowerLight);

					if (UI.UI.IsDirty)
					{
						this.Invoke((Action)delegate()
						{
							SetSelection();
							UpdateDisplay();
						});
						UI.UI.IsDirty = false;
					}

					Task.Delay(500).Wait(uiUpdateTokenSource.Token);
				}
			}, uiUpdateTokenSource.Token);
			uiUpdateTask.Start();
		}

		private void UpdateDisassembly()
		{
			Machine.LockEmulation();

			//string dmp;
			//dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>> {new Tuple<uint, uint>(0xfe88d6, 0xfe8e18 - 0xfe88d6 + 1)}, new DisassemblyOptions{IncludeBytes = false, CommentPad = true});
			//File.WriteAllText("boot_disassembly.txt", dmp);

			//dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>>
			//{
			//	new Tuple<uint, uint>(0xFEa734, 0xFEB460 - 0xFEa734 + 1),
			//	//new Tuple<uint, uint>(0xFE9930, 0xFEB460 - 0xFE9930 + 1),
			//}, new DisassemblyOptions{ IncludeBytes = false, CommentPad = true});
			//File.WriteAllText("trackdisk_disassembly.txt", dmp);

			//dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>>
			//{
			//	new Tuple<uint, uint>(0xfe489a , 0xFE889E  - 0xfe489a + 1),
			//},
			//new List<uint>(), 
			//new DisassemblyOptions { IncludeBytes = false, CommentPad = true });
			//File.WriteAllText("keymap.resource_disassembly.txt", dmp);

			//dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>>
			//	{
			//		new Tuple<uint, uint>(0xFE90EC, 0xfe98e4 - 0xFE90EC + 1)
			//	}, new List<uint>(),
			//	new DisassemblyOptions {IncludeBytes = false, CommentPad = true});
			//File.WriteAllText("timer.device_disassembly.txt", dmp);

			//dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>>
			//	{
			//		new Tuple<uint, uint>(0x00F8574C, 0x00F8618C - 0x00F8574C + 1)
			//	}, new List<uint>(),
			//	new DisassemblyOptions { IncludeBytes = false, CommentPad = true });
			//File.WriteAllText("battclock.resource_disassembly.txt", dmp);

			//dmp = debugger.DisassembleTxt(new List<Tuple<uint, uint>>
			//	{
			//		new Tuple<uint, uint>(0x00FC450C, 0x00FC4794 - 0x00FC450C + 1)
			//	}, new List<uint>(),
			//	new DisassemblyOptions { IncludeBytes = false, CommentPad = true });
			//File.WriteAllText("cia.resource_disassembly.txt", dmp);

			var ranges = new List<Tuple<uint, uint>>
			{
				new Tuple<uint, uint>(0x000000, 0x400),
				//new Tuple<uint, uint> (0xc00000, 0xa000),
				//new Tuple<uint, uint> (0xf80000, 0x40000),
				new Tuple<uint, uint>(0xfc0000, 0x40000),
			};
			if (settings.KickStart == "3.1" || settings.KickStart == "2.04")
				ranges.Add(new Tuple<uint, uint>(0xf80000, 0x40000));

			var disasm = disassembly.DisassembleTxt(
				ranges,
				new List<uint>
				{
					0xC0937b, 0xfe490c, 0xfe4916, 0xfe4f70, 0xfe5388, 0xFE53E8, 0xFE5478, 0xFE57D0, 0xFE5BC2,
					0xFE5D4C, 0xFE6994, 0xfe6dec, 0xFE6332, 0xfe66d8, 0xFE93C2, 0xFE571C, 0xFC5170, 0xFE5A04, 0xfe61d0, 0x00FE6FD4,

					0xFE43CC, 0xFE4588, 0xFE46CC, 0xfe42ee, 0xFC3A40, 0xfc43f4, 0xfc4408, 0xfc441c, 0xfc4474, 0xfc44a4, 0xfc44d0,0xFE62D4

				},
				new DisassemblyOptions {IncludeBytes = true, IncludeBreakpoints = true, IncludeComments = true});

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

		private void UpdateRegs()
		{
			Machine.LockEmulation();
			var regs = debugger.GetRegs();
			var memory = debugger.GetMemory();
			Machine.UnlockEmulation();

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

			lbRegisters.Items.Clear();
			lbRegisters.Items.AddRange(regs.Items().Cast<object>().ToArray());
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

		private void SetSelection()
		{
			uint pc = debugger.GetRegs().PC;
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

			SetSelection();
			UpdateDisplay();
		}

		private void btnGo_Click(object sender, EventArgs e)
		{
			txtDisassembly.DeselectAll();
			Machine.SetEmulationMode(EmulationMode.Running);
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			Machine.SetEmulationMode(EmulationMode.Stopped);
			emulation.Reset();

			SetSelection();
			UpdateDisplay();
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			UI.UI.IsDirty = false;

			uiUpdateTokenSource.Cancel();
			uiUpdateTask.Wait(1000);

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
			if (cbCIA.Text == "TIMERA") debugger.CIAInt(ICRB.TIMERA);
			if (cbCIA.Text == "TIMERB") debugger.CIAInt(ICRB.TIMERB);
			if (cbCIA.Text == "TODALARM") debugger.CIAInt(ICRB.TODALARM);
			if (cbCIA.Text == "SERIAL") debugger.CIAInt(ICRB.SERIAL);
			if (cbCIA.Text == "FLAG") debugger.CIAInt(ICRB.FLAG);
		}

		private void btnIRQ_Click(object sender, EventArgs e)
		{
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
							Machine.UnlockEmulation();
						}
					}
				}
			}
		}

		private void cbTypes_SelectionChangeCommitted(object sender, EventArgs e)
		{
			UpdateExecBase();
		}
	}
}
