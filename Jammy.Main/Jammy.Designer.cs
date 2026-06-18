
/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

using System.Windows.Forms;

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
			lbRegisters = new ListBox();
			txtDisassembly = new RichTextBox();
			menuDisassembly = new ContextMenuStrip(components);
			toolStripBreakpoint = new ToolStripMenuItem();
			toolStripSkip = new ToolStripMenuItem();
			toolStripGoto = new ToolStripMenuItem();
			toolStripFind = new ToolStripMenuItem();
			toolStripFindNext = new ToolStripMenuItem();
			btnStep = new Button();
			btnStop = new Button();
			btnGo = new Button();
			btnReset = new Button();
			txtMemory = new RichTextBox();
			menuMemory = new ContextMenuStrip(components);
			menuMemoryGotoItem = new ToolStripMenuItem();
			menuMemoryFindItem = new ToolStripMenuItem();
			splitDisassembly = new SplitContainer();
			btnRefresh = new Button();
			btnStepOver = new Button();
			picPower = new PictureBox();
			picDisk = new PictureBox();
			btnDisassemble = new Button();
			addressFollowBox = new ComboBox();
			txtCopper = new RichTextBox();
			btnInsertDisk = new Button();
			btnRemoveDisk = new Button();
			btnCIAInt = new Button();
			btnIRQ = new Button();
			cbIRQ = new ComboBox();
			cbCIA = new ComboBox();
			cbTypes = new ComboBox();
			lbCallStack = new ListBox();
			btnStepOut = new Button();
			btnINTENA = new Button();
			lbCustom = new ListBox();
			btnDumpTrace = new Button();
			btnIDEACK = new Button();
			btnChange = new Button();
			radioDF0 = new RadioButton();
			radioDF1 = new RadioButton();
			radioDF2 = new RadioButton();
			radioDF3 = new RadioButton();
			btnGfxScan = new Button();
			btnClearBBUSY = new Button();
			btnCribSheet = new Button();
			splitContent = new SplitContainer();
			pnlButtons = new Panel();
			btnPluginReload = new Button();
			btnGenDisassemblies = new Button();
			tbClock = new TextBox();
			btnDMAExplorer = new Button();
			btnAnalyseFlow = new Button();
			btnStringScan = new Button();
			btnINTDIS = new Button();
			tbCommand = new TextBox();
			btnReadyDisk = new Button();
			lbIntvec = new ListBox();
			tabControl1 = new TabControl();
			tabCopper = new TabPage();
			tabExec = new TabPage();
			txtExecBase = new RichTextBox();
			tabVectors = new TabPage();
			txtVectors = new RichTextBox();
			tabLibraries = new TabPage();
			txtLibraries = new RichTextBox();
			tabAllocations = new TabPage();
			txtAllocations = new RichTextBox();
			menuDisassembly.SuspendLayout();
			menuMemory.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitDisassembly).BeginInit();
			splitDisassembly.Panel1.SuspendLayout();
			splitDisassembly.Panel2.SuspendLayout();
			splitDisassembly.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)picPower).BeginInit();
			((System.ComponentModel.ISupportInitialize)picDisk).BeginInit();
			((System.ComponentModel.ISupportInitialize)splitContent).BeginInit();
			splitContent.Panel1.SuspendLayout();
			splitContent.Panel2.SuspendLayout();
			splitContent.SuspendLayout();
			pnlButtons.SuspendLayout();
			tabControl1.SuspendLayout();
			tabCopper.SuspendLayout();
			tabExec.SuspendLayout();
			tabVectors.SuspendLayout();
			tabLibraries.SuspendLayout();
			tabAllocations.SuspendLayout();
			SuspendLayout();
			// 
			// lbRegisters
			// 
			lbRegisters.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lbRegisters.BorderStyle = BorderStyle.FixedSingle;
			lbRegisters.ColumnWidth = 85;
			lbRegisters.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			lbRegisters.IntegralHeight = false;
			lbRegisters.Location = new System.Drawing.Point(569, 13);
			lbRegisters.Margin = new Padding(11, 13, 11, 13);
			lbRegisters.MultiColumn = true;
			lbRegisters.Name = "lbRegisters";
			lbRegisters.SelectionMode = SelectionMode.None;
			lbRegisters.Size = new System.Drawing.Size(275, 403);
			lbRegisters.TabIndex = 0;
			lbRegisters.MouseDoubleClick += lbRegisters_MouseDoubleClick;
			// 
			// txtDisassembly
			// 
			txtDisassembly.BackColor = System.Drawing.SystemColors.Window;
			txtDisassembly.BorderStyle = BorderStyle.FixedSingle;
			txtDisassembly.ContextMenuStrip = menuDisassembly;
			txtDisassembly.DetectUrls = false;
			txtDisassembly.Dock = DockStyle.Fill;
			txtDisassembly.Font = new System.Drawing.Font("Cascadia Mono", 7.25F);
			txtDisassembly.HideSelection = false;
			txtDisassembly.Location = new System.Drawing.Point(0, 0);
			txtDisassembly.Margin = new Padding(6);
			txtDisassembly.Name = "txtDisassembly";
			txtDisassembly.ReadOnly = true;
			txtDisassembly.Size = new System.Drawing.Size(1267, 569);
			txtDisassembly.TabIndex = 1;
			txtDisassembly.Text = "";
			txtDisassembly.WordWrap = false;
			// 
			// menuDisassembly
			// 
			menuDisassembly.ImageScalingSize = new System.Drawing.Size(32, 32);
			menuDisassembly.Items.AddRange(new ToolStripItem[] { toolStripBreakpoint, toolStripSkip, toolStripGoto, toolStripFind, toolStripFindNext });
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
			btnStep.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStep.Location = new System.Drawing.Point(569, 422);
			btnStep.Margin = new Padding(11, 13, 11, 13);
			btnStep.Name = "btnStep";
			btnStep.Size = new System.Drawing.Size(150, 47);
			btnStep.TabIndex = 2;
			btnStep.Text = "Step";
			btnStep.UseVisualStyleBackColor = true;
			btnStep.Click += btnStep_Click;
			// 
			// btnStop
			// 
			btnStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStop.Location = new System.Drawing.Point(569, 478);
			btnStop.Margin = new Padding(11, 13, 11, 13);
			btnStop.Name = "btnStop";
			btnStop.Size = new System.Drawing.Size(150, 47);
			btnStop.TabIndex = 3;
			btnStop.Text = "Stop";
			btnStop.UseVisualStyleBackColor = true;
			btnStop.Click += btnStop_Click;
			// 
			// btnGo
			// 
			btnGo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnGo.Location = new System.Drawing.Point(569, 533);
			btnGo.Margin = new Padding(11, 13, 11, 13);
			btnGo.Name = "btnGo";
			btnGo.Size = new System.Drawing.Size(150, 47);
			btnGo.TabIndex = 4;
			btnGo.Text = "Go";
			btnGo.UseVisualStyleBackColor = true;
			btnGo.Click += btnGo_Click;
			// 
			// btnReset
			// 
			btnReset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnReset.Location = new System.Drawing.Point(569, 587);
			btnReset.Margin = new Padding(11, 13, 11, 13);
			btnReset.Name = "btnReset";
			btnReset.Size = new System.Drawing.Size(150, 47);
			btnReset.TabIndex = 5;
			btnReset.Text = "Reset";
			btnReset.UseVisualStyleBackColor = true;
			btnReset.Click += btnReset_Click;
			// 
			// txtMemory
			// 
			txtMemory.BackColor = System.Drawing.SystemColors.Window;
			txtMemory.BorderStyle = BorderStyle.FixedSingle;
			txtMemory.ContextMenuStrip = menuMemory;
			txtMemory.DetectUrls = false;
			txtMemory.Dock = DockStyle.Fill;
			txtMemory.Font = new System.Drawing.Font("Cascadia Mono", 7.25F);
			txtMemory.Location = new System.Drawing.Point(0, 0);
			txtMemory.Margin = new Padding(6);
			txtMemory.Name = "txtMemory";
			txtMemory.ReadOnly = true;
			txtMemory.Size = new System.Drawing.Size(1267, 661);
			txtMemory.TabIndex = 6;
			txtMemory.Text = "00000160 0000000000000000 0000000000000000 0000000000000000 0000000000000000   ................................";
			txtMemory.WordWrap = false;
			// 
			// menuMemory
			// 
			menuMemory.ImageScalingSize = new System.Drawing.Size(32, 32);
			menuMemory.Items.AddRange(new ToolStripItem[] { menuMemoryGotoItem, menuMemoryFindItem });
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
			// splitDisassembly
			// 
			splitDisassembly.Dock = DockStyle.Fill;
			splitDisassembly.Location = new System.Drawing.Point(0, 0);
			splitDisassembly.Margin = new Padding(0);
			splitDisassembly.Name = "splitDisassembly";
			splitDisassembly.Orientation = Orientation.Horizontal;
			// 
			// splitDisassembly.Panel1
			// 
			splitDisassembly.Panel1.Controls.Add(txtDisassembly);
			// 
			// splitDisassembly.Panel2
			// 
			splitDisassembly.Panel2.Controls.Add(txtMemory);
			splitDisassembly.Size = new System.Drawing.Size(1267, 1239);
			splitDisassembly.SplitterDistance = 569;
			splitDisassembly.SplitterWidth = 9;
			splitDisassembly.TabIndex = 7;
			// 
			// btnRefresh
			// 
			btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRefresh.Location = new System.Drawing.Point(569, 713);
			btnRefresh.Margin = new Padding(11, 13, 11, 13);
			btnRefresh.Name = "btnRefresh";
			btnRefresh.Size = new System.Drawing.Size(150, 47);
			btnRefresh.TabIndex = 8;
			btnRefresh.Text = "Refresh";
			btnRefresh.UseVisualStyleBackColor = true;
			btnRefresh.Click += btnRefresh_Click;
			// 
			// btnStepOver
			// 
			btnStepOver.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStepOver.Location = new System.Drawing.Point(727, 422);
			btnStepOver.Margin = new Padding(11, 13, 11, 13);
			btnStepOver.Name = "btnStepOver";
			btnStepOver.Size = new System.Drawing.Size(150, 47);
			btnStepOver.TabIndex = 9;
			btnStepOver.Text = "Step Over";
			btnStepOver.UseVisualStyleBackColor = true;
			btnStepOver.Click += btnStepOver_Click;
			// 
			// picPower
			// 
			picPower.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			picPower.Location = new System.Drawing.Point(731, 533);
			picPower.Margin = new Padding(11, 13, 11, 13);
			picPower.Name = "picPower";
			picPower.Size = new System.Drawing.Size(97, 21);
			picPower.TabIndex = 10;
			picPower.TabStop = false;
			// 
			// picDisk
			// 
			picDisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			picDisk.Location = new System.Drawing.Point(731, 574);
			picDisk.Margin = new Padding(11, 13, 11, 13);
			picDisk.Name = "picDisk";
			picDisk.Size = new System.Drawing.Size(97, 21);
			picDisk.TabIndex = 11;
			picDisk.TabStop = false;
			// 
			// btnDisassemble
			// 
			btnDisassemble.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnDisassemble.Location = new System.Drawing.Point(727, 713);
			btnDisassemble.Margin = new Padding(11, 13, 11, 13);
			btnDisassemble.Name = "btnDisassemble";
			btnDisassemble.Size = new System.Drawing.Size(163, 47);
			btnDisassemble.TabIndex = 12;
			btnDisassemble.Text = "Disassemble";
			btnDisassemble.UseVisualStyleBackColor = true;
			btnDisassemble.Click += btnDisassemble_Click;
			// 
			// addressFollowBox
			// 
			addressFollowBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			addressFollowBox.FormattingEnabled = true;
			addressFollowBox.Items.AddRange(new object[] { "(None)", "A0", "A1", "A2", "A3", "A4", "A5", "A6", "SP", "SSP", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "PC" });
			addressFollowBox.Location = new System.Drawing.Point(569, 768);
			addressFollowBox.Margin = new Padding(11, 13, 11, 13);
			addressFollowBox.Name = "addressFollowBox";
			addressFollowBox.Size = new System.Drawing.Size(258, 40);
			addressFollowBox.TabIndex = 25;
			addressFollowBox.SelectionChangeCommitted += addressFollowBox_SelectionChangeCommitted;
			// 
			// txtCopper
			// 
			txtCopper.BackColor = System.Drawing.SystemColors.Window;
			txtCopper.BorderStyle = BorderStyle.FixedSingle;
			txtCopper.DetectUrls = false;
			txtCopper.Dock = DockStyle.Fill;
			txtCopper.Font = new System.Drawing.Font("Cascadia Mono", 7.125F);
			txtCopper.Location = new System.Drawing.Point(4, 2);
			txtCopper.Margin = new Padding(4, 2, 4, 2);
			txtCopper.Name = "txtCopper";
			txtCopper.ReadOnly = true;
			txtCopper.Size = new System.Drawing.Size(538, 493);
			txtCopper.TabIndex = 26;
			txtCopper.Text = "";
			txtCopper.WordWrap = false;
			// 
			// btnInsertDisk
			// 
			btnInsertDisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnInsertDisk.Location = new System.Drawing.Point(569, 815);
			btnInsertDisk.Margin = new Padding(11, 13, 11, 13);
			btnInsertDisk.Name = "btnInsertDisk";
			btnInsertDisk.Size = new System.Drawing.Size(150, 47);
			btnInsertDisk.TabIndex = 27;
			btnInsertDisk.Text = "Insert Disk";
			btnInsertDisk.UseVisualStyleBackColor = true;
			btnInsertDisk.Click += btnInsertDisk_Click;
			// 
			// btnRemoveDisk
			// 
			btnRemoveDisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRemoveDisk.Location = new System.Drawing.Point(727, 815);
			btnRemoveDisk.Margin = new Padding(11, 13, 11, 13);
			btnRemoveDisk.Name = "btnRemoveDisk";
			btnRemoveDisk.Size = new System.Drawing.Size(150, 47);
			btnRemoveDisk.TabIndex = 28;
			btnRemoveDisk.Text = "Remove Disk";
			btnRemoveDisk.UseVisualStyleBackColor = true;
			btnRemoveDisk.Click += btnRemoveDisk_Click;
			// 
			// btnCIAInt
			// 
			btnCIAInt.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnCIAInt.Location = new System.Drawing.Point(569, 932);
			btnCIAInt.Margin = new Padding(11, 13, 11, 13);
			btnCIAInt.Name = "btnCIAInt";
			btnCIAInt.Size = new System.Drawing.Size(150, 47);
			btnCIAInt.TabIndex = 29;
			btnCIAInt.Text = "CIA Int";
			btnCIAInt.UseVisualStyleBackColor = true;
			btnCIAInt.Click += btnCIAInt_Click;
			// 
			// btnIRQ
			// 
			btnIRQ.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnIRQ.Location = new System.Drawing.Point(569, 990);
			btnIRQ.Margin = new Padding(11, 13, 11, 13);
			btnIRQ.Name = "btnIRQ";
			btnIRQ.Size = new System.Drawing.Size(150, 47);
			btnIRQ.TabIndex = 31;
			btnIRQ.Text = "IRQ";
			btnIRQ.UseVisualStyleBackColor = true;
			btnIRQ.Click += btnIRQ_Click;
			// 
			// cbIRQ
			// 
			cbIRQ.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			cbIRQ.FormattingEnabled = true;
			cbIRQ.Items.AddRange(new object[] { "EXTER", "DSKSYNC", "AUD0", "AUD1", "AUD2", "AUD3", "BLIT", "VERTB", "COPPER", "PORTS", "DSKBLK", "SOFTINT" });
			cbIRQ.Location = new System.Drawing.Point(727, 994);
			cbIRQ.Margin = new Padding(11, 13, 11, 13);
			cbIRQ.Name = "cbIRQ";
			cbIRQ.Size = new System.Drawing.Size(234, 40);
			cbIRQ.TabIndex = 32;
			cbIRQ.Text = "BLIT";
			// 
			// cbCIA
			// 
			cbCIA.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			cbCIA.FormattingEnabled = true;
			cbCIA.Items.AddRange(new object[] { "TIMERA", "TIMERB", "TODALARM", "SERIAL", "FLAG" });
			cbCIA.Location = new System.Drawing.Point(727, 937);
			cbCIA.Margin = new Padding(11, 13, 11, 13);
			cbCIA.Name = "cbCIA";
			cbCIA.Size = new System.Drawing.Size(234, 40);
			cbCIA.TabIndex = 33;
			cbCIA.Text = "TIMERA";
			// 
			// cbTypes
			// 
			cbTypes.FormattingEnabled = true;
			cbTypes.Items.AddRange(new object[] { "(None)", "ExecBase", "timerequest", "Library", "Task", "KeyMapResource", "MsgPort", "Unit", "Resident" });
			cbTypes.Location = new System.Drawing.Point(7, 11);
			cbTypes.Margin = new Padding(6);
			cbTypes.Name = "cbTypes";
			cbTypes.Size = new System.Drawing.Size(292, 33);
			cbTypes.TabIndex = 34;
			cbTypes.SelectionChangeCommitted += cbTypes_SelectionChangeCommitted;
			// 
			// lbCallStack
			// 
			lbCallStack.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lbCallStack.BorderStyle = BorderStyle.FixedSingle;
			lbCallStack.ColumnWidth = 83;
			lbCallStack.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			lbCallStack.IntegralHeight = false;
			lbCallStack.Location = new System.Drawing.Point(855, 13);
			lbCallStack.Margin = new Padding(11, 13, 11, 13);
			lbCallStack.MultiColumn = true;
			lbCallStack.Name = "lbCallStack";
			lbCallStack.SelectionMode = SelectionMode.None;
			lbCallStack.Size = new System.Drawing.Size(275, 403);
			lbCallStack.TabIndex = 35;
			lbCallStack.MouseDoubleClick += lbCallStack_MouseDoubleClick;
			// 
			// btnStepOut
			// 
			btnStepOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStepOut.Location = new System.Drawing.Point(727, 478);
			btnStepOut.Margin = new Padding(11, 13, 11, 13);
			btnStepOut.Name = "btnStepOut";
			btnStepOut.Size = new System.Drawing.Size(150, 47);
			btnStepOut.TabIndex = 36;
			btnStepOut.Text = "Step Out";
			btnStepOut.UseVisualStyleBackColor = true;
			btnStepOut.Click += btnStepOut_Click;
			// 
			// btnINTENA
			// 
			btnINTENA.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnINTENA.Location = new System.Drawing.Point(569, 1045);
			btnINTENA.Margin = new Padding(11, 13, 11, 13);
			btnINTENA.Name = "btnINTENA";
			btnINTENA.Size = new System.Drawing.Size(69, 47);
			btnINTENA.TabIndex = 37;
			btnINTENA.Text = "EN";
			btnINTENA.UseVisualStyleBackColor = true;
			btnINTENA.Click += btnINTENA_Click;
			// 
			// lbCustom
			// 
			lbCustom.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			lbCustom.BorderStyle = BorderStyle.FixedSingle;
			lbCustom.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			lbCustom.FormattingEnabled = true;
			lbCustom.Location = new System.Drawing.Point(15, 730);
			lbCustom.Margin = new Padding(11, 13, 11, 13);
			lbCustom.Name = "lbCustom";
			lbCustom.Size = new System.Drawing.Size(544, 477);
			lbCustom.TabIndex = 38;
			// 
			// btnDumpTrace
			// 
			btnDumpTrace.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnDumpTrace.Location = new System.Drawing.Point(727, 659);
			btnDumpTrace.Margin = new Padding(11, 13, 11, 13);
			btnDumpTrace.Name = "btnDumpTrace";
			btnDumpTrace.Size = new System.Drawing.Size(163, 47);
			btnDumpTrace.TabIndex = 39;
			btnDumpTrace.Text = "Dump Trace";
			btnDumpTrace.UseVisualStyleBackColor = true;
			btnDumpTrace.Click += btnDumpTrace_Click;
			// 
			// btnIDEACK
			// 
			btnIDEACK.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnIDEACK.Location = new System.Drawing.Point(880, 1045);
			btnIDEACK.Margin = new Padding(11, 13, 11, 13);
			btnIDEACK.Name = "btnIDEACK";
			btnIDEACK.Size = new System.Drawing.Size(150, 47);
			btnIDEACK.TabIndex = 40;
			btnIDEACK.Text = "IDEACK";
			btnIDEACK.UseVisualStyleBackColor = true;
			btnIDEACK.Click += btnIDEACK_Click;
			// 
			// btnChange
			// 
			btnChange.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnChange.Location = new System.Drawing.Point(569, 870);
			btnChange.Margin = new Padding(11, 13, 11, 13);
			btnChange.Name = "btnChange";
			btnChange.Size = new System.Drawing.Size(150, 47);
			btnChange.TabIndex = 41;
			btnChange.Text = "Change";
			btnChange.UseVisualStyleBackColor = true;
			btnChange.Click += btnChange_Click;
			// 
			// radioDF0
			// 
			radioDF0.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			radioDF0.AutoSize = true;
			radioDF0.Checked = true;
			radioDF0.Location = new System.Drawing.Point(728, 883);
			radioDF0.Margin = new Padding(11, 13, 11, 13);
			radioDF0.Name = "radioDF0";
			radioDF0.Size = new System.Drawing.Size(27, 26);
			radioDF0.TabIndex = 42;
			radioDF0.TabStop = true;
			radioDF0.UseVisualStyleBackColor = true;
			radioDF0.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF1
			// 
			radioDF1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			radioDF1.AutoSize = true;
			radioDF1.Location = new System.Drawing.Point(758, 883);
			radioDF1.Margin = new Padding(11, 13, 11, 13);
			radioDF1.Name = "radioDF1";
			radioDF1.Size = new System.Drawing.Size(27, 26);
			radioDF1.TabIndex = 43;
			radioDF1.TabStop = true;
			radioDF1.UseVisualStyleBackColor = true;
			radioDF1.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF2
			// 
			radioDF2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			radioDF2.AutoSize = true;
			radioDF2.Location = new System.Drawing.Point(789, 883);
			radioDF2.Margin = new Padding(11, 13, 11, 13);
			radioDF2.Name = "radioDF2";
			radioDF2.Size = new System.Drawing.Size(27, 26);
			radioDF2.TabIndex = 44;
			radioDF2.TabStop = true;
			radioDF2.UseVisualStyleBackColor = true;
			radioDF2.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// radioDF3
			// 
			radioDF3.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			radioDF3.AutoSize = true;
			radioDF3.Location = new System.Drawing.Point(821, 883);
			radioDF3.Margin = new Padding(11, 13, 11, 13);
			radioDF3.Name = "radioDF3";
			radioDF3.Size = new System.Drawing.Size(27, 26);
			radioDF3.TabIndex = 45;
			radioDF3.TabStop = true;
			radioDF3.UseVisualStyleBackColor = true;
			radioDF3.CheckedChanged += radioDFx_CheckedChanged;
			// 
			// btnGfxScan
			// 
			btnGfxScan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnGfxScan.Location = new System.Drawing.Point(569, 1105);
			btnGfxScan.Margin = new Padding(11, 13, 11, 13);
			btnGfxScan.Name = "btnGfxScan";
			btnGfxScan.Size = new System.Drawing.Size(150, 47);
			btnGfxScan.TabIndex = 46;
			btnGfxScan.Text = "Gfx Scan";
			btnGfxScan.UseVisualStyleBackColor = true;
			btnGfxScan.Click += btnGfxScan_Click;
			// 
			// btnClearBBUSY
			// 
			btnClearBBUSY.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnClearBBUSY.Location = new System.Drawing.Point(727, 1045);
			btnClearBBUSY.Margin = new Padding(11, 13, 11, 13);
			btnClearBBUSY.Name = "btnClearBBUSY";
			btnClearBBUSY.Size = new System.Drawing.Size(150, 47);
			btnClearBBUSY.TabIndex = 47;
			btnClearBBUSY.Text = "~BBUSY";
			btnClearBBUSY.UseVisualStyleBackColor = true;
			btnClearBBUSY.Click += btnClearBBUSY_Click;
			// 
			// btnCribSheet
			// 
			btnCribSheet.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnCribSheet.Location = new System.Drawing.Point(989, 1105);
			btnCribSheet.Margin = new Padding(6);
			btnCribSheet.Name = "btnCribSheet";
			btnCribSheet.Size = new System.Drawing.Size(139, 47);
			btnCribSheet.TabIndex = 48;
			btnCribSheet.Text = "Crib Sheet";
			btnCribSheet.UseVisualStyleBackColor = true;
			btnCribSheet.Click += btnCribSheet_Click;
			// 
			// splitContent
			// 
			splitContent.Dock = DockStyle.Fill;
			splitContent.Location = new System.Drawing.Point(0, 0);
			splitContent.Margin = new Padding(4, 2, 4, 2);
			splitContent.Name = "splitContent";
			// 
			// splitContent.Panel1
			// 
			splitContent.Panel1.Controls.Add(splitDisassembly);
			// 
			// splitContent.Panel2
			// 
			splitContent.Panel2.Controls.Add(pnlButtons);
			splitContent.Size = new System.Drawing.Size(2427, 1239);
			splitContent.SplitterDistance = 1267;
			splitContent.SplitterWidth = 9;
			splitContent.TabIndex = 49;
			// 
			// pnlButtons
			// 
			pnlButtons.Controls.Add(lbCallStack);
			pnlButtons.Controls.Add(btnPluginReload);
			pnlButtons.Controls.Add(btnGenDisassemblies);
			pnlButtons.Controls.Add(tbClock);
			pnlButtons.Controls.Add(btnDMAExplorer);
			pnlButtons.Controls.Add(btnAnalyseFlow);
			pnlButtons.Controls.Add(btnStringScan);
			pnlButtons.Controls.Add(btnINTDIS);
			pnlButtons.Controls.Add(tbCommand);
			pnlButtons.Controls.Add(btnReadyDisk);
			pnlButtons.Controls.Add(btnCribSheet);
			pnlButtons.Controls.Add(btnClearBBUSY);
			pnlButtons.Controls.Add(btnGfxScan);
			pnlButtons.Controls.Add(radioDF3);
			pnlButtons.Controls.Add(radioDF2);
			pnlButtons.Controls.Add(radioDF1);
			pnlButtons.Controls.Add(radioDF0);
			pnlButtons.Controls.Add(btnChange);
			pnlButtons.Controls.Add(btnIDEACK);
			pnlButtons.Controls.Add(btnDumpTrace);
			pnlButtons.Controls.Add(lbCustom);
			pnlButtons.Controls.Add(lbIntvec);
			pnlButtons.Controls.Add(btnINTENA);
			pnlButtons.Controls.Add(btnStepOut);
			pnlButtons.Controls.Add(cbCIA);
			pnlButtons.Controls.Add(cbIRQ);
			pnlButtons.Controls.Add(btnIRQ);
			pnlButtons.Controls.Add(btnCIAInt);
			pnlButtons.Controls.Add(btnRemoveDisk);
			pnlButtons.Controls.Add(btnInsertDisk);
			pnlButtons.Controls.Add(addressFollowBox);
			pnlButtons.Controls.Add(btnDisassemble);
			pnlButtons.Controls.Add(picDisk);
			pnlButtons.Controls.Add(picPower);
			pnlButtons.Controls.Add(btnStepOver);
			pnlButtons.Controls.Add(btnRefresh);
			pnlButtons.Controls.Add(btnReset);
			pnlButtons.Controls.Add(btnGo);
			pnlButtons.Controls.Add(btnStop);
			pnlButtons.Controls.Add(btnStep);
			pnlButtons.Controls.Add(lbRegisters);
			pnlButtons.Controls.Add(tabControl1);
			pnlButtons.Dock = DockStyle.Fill;
			pnlButtons.Location = new System.Drawing.Point(0, 0);
			pnlButtons.Margin = new Padding(6);
			pnlButtons.Name = "pnlButtons";
			pnlButtons.Size = new System.Drawing.Size(1151, 1239);
			pnlButtons.TabIndex = 0;
			// 
			// btnPluginReload
			// 
			btnPluginReload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnPluginReload.Location = new System.Drawing.Point(939, 1158);
			btnPluginReload.Margin = new Padding(6);
			btnPluginReload.Name = "btnPluginReload";
			btnPluginReload.Size = new System.Drawing.Size(191, 47);
			btnPluginReload.TabIndex = 58;
			btnPluginReload.Text = "Reload Plugins";
			btnPluginReload.UseVisualStyleBackColor = true;
			btnPluginReload.Click += btnPluginReload_Click;
			// 
			// btnGenDisassemblies
			// 
			btnGenDisassemblies.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnGenDisassemblies.Location = new System.Drawing.Point(900, 713);
			btnGenDisassemblies.Margin = new Padding(6);
			btnGenDisassemblies.Name = "btnGenDisassemblies";
			btnGenDisassemblies.Size = new System.Drawing.Size(230, 47);
			btnGenDisassemblies.TabIndex = 57;
			btnGenDisassemblies.Text = "Gen Disassemblies";
			btnGenDisassemblies.UseVisualStyleBackColor = true;
			btnGenDisassemblies.Click += btnGenDisassemblies_Click;
			// 
			// tbClock
			// 
			tbClock.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			tbClock.BackColor = System.Drawing.SystemColors.Window;
			tbClock.BorderStyle = BorderStyle.FixedSingle;
			tbClock.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			tbClock.Location = new System.Drawing.Point(731, 608);
			tbClock.Margin = new Padding(6);
			tbClock.Multiline = true;
			tbClock.Name = "tbClock";
			tbClock.ReadOnly = true;
			tbClock.Size = new System.Drawing.Size(386, 43);
			tbClock.TabIndex = 56;
			// 
			// btnDMAExplorer
			// 
			btnDMAExplorer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnDMAExplorer.Location = new System.Drawing.Point(880, 1105);
			btnDMAExplorer.Margin = new Padding(6);
			btnDMAExplorer.Name = "btnDMAExplorer";
			btnDMAExplorer.Size = new System.Drawing.Size(100, 47);
			btnDMAExplorer.TabIndex = 55;
			btnDMAExplorer.Text = "DMA Explorer";
			btnDMAExplorer.UseVisualStyleBackColor = true;
			btnDMAExplorer.Click += btnDMAExplorer_Click;
			// 
			// btnAnalyseFlow
			// 
			btnAnalyseFlow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnAnalyseFlow.Location = new System.Drawing.Point(569, 659);
			btnAnalyseFlow.Margin = new Padding(11, 13, 11, 13);
			btnAnalyseFlow.Name = "btnAnalyseFlow";
			btnAnalyseFlow.Size = new System.Drawing.Size(150, 47);
			btnAnalyseFlow.TabIndex = 54;
			btnAnalyseFlow.Text = "Analyse";
			btnAnalyseFlow.UseVisualStyleBackColor = true;
			btnAnalyseFlow.Click += btnAnalyseFlow_Click;
			// 
			// btnStringScan
			// 
			btnStringScan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStringScan.Location = new System.Drawing.Point(727, 1105);
			btnStringScan.Margin = new Padding(11, 13, 11, 13);
			btnStringScan.Name = "btnStringScan";
			btnStringScan.Size = new System.Drawing.Size(150, 47);
			btnStringScan.TabIndex = 53;
			btnStringScan.Text = "String Scan";
			btnStringScan.UseVisualStyleBackColor = true;
			btnStringScan.Click += btnStringScan_Click;
			// 
			// btnINTDIS
			// 
			btnINTDIS.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnINTDIS.Location = new System.Drawing.Point(651, 1045);
			btnINTDIS.Margin = new Padding(11, 13, 11, 13);
			btnINTDIS.Name = "btnINTDIS";
			btnINTDIS.Size = new System.Drawing.Size(69, 47);
			btnINTDIS.TabIndex = 52;
			btnINTDIS.Text = "~EN";
			btnINTDIS.UseVisualStyleBackColor = true;
			btnINTDIS.Click += btnINTDIS_Click;
			// 
			// tbCommand
			// 
			tbCommand.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			tbCommand.BackColor = System.Drawing.SystemColors.WindowText;
			tbCommand.BorderStyle = BorderStyle.FixedSingle;
			tbCommand.CausesValidation = false;
			tbCommand.ForeColor = System.Drawing.SystemColors.Window;
			tbCommand.Location = new System.Drawing.Point(569, 1165);
			tbCommand.Margin = new Padding(6);
			tbCommand.Name = "tbCommand";
			tbCommand.PlaceholderText = ">";
			tbCommand.Size = new System.Drawing.Size(360, 39);
			tbCommand.TabIndex = 51;
			tbCommand.KeyDown += tbCommand_KeyDown;
			// 
			// btnReadyDisk
			// 
			btnReadyDisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnReadyDisk.Location = new System.Drawing.Point(883, 815);
			btnReadyDisk.Margin = new Padding(11, 13, 11, 13);
			btnReadyDisk.Name = "btnReadyDisk";
			btnReadyDisk.Size = new System.Drawing.Size(150, 47);
			btnReadyDisk.TabIndex = 50;
			btnReadyDisk.Text = "Ready";
			btnReadyDisk.UseVisualStyleBackColor = true;
			btnReadyDisk.Click += btnReadyDisk_Click;
			// 
			// lbIntvec
			// 
			lbIntvec.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			lbIntvec.BorderStyle = BorderStyle.FixedSingle;
			lbIntvec.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			lbIntvec.FormattingEnabled = true;
			lbIntvec.Location = new System.Drawing.Point(11, 546);
			lbIntvec.Margin = new Padding(2, 0, 2, 0);
			lbIntvec.Name = "lbIntvec";
			lbIntvec.Size = new System.Drawing.Size(547, 177);
			lbIntvec.TabIndex = 27;
			lbIntvec.MouseDoubleClick += lbIntvec_MouseDoubleClick;
			// 
			// tabControl1
			// 
			tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			tabControl1.Controls.Add(tabCopper);
			tabControl1.Controls.Add(tabExec);
			tabControl1.Controls.Add(tabVectors);
			tabControl1.Controls.Add(tabLibraries);
			tabControl1.Controls.Add(tabAllocations);
			tabControl1.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			tabControl1.Location = new System.Drawing.Point(4, 2);
			tabControl1.Margin = new Padding(4, 2, 4, 2);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new System.Drawing.Size(562, 544);
			tabControl1.TabIndex = 28;
			// 
			// tabCopper
			// 
			tabCopper.Controls.Add(txtCopper);
			tabCopper.Location = new System.Drawing.Point(8, 39);
			tabCopper.Margin = new Padding(4, 2, 4, 2);
			tabCopper.Name = "tabCopper";
			tabCopper.Padding = new Padding(4, 2, 4, 2);
			tabCopper.Size = new System.Drawing.Size(546, 497);
			tabCopper.TabIndex = 0;
			tabCopper.Text = "Copper";
			tabCopper.UseVisualStyleBackColor = true;
			// 
			// tabExec
			// 
			tabExec.Controls.Add(txtExecBase);
			tabExec.Controls.Add(cbTypes);
			tabExec.Location = new System.Drawing.Point(8, 39);
			tabExec.Margin = new Padding(4, 2, 4, 2);
			tabExec.Name = "tabExec";
			tabExec.Padding = new Padding(4, 2, 4, 2);
			tabExec.Size = new System.Drawing.Size(560, 497);
			tabExec.TabIndex = 1;
			tabExec.Text = "Object";
			tabExec.UseVisualStyleBackColor = true;
			// 
			// txtExecBase
			// 
			txtExecBase.BackColor = System.Drawing.SystemColors.Window;
			txtExecBase.BorderStyle = BorderStyle.FixedSingle;
			txtExecBase.DetectUrls = false;
			txtExecBase.Dock = DockStyle.Bottom;
			txtExecBase.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			txtExecBase.Location = new System.Drawing.Point(4, 52);
			txtExecBase.Margin = new Padding(4, 2, 4, 2);
			txtExecBase.Name = "txtExecBase";
			txtExecBase.ReadOnly = true;
			txtExecBase.Size = new System.Drawing.Size(552, 443);
			txtExecBase.TabIndex = 0;
			txtExecBase.Text = "";
			txtExecBase.WordWrap = false;
			// 
			// tabVectors
			// 
			tabVectors.Controls.Add(txtVectors);
			tabVectors.Location = new System.Drawing.Point(8, 39);
			tabVectors.Margin = new Padding(4, 2, 4, 2);
			tabVectors.Name = "tabVectors";
			tabVectors.Padding = new Padding(4, 2, 4, 2);
			tabVectors.Size = new System.Drawing.Size(560, 497);
			tabVectors.TabIndex = 2;
			tabVectors.Text = "Vectors";
			tabVectors.UseVisualStyleBackColor = true;
			// 
			// txtVectors
			// 
			txtVectors.BackColor = System.Drawing.SystemColors.Window;
			txtVectors.BorderStyle = BorderStyle.FixedSingle;
			txtVectors.DetectUrls = false;
			txtVectors.Dock = DockStyle.Fill;
			txtVectors.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			txtVectors.Location = new System.Drawing.Point(4, 2);
			txtVectors.Margin = new Padding(4, 2, 4, 2);
			txtVectors.Name = "txtVectors";
			txtVectors.ReadOnly = true;
			txtVectors.Size = new System.Drawing.Size(552, 493);
			txtVectors.TabIndex = 0;
			txtVectors.Text = "";
			txtVectors.WordWrap = false;
			// 
			// tabLibraries
			// 
			tabLibraries.Controls.Add(txtLibraries);
			tabLibraries.Location = new System.Drawing.Point(8, 39);
			tabLibraries.Margin = new Padding(4, 2, 4, 2);
			tabLibraries.Name = "tabLibraries";
			tabLibraries.Padding = new Padding(4, 2, 4, 2);
			tabLibraries.Size = new System.Drawing.Size(560, 497);
			tabLibraries.TabIndex = 3;
			tabLibraries.Text = "Libraries";
			tabLibraries.UseVisualStyleBackColor = true;
			// 
			// txtLibraries
			// 
			txtLibraries.BackColor = System.Drawing.SystemColors.Window;
			txtLibraries.BorderStyle = BorderStyle.FixedSingle;
			txtLibraries.DetectUrls = false;
			txtLibraries.Dock = DockStyle.Fill;
			txtLibraries.Location = new System.Drawing.Point(4, 2);
			txtLibraries.Margin = new Padding(4, 2, 4, 2);
			txtLibraries.Name = "txtLibraries";
			txtLibraries.ReadOnly = true;
			txtLibraries.Size = new System.Drawing.Size(552, 493);
			txtLibraries.TabIndex = 0;
			txtLibraries.Text = "";
			txtLibraries.WordWrap = false;
			// 
			// tabAllocations
			// 
			tabAllocations.Controls.Add(txtAllocations);
			tabAllocations.Location = new System.Drawing.Point(8, 39);
			tabAllocations.Margin = new Padding(4, 2, 4, 2);
			tabAllocations.Name = "tabAllocations";
			tabAllocations.Size = new System.Drawing.Size(560, 497);
			tabAllocations.TabIndex = 4;
			tabAllocations.Text = "Allocations";
			tabAllocations.UseVisualStyleBackColor = true;
			// 
			// txtAllocations
			// 
			txtAllocations.BackColor = System.Drawing.SystemColors.Window;
			txtAllocations.BorderStyle = BorderStyle.FixedSingle;
			txtAllocations.DetectUrls = false;
			txtAllocations.Dock = DockStyle.Fill;
			txtAllocations.Location = new System.Drawing.Point(0, 0);
			txtAllocations.Margin = new Padding(4, 2, 4, 2);
			txtAllocations.Name = "txtAllocations";
			txtAllocations.ReadOnly = true;
			txtAllocations.Size = new System.Drawing.Size(560, 497);
			txtAllocations.TabIndex = 1;
			txtAllocations.Text = "";
			txtAllocations.WordWrap = false;
			// 
			// Jammy
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(2427, 1239);
			Controls.Add(splitContent);
			Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
			Margin = new Padding(6);
			Name = "Jammy";
			Text = "Jammy";
			FormClosing += Form1_FormClosing;
			menuDisassembly.ResumeLayout(false);
			menuMemory.ResumeLayout(false);
			splitDisassembly.Panel1.ResumeLayout(false);
			splitDisassembly.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitDisassembly).EndInit();
			splitDisassembly.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)picPower).EndInit();
			((System.ComponentModel.ISupportInitialize)picDisk).EndInit();
			splitContent.Panel1.ResumeLayout(false);
			splitContent.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContent).EndInit();
			splitContent.ResumeLayout(false);
			pnlButtons.ResumeLayout(false);
			pnlButtons.PerformLayout();
			tabControl1.ResumeLayout(false);
			tabCopper.ResumeLayout(false);
			tabExec.ResumeLayout(false);
			tabVectors.ResumeLayout(false);
			tabLibraries.ResumeLayout(false);
			tabAllocations.ResumeLayout(false);
			ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.ListBox lbRegisters;
		private System.Windows.Forms.RichTextBox txtDisassembly;
		private System.Windows.Forms.Button btnStep;
		private System.Windows.Forms.Button btnStop;
		private System.Windows.Forms.Button btnGo;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.RichTextBox txtMemory;
		private System.Windows.Forms.SplitContainer splitDisassembly;
		private System.Windows.Forms.Button btnRefresh;
		private System.Windows.Forms.Button btnStepOver;
		private System.Windows.Forms.PictureBox picPower;
		private System.Windows.Forms.PictureBox picDisk;
		private System.Windows.Forms.Button btnDisassemble;
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
		private System.Windows.Forms.SplitContainer splitContent;
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
		private System.Windows.Forms.TabPage tabLibraries;
		private System.Windows.Forms.RichTextBox txtLibraries;
		private System.Windows.Forms.TabPage tabAllocations;
		private System.Windows.Forms.RichTextBox txtAllocations;
		private System.Windows.Forms.Button btnPluginReload;
		private System.Windows.Forms.Panel pnlButtons;
	}
}

