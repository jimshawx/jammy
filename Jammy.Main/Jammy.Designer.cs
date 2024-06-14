
/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main
{
	partial class Jammy
	{
		/// <summary>
		///  Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		///  Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Jammy));
			lbRegisters = new System.Windows.Forms.ListBox();
			txtDisassembly = new System.Windows.Forms.RichTextBox();
			menuDisassembly = new System.Windows.Forms.ContextMenuStrip(components);
			toolStripBreakpoint = new System.Windows.Forms.ToolStripMenuItem();
			toolStripSkip = new System.Windows.Forms.ToolStripMenuItem();
			toolStripGoto = new System.Windows.Forms.ToolStripMenuItem();
			btnStep = new System.Windows.Forms.Button();
			btnStop = new System.Windows.Forms.Button();
			btnGo = new System.Windows.Forms.Button();
			btnReset = new System.Windows.Forms.Button();
			txtMemory = new System.Windows.Forms.RichTextBox();
			menuMemory = new System.Windows.Forms.ContextMenuStrip(components);
			menuMemoryGotoItem = new System.Windows.Forms.ToolStripMenuItem();
			menuMemoryFindItem = new System.Windows.Forms.ToolStripMenuItem();
			splitContainer1 = new System.Windows.Forms.SplitContainer();
			btnRefresh = new System.Windows.Forms.Button();
			btnStepOver = new System.Windows.Forms.Button();
			picPower = new System.Windows.Forms.PictureBox();
			picDisk = new System.Windows.Forms.PictureBox();
			btnDisassemble = new System.Windows.Forms.Button();
			radioButton10 = new System.Windows.Forms.RadioButton();
			radioButton11 = new System.Windows.Forms.RadioButton();
			radioButton12 = new System.Windows.Forms.RadioButton();
			radioButton13 = new System.Windows.Forms.RadioButton();
			radioButton14 = new System.Windows.Forms.RadioButton();
			radioButton15 = new System.Windows.Forms.RadioButton();
			radioButton16 = new System.Windows.Forms.RadioButton();
			radioButton17 = new System.Windows.Forms.RadioButton();
			addressFollowBox = new System.Windows.Forms.ComboBox();
			txtExecBase = new System.Windows.Forms.RichTextBox();
			btnInsertDisk = new System.Windows.Forms.Button();
			btnRemoveDisk = new System.Windows.Forms.Button();
			btnCIAInt = new System.Windows.Forms.Button();
			btnIRQ = new System.Windows.Forms.Button();
			cbIRQ = new System.Windows.Forms.ComboBox();
			cbCIA = new System.Windows.Forms.ComboBox();
			cbTypes = new System.Windows.Forms.ComboBox();
			lbCallStack = new System.Windows.Forms.ListBox();
			btnStepOut = new System.Windows.Forms.Button();
			btnINTENA = new System.Windows.Forms.Button();
			lbCustom = new System.Windows.Forms.ListBox();
			btnDumpTrace = new System.Windows.Forms.Button();
			btnIDEACK = new System.Windows.Forms.Button();
			btnChange = new System.Windows.Forms.Button();
			radioDF0 = new System.Windows.Forms.RadioButton();
			radioDF1 = new System.Windows.Forms.RadioButton();
			radioDF2 = new System.Windows.Forms.RadioButton();
			radioDF3 = new System.Windows.Forms.RadioButton();
			btnGfxScan = new System.Windows.Forms.Button();
			btnClearBBUSY = new System.Windows.Forms.Button();
			menuDisassembly.SuspendLayout();
			menuMemory.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)picPower).BeginInit();
			((System.ComponentModel.ISupportInitialize)picDisk).BeginInit();
			SuspendLayout();
			// 
			// lbRegisters
			// 
			lbRegisters.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			lbRegisters.Font = new System.Drawing.Font("Consolas", 8.25F);
			lbRegisters.FormattingEnabled = true;
			lbRegisters.IntegralHeight = false;
			lbRegisters.ItemHeight = 13;
			lbRegisters.Location = new System.Drawing.Point(1038, 12);
			lbRegisters.Name = "lbRegisters";
			lbRegisters.SelectionMode = System.Windows.Forms.SelectionMode.None;
			lbRegisters.Size = new System.Drawing.Size(175, 160);
			lbRegisters.TabIndex = 0;
			// 
			// txtDisassembly
			// 
			txtDisassembly.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			txtDisassembly.ContextMenuStrip = menuDisassembly;
			txtDisassembly.DetectUrls = false;
			txtDisassembly.Font = new System.Drawing.Font("Consolas", 8.25F);
			txtDisassembly.HideSelection = false;
			txtDisassembly.Location = new System.Drawing.Point(3, 3);
			txtDisassembly.Name = "txtDisassembly";
			txtDisassembly.ReadOnly = true;
			txtDisassembly.Size = new System.Drawing.Size(717, 242);
			txtDisassembly.TabIndex = 1;
			txtDisassembly.Text = "";
			txtDisassembly.WordWrap = false;
			// 
			// menuDisassembly
			// 
			menuDisassembly.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripBreakpoint, toolStripSkip, toolStripGoto });
			menuDisassembly.Name = "menuDisassembly";
			menuDisassembly.Size = new System.Drawing.Size(132, 70);
			menuDisassembly.ItemClicked += menuDisassembly_ItemClicked;
			// 
			// toolStripBreakpoint
			// 
			toolStripBreakpoint.Name = "toolStripBreakpoint";
			toolStripBreakpoint.Size = new System.Drawing.Size(131, 22);
			toolStripBreakpoint.Text = "Breakpoint";
			// 
			// toolStripSkip
			// 
			toolStripSkip.Name = "toolStripSkip";
			toolStripSkip.Size = new System.Drawing.Size(131, 22);
			toolStripSkip.Text = "Skip";
			// 
			// toolStripGoto
			// 
			toolStripGoto.Name = "toolStripGoto";
			toolStripGoto.Size = new System.Drawing.Size(131, 22);
			toolStripGoto.Text = "Go To...";
			// 
			// btnStep
			// 
			btnStep.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStep.Location = new System.Drawing.Point(1038, 179);
			btnStep.Name = "btnStep";
			btnStep.Size = new System.Drawing.Size(71, 23);
			btnStep.TabIndex = 2;
			btnStep.Text = "Step";
			btnStep.UseVisualStyleBackColor = true;
			btnStep.Click += btnStep_Click;
			// 
			// btnStop
			// 
			btnStop.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStop.Location = new System.Drawing.Point(1038, 208);
			btnStop.Name = "btnStop";
			btnStop.Size = new System.Drawing.Size(71, 23);
			btnStop.TabIndex = 3;
			btnStop.Text = "Stop";
			btnStop.UseVisualStyleBackColor = true;
			btnStop.Click += btnStop_Click;
			// 
			// btnGo
			// 
			btnGo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnGo.Location = new System.Drawing.Point(1038, 238);
			btnGo.Name = "btnGo";
			btnGo.Size = new System.Drawing.Size(71, 23);
			btnGo.TabIndex = 4;
			btnGo.Text = "Go";
			btnGo.UseVisualStyleBackColor = true;
			btnGo.Click += btnGo_Click;
			// 
			// btnReset
			// 
			btnReset.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnReset.Location = new System.Drawing.Point(1038, 268);
			btnReset.Name = "btnReset";
			btnReset.Size = new System.Drawing.Size(71, 23);
			btnReset.TabIndex = 5;
			btnReset.Text = "Reset";
			btnReset.UseVisualStyleBackColor = true;
			btnReset.Click += btnReset_Click;
			// 
			// txtMemory
			// 
			txtMemory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
			txtMemory.ContextMenuStrip = menuMemory;
			txtMemory.DetectUrls = false;
			txtMemory.Font = new System.Drawing.Font("Consolas", 8.25F);
			txtMemory.Location = new System.Drawing.Point(3, 3);
			txtMemory.Name = "txtMemory";
			txtMemory.ReadOnly = true;
			txtMemory.Size = new System.Drawing.Size(717, 323);
			txtMemory.TabIndex = 6;
			txtMemory.Text = "";
			txtMemory.WordWrap = false;
			// 
			// menuMemory
			// 
			menuMemory.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuMemoryGotoItem, menuMemoryFindItem });
			menuMemory.Name = "menuMemory";
			menuMemory.Size = new System.Drawing.Size(114, 48);
			menuMemory.ItemClicked += menuMemory_ItemClicked;
			// 
			// menuMemoryGotoItem
			// 
			menuMemoryGotoItem.Name = "menuMemoryGotoItem";
			menuMemoryGotoItem.Size = new System.Drawing.Size(113, 22);
			menuMemoryGotoItem.Text = "Go To...";
			// 
			// menuMemoryFindItem
			// 
			menuMemoryFindItem.Name = "menuMemoryFindItem";
			menuMemoryFindItem.Size = new System.Drawing.Size(113, 22);
			menuMemoryFindItem.Text = "Find...";
			// 
			// splitContainer1
			// 
			splitContainer1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
			splitContainer1.Location = new System.Drawing.Point(12, 12);
			splitContainer1.Name = "splitContainer1";
			splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			splitContainer1.Panel1.Controls.Add(txtDisassembly);
			// 
			// splitContainer1.Panel2
			// 
			splitContainer1.Panel2.Controls.Add(txtMemory);
			splitContainer1.Size = new System.Drawing.Size(723, 581);
			splitContainer1.SplitterDistance = 248;
			splitContainer1.TabIndex = 7;
			// 
			// btnRefresh
			// 
			btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnRefresh.Location = new System.Drawing.Point(1039, 332);
			btnRefresh.Name = "btnRefresh";
			btnRefresh.Size = new System.Drawing.Size(71, 23);
			btnRefresh.TabIndex = 8;
			btnRefresh.Text = "Refresh";
			btnRefresh.UseVisualStyleBackColor = true;
			btnRefresh.Click += btnRefresh_Click;
			// 
			// btnStepOver
			// 
			btnStepOver.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStepOver.Location = new System.Drawing.Point(1120, 179);
			btnStepOver.Name = "btnStepOver";
			btnStepOver.Size = new System.Drawing.Size(71, 23);
			btnStepOver.TabIndex = 9;
			btnStepOver.Text = "Step Over";
			btnStepOver.UseVisualStyleBackColor = true;
			btnStepOver.Click += btnStepOver_Click;
			// 
			// picPower
			// 
			picPower.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			picPower.Location = new System.Drawing.Point(1144, 250);
			picPower.Name = "picPower";
			picPower.Size = new System.Drawing.Size(47, 10);
			picPower.TabIndex = 10;
			picPower.TabStop = false;
			// 
			// picDisk
			// 
			picDisk.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			picDisk.Location = new System.Drawing.Point(1144, 268);
			picDisk.Name = "picDisk";
			picDisk.Size = new System.Drawing.Size(47, 10);
			picDisk.TabIndex = 11;
			picDisk.TabStop = false;
			// 
			// btnDisassemble
			// 
			btnDisassemble.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnDisassemble.Location = new System.Drawing.Point(1120, 332);
			btnDisassemble.Name = "btnDisassemble";
			btnDisassemble.Size = new System.Drawing.Size(93, 23);
			btnDisassemble.TabIndex = 12;
			btnDisassemble.Text = "Disassemble";
			btnDisassemble.UseVisualStyleBackColor = true;
			btnDisassemble.Click += btnDisassemble_Click;
			// 
			// radioButton10
			// 
			radioButton10.AutoSize = true;
			radioButton10.Location = new System.Drawing.Point(404, 280);
			radioButton10.Name = "radioButton10";
			radioButton10.Size = new System.Drawing.Size(100, 19);
			radioButton10.TabIndex = 27;
			radioButton10.TabStop = true;
			radioButton10.Text = "radioButton10";
			radioButton10.UseVisualStyleBackColor = true;
			// 
			// radioButton11
			// 
			radioButton11.AutoSize = true;
			radioButton11.Location = new System.Drawing.Point(412, 288);
			radioButton11.Name = "radioButton11";
			radioButton11.Size = new System.Drawing.Size(100, 19);
			radioButton11.TabIndex = 28;
			radioButton11.TabStop = true;
			radioButton11.Text = "radioButton11";
			radioButton11.UseVisualStyleBackColor = true;
			// 
			// radioButton12
			// 
			radioButton12.AutoSize = true;
			radioButton12.Location = new System.Drawing.Point(420, 296);
			radioButton12.Name = "radioButton12";
			radioButton12.Size = new System.Drawing.Size(100, 19);
			radioButton12.TabIndex = 29;
			radioButton12.TabStop = true;
			radioButton12.Text = "radioButton12";
			radioButton12.UseVisualStyleBackColor = true;
			// 
			// radioButton13
			// 
			radioButton13.AutoSize = true;
			radioButton13.Location = new System.Drawing.Point(428, 304);
			radioButton13.Name = "radioButton13";
			radioButton13.Size = new System.Drawing.Size(100, 19);
			radioButton13.TabIndex = 30;
			radioButton13.TabStop = true;
			radioButton13.Text = "radioButton13";
			radioButton13.UseVisualStyleBackColor = true;
			// 
			// radioButton14
			// 
			radioButton14.AutoSize = true;
			radioButton14.Location = new System.Drawing.Point(436, 312);
			radioButton14.Name = "radioButton14";
			radioButton14.Size = new System.Drawing.Size(100, 19);
			radioButton14.TabIndex = 31;
			radioButton14.TabStop = true;
			radioButton14.Text = "radioButton14";
			radioButton14.UseVisualStyleBackColor = true;
			// 
			// radioButton15
			// 
			radioButton15.AutoSize = true;
			radioButton15.Location = new System.Drawing.Point(444, 320);
			radioButton15.Name = "radioButton15";
			radioButton15.Size = new System.Drawing.Size(100, 19);
			radioButton15.TabIndex = 32;
			radioButton15.TabStop = true;
			radioButton15.Text = "radioButton15";
			radioButton15.UseVisualStyleBackColor = true;
			// 
			// radioButton16
			// 
			radioButton16.AutoSize = true;
			radioButton16.Location = new System.Drawing.Point(452, 328);
			radioButton16.Name = "radioButton16";
			radioButton16.Size = new System.Drawing.Size(100, 19);
			radioButton16.TabIndex = 33;
			radioButton16.TabStop = true;
			radioButton16.Text = "radioButton16";
			radioButton16.UseVisualStyleBackColor = true;
			// 
			// radioButton17
			// 
			radioButton17.AutoSize = true;
			radioButton17.Location = new System.Drawing.Point(460, 336);
			radioButton17.Name = "radioButton17";
			radioButton17.Size = new System.Drawing.Size(100, 19);
			radioButton17.TabIndex = 34;
			radioButton17.TabStop = true;
			radioButton17.Text = "radioButton17";
			radioButton17.UseVisualStyleBackColor = true;
			// 
			// addressFollowBox
			// 
			addressFollowBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			addressFollowBox.FormattingEnabled = true;
			addressFollowBox.Items.AddRange(new object[] { "(None)", "A0", "A1", "A2", "A3", "A4", "A5", "A6", "SP", "SSP", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "PC" });
			addressFollowBox.Location = new System.Drawing.Point(1039, 362);
			addressFollowBox.Name = "addressFollowBox";
			addressFollowBox.Size = new System.Drawing.Size(117, 23);
			addressFollowBox.TabIndex = 25;
			addressFollowBox.SelectionChangeCommitted += addressFollowBox_SelectionChangeCommitted;
			// 
			// txtExecBase
			// 
			txtExecBase.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			txtExecBase.DetectUrls = false;
			txtExecBase.Font = new System.Drawing.Font("Consolas", 8.25F);
			txtExecBase.Location = new System.Drawing.Point(742, 39);
			txtExecBase.Name = "txtExecBase";
			txtExecBase.ReadOnly = true;
			txtExecBase.Size = new System.Drawing.Size(290, 551);
			txtExecBase.TabIndex = 26;
			txtExecBase.Text = "";
			txtExecBase.WordWrap = false;
			// 
			// btnInsertDisk
			// 
			btnInsertDisk.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnInsertDisk.Location = new System.Drawing.Point(1039, 392);
			btnInsertDisk.Name = "btnInsertDisk";
			btnInsertDisk.Size = new System.Drawing.Size(72, 23);
			btnInsertDisk.TabIndex = 27;
			btnInsertDisk.Text = "Insert Disk";
			btnInsertDisk.UseVisualStyleBackColor = true;
			btnInsertDisk.Click += btnInsertDisk_Click;
			// 
			// btnRemoveDisk
			// 
			btnRemoveDisk.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnRemoveDisk.Location = new System.Drawing.Point(1120, 392);
			btnRemoveDisk.Name = "btnRemoveDisk";
			btnRemoveDisk.Size = new System.Drawing.Size(71, 23);
			btnRemoveDisk.TabIndex = 28;
			btnRemoveDisk.Text = "Remove Disk";
			btnRemoveDisk.UseVisualStyleBackColor = true;
			btnRemoveDisk.Click += btnRemoveDisk_Click;
			// 
			// btnCIAInt
			// 
			btnCIAInt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnCIAInt.Location = new System.Drawing.Point(1039, 451);
			btnCIAInt.Name = "btnCIAInt";
			btnCIAInt.Size = new System.Drawing.Size(72, 23);
			btnCIAInt.TabIndex = 29;
			btnCIAInt.Text = "CIA Int";
			btnCIAInt.UseVisualStyleBackColor = true;
			btnCIAInt.Click += btnCIAInt_Click;
			// 
			// btnIRQ
			// 
			btnIRQ.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnIRQ.Location = new System.Drawing.Point(1039, 480);
			btnIRQ.Name = "btnIRQ";
			btnIRQ.Size = new System.Drawing.Size(72, 23);
			btnIRQ.TabIndex = 31;
			btnIRQ.Text = "IRQ";
			btnIRQ.UseVisualStyleBackColor = true;
			btnIRQ.Click += btnIRQ_Click;
			// 
			// cbIRQ
			// 
			cbIRQ.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			cbIRQ.FormattingEnabled = true;
			cbIRQ.Items.AddRange(new object[] { "EXTER", "DSKSYNC", "AUD0", "AUD1", "AUD2", "AUD3", "BLIT", "VERTB", "COPPER", "PORTS", "DSKBLK", "SOFTINT" });
			cbIRQ.Location = new System.Drawing.Point(1120, 480);
			cbIRQ.Name = "cbIRQ";
			cbIRQ.Size = new System.Drawing.Size(71, 23);
			cbIRQ.TabIndex = 32;
			cbIRQ.Text = "BLIT";
			// 
			// cbCIA
			// 
			cbCIA.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			cbCIA.FormattingEnabled = true;
			cbCIA.Items.AddRange(new object[] { "TIMERA", "TIMERB", "TODALARM", "SERIAL", "FLAG" });
			cbCIA.Location = new System.Drawing.Point(1120, 451);
			cbCIA.Name = "cbCIA";
			cbCIA.Size = new System.Drawing.Size(71, 23);
			cbCIA.TabIndex = 33;
			cbCIA.Text = "TIMERA";
			// 
			// cbTypes
			// 
			cbTypes.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			cbTypes.FormattingEnabled = true;
			cbTypes.Items.AddRange(new object[] { "(None)", "ExecBase", "timerequest", "Library", "Task", "KeyMapResource", "MsgPort", "Unit", "Resident" });
			cbTypes.Location = new System.Drawing.Point(873, 15);
			cbTypes.Name = "cbTypes";
			cbTypes.Size = new System.Drawing.Size(159, 23);
			cbTypes.TabIndex = 34;
			cbTypes.SelectionChangeCommitted += cbTypes_SelectionChangeCommitted;
			// 
			// lbCallStack
			// 
			lbCallStack.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			lbCallStack.Font = new System.Drawing.Font("Consolas", 8.25F);
			lbCallStack.FormattingEnabled = true;
			lbCallStack.ItemHeight = 13;
			lbCallStack.Location = new System.Drawing.Point(1219, 12);
			lbCallStack.Name = "lbCallStack";
			lbCallStack.SelectionMode = System.Windows.Forms.SelectionMode.None;
			lbCallStack.Size = new System.Drawing.Size(167, 238);
			lbCallStack.TabIndex = 35;
			// 
			// btnStepOut
			// 
			btnStepOut.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStepOut.Location = new System.Drawing.Point(1120, 208);
			btnStepOut.Name = "btnStepOut";
			btnStepOut.Size = new System.Drawing.Size(71, 23);
			btnStepOut.TabIndex = 36;
			btnStepOut.Text = "Step Out";
			btnStepOut.UseVisualStyleBackColor = true;
			btnStepOut.Click += btnStepOut_Click;
			// 
			// btnINTENA
			// 
			btnINTENA.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnINTENA.Location = new System.Drawing.Point(1039, 507);
			btnINTENA.Name = "btnINTENA";
			btnINTENA.Size = new System.Drawing.Size(72, 23);
			btnINTENA.TabIndex = 37;
			btnINTENA.Text = "INTENA";
			btnINTENA.UseVisualStyleBackColor = true;
			btnINTENA.Click += btnINTENA_Click;
			// 
			// lbCustom
			// 
			lbCustom.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			lbCustom.Font = new System.Drawing.Font("Consolas", 8.25F);
			lbCustom.FormattingEnabled = true;
			lbCustom.ItemHeight = 13;
			lbCustom.Location = new System.Drawing.Point(1219, 256);
			lbCustom.Name = "lbCustom";
			lbCustom.Size = new System.Drawing.Size(167, 303);
			lbCustom.TabIndex = 38;
			// 
			// btnDumpTrace
			// 
			btnDumpTrace.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnDumpTrace.Location = new System.Drawing.Point(1120, 303);
			btnDumpTrace.Name = "btnDumpTrace";
			btnDumpTrace.Size = new System.Drawing.Size(93, 23);
			btnDumpTrace.TabIndex = 39;
			btnDumpTrace.Text = "Dump Trace";
			btnDumpTrace.UseVisualStyleBackColor = true;
			btnDumpTrace.Click += btnDumpTrace_Click;
			// 
			// btnIDEACK
			// 
			btnIDEACK.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnIDEACK.Location = new System.Drawing.Point(1039, 536);
			btnIDEACK.Name = "btnIDEACK";
			btnIDEACK.Size = new System.Drawing.Size(72, 23);
			btnIDEACK.TabIndex = 40;
			btnIDEACK.Text = "IDEACK";
			btnIDEACK.UseVisualStyleBackColor = true;
			btnIDEACK.Click += btnIDEACK_Click;
			// 
			// btnChange
			// 
			btnChange.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnChange.Location = new System.Drawing.Point(1039, 422);
			btnChange.Name = "btnChange";
			btnChange.Size = new System.Drawing.Size(72, 23);
			btnChange.TabIndex = 41;
			btnChange.Text = "Change";
			btnChange.UseVisualStyleBackColor = true;
			btnChange.Click += btnChange_Click;
			// 
			// radioDF0
			// 
			radioDF0.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			radioDF0.AutoSize = true;
			radioDF0.Checked = true;
			radioDF0.Location = new System.Drawing.Point(1116, 424);
			radioDF0.Name = "radioDF0";
			radioDF0.Size = new System.Drawing.Size(14, 13);
			radioDF0.TabIndex = 42;
			radioDF0.TabStop = true;
			radioDF0.UseVisualStyleBackColor = true;
			radioDF0.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF1
			// 
			radioDF1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			radioDF1.AutoSize = true;
			radioDF1.Location = new System.Drawing.Point(1136, 424);
			radioDF1.Name = "radioDF1";
			radioDF1.Size = new System.Drawing.Size(14, 13);
			radioDF1.TabIndex = 43;
			radioDF1.TabStop = true;
			radioDF1.UseVisualStyleBackColor = true;
			radioDF1.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF2
			// 
			radioDF2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			radioDF2.AutoSize = true;
			radioDF2.Location = new System.Drawing.Point(1156, 424);
			radioDF2.Name = "radioDF2";
			radioDF2.Size = new System.Drawing.Size(14, 13);
			radioDF2.TabIndex = 44;
			radioDF2.TabStop = true;
			radioDF2.UseVisualStyleBackColor = true;
			radioDF2.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF3
			// 
			radioDF3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			radioDF3.AutoSize = true;
			radioDF3.Location = new System.Drawing.Point(1177, 424);
			radioDF3.Name = "radioDF3";
			radioDF3.Size = new System.Drawing.Size(14, 13);
			radioDF3.TabIndex = 45;
			radioDF3.TabStop = true;
			radioDF3.UseVisualStyleBackColor = true;
			radioDF3.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// btnGfxScan
			// 
			btnGfxScan.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnGfxScan.Location = new System.Drawing.Point(1120, 536);
			btnGfxScan.Name = "btnGfxScan";
			btnGfxScan.Size = new System.Drawing.Size(75, 23);
			btnGfxScan.TabIndex = 46;
			btnGfxScan.Text = "Gfx Scan";
			btnGfxScan.UseVisualStyleBackColor = true;
			btnGfxScan.Click += btnGfxScan_Click;
			// 
			// btnClearBBUSY
			// 
			btnClearBBUSY.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnClearBBUSY.Location = new System.Drawing.Point(1120, 507);
			btnClearBBUSY.Name = "btnClearBBUSY";
			btnClearBBUSY.Size = new System.Drawing.Size(75, 23);
			btnClearBBUSY.TabIndex = 47;
			btnClearBBUSY.Text = "~BBUSY";
			btnClearBBUSY.UseVisualStyleBackColor = true;
			btnClearBBUSY.Click += btnClearBBUSY_Click;
			// 
			// Jammy
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(1414, 605);
			Controls.Add(btnClearBBUSY);
			Controls.Add(btnGfxScan);
			Controls.Add(radioDF3);
			Controls.Add(radioDF2);
			Controls.Add(radioDF1);
			Controls.Add(radioDF0);
			Controls.Add(btnChange);
			Controls.Add(btnIDEACK);
			Controls.Add(btnDumpTrace);
			Controls.Add(lbCustom);
			Controls.Add(btnINTENA);
			Controls.Add(btnStepOut);
			Controls.Add(lbCallStack);
			Controls.Add(cbTypes);
			Controls.Add(cbCIA);
			Controls.Add(cbIRQ);
			Controls.Add(btnIRQ);
			Controls.Add(btnCIAInt);
			Controls.Add(btnRemoveDisk);
			Controls.Add(btnInsertDisk);
			Controls.Add(txtExecBase);
			Controls.Add(addressFollowBox);
			Controls.Add(btnDisassemble);
			Controls.Add(picDisk);
			Controls.Add(picPower);
			Controls.Add(btnStepOver);
			Controls.Add(btnRefresh);
			Controls.Add(splitContainer1);
			Controls.Add(btnReset);
			Controls.Add(btnGo);
			Controls.Add(btnStop);
			Controls.Add(btnStep);
			Controls.Add(lbRegisters);
			Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
			Name = "Jammy";
			Text = "Jammy";
			FormClosing += Form1_FormClosing;
			menuDisassembly.ResumeLayout(false);
			menuMemory.ResumeLayout(false);
			splitContainer1.Panel1.ResumeLayout(false);
			splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
			splitContainer1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)picPower).EndInit();
			((System.ComponentModel.ISupportInitialize)picDisk).EndInit();
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.ListBox lbRegisters;
		private System.Windows.Forms.RichTextBox txtDisassembly;
		private System.Windows.Forms.Button btnStep;
		private System.Windows.Forms.Button btnStop;
		private System.Windows.Forms.Button btnGo;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.RichTextBox txtMemory;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.Button btnRefresh;
		private System.Windows.Forms.Button btnStepOver;
		private System.Windows.Forms.PictureBox picPower;
		private System.Windows.Forms.PictureBox picDisk;
		private System.Windows.Forms.Button btnDisassemble;
		private System.Windows.Forms.RadioButton radioButton10;
		private System.Windows.Forms.RadioButton radioButton11;
		private System.Windows.Forms.RadioButton radioButton12;
		private System.Windows.Forms.RadioButton radioButton13;
		private System.Windows.Forms.RadioButton radioButton14;
		private System.Windows.Forms.RadioButton radioButton15;
		private System.Windows.Forms.RadioButton radioButton16;
		private System.Windows.Forms.RadioButton radioButton17;
		private System.Windows.Forms.ComboBox addressFollowBox;
		private System.Windows.Forms.RichTextBox txtExecBase;
		private System.Windows.Forms.ContextMenuStrip menuDisassembly;
		private System.Windows.Forms.ToolStripMenuItem toolStripBreakpoint;
		private System.Windows.Forms.ToolStripMenuItem toolStripSkip;
		private System.Windows.Forms.ToolStripMenuItem toolStripGoto;
		private System.Windows.Forms.ContextMenuStrip menuMemory;
		private System.Windows.Forms.ToolStripMenuItem menuMemoryGotoItem;
		private System.Windows.Forms.ToolStripMenuItem menuMemoryFindItem;
		private System.Windows.Forms.Button btnInsertDisk;
		private System.Windows.Forms.Button btnRemoveDisk;
		private System.Windows.Forms.Button btnCIAInt;
		private System.Windows.Forms.Button btnIRQ;
		private System.Windows.Forms.ComboBox cbIRQ;
		private System.Windows.Forms.ComboBox cbCIA;
		private System.Windows.Forms.ComboBox cbTypes;
		private System.Windows.Forms.ListBox lbCallStack;
		private System.Windows.Forms.Button btnStepOut;
		private System.Windows.Forms.Button btnINTENA;
		private System.Windows.Forms.ListBox lbCustom;
		private System.Windows.Forms.Button btnDumpTrace;
		private System.Windows.Forms.Button btnIDEACK;
		private System.Windows.Forms.Button btnChange;
		private System.Windows.Forms.RadioButton radioDF0;
		private System.Windows.Forms.RadioButton radioDF1;
		private System.Windows.Forms.RadioButton radioDF2;
		private System.Windows.Forms.RadioButton radioDF3;
		private System.Windows.Forms.Button btnGfxScan;
		private System.Windows.Forms.Button btnClearBBUSY;
	}
}

