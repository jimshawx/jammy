
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
			toolStripFind = new System.Windows.Forms.ToolStripMenuItem();
			toolStripFindNext = new System.Windows.Forms.ToolStripMenuItem();
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
			txtCopper = new System.Windows.Forms.RichTextBox();
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
			btnCribSheet = new System.Windows.Forms.Button();
			splitContainer2 = new System.Windows.Forms.SplitContainer();
			tabControl1 = new System.Windows.Forms.TabControl();
			tabCopper = new System.Windows.Forms.TabPage();
			tabExec = new System.Windows.Forms.TabPage();
			txtExecBase = new System.Windows.Forms.RichTextBox();
			tabVectors = new System.Windows.Forms.TabPage();
			txtVectors = new System.Windows.Forms.RichTextBox();
			lbIntvec = new System.Windows.Forms.ListBox();
			btnReadyDisk = new System.Windows.Forms.Button();
			tbCommand = new System.Windows.Forms.TextBox();
			btnINTDIS = new System.Windows.Forms.Button();
			btnStringScan = new System.Windows.Forms.Button();
			btnAnalyseFlow = new System.Windows.Forms.Button();
			btnDMAExplorer = new System.Windows.Forms.Button();
			tbClock = new System.Windows.Forms.TextBox();
			btnGenDisassemblies = new System.Windows.Forms.Button();
			menuDisassembly.SuspendLayout();
			menuMemory.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)picPower).BeginInit();
			((System.ComponentModel.ISupportInitialize)picDisk).BeginInit();
			((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
			splitContainer2.Panel1.SuspendLayout();
			splitContainer2.Panel2.SuspendLayout();
			splitContainer2.SuspendLayout();
			tabControl1.SuspendLayout();
			tabCopper.SuspendLayout();
			tabExec.SuspendLayout();
			tabVectors.SuspendLayout();
			SuspendLayout();
			// 
			// lbRegisters
			// 
			lbRegisters.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			lbRegisters.ColumnWidth = 85;
			lbRegisters.Font = new System.Drawing.Font("Cascadia Mono", 8.25F);
			lbRegisters.IntegralHeight = false;
			lbRegisters.Location = new System.Drawing.Point(2012, 26);
			lbRegisters.Margin = new System.Windows.Forms.Padding(6);
			lbRegisters.MultiColumn = true;
			lbRegisters.Name = "lbRegisters";
			lbRegisters.SelectionMode = System.Windows.Forms.SelectionMode.None;
			lbRegisters.Size = new System.Drawing.Size(322, 315);
			lbRegisters.TabIndex = 0;
			lbRegisters.MouseDoubleClick += lbRegisters_MouseDoubleClick;
			// 
			// txtDisassembly
			// 
			txtDisassembly.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			txtDisassembly.BackColor = System.Drawing.SystemColors.Window;
			txtDisassembly.ContextMenuStrip = menuDisassembly;
			txtDisassembly.DetectUrls = false;
			txtDisassembly.Font = new System.Drawing.Font("Cascadia Mono", 7.25F);
			txtDisassembly.HideSelection = false;
			txtDisassembly.Location = new System.Drawing.Point(6, 6);
			txtDisassembly.Margin = new System.Windows.Forms.Padding(6);
			txtDisassembly.Name = "txtDisassembly";
			txtDisassembly.ReadOnly = true;
			txtDisassembly.Size = new System.Drawing.Size(1390, 533);
			txtDisassembly.TabIndex = 1;
			txtDisassembly.Text = "";
			txtDisassembly.WordWrap = false;
			// 
			// menuDisassembly
			// 
			menuDisassembly.ImageScalingSize = new System.Drawing.Size(32, 32);
			menuDisassembly.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripBreakpoint, toolStripSkip, toolStripGoto, toolStripFind, toolStripFindNext });
			menuDisassembly.Name = "menuDisassembly";
			menuDisassembly.Size = new System.Drawing.Size(204, 194);
			menuDisassembly.ItemClicked += menuDisassembly_ItemClicked;
			// 
			// toolStripBreakpoint
			// 
			toolStripBreakpoint.Name = "toolStripBreakpoint";
			toolStripBreakpoint.Size = new System.Drawing.Size(203, 38);
			toolStripBreakpoint.Text = "Breakpoint";
			// 
			// toolStripSkip
			// 
			toolStripSkip.Name = "toolStripSkip";
			toolStripSkip.Size = new System.Drawing.Size(203, 38);
			toolStripSkip.Text = "Skip";
			// 
			// toolStripGoto
			// 
			toolStripGoto.Name = "toolStripGoto";
			toolStripGoto.Size = new System.Drawing.Size(203, 38);
			toolStripGoto.Text = "Go To...";
			// 
			// toolStripFind
			// 
			toolStripFind.Name = "toolStripFind";
			toolStripFind.Size = new System.Drawing.Size(203, 38);
			toolStripFind.Text = "Find...";
			// 
			// toolStripFindNext
			// 
			toolStripFindNext.Name = "toolStripFindNext";
			toolStripFindNext.Size = new System.Drawing.Size(203, 38);
			toolStripFindNext.Text = "Find Next";
			// 
			// btnStep
			// 
			btnStep.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStep.Location = new System.Drawing.Point(2012, 382);
			btnStep.Margin = new System.Windows.Forms.Padding(6);
			btnStep.Name = "btnStep";
			btnStep.Size = new System.Drawing.Size(132, 49);
			btnStep.TabIndex = 2;
			btnStep.Text = "Step";
			btnStep.UseVisualStyleBackColor = true;
			btnStep.Click += btnStep_Click;
			// 
			// btnStop
			// 
			btnStop.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStop.Location = new System.Drawing.Point(2012, 444);
			btnStop.Margin = new System.Windows.Forms.Padding(6);
			btnStop.Name = "btnStop";
			btnStop.Size = new System.Drawing.Size(132, 49);
			btnStop.TabIndex = 3;
			btnStop.Text = "Stop";
			btnStop.UseVisualStyleBackColor = true;
			btnStop.Click += btnStop_Click;
			// 
			// btnGo
			// 
			btnGo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnGo.Location = new System.Drawing.Point(2012, 508);
			btnGo.Margin = new System.Windows.Forms.Padding(6);
			btnGo.Name = "btnGo";
			btnGo.Size = new System.Drawing.Size(132, 49);
			btnGo.TabIndex = 4;
			btnGo.Text = "Go";
			btnGo.UseVisualStyleBackColor = true;
			btnGo.Click += btnGo_Click;
			// 
			// btnReset
			// 
			btnReset.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnReset.Location = new System.Drawing.Point(2012, 572);
			btnReset.Margin = new System.Windows.Forms.Padding(6);
			btnReset.Name = "btnReset";
			btnReset.Size = new System.Drawing.Size(132, 49);
			btnReset.TabIndex = 5;
			btnReset.Text = "Reset";
			btnReset.UseVisualStyleBackColor = true;
			btnReset.Click += btnReset_Click;
			// 
			// txtMemory
			// 
			txtMemory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			txtMemory.BackColor = System.Drawing.SystemColors.Window;
			txtMemory.ContextMenuStrip = menuMemory;
			txtMemory.DetectUrls = false;
			txtMemory.Font = new System.Drawing.Font("Cascadia Mono", 7.25F);
			txtMemory.Location = new System.Drawing.Point(6, 6);
			txtMemory.Margin = new System.Windows.Forms.Padding(6);
			txtMemory.Name = "txtMemory";
			txtMemory.ReadOnly = true;
			txtMemory.Size = new System.Drawing.Size(1390, 602);
			txtMemory.TabIndex = 6;
			txtMemory.Text = "00000160 0000000000000000 0000000000000000 0000000000000000 0000000000000000   ................................";
			txtMemory.WordWrap = false;
			// 
			// menuMemory
			// 
			menuMemory.ImageScalingSize = new System.Drawing.Size(32, 32);
			menuMemory.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuMemoryGotoItem, menuMemoryFindItem });
			menuMemory.Name = "menuMemory";
			menuMemory.Size = new System.Drawing.Size(166, 80);
			menuMemory.ItemClicked += menuMemory_ItemClicked;
			// 
			// menuMemoryGotoItem
			// 
			menuMemoryGotoItem.Name = "menuMemoryGotoItem";
			menuMemoryGotoItem.Size = new System.Drawing.Size(165, 38);
			menuMemoryGotoItem.Text = "Go To...";
			// 
			// menuMemoryFindItem
			// 
			menuMemoryFindItem.Name = "menuMemoryFindItem";
			menuMemoryFindItem.Size = new System.Drawing.Size(165, 38);
			menuMemoryFindItem.Text = "Find...";
			// 
			// splitContainer1
			// 
			splitContainer1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			splitContainer1.Location = new System.Drawing.Point(0, 6);
			splitContainer1.Margin = new System.Windows.Forms.Padding(0);
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
			splitContainer1.Size = new System.Drawing.Size(1403, 1282);
			splitContainer1.SplitterDistance = 545;
			splitContainer1.SplitterWidth = 9;
			splitContainer1.TabIndex = 7;
			// 
			// btnRefresh
			// 
			btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnRefresh.Location = new System.Drawing.Point(2014, 708);
			btnRefresh.Margin = new System.Windows.Forms.Padding(6);
			btnRefresh.Name = "btnRefresh";
			btnRefresh.Size = new System.Drawing.Size(132, 49);
			btnRefresh.TabIndex = 8;
			btnRefresh.Text = "Refresh";
			btnRefresh.UseVisualStyleBackColor = true;
			btnRefresh.Click += btnRefresh_Click;
			// 
			// btnStepOver
			// 
			btnStepOver.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStepOver.Location = new System.Drawing.Point(2164, 382);
			btnStepOver.Margin = new System.Windows.Forms.Padding(6);
			btnStepOver.Name = "btnStepOver";
			btnStepOver.Size = new System.Drawing.Size(132, 49);
			btnStepOver.TabIndex = 9;
			btnStepOver.Text = "Step Over";
			btnStepOver.UseVisualStyleBackColor = true;
			btnStepOver.Click += btnStepOver_Click;
			// 
			// picPower
			// 
			picPower.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			picPower.Location = new System.Drawing.Point(2209, 533);
			picPower.Margin = new System.Windows.Forms.Padding(6);
			picPower.Name = "picPower";
			picPower.Size = new System.Drawing.Size(87, 21);
			picPower.TabIndex = 10;
			picPower.TabStop = false;
			// 
			// picDisk
			// 
			picDisk.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			picDisk.Location = new System.Drawing.Point(2209, 572);
			picDisk.Margin = new System.Windows.Forms.Padding(6);
			picDisk.Name = "picDisk";
			picDisk.Size = new System.Drawing.Size(87, 21);
			picDisk.TabIndex = 11;
			picDisk.TabStop = false;
			// 
			// btnDisassemble
			// 
			btnDisassemble.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnDisassemble.Location = new System.Drawing.Point(2164, 708);
			btnDisassemble.Margin = new System.Windows.Forms.Padding(6);
			btnDisassemble.Name = "btnDisassemble";
			btnDisassemble.Size = new System.Drawing.Size(173, 49);
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
			addressFollowBox.Location = new System.Drawing.Point(2014, 772);
			addressFollowBox.Margin = new System.Windows.Forms.Padding(6);
			addressFollowBox.Name = "addressFollowBox";
			addressFollowBox.Size = new System.Drawing.Size(214, 40);
			addressFollowBox.TabIndex = 25;
			addressFollowBox.SelectionChangeCommitted += addressFollowBox_SelectionChangeCommitted;
			// 
			// txtCopper
			// 
			txtCopper.BackColor = System.Drawing.SystemColors.Window;
			txtCopper.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			txtCopper.DetectUrls = false;
			txtCopper.Dock = System.Windows.Forms.DockStyle.Fill;
			txtCopper.Font = new System.Drawing.Font("Cascadia Mono", 7.25F);
			txtCopper.Location = new System.Drawing.Point(3, 3);
			txtCopper.Name = "txtCopper";
			txtCopper.ReadOnly = true;
			txtCopper.Size = new System.Drawing.Size(552, 939);
			txtCopper.TabIndex = 26;
			txtCopper.Text = "";
			txtCopper.WordWrap = false;
			// 
			// btnInsertDisk
			// 
			btnInsertDisk.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnInsertDisk.Location = new System.Drawing.Point(2014, 836);
			btnInsertDisk.Margin = new System.Windows.Forms.Padding(6);
			btnInsertDisk.Name = "btnInsertDisk";
			btnInsertDisk.Size = new System.Drawing.Size(134, 49);
			btnInsertDisk.TabIndex = 27;
			btnInsertDisk.Text = "Insert Disk";
			btnInsertDisk.UseVisualStyleBackColor = true;
			btnInsertDisk.Click += btnInsertDisk_Click;
			// 
			// btnRemoveDisk
			// 
			btnRemoveDisk.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnRemoveDisk.Location = new System.Drawing.Point(2164, 836);
			btnRemoveDisk.Margin = new System.Windows.Forms.Padding(6);
			btnRemoveDisk.Name = "btnRemoveDisk";
			btnRemoveDisk.Size = new System.Drawing.Size(132, 49);
			btnRemoveDisk.TabIndex = 28;
			btnRemoveDisk.Text = "Remove Disk";
			btnRemoveDisk.UseVisualStyleBackColor = true;
			btnRemoveDisk.Click += btnRemoveDisk_Click;
			// 
			// btnCIAInt
			// 
			btnCIAInt.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnCIAInt.Location = new System.Drawing.Point(2014, 962);
			btnCIAInt.Margin = new System.Windows.Forms.Padding(6);
			btnCIAInt.Name = "btnCIAInt";
			btnCIAInt.Size = new System.Drawing.Size(134, 49);
			btnCIAInt.TabIndex = 29;
			btnCIAInt.Text = "CIA Int";
			btnCIAInt.UseVisualStyleBackColor = true;
			btnCIAInt.Click += btnCIAInt_Click;
			// 
			// btnIRQ
			// 
			btnIRQ.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnIRQ.Location = new System.Drawing.Point(2014, 1024);
			btnIRQ.Margin = new System.Windows.Forms.Padding(6);
			btnIRQ.Name = "btnIRQ";
			btnIRQ.Size = new System.Drawing.Size(134, 49);
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
			cbIRQ.Location = new System.Drawing.Point(2164, 1024);
			cbIRQ.Margin = new System.Windows.Forms.Padding(6);
			cbIRQ.Name = "cbIRQ";
			cbIRQ.Size = new System.Drawing.Size(128, 40);
			cbIRQ.TabIndex = 32;
			cbIRQ.Text = "BLIT";
			// 
			// cbCIA
			// 
			cbCIA.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			cbCIA.FormattingEnabled = true;
			cbCIA.Items.AddRange(new object[] { "TIMERA", "TIMERB", "TODALARM", "SERIAL", "FLAG" });
			cbCIA.Location = new System.Drawing.Point(2164, 962);
			cbCIA.Margin = new System.Windows.Forms.Padding(6);
			cbCIA.Name = "cbCIA";
			cbCIA.Size = new System.Drawing.Size(128, 40);
			cbCIA.TabIndex = 33;
			cbCIA.Text = "TIMERA";
			// 
			// cbTypes
			// 
			cbTypes.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			cbTypes.FormattingEnabled = true;
			cbTypes.Items.AddRange(new object[] { "(None)", "ExecBase", "timerequest", "Library", "Task", "KeyMapResource", "MsgPort", "Unit", "Resident" });
			cbTypes.Location = new System.Drawing.Point(2312, 1207);
			cbTypes.Margin = new System.Windows.Forms.Padding(6);
			cbTypes.Name = "cbTypes";
			cbTypes.Size = new System.Drawing.Size(292, 40);
			cbTypes.TabIndex = 34;
			cbTypes.SelectionChangeCommitted += cbTypes_SelectionChangeCommitted;
			// 
			// lbCallStack
			// 
			lbCallStack.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			lbCallStack.ColumnWidth = 83;
			lbCallStack.Font = new System.Drawing.Font("Cascadia Mono", 8.25F);
			lbCallStack.IntegralHeight = false;
			lbCallStack.Location = new System.Drawing.Point(2348, 26);
			lbCallStack.Margin = new System.Windows.Forms.Padding(6);
			lbCallStack.MultiColumn = true;
			lbCallStack.Name = "lbCallStack";
			lbCallStack.SelectionMode = System.Windows.Forms.SelectionMode.None;
			lbCallStack.Size = new System.Drawing.Size(328, 443);
			lbCallStack.TabIndex = 35;
			lbCallStack.MouseDoubleClick += lbCallStack_MouseDoubleClick;
			// 
			// btnStepOut
			// 
			btnStepOut.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStepOut.Location = new System.Drawing.Point(2164, 444);
			btnStepOut.Margin = new System.Windows.Forms.Padding(6);
			btnStepOut.Name = "btnStepOut";
			btnStepOut.Size = new System.Drawing.Size(132, 49);
			btnStepOut.TabIndex = 36;
			btnStepOut.Text = "Step Out";
			btnStepOut.UseVisualStyleBackColor = true;
			btnStepOut.Click += btnStepOut_Click;
			// 
			// btnINTENA
			// 
			btnINTENA.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnINTENA.Location = new System.Drawing.Point(2014, 1082);
			btnINTENA.Margin = new System.Windows.Forms.Padding(6);
			btnINTENA.Name = "btnINTENA";
			btnINTENA.Size = new System.Drawing.Size(69, 49);
			btnINTENA.TabIndex = 37;
			btnINTENA.Text = "EN";
			btnINTENA.UseVisualStyleBackColor = true;
			btnINTENA.Click += btnINTENA_Click;
			// 
			// lbCustom
			// 
			lbCustom.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			lbCustom.Font = new System.Drawing.Font("Cascadia Mono", 8.25F);
			lbCustom.FormattingEnabled = true;
			lbCustom.Location = new System.Drawing.Point(2348, 546);
			lbCustom.Margin = new System.Windows.Forms.Padding(6);
			lbCustom.Name = "lbCustom";
			lbCustom.Size = new System.Drawing.Size(328, 555);
			lbCustom.TabIndex = 38;
			// 
			// btnDumpTrace
			// 
			btnDumpTrace.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnDumpTrace.Location = new System.Drawing.Point(2164, 646);
			btnDumpTrace.Margin = new System.Windows.Forms.Padding(6);
			btnDumpTrace.Name = "btnDumpTrace";
			btnDumpTrace.Size = new System.Drawing.Size(173, 49);
			btnDumpTrace.TabIndex = 39;
			btnDumpTrace.Text = "Dump Trace";
			btnDumpTrace.UseVisualStyleBackColor = true;
			btnDumpTrace.Click += btnDumpTrace_Click;
			// 
			// btnIDEACK
			// 
			btnIDEACK.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnIDEACK.Location = new System.Drawing.Point(2014, 1143);
			btnIDEACK.Margin = new System.Windows.Forms.Padding(6);
			btnIDEACK.Name = "btnIDEACK";
			btnIDEACK.Size = new System.Drawing.Size(134, 49);
			btnIDEACK.TabIndex = 40;
			btnIDEACK.Text = "IDEACK";
			btnIDEACK.UseVisualStyleBackColor = true;
			btnIDEACK.Click += btnIDEACK_Click;
			// 
			// btnChange
			// 
			btnChange.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnChange.Location = new System.Drawing.Point(2014, 900);
			btnChange.Margin = new System.Windows.Forms.Padding(6);
			btnChange.Name = "btnChange";
			btnChange.Size = new System.Drawing.Size(134, 49);
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
			radioDF0.Location = new System.Drawing.Point(2156, 905);
			radioDF0.Margin = new System.Windows.Forms.Padding(6);
			radioDF0.Name = "radioDF0";
			radioDF0.Size = new System.Drawing.Size(27, 26);
			radioDF0.TabIndex = 42;
			radioDF0.TabStop = true;
			radioDF0.UseVisualStyleBackColor = true;
			radioDF0.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF1
			// 
			radioDF1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			radioDF1.AutoSize = true;
			radioDF1.Location = new System.Drawing.Point(2193, 905);
			radioDF1.Margin = new System.Windows.Forms.Padding(6);
			radioDF1.Name = "radioDF1";
			radioDF1.Size = new System.Drawing.Size(27, 26);
			radioDF1.TabIndex = 43;
			radioDF1.TabStop = true;
			radioDF1.UseVisualStyleBackColor = true;
			radioDF1.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF2
			// 
			radioDF2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			radioDF2.AutoSize = true;
			radioDF2.Location = new System.Drawing.Point(2230, 905);
			radioDF2.Margin = new System.Windows.Forms.Padding(6);
			radioDF2.Name = "radioDF2";
			radioDF2.Size = new System.Drawing.Size(27, 26);
			radioDF2.TabIndex = 44;
			radioDF2.TabStop = true;
			radioDF2.UseVisualStyleBackColor = true;
			radioDF2.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF3
			// 
			radioDF3.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			radioDF3.AutoSize = true;
			radioDF3.Location = new System.Drawing.Point(2269, 905);
			radioDF3.Margin = new System.Windows.Forms.Padding(6);
			radioDF3.Name = "radioDF3";
			radioDF3.Size = new System.Drawing.Size(27, 26);
			radioDF3.TabIndex = 45;
			radioDF3.TabStop = true;
			radioDF3.UseVisualStyleBackColor = true;
			radioDF3.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// btnGfxScan
			// 
			btnGfxScan.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnGfxScan.Location = new System.Drawing.Point(2164, 1143);
			btnGfxScan.Margin = new System.Windows.Forms.Padding(6);
			btnGfxScan.Name = "btnGfxScan";
			btnGfxScan.Size = new System.Drawing.Size(139, 49);
			btnGfxScan.TabIndex = 46;
			btnGfxScan.Text = "Gfx Scan";
			btnGfxScan.UseVisualStyleBackColor = true;
			btnGfxScan.Click += btnGfxScan_Click;
			// 
			// btnClearBBUSY
			// 
			btnClearBBUSY.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnClearBBUSY.Location = new System.Drawing.Point(2164, 1082);
			btnClearBBUSY.Margin = new System.Windows.Forms.Padding(6);
			btnClearBBUSY.Name = "btnClearBBUSY";
			btnClearBBUSY.Size = new System.Drawing.Size(139, 49);
			btnClearBBUSY.TabIndex = 47;
			btnClearBBUSY.Text = "~BBUSY";
			btnClearBBUSY.UseVisualStyleBackColor = true;
			btnClearBBUSY.Click += btnClearBBUSY_Click;
			// 
			// btnCribSheet
			// 
			btnCribSheet.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnCribSheet.Location = new System.Drawing.Point(2164, 1201);
			btnCribSheet.Name = "btnCribSheet";
			btnCribSheet.Size = new System.Drawing.Size(139, 49);
			btnCribSheet.TabIndex = 48;
			btnCribSheet.Text = "Crib Sheet";
			btnCribSheet.UseVisualStyleBackColor = true;
			btnCribSheet.Click += btnCribSheet_Click;
			// 
			// splitContainer2
			// 
			splitContainer2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			splitContainer2.Location = new System.Drawing.Point(4, -1);
			splitContainer2.Name = "splitContainer2";
			// 
			// splitContainer2.Panel1
			// 
			splitContainer2.Panel1.Controls.Add(splitContainer1);
			// 
			// splitContainer2.Panel2
			// 
			splitContainer2.Panel2.Controls.Add(tabControl1);
			splitContainer2.Panel2.Controls.Add(lbIntvec);
			splitContainer2.Size = new System.Drawing.Size(1998, 1288);
			splitContainer2.SplitterDistance = 1409;
			splitContainer2.SplitterWidth = 9;
			splitContainer2.TabIndex = 49;
			// 
			// tabControl1
			// 
			tabControl1.Controls.Add(tabCopper);
			tabControl1.Controls.Add(tabExec);
			tabControl1.Controls.Add(tabVectors);
			tabControl1.Font = new System.Drawing.Font("Cascadia Mono", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			tabControl1.Location = new System.Drawing.Point(3, 6);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new System.Drawing.Size(574, 995);
			tabControl1.TabIndex = 28;
			// 
			// tabCopper
			// 
			tabCopper.Controls.Add(txtCopper);
			tabCopper.Location = new System.Drawing.Point(8, 42);
			tabCopper.Name = "tabCopper";
			tabCopper.Padding = new System.Windows.Forms.Padding(3);
			tabCopper.Size = new System.Drawing.Size(558, 945);
			tabCopper.TabIndex = 0;
			tabCopper.Text = "Copper";
			tabCopper.UseVisualStyleBackColor = true;
			// 
			// tabExec
			// 
			tabExec.Controls.Add(txtExecBase);
			tabExec.Location = new System.Drawing.Point(8, 42);
			tabExec.Name = "tabExec";
			tabExec.Padding = new System.Windows.Forms.Padding(3);
			tabExec.Size = new System.Drawing.Size(558, 945);
			tabExec.TabIndex = 1;
			tabExec.Text = "ExecBase";
			tabExec.UseVisualStyleBackColor = true;
			// 
			// txtExecBase
			// 
			txtExecBase.BackColor = System.Drawing.SystemColors.Window;
			txtExecBase.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			txtExecBase.DetectUrls = false;
			txtExecBase.Dock = System.Windows.Forms.DockStyle.Fill;
			txtExecBase.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			txtExecBase.Location = new System.Drawing.Point(3, 3);
			txtExecBase.Name = "txtExecBase";
			txtExecBase.ReadOnly = true;
			txtExecBase.Size = new System.Drawing.Size(552, 939);
			txtExecBase.TabIndex = 0;
			txtExecBase.Text = "";
			txtExecBase.WordWrap = false;
			// 
			// tabVectors
			// 
			tabVectors.Controls.Add(txtVectors);
			tabVectors.Location = new System.Drawing.Point(8, 42);
			tabVectors.Name = "tabVectors";
			tabVectors.Padding = new System.Windows.Forms.Padding(3);
			tabVectors.Size = new System.Drawing.Size(558, 945);
			tabVectors.TabIndex = 2;
			tabVectors.Text = "Vectors";
			tabVectors.UseVisualStyleBackColor = true;
			// 
			// txtVectors
			// 
			txtVectors.BackColor = System.Drawing.SystemColors.Window;
			txtVectors.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			txtVectors.DetectUrls = false;
			txtVectors.Dock = System.Windows.Forms.DockStyle.Fill;
			txtVectors.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			txtVectors.Location = new System.Drawing.Point(3, 3);
			txtVectors.Name = "txtVectors";
			txtVectors.ReadOnly = true;
			txtVectors.Size = new System.Drawing.Size(552, 939);
			txtVectors.TabIndex = 0;
			txtVectors.Text = "";
			txtVectors.WordWrap = false;
			// 
			// lbIntvec
			// 
			lbIntvec.Font = new System.Drawing.Font("Cascadia Mono", 8.25F);
			lbIntvec.FormattingEnabled = true;
			lbIntvec.Location = new System.Drawing.Point(14, 999);
			lbIntvec.Name = "lbIntvec";
			lbIntvec.Size = new System.Drawing.Size(466, 236);
			lbIntvec.TabIndex = 27;
			lbIntvec.MouseDoubleClick += lbIntvec_MouseDoubleClick;
			// 
			// btnReadyDisk
			// 
			btnReadyDisk.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnReadyDisk.Location = new System.Drawing.Point(2240, 787);
			btnReadyDisk.Margin = new System.Windows.Forms.Padding(6);
			btnReadyDisk.Name = "btnReadyDisk";
			btnReadyDisk.Size = new System.Drawing.Size(97, 49);
			btnReadyDisk.TabIndex = 50;
			btnReadyDisk.Text = "Ready";
			btnReadyDisk.UseVisualStyleBackColor = true;
			btnReadyDisk.Click += btnReadyDisk_Click;
			// 
			// tbCommand
			// 
			tbCommand.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			tbCommand.BackColor = System.Drawing.SystemColors.WindowText;
			tbCommand.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			tbCommand.CausesValidation = false;
			tbCommand.ForeColor = System.Drawing.SystemColors.Window;
			tbCommand.Location = new System.Drawing.Point(2014, 1207);
			tbCommand.Name = "tbCommand";
			tbCommand.PlaceholderText = ">";
			tbCommand.Size = new System.Drawing.Size(145, 39);
			tbCommand.TabIndex = 51;
			tbCommand.KeyDown += tbCommand_KeyDown;
			// 
			// btnINTDIS
			// 
			btnINTDIS.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnINTDIS.Location = new System.Drawing.Point(2083, 1082);
			btnINTDIS.Margin = new System.Windows.Forms.Padding(6);
			btnINTDIS.Name = "btnINTDIS";
			btnINTDIS.Size = new System.Drawing.Size(76, 49);
			btnINTDIS.TabIndex = 52;
			btnINTDIS.Text = "~EN";
			btnINTDIS.UseVisualStyleBackColor = true;
			btnINTDIS.Click += btnINTDIS_Click;
			// 
			// btnStringScan
			// 
			btnStringScan.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnStringScan.Location = new System.Drawing.Point(2304, 1143);
			btnStringScan.Margin = new System.Windows.Forms.Padding(6);
			btnStringScan.Name = "btnStringScan";
			btnStringScan.Size = new System.Drawing.Size(154, 49);
			btnStringScan.TabIndex = 53;
			btnStringScan.Text = "String Scan";
			btnStringScan.UseVisualStyleBackColor = true;
			btnStringScan.Click += btnStringScan_Click;
			// 
			// btnAnalyseFlow
			// 
			btnAnalyseFlow.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnAnalyseFlow.Location = new System.Drawing.Point(2014, 647);
			btnAnalyseFlow.Margin = new System.Windows.Forms.Padding(6);
			btnAnalyseFlow.Name = "btnAnalyseFlow";
			btnAnalyseFlow.Size = new System.Drawing.Size(132, 49);
			btnAnalyseFlow.TabIndex = 54;
			btnAnalyseFlow.Text = "Analyse";
			btnAnalyseFlow.UseVisualStyleBackColor = true;
			btnAnalyseFlow.Click += btnAnalyseFlow_Click;
			// 
			// btnDMAExplorer
			// 
			btnDMAExplorer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			btnDMAExplorer.Location = new System.Drawing.Point(2467, 1144);
			btnDMAExplorer.Name = "btnDMAExplorer";
			btnDMAExplorer.Size = new System.Drawing.Size(173, 46);
			btnDMAExplorer.TabIndex = 55;
			btnDMAExplorer.Text = "DMA Explorer";
			btnDMAExplorer.UseVisualStyleBackColor = true;
			btnDMAExplorer.Click += btnDMAExplorer_Click;
			// 
			// tbClock
			// 
			tbClock.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			tbClock.BackColor = System.Drawing.SystemColors.Window;
			tbClock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			tbClock.Font = new System.Drawing.Font("Cascadia Mono", 7.875F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			tbClock.Location = new System.Drawing.Point(2348, 478);
			tbClock.Multiline = true;
			tbClock.Name = "tbClock";
			tbClock.ReadOnly = true;
			tbClock.Size = new System.Drawing.Size(328, 59);
			tbClock.TabIndex = 56;
			// 
			// btnGenDisassemblies
			// 
			btnGenDisassemblies.Location = new System.Drawing.Point(2613, 1204);
			btnGenDisassemblies.Name = "btnGenDisassemblies";
			btnGenDisassemblies.Size = new System.Drawing.Size(79, 46);
			btnGenDisassemblies.TabIndex = 57;
			btnGenDisassemblies.Text = "Dis";
			btnGenDisassemblies.UseVisualStyleBackColor = true;
			btnGenDisassemblies.Click += btnGenDisassemblies_Click;
			// 
			// Jammy
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(2710, 1291);
			Controls.Add(btnGenDisassemblies);
			Controls.Add(tbClock);
			Controls.Add(btnDMAExplorer);
			Controls.Add(btnAnalyseFlow);
			Controls.Add(btnStringScan);
			Controls.Add(btnINTDIS);
			Controls.Add(tbCommand);
			Controls.Add(btnReadyDisk);
			Controls.Add(splitContainer2);
			Controls.Add(btnCribSheet);
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
			Controls.Add(addressFollowBox);
			Controls.Add(btnDisassemble);
			Controls.Add(picDisk);
			Controls.Add(picPower);
			Controls.Add(btnStepOver);
			Controls.Add(btnRefresh);
			Controls.Add(btnReset);
			Controls.Add(btnGo);
			Controls.Add(btnStop);
			Controls.Add(btnStep);
			Controls.Add(lbRegisters);
			Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
			Margin = new System.Windows.Forms.Padding(6);
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
			splitContainer2.Panel1.ResumeLayout(false);
			splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
			splitContainer2.ResumeLayout(false);
			tabControl1.ResumeLayout(false);
			tabCopper.ResumeLayout(false);
			tabExec.ResumeLayout(false);
			tabVectors.ResumeLayout(false);
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
		private System.Windows.Forms.RichTextBox txtCopper;
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
		private System.Windows.Forms.Button btnCribSheet;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.Button btnReadyDisk;
		private System.Windows.Forms.ListBox lbIntvec;
		private System.Windows.Forms.TextBox tbCommand;
		private System.Windows.Forms.Button btnINTDIS;
		private System.Windows.Forms.Button btnStringScan;
		private System.Windows.Forms.Button btnAnalyseFlow;
		private System.Windows.Forms.ToolStripMenuItem toolStripFind;
		private System.Windows.Forms.ToolStripMenuItem toolStripFindNext;
		private System.Windows.Forms.Button btnDMAExplorer;
		private System.Windows.Forms.TextBox tbClock;
		private System.Windows.Forms.Button btnGenDisassemblies;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabCopper;
		private System.Windows.Forms.TabPage tabExec;
		private System.Windows.Forms.TabPage tabVectors;
		private System.Windows.Forms.RichTextBox txtExecBase;
		private System.Windows.Forms.RichTextBox txtVectors;
	}
}

