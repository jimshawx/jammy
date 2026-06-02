
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
			radioButton10 = new RadioButton();
			radioButton11 = new RadioButton();
			radioButton12 = new RadioButton();
			radioButton13 = new RadioButton();
			radioButton14 = new RadioButton();
			radioButton15 = new RadioButton();
			radioButton16 = new RadioButton();
			radioButton17 = new RadioButton();
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
			lbRegisters.Location = new System.Drawing.Point(499, 13);
			lbRegisters.Margin = new Padding(11, 13, 11, 13);
			lbRegisters.MultiColumn = true;
			lbRegisters.Name = "lbRegisters";
			lbRegisters.SelectionMode = SelectionMode.None;
			lbRegisters.Size = new System.Drawing.Size(275, 402);
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
			txtDisassembly.Margin = new Padding(5, 6, 5, 6);
			txtDisassembly.Name = "txtDisassembly";
			txtDisassembly.ReadOnly = true;
			txtDisassembly.Size = new System.Drawing.Size(643, 568);
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
			btnStep.Location = new System.Drawing.Point(499, 423);
			btnStep.Margin = new Padding(11, 13, 11, 13);
			btnStep.Name = "btnStep";
			btnStep.Size = new System.Drawing.Size(150, 48);
			btnStep.TabIndex = 2;
			btnStep.Text = "Step";
			btnStep.UseVisualStyleBackColor = true;
			btnStep.Click += btnStep_Click;
			// 
			// btnStop
			// 
			btnStop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStop.Location = new System.Drawing.Point(499, 478);
			btnStop.Margin = new Padding(11, 13, 11, 13);
			btnStop.Name = "btnStop";
			btnStop.Size = new System.Drawing.Size(150, 48);
			btnStop.TabIndex = 3;
			btnStop.Text = "Stop";
			btnStop.UseVisualStyleBackColor = true;
			btnStop.Click += btnStop_Click;
			// 
			// btnGo
			// 
			btnGo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnGo.Location = new System.Drawing.Point(499, 534);
			btnGo.Margin = new Padding(11, 13, 11, 13);
			btnGo.Name = "btnGo";
			btnGo.Size = new System.Drawing.Size(150, 48);
			btnGo.TabIndex = 4;
			btnGo.Text = "Go";
			btnGo.UseVisualStyleBackColor = true;
			btnGo.Click += btnGo_Click;
			// 
			// btnReset
			// 
			btnReset.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnReset.Location = new System.Drawing.Point(499, 586);
			btnReset.Margin = new Padding(11, 13, 11, 13);
			btnReset.Name = "btnReset";
			btnReset.Size = new System.Drawing.Size(150, 48);
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
			txtMemory.Margin = new Padding(5, 6, 5, 6);
			txtMemory.Name = "txtMemory";
			txtMemory.ReadOnly = true;
			txtMemory.Size = new System.Drawing.Size(643, 658);
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
			splitDisassembly.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
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
			splitDisassembly.Size = new System.Drawing.Size(643, 1235);
			splitDisassembly.SplitterDistance = 568;
			splitDisassembly.SplitterWidth = 9;
			splitDisassembly.TabIndex = 7;
			// 
			// btnRefresh
			// 
			btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRefresh.Location = new System.Drawing.Point(499, 713);
			btnRefresh.Margin = new Padding(11, 13, 11, 13);
			btnRefresh.Name = "btnRefresh";
			btnRefresh.Size = new System.Drawing.Size(150, 48);
			btnRefresh.TabIndex = 8;
			btnRefresh.Text = "Refresh";
			btnRefresh.UseVisualStyleBackColor = true;
			btnRefresh.Click += btnRefresh_Click;
			// 
			// btnStepOver
			// 
			btnStepOver.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStepOver.Location = new System.Drawing.Point(657, 423);
			btnStepOver.Margin = new Padding(11, 13, 11, 13);
			btnStepOver.Name = "btnStepOver";
			btnStepOver.Size = new System.Drawing.Size(150, 48);
			btnStepOver.TabIndex = 9;
			btnStepOver.Text = "Step Over";
			btnStepOver.UseVisualStyleBackColor = true;
			btnStepOver.Click += btnStepOver_Click;
			// 
			// picPower
			// 
			picPower.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			picPower.Location = new System.Drawing.Point(661, 534);
			picPower.Margin = new Padding(11, 13, 11, 13);
			picPower.Name = "picPower";
			picPower.Size = new System.Drawing.Size(96, 22);
			picPower.TabIndex = 10;
			picPower.TabStop = false;
			// 
			// picDisk
			// 
			picDisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			picDisk.Location = new System.Drawing.Point(661, 574);
			picDisk.Margin = new Padding(11, 13, 11, 13);
			picDisk.Name = "picDisk";
			picDisk.Size = new System.Drawing.Size(96, 22);
			picDisk.TabIndex = 11;
			picDisk.TabStop = false;
			// 
			// btnDisassemble
			// 
			btnDisassemble.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnDisassemble.Location = new System.Drawing.Point(657, 713);
			btnDisassemble.Margin = new Padding(11, 13, 11, 13);
			btnDisassemble.Name = "btnDisassemble";
			btnDisassemble.Size = new System.Drawing.Size(163, 48);
			btnDisassemble.TabIndex = 12;
			btnDisassemble.Text = "Disassemble";
			btnDisassemble.UseVisualStyleBackColor = true;
			btnDisassemble.Click += btnDisassemble_Click;
			// 
			// radioButton10
			// 
			radioButton10.AutoSize = true;
			radioButton10.Location = new System.Drawing.Point(0, 0);
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
			radioButton11.Location = new System.Drawing.Point(0, 0);
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
			radioButton12.Location = new System.Drawing.Point(0, 0);
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
			radioButton13.Location = new System.Drawing.Point(0, 0);
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
			radioButton14.Location = new System.Drawing.Point(0, 0);
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
			radioButton15.Location = new System.Drawing.Point(0, 0);
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
			radioButton16.Location = new System.Drawing.Point(0, 0);
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
			radioButton17.Location = new System.Drawing.Point(0, 0);
			radioButton17.Name = "radioButton17";
			radioButton17.Size = new System.Drawing.Size(100, 19);
			radioButton17.TabIndex = 34;
			radioButton17.TabStop = true;
			radioButton17.Text = "radioButton17";
			radioButton17.UseVisualStyleBackColor = true;
			// 
			// addressFollowBox
			// 
			addressFollowBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			addressFollowBox.FormattingEnabled = true;
			addressFollowBox.Items.AddRange(new object[] { "(None)", "A0", "A1", "A2", "A3", "A4", "A5", "A6", "SP", "SSP", "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "PC" });
			addressFollowBox.Location = new System.Drawing.Point(499, 769);
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
			txtCopper.Font = new System.Drawing.Font("Cascadia Mono", 7.25F);
			txtCopper.Location = new System.Drawing.Point(3, 2);
			txtCopper.Margin = new Padding(3, 2, 3, 2);
			txtCopper.Name = "txtCopper";
			txtCopper.ReadOnly = true;
			txtCopper.Size = new System.Drawing.Size(471, 492);
			txtCopper.TabIndex = 26;
			txtCopper.Text = "";
			txtCopper.WordWrap = false;
			// 
			// btnInsertDisk
			// 
			btnInsertDisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnInsertDisk.Location = new System.Drawing.Point(499, 816);
			btnInsertDisk.Margin = new Padding(11, 13, 11, 13);
			btnInsertDisk.Name = "btnInsertDisk";
			btnInsertDisk.Size = new System.Drawing.Size(150, 48);
			btnInsertDisk.TabIndex = 27;
			btnInsertDisk.Text = "Insert Disk";
			btnInsertDisk.UseVisualStyleBackColor = true;
			btnInsertDisk.Click += btnInsertDisk_Click;
			// 
			// btnRemoveDisk
			// 
			btnRemoveDisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRemoveDisk.Location = new System.Drawing.Point(657, 816);
			btnRemoveDisk.Margin = new Padding(11, 13, 11, 13);
			btnRemoveDisk.Name = "btnRemoveDisk";
			btnRemoveDisk.Size = new System.Drawing.Size(150, 48);
			btnRemoveDisk.TabIndex = 28;
			btnRemoveDisk.Text = "Remove Disk";
			btnRemoveDisk.UseVisualStyleBackColor = true;
			btnRemoveDisk.Click += btnRemoveDisk_Click;
			// 
			// btnCIAInt
			// 
			btnCIAInt.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnCIAInt.Location = new System.Drawing.Point(499, 932);
			btnCIAInt.Margin = new Padding(11, 13, 11, 13);
			btnCIAInt.Name = "btnCIAInt";
			btnCIAInt.Size = new System.Drawing.Size(150, 48);
			btnCIAInt.TabIndex = 29;
			btnCIAInt.Text = "CIA Int";
			btnCIAInt.UseVisualStyleBackColor = true;
			btnCIAInt.Click += btnCIAInt_Click;
			// 
			// btnIRQ
			// 
			btnIRQ.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnIRQ.Location = new System.Drawing.Point(499, 989);
			btnIRQ.Margin = new Padding(11, 13, 11, 13);
			btnIRQ.Name = "btnIRQ";
			btnIRQ.Size = new System.Drawing.Size(150, 48);
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
			cbIRQ.Location = new System.Drawing.Point(657, 994);
			cbIRQ.Margin = new Padding(11, 13, 11, 13);
			cbIRQ.Name = "cbIRQ";
			cbIRQ.Size = new System.Drawing.Size(235, 40);
			cbIRQ.TabIndex = 32;
			cbIRQ.Text = "BLIT";
			// 
			// cbCIA
			// 
			cbCIA.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			cbCIA.FormattingEnabled = true;
			cbCIA.Items.AddRange(new object[] { "TIMERA", "TIMERB", "TODALARM", "SERIAL", "FLAG" });
			cbCIA.Location = new System.Drawing.Point(657, 937);
			cbCIA.Margin = new Padding(11, 13, 11, 13);
			cbCIA.Name = "cbCIA";
			cbCIA.Size = new System.Drawing.Size(235, 40);
			cbCIA.TabIndex = 33;
			cbCIA.Text = "TIMERA";
			// 
			// cbTypes
			// 
			cbTypes.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			cbTypes.FormattingEnabled = true;
			cbTypes.Items.AddRange(new object[] { "(None)", "ExecBase", "timerequest", "Library", "Task", "KeyMapResource", "MsgPort", "Unit", "Resident" });
			cbTypes.Location = new System.Drawing.Point(0, 0);
			cbTypes.Margin = new Padding(5, 6, 5, 6);
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
			lbCallStack.Location = new System.Drawing.Point(786, 13);
			lbCallStack.Margin = new Padding(11, 13, 11, 13);
			lbCallStack.MultiColumn = true;
			lbCallStack.Name = "lbCallStack";
			lbCallStack.SelectionMode = SelectionMode.None;
			lbCallStack.Size = new System.Drawing.Size(275, 402);
			lbCallStack.TabIndex = 35;
			lbCallStack.MouseDoubleClick += lbCallStack_MouseDoubleClick;
			// 
			// btnStepOut
			// 
			btnStepOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStepOut.Location = new System.Drawing.Point(657, 478);
			btnStepOut.Margin = new Padding(11, 13, 11, 13);
			btnStepOut.Name = "btnStepOut";
			btnStepOut.Size = new System.Drawing.Size(150, 48);
			btnStepOut.TabIndex = 36;
			btnStepOut.Text = "Step Out";
			btnStepOut.UseVisualStyleBackColor = true;
			btnStepOut.Click += btnStepOut_Click;
			// 
			// btnINTENA
			// 
			btnINTENA.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnINTENA.Location = new System.Drawing.Point(499, 1046);
			btnINTENA.Margin = new Padding(11, 13, 11, 13);
			btnINTENA.Name = "btnINTENA";
			btnINTENA.Size = new System.Drawing.Size(68, 48);
			btnINTENA.TabIndex = 37;
			btnINTENA.Text = "EN";
			btnINTENA.UseVisualStyleBackColor = true;
			btnINTENA.Click += btnINTENA_Click;
			// 
			// lbCustom
			// 
			lbCustom.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			lbCustom.BorderStyle = BorderStyle.FixedSingle;
			lbCustom.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			lbCustom.FormattingEnabled = true;
			lbCustom.Location = new System.Drawing.Point(14, 729);
			lbCustom.Margin = new Padding(11, 13, 11, 13);
			lbCustom.Name = "lbCustom";
			lbCustom.Size = new System.Drawing.Size(474, 502);
			lbCustom.TabIndex = 38;
			// 
			// btnDumpTrace
			// 
			btnDumpTrace.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnDumpTrace.Location = new System.Drawing.Point(657, 659);
			btnDumpTrace.Margin = new Padding(11, 13, 11, 13);
			btnDumpTrace.Name = "btnDumpTrace";
			btnDumpTrace.Size = new System.Drawing.Size(163, 48);
			btnDumpTrace.TabIndex = 39;
			btnDumpTrace.Text = "Dump Trace";
			btnDumpTrace.UseVisualStyleBackColor = true;
			btnDumpTrace.Click += btnDumpTrace_Click;
			// 
			// btnIDEACK
			// 
			btnIDEACK.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnIDEACK.Location = new System.Drawing.Point(810, 1046);
			btnIDEACK.Margin = new Padding(11, 13, 11, 13);
			btnIDEACK.Name = "btnIDEACK";
			btnIDEACK.Size = new System.Drawing.Size(150, 48);
			btnIDEACK.TabIndex = 40;
			btnIDEACK.Text = "IDEACK";
			btnIDEACK.UseVisualStyleBackColor = true;
			btnIDEACK.Click += btnIDEACK_Click;
			// 
			// btnChange
			// 
			btnChange.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnChange.Location = new System.Drawing.Point(499, 871);
			btnChange.Margin = new Padding(11, 13, 11, 13);
			btnChange.Name = "btnChange";
			btnChange.Size = new System.Drawing.Size(150, 48);
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
			radioDF0.Location = new System.Drawing.Point(657, 884);
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
			radioDF1.Location = new System.Drawing.Point(688, 884);
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
			radioDF2.Location = new System.Drawing.Point(719, 884);
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
			radioDF3.Location = new System.Drawing.Point(750, 884);
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
			btnGfxScan.Location = new System.Drawing.Point(499, 1105);
			btnGfxScan.Margin = new Padding(11, 13, 11, 13);
			btnGfxScan.Name = "btnGfxScan";
			btnGfxScan.Size = new System.Drawing.Size(150, 48);
			btnGfxScan.TabIndex = 46;
			btnGfxScan.Text = "Gfx Scan";
			btnGfxScan.UseVisualStyleBackColor = true;
			btnGfxScan.Click += btnGfxScan_Click;
			// 
			// btnClearBBUSY
			// 
			btnClearBBUSY.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnClearBBUSY.Location = new System.Drawing.Point(657, 1046);
			btnClearBBUSY.Margin = new Padding(11, 13, 11, 13);
			btnClearBBUSY.Name = "btnClearBBUSY";
			btnClearBBUSY.Size = new System.Drawing.Size(150, 48);
			btnClearBBUSY.TabIndex = 47;
			btnClearBBUSY.Text = "~BBUSY";
			btnClearBBUSY.UseVisualStyleBackColor = true;
			btnClearBBUSY.Click += btnClearBBUSY_Click;
			// 
			// btnCribSheet
			// 
			btnCribSheet.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnCribSheet.Location = new System.Drawing.Point(920, 1105);
			btnCribSheet.Margin = new Padding(5, 6, 5, 6);
			btnCribSheet.Name = "btnCribSheet";
			btnCribSheet.Size = new System.Drawing.Size(140, 48);
			btnCribSheet.TabIndex = 48;
			btnCribSheet.Text = "Crib Sheet";
			btnCribSheet.UseVisualStyleBackColor = true;
			btnCribSheet.Click += btnCribSheet_Click;
			// 
			// splitContent
			// 
			splitContent.Dock = DockStyle.Fill;
			splitContent.Location = new System.Drawing.Point(0, 0);
			splitContent.Margin = new Padding(3, 2, 3, 2);
			splitContent.Name = "splitContent";
			// 
			// splitContent.Panel1
			// 
			splitContent.Panel1.Controls.Add(splitDisassembly);
			// 
			// splitContent.Panel2
			// 
			splitContent.Panel2.Controls.Add(pnlButtons);
			splitContent.Size = new System.Drawing.Size(1800, 1235);
			splitContent.SplitterDistance = 709;
			splitContent.SplitterWidth = 10;
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
			pnlButtons.Margin = new Padding(5, 6, 5, 6);
			pnlButtons.Name = "pnlButtons";
			pnlButtons.Size = new System.Drawing.Size(1081, 1235);
			pnlButtons.TabIndex = 0;
			// 
			// btnPluginReload
			// 
			btnPluginReload.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnPluginReload.Location = new System.Drawing.Point(870, 1159);
			btnPluginReload.Margin = new Padding(5, 6, 5, 6);
			btnPluginReload.Name = "btnPluginReload";
			btnPluginReload.Size = new System.Drawing.Size(191, 48);
			btnPluginReload.TabIndex = 58;
			btnPluginReload.Text = "Reload Plugins";
			btnPluginReload.UseVisualStyleBackColor = true;
			btnPluginReload.Click += btnPluginReload_Click;
			// 
			// btnGenDisassemblies
			// 
			btnGenDisassemblies.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnGenDisassemblies.Location = new System.Drawing.Point(657, 713);
			btnGenDisassemblies.Margin = new Padding(5, 6, 5, 6);
			btnGenDisassemblies.Name = "btnGenDisassemblies";
			btnGenDisassemblies.Size = new System.Drawing.Size(230, 48);
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
			tbClock.Location = new System.Drawing.Point(661, 607);
			tbClock.Margin = new Padding(5, 6, 5, 6);
			tbClock.Multiline = true;
			tbClock.Name = "tbClock";
			tbClock.ReadOnly = true;
			tbClock.Size = new System.Drawing.Size(386, 43);
			tbClock.TabIndex = 56;
			// 
			// btnDMAExplorer
			// 
			btnDMAExplorer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnDMAExplorer.Location = new System.Drawing.Point(810, 1105);
			btnDMAExplorer.Margin = new Padding(5, 6, 5, 6);
			btnDMAExplorer.Name = "btnDMAExplorer";
			btnDMAExplorer.Size = new System.Drawing.Size(100, 48);
			btnDMAExplorer.TabIndex = 55;
			btnDMAExplorer.Text = "DMA Explorer";
			btnDMAExplorer.UseVisualStyleBackColor = true;
			btnDMAExplorer.Click += btnDMAExplorer_Click;
			// 
			// btnAnalyseFlow
			// 
			btnAnalyseFlow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnAnalyseFlow.Location = new System.Drawing.Point(499, 659);
			btnAnalyseFlow.Margin = new Padding(11, 13, 11, 13);
			btnAnalyseFlow.Name = "btnAnalyseFlow";
			btnAnalyseFlow.Size = new System.Drawing.Size(150, 48);
			btnAnalyseFlow.TabIndex = 54;
			btnAnalyseFlow.Text = "Analyse";
			btnAnalyseFlow.UseVisualStyleBackColor = true;
			btnAnalyseFlow.Click += btnAnalyseFlow_Click;
			// 
			// btnStringScan
			// 
			btnStringScan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnStringScan.Location = new System.Drawing.Point(657, 1105);
			btnStringScan.Margin = new Padding(11, 13, 11, 13);
			btnStringScan.Name = "btnStringScan";
			btnStringScan.Size = new System.Drawing.Size(150, 48);
			btnStringScan.TabIndex = 53;
			btnStringScan.Text = "String Scan";
			btnStringScan.UseVisualStyleBackColor = true;
			btnStringScan.Click += btnStringScan_Click;
			// 
			// btnINTDIS
			// 
			btnINTDIS.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnINTDIS.Location = new System.Drawing.Point(581, 1046);
			btnINTDIS.Margin = new Padding(11, 13, 11, 13);
			btnINTDIS.Name = "btnINTDIS";
			btnINTDIS.Size = new System.Drawing.Size(68, 48);
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
			tbCommand.Location = new System.Drawing.Point(499, 1165);
			tbCommand.Margin = new Padding(5, 6, 5, 6);
			tbCommand.Name = "tbCommand";
			tbCommand.PlaceholderText = ">";
			tbCommand.Size = new System.Drawing.Size(361, 39);
			tbCommand.TabIndex = 51;
			tbCommand.KeyDown += tbCommand_KeyDown;
			// 
			// btnReadyDisk
			// 
			btnReadyDisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnReadyDisk.Location = new System.Drawing.Point(814, 816);
			btnReadyDisk.Margin = new Padding(11, 13, 11, 13);
			btnReadyDisk.Name = "btnReadyDisk";
			btnReadyDisk.Size = new System.Drawing.Size(150, 48);
			btnReadyDisk.TabIndex = 50;
			btnReadyDisk.Text = "Ready";
			btnReadyDisk.UseVisualStyleBackColor = true;
			btnReadyDisk.Click += btnReadyDisk_Click;
			// 
			// lbIntvec
			// 
			lbIntvec.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			lbIntvec.BorderStyle = BorderStyle.FixedSingle;
			lbIntvec.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			lbIntvec.FormattingEnabled = true;
			lbIntvec.Location = new System.Drawing.Point(11, 546);
			lbIntvec.Margin = new Padding(2, 1, 2, 1);
			lbIntvec.Name = "lbIntvec";
			lbIntvec.Size = new System.Drawing.Size(477, 177);
			lbIntvec.TabIndex = 27;
			lbIntvec.MouseDoubleClick += lbIntvec_MouseDoubleClick;
			// 
			// tabControl1
			// 
			tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			tabControl1.Controls.Add(tabCopper);
			tabControl1.Controls.Add(tabExec);
			tabControl1.Controls.Add(tabVectors);
			tabControl1.Controls.Add(tabLibraries);
			tabControl1.Controls.Add(tabAllocations);
			tabControl1.Font = new System.Drawing.Font("Cascadia Mono", 7.125F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			tabControl1.Location = new System.Drawing.Point(3, 2);
			tabControl1.Margin = new Padding(3, 2, 3, 2);
			tabControl1.Name = "tabControl1";
			tabControl1.SelectedIndex = 0;
			tabControl1.Size = new System.Drawing.Size(493, 543);
			tabControl1.TabIndex = 28;
			// 
			// tabCopper
			// 
			tabCopper.Controls.Add(txtCopper);
			tabCopper.Location = new System.Drawing.Point(8, 39);
			tabCopper.Margin = new Padding(3, 2, 3, 2);
			tabCopper.Name = "tabCopper";
			tabCopper.Padding = new Padding(3, 2, 3, 2);
			tabCopper.Size = new System.Drawing.Size(477, 496);
			tabCopper.TabIndex = 0;
			tabCopper.Text = "Copper";
			tabCopper.UseVisualStyleBackColor = true;
			// 
			// tabExec
			// 
			tabExec.Controls.Add(txtExecBase);
			tabExec.Controls.Add(cbTypes);
			tabExec.Location = new System.Drawing.Point(8, 39);
			tabExec.Margin = new Padding(3, 2, 3, 2);
			tabExec.Name = "tabExec";
			tabExec.Padding = new Padding(3, 2, 3, 2);
			tabExec.Size = new System.Drawing.Size(477, 496);
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
			txtExecBase.Location = new System.Drawing.Point(3, -385);
			txtExecBase.Margin = new Padding(3, 2, 3, 2);
			txtExecBase.Name = "txtExecBase";
			txtExecBase.ReadOnly = true;
			txtExecBase.Size = new System.Drawing.Size(471, 879);
			txtExecBase.TabIndex = 0;
			txtExecBase.Text = "";
			txtExecBase.WordWrap = false;
			// 
			// tabVectors
			// 
			tabVectors.Controls.Add(txtVectors);
			tabVectors.Location = new System.Drawing.Point(8, 39);
			tabVectors.Margin = new Padding(3, 2, 3, 2);
			tabVectors.Name = "tabVectors";
			tabVectors.Padding = new Padding(3, 2, 3, 2);
			tabVectors.Size = new System.Drawing.Size(477, 496);
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
			txtVectors.Location = new System.Drawing.Point(3, 2);
			txtVectors.Margin = new Padding(3, 2, 3, 2);
			txtVectors.Name = "txtVectors";
			txtVectors.ReadOnly = true;
			txtVectors.Size = new System.Drawing.Size(471, 492);
			txtVectors.TabIndex = 0;
			txtVectors.Text = "";
			txtVectors.WordWrap = false;
			// 
			// tabLibraries
			// 
			tabLibraries.Controls.Add(txtLibraries);
			tabLibraries.Location = new System.Drawing.Point(8, 39);
			tabLibraries.Margin = new Padding(3, 2, 3, 2);
			tabLibraries.Name = "tabLibraries";
			tabLibraries.Padding = new Padding(3, 2, 3, 2);
			tabLibraries.Size = new System.Drawing.Size(477, 496);
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
			txtLibraries.Location = new System.Drawing.Point(3, 2);
			txtLibraries.Margin = new Padding(3, 2, 3, 2);
			txtLibraries.Name = "txtLibraries";
			txtLibraries.ReadOnly = true;
			txtLibraries.Size = new System.Drawing.Size(471, 492);
			txtLibraries.TabIndex = 0;
			txtLibraries.Text = "";
			txtLibraries.WordWrap = false;
			// 
			// tabAllocations
			// 
			tabAllocations.Controls.Add(txtAllocations);
			tabAllocations.Location = new System.Drawing.Point(8, 39);
			tabAllocations.Margin = new Padding(3, 2, 3, 2);
			tabAllocations.Name = "tabAllocations";
			tabAllocations.Size = new System.Drawing.Size(477, 496);
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
			txtAllocations.Margin = new Padding(3, 2, 3, 2);
			txtAllocations.Name = "txtAllocations";
			txtAllocations.ReadOnly = true;
			txtAllocations.Size = new System.Drawing.Size(477, 496);
			txtAllocations.TabIndex = 1;
			txtAllocations.Text = "";
			txtAllocations.WordWrap = false;
			// 
			// Jammy
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
			AutoScaleMode = AutoScaleMode.Font;
			AutoSize = true;
			ClientSize = new System.Drawing.Size(1800, 1235);
			Controls.Add(splitContent);
			Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
			Margin = new Padding(5, 6, 5, 6);
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

