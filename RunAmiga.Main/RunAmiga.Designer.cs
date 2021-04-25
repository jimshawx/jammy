
namespace RunAmiga.Main
{
	partial class RunAmiga
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RunAmiga));
			this.lbRegisters = new System.Windows.Forms.ListBox();
			this.txtDisassembly = new System.Windows.Forms.RichTextBox();
			this.menuDisassembly = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.toolStripBreakpoint = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSkip = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripGoto = new System.Windows.Forms.ToolStripMenuItem();
			this.btnStep = new System.Windows.Forms.Button();
			this.btnStop = new System.Windows.Forms.Button();
			this.btnGo = new System.Windows.Forms.Button();
			this.btnReset = new System.Windows.Forms.Button();
			this.txtMemory = new System.Windows.Forms.RichTextBox();
			this.menuMemory = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuMemoryGotoItem = new System.Windows.Forms.ToolStripMenuItem();
			this.menuMemoryFindItem = new System.Windows.Forms.ToolStripMenuItem();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.btnStepOver = new System.Windows.Forms.Button();
			this.picPower = new System.Windows.Forms.PictureBox();
			this.picDisk = new System.Windows.Forms.PictureBox();
			this.btnDisassemble = new System.Windows.Forms.Button();
			this.radioButton10 = new System.Windows.Forms.RadioButton();
			this.radioButton11 = new System.Windows.Forms.RadioButton();
			this.radioButton12 = new System.Windows.Forms.RadioButton();
			this.radioButton13 = new System.Windows.Forms.RadioButton();
			this.radioButton14 = new System.Windows.Forms.RadioButton();
			this.radioButton15 = new System.Windows.Forms.RadioButton();
			this.radioButton16 = new System.Windows.Forms.RadioButton();
			this.radioButton17 = new System.Windows.Forms.RadioButton();
			this.addressFollowBox = new System.Windows.Forms.ComboBox();
			this.txtExecBase = new System.Windows.Forms.RichTextBox();
			this.btnInsertDisk = new System.Windows.Forms.Button();
			this.btnRemoveDisk = new System.Windows.Forms.Button();
			this.btnCIAInt = new System.Windows.Forms.Button();
			this.btnIRQ = new System.Windows.Forms.Button();
			this.cbIRQ = new System.Windows.Forms.ComboBox();
			this.cbCIA = new System.Windows.Forms.ComboBox();
			this.cbTypes = new System.Windows.Forms.ComboBox();
			this.lbCallStack = new System.Windows.Forms.ListBox();
			this.btnStepOut = new System.Windows.Forms.Button();
			this.btnINTENA = new System.Windows.Forms.Button();
			this.lbCustom = new System.Windows.Forms.ListBox();
			this.btnDumpTrace = new System.Windows.Forms.Button();
			this.btnIDEACK = new System.Windows.Forms.Button();
			this.btnChange = new System.Windows.Forms.Button();
			this.radioDF0 = new System.Windows.Forms.RadioButton();
			this.radioDF1 = new System.Windows.Forms.RadioButton();
			this.radioDF2 = new System.Windows.Forms.RadioButton();
			this.radioDF3 = new System.Windows.Forms.RadioButton();
			this.btnGfxScan = new System.Windows.Forms.Button();
			this.menuDisassembly.SuspendLayout();
			this.menuMemory.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.picPower)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.picDisk)).BeginInit();
			this.SuspendLayout();
			// 
			// lbRegisters
			// 
			this.lbRegisters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lbRegisters.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.lbRegisters.FormattingEnabled = true;
			this.lbRegisters.IntegralHeight = false;
			this.lbRegisters.Location = new System.Drawing.Point(1038, 12);
			this.lbRegisters.Name = "lbRegisters";
			this.lbRegisters.SelectionMode = System.Windows.Forms.SelectionMode.None;
			this.lbRegisters.Size = new System.Drawing.Size(175, 160);
			this.lbRegisters.TabIndex = 0;
			// 
			// txtDisassembly
			// 
			this.txtDisassembly.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtDisassembly.ContextMenuStrip = this.menuDisassembly;
			this.txtDisassembly.DetectUrls = false;
			this.txtDisassembly.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.txtDisassembly.HideSelection = false;
			this.txtDisassembly.Location = new System.Drawing.Point(3, 3);
			this.txtDisassembly.Name = "txtDisassembly";
			this.txtDisassembly.ReadOnly = true;
			this.txtDisassembly.Size = new System.Drawing.Size(717, 242);
			this.txtDisassembly.TabIndex = 1;
			this.txtDisassembly.Text = "";
			this.txtDisassembly.WordWrap = false;
			// 
			// menuDisassembly
			// 
			this.menuDisassembly.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripBreakpoint,
            this.toolStripSkip,
            this.toolStripGoto});
			this.menuDisassembly.Name = "menuDisassembly";
			this.menuDisassembly.Size = new System.Drawing.Size(132, 70);
			this.menuDisassembly.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuDisassembly_ItemClicked);
			// 
			// toolStripBreakpoint
			// 
			this.toolStripBreakpoint.Name = "toolStripBreakpoint";
			this.toolStripBreakpoint.Size = new System.Drawing.Size(131, 22);
			this.toolStripBreakpoint.Text = "Breakpoint";
			// 
			// toolStripSkip
			// 
			this.toolStripSkip.Name = "toolStripSkip";
			this.toolStripSkip.Size = new System.Drawing.Size(131, 22);
			this.toolStripSkip.Text = "Skip";
			// 
			// toolStripGoto
			// 
			this.toolStripGoto.Name = "toolStripGoto";
			this.toolStripGoto.Size = new System.Drawing.Size(131, 22);
			this.toolStripGoto.Text = "Go To...";
			// 
			// btnStep
			// 
			this.btnStep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStep.Location = new System.Drawing.Point(1038, 179);
			this.btnStep.Name = "btnStep";
			this.btnStep.Size = new System.Drawing.Size(71, 23);
			this.btnStep.TabIndex = 2;
			this.btnStep.Text = "Step";
			this.btnStep.UseVisualStyleBackColor = true;
			this.btnStep.Click += new System.EventHandler(this.btnStep_Click);
			// 
			// btnStop
			// 
			this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStop.Location = new System.Drawing.Point(1038, 208);
			this.btnStop.Name = "btnStop";
			this.btnStop.Size = new System.Drawing.Size(71, 23);
			this.btnStop.TabIndex = 3;
			this.btnStop.Text = "Stop";
			this.btnStop.UseVisualStyleBackColor = true;
			this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
			// 
			// btnGo
			// 
			this.btnGo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnGo.Location = new System.Drawing.Point(1038, 238);
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new System.Drawing.Size(71, 23);
			this.btnGo.TabIndex = 4;
			this.btnGo.Text = "Go";
			this.btnGo.UseVisualStyleBackColor = true;
			this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
			// 
			// btnReset
			// 
			this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnReset.Location = new System.Drawing.Point(1038, 268);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(71, 23);
			this.btnReset.TabIndex = 5;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// txtMemory
			// 
			this.txtMemory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.txtMemory.ContextMenuStrip = this.menuMemory;
			this.txtMemory.DetectUrls = false;
			this.txtMemory.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.txtMemory.Location = new System.Drawing.Point(3, 3);
			this.txtMemory.Name = "txtMemory";
			this.txtMemory.ReadOnly = true;
			this.txtMemory.Size = new System.Drawing.Size(717, 323);
			this.txtMemory.TabIndex = 6;
			this.txtMemory.Text = "";
			this.txtMemory.WordWrap = false;
			// 
			// menuMemory
			// 
			this.menuMemory.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuMemoryGotoItem,
            this.menuMemoryFindItem});
			this.menuMemory.Name = "menuMemory";
			this.menuMemory.Size = new System.Drawing.Size(114, 48);
			this.menuMemory.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuMemory_ItemClicked);
			// 
			// menuMemoryGotoItem
			// 
			this.menuMemoryGotoItem.Name = "menuMemoryGotoItem";
			this.menuMemoryGotoItem.Size = new System.Drawing.Size(113, 22);
			this.menuMemoryGotoItem.Text = "Go To...";
			// 
			// menuMemoryFindItem
			// 
			this.menuMemoryFindItem.Name = "menuMemoryFindItem";
			this.menuMemoryFindItem.Size = new System.Drawing.Size(113, 22);
			this.menuMemoryFindItem.Text = "Find...";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.splitContainer1.Location = new System.Drawing.Point(12, 12);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.txtDisassembly);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.txtMemory);
			this.splitContainer1.Size = new System.Drawing.Size(723, 581);
			this.splitContainer1.SplitterDistance = 248;
			this.splitContainer1.TabIndex = 7;
			// 
			// btnRefresh
			// 
			this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRefresh.Location = new System.Drawing.Point(1039, 332);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(71, 23);
			this.btnRefresh.TabIndex = 8;
			this.btnRefresh.Text = "Refresh";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
			// 
			// btnStepOver
			// 
			this.btnStepOver.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStepOver.Location = new System.Drawing.Point(1120, 179);
			this.btnStepOver.Name = "btnStepOver";
			this.btnStepOver.Size = new System.Drawing.Size(71, 23);
			this.btnStepOver.TabIndex = 9;
			this.btnStepOver.Text = "Step Over";
			this.btnStepOver.UseVisualStyleBackColor = true;
			this.btnStepOver.Click += new System.EventHandler(this.btnStepOver_Click);
			// 
			// picPower
			// 
			this.picPower.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.picPower.Location = new System.Drawing.Point(1144, 250);
			this.picPower.Name = "picPower";
			this.picPower.Size = new System.Drawing.Size(47, 10);
			this.picPower.TabIndex = 10;
			this.picPower.TabStop = false;
			// 
			// picDisk
			// 
			this.picDisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.picDisk.Location = new System.Drawing.Point(1144, 268);
			this.picDisk.Name = "picDisk";
			this.picDisk.Size = new System.Drawing.Size(47, 10);
			this.picDisk.TabIndex = 11;
			this.picDisk.TabStop = false;
			// 
			// btnDisassemble
			// 
			this.btnDisassemble.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDisassemble.Location = new System.Drawing.Point(1120, 332);
			this.btnDisassemble.Name = "btnDisassemble";
			this.btnDisassemble.Size = new System.Drawing.Size(93, 23);
			this.btnDisassemble.TabIndex = 12;
			this.btnDisassemble.Text = "Disassemble";
			this.btnDisassemble.UseVisualStyleBackColor = true;
			this.btnDisassemble.Click += new System.EventHandler(this.btnDisassemble_Click);
			// 
			// radioButton10
			// 
			this.radioButton10.AutoSize = true;
			this.radioButton10.Location = new System.Drawing.Point(404, 280);
			this.radioButton10.Name = "radioButton10";
			this.radioButton10.Size = new System.Drawing.Size(100, 19);
			this.radioButton10.TabIndex = 27;
			this.radioButton10.TabStop = true;
			this.radioButton10.Text = "radioButton10";
			this.radioButton10.UseVisualStyleBackColor = true;
			// 
			// radioButton11
			// 
			this.radioButton11.AutoSize = true;
			this.radioButton11.Location = new System.Drawing.Point(412, 288);
			this.radioButton11.Name = "radioButton11";
			this.radioButton11.Size = new System.Drawing.Size(100, 19);
			this.radioButton11.TabIndex = 28;
			this.radioButton11.TabStop = true;
			this.radioButton11.Text = "radioButton11";
			this.radioButton11.UseVisualStyleBackColor = true;
			// 
			// radioButton12
			// 
			this.radioButton12.AutoSize = true;
			this.radioButton12.Location = new System.Drawing.Point(420, 296);
			this.radioButton12.Name = "radioButton12";
			this.radioButton12.Size = new System.Drawing.Size(100, 19);
			this.radioButton12.TabIndex = 29;
			this.radioButton12.TabStop = true;
			this.radioButton12.Text = "radioButton12";
			this.radioButton12.UseVisualStyleBackColor = true;
			// 
			// radioButton13
			// 
			this.radioButton13.AutoSize = true;
			this.radioButton13.Location = new System.Drawing.Point(428, 304);
			this.radioButton13.Name = "radioButton13";
			this.radioButton13.Size = new System.Drawing.Size(100, 19);
			this.radioButton13.TabIndex = 30;
			this.radioButton13.TabStop = true;
			this.radioButton13.Text = "radioButton13";
			this.radioButton13.UseVisualStyleBackColor = true;
			// 
			// radioButton14
			// 
			this.radioButton14.AutoSize = true;
			this.radioButton14.Location = new System.Drawing.Point(436, 312);
			this.radioButton14.Name = "radioButton14";
			this.radioButton14.Size = new System.Drawing.Size(100, 19);
			this.radioButton14.TabIndex = 31;
			this.radioButton14.TabStop = true;
			this.radioButton14.Text = "radioButton14";
			this.radioButton14.UseVisualStyleBackColor = true;
			// 
			// radioButton15
			// 
			this.radioButton15.AutoSize = true;
			this.radioButton15.Location = new System.Drawing.Point(444, 320);
			this.radioButton15.Name = "radioButton15";
			this.radioButton15.Size = new System.Drawing.Size(100, 19);
			this.radioButton15.TabIndex = 32;
			this.radioButton15.TabStop = true;
			this.radioButton15.Text = "radioButton15";
			this.radioButton15.UseVisualStyleBackColor = true;
			// 
			// radioButton16
			// 
			this.radioButton16.AutoSize = true;
			this.radioButton16.Location = new System.Drawing.Point(452, 328);
			this.radioButton16.Name = "radioButton16";
			this.radioButton16.Size = new System.Drawing.Size(100, 19);
			this.radioButton16.TabIndex = 33;
			this.radioButton16.TabStop = true;
			this.radioButton16.Text = "radioButton16";
			this.radioButton16.UseVisualStyleBackColor = true;
			// 
			// radioButton17
			// 
			this.radioButton17.AutoSize = true;
			this.radioButton17.Location = new System.Drawing.Point(460, 336);
			this.radioButton17.Name = "radioButton17";
			this.radioButton17.Size = new System.Drawing.Size(100, 19);
			this.radioButton17.TabIndex = 34;
			this.radioButton17.TabStop = true;
			this.radioButton17.Text = "radioButton17";
			this.radioButton17.UseVisualStyleBackColor = true;
			// 
			// addressFollowBox
			// 
			this.addressFollowBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.addressFollowBox.FormattingEnabled = true;
			this.addressFollowBox.Items.AddRange(new object[] {
            "(None)",
            "A0",
            "A1",
            "A2",
            "A3",
            "A4",
            "A5",
            "A6",
            "SP",
            "SSP",
            "D0",
            "D1",
            "D2",
            "D3",
            "D4",
            "D5",
            "D6",
            "D7",
            "PC"});
			this.addressFollowBox.Location = new System.Drawing.Point(1039, 362);
			this.addressFollowBox.Name = "addressFollowBox";
			this.addressFollowBox.Size = new System.Drawing.Size(117, 23);
			this.addressFollowBox.TabIndex = 25;
			this.addressFollowBox.SelectionChangeCommitted += new System.EventHandler(this.addressFollowBox_SelectionChangeCommitted);
			// 
			// txtExecBase
			// 
			this.txtExecBase.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtExecBase.DetectUrls = false;
			this.txtExecBase.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.txtExecBase.Location = new System.Drawing.Point(742, 39);
			this.txtExecBase.Name = "txtExecBase";
			this.txtExecBase.ReadOnly = true;
			this.txtExecBase.Size = new System.Drawing.Size(290, 551);
			this.txtExecBase.TabIndex = 26;
			this.txtExecBase.Text = "";
			this.txtExecBase.WordWrap = false;
			// 
			// btnInsertDisk
			// 
			this.btnInsertDisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnInsertDisk.Location = new System.Drawing.Point(1039, 392);
			this.btnInsertDisk.Name = "btnInsertDisk";
			this.btnInsertDisk.Size = new System.Drawing.Size(72, 23);
			this.btnInsertDisk.TabIndex = 27;
			this.btnInsertDisk.Text = "Insert Disk";
			this.btnInsertDisk.UseVisualStyleBackColor = true;
			this.btnInsertDisk.Click += new System.EventHandler(this.btnInsertDisk_Click);
			// 
			// btnRemoveDisk
			// 
			this.btnRemoveDisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRemoveDisk.Location = new System.Drawing.Point(1120, 392);
			this.btnRemoveDisk.Name = "btnRemoveDisk";
			this.btnRemoveDisk.Size = new System.Drawing.Size(71, 23);
			this.btnRemoveDisk.TabIndex = 28;
			this.btnRemoveDisk.Text = "Remove Disk";
			this.btnRemoveDisk.UseVisualStyleBackColor = true;
			this.btnRemoveDisk.Click += new System.EventHandler(this.btnRemoveDisk_Click);
			// 
			// btnCIAInt
			// 
			this.btnCIAInt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCIAInt.Location = new System.Drawing.Point(1039, 451);
			this.btnCIAInt.Name = "btnCIAInt";
			this.btnCIAInt.Size = new System.Drawing.Size(72, 23);
			this.btnCIAInt.TabIndex = 29;
			this.btnCIAInt.Text = "CIA Int";
			this.btnCIAInt.UseVisualStyleBackColor = true;
			this.btnCIAInt.Click += new System.EventHandler(this.btnCIAInt_Click);
			// 
			// btnIRQ
			// 
			this.btnIRQ.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnIRQ.Location = new System.Drawing.Point(1039, 480);
			this.btnIRQ.Name = "btnIRQ";
			this.btnIRQ.Size = new System.Drawing.Size(72, 23);
			this.btnIRQ.TabIndex = 31;
			this.btnIRQ.Text = "IRQ";
			this.btnIRQ.UseVisualStyleBackColor = true;
			this.btnIRQ.Click += new System.EventHandler(this.btnIRQ_Click);
			// 
			// cbIRQ
			// 
			this.cbIRQ.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbIRQ.FormattingEnabled = true;
			this.cbIRQ.Items.AddRange(new object[] {
            "EXTER",
            "DSKSYNC",
            "AUD0",
            "AUD1",
            "AUD2",
            "AUD3",
            "BLIT",
            "VERTB",
            "COPPER",
            "PORTS",
            "DSKBLK",
            "SOFTINT"});
			this.cbIRQ.Location = new System.Drawing.Point(1120, 480);
			this.cbIRQ.Name = "cbIRQ";
			this.cbIRQ.Size = new System.Drawing.Size(71, 23);
			this.cbIRQ.TabIndex = 32;
			this.cbIRQ.Text = "BLIT";
			// 
			// cbCIA
			// 
			this.cbCIA.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbCIA.FormattingEnabled = true;
			this.cbCIA.Items.AddRange(new object[] {
            "TIMERA",
            "TIMERB",
            "TODALARM",
            "SERIAL",
            "FLAG"});
			this.cbCIA.Location = new System.Drawing.Point(1120, 451);
			this.cbCIA.Name = "cbCIA";
			this.cbCIA.Size = new System.Drawing.Size(71, 23);
			this.cbCIA.TabIndex = 33;
			this.cbCIA.Text = "TIMERA";
			// 
			// cbTypes
			// 
			this.cbTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbTypes.FormattingEnabled = true;
			this.cbTypes.Items.AddRange(new object[] {
            "(None)",
            "ExecBase",
            "timerequest",
            "Library",
            "Task",
            "KeyMapResource",
            "MsgPort",
            "Unit",
            "Resident"});
			this.cbTypes.Location = new System.Drawing.Point(873, 15);
			this.cbTypes.Name = "cbTypes";
			this.cbTypes.Size = new System.Drawing.Size(159, 23);
			this.cbTypes.TabIndex = 34;
			this.cbTypes.SelectionChangeCommitted += new System.EventHandler(this.cbTypes_SelectionChangeCommitted);
			// 
			// lbCallStack
			// 
			this.lbCallStack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lbCallStack.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.lbCallStack.FormattingEnabled = true;
			this.lbCallStack.Location = new System.Drawing.Point(1219, 12);
			this.lbCallStack.Name = "lbCallStack";
			this.lbCallStack.SelectionMode = System.Windows.Forms.SelectionMode.None;
			this.lbCallStack.Size = new System.Drawing.Size(167, 238);
			this.lbCallStack.TabIndex = 35;
			// 
			// btnStepOut
			// 
			this.btnStepOut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStepOut.Location = new System.Drawing.Point(1120, 208);
			this.btnStepOut.Name = "btnStepOut";
			this.btnStepOut.Size = new System.Drawing.Size(71, 23);
			this.btnStepOut.TabIndex = 36;
			this.btnStepOut.Text = "Step Out";
			this.btnStepOut.UseVisualStyleBackColor = true;
			this.btnStepOut.Click += new System.EventHandler(this.btnStepOut_Click);
			// 
			// btnINTENA
			// 
			this.btnINTENA.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnINTENA.Location = new System.Drawing.Point(1039, 507);
			this.btnINTENA.Name = "btnINTENA";
			this.btnINTENA.Size = new System.Drawing.Size(72, 23);
			this.btnINTENA.TabIndex = 37;
			this.btnINTENA.Text = "INTENA";
			this.btnINTENA.UseVisualStyleBackColor = true;
			this.btnINTENA.Click += new System.EventHandler(this.btnINTENA_Click);
			// 
			// lbCustom
			// 
			this.lbCustom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lbCustom.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.lbCustom.FormattingEnabled = true;
			this.lbCustom.Location = new System.Drawing.Point(1219, 256);
			this.lbCustom.Name = "lbCustom";
			this.lbCustom.Size = new System.Drawing.Size(167, 303);
			this.lbCustom.TabIndex = 38;
			// 
			// btnDumpTrace
			// 
			this.btnDumpTrace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDumpTrace.Location = new System.Drawing.Point(1120, 303);
			this.btnDumpTrace.Name = "btnDumpTrace";
			this.btnDumpTrace.Size = new System.Drawing.Size(93, 23);
			this.btnDumpTrace.TabIndex = 39;
			this.btnDumpTrace.Text = "Dump Trace";
			this.btnDumpTrace.UseVisualStyleBackColor = true;
			this.btnDumpTrace.Click += new System.EventHandler(this.btnDumpTrace_Click);
			// 
			// btnIDEACK
			// 
			this.btnIDEACK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnIDEACK.Location = new System.Drawing.Point(1039, 536);
			this.btnIDEACK.Name = "btnIDEACK";
			this.btnIDEACK.Size = new System.Drawing.Size(72, 23);
			this.btnIDEACK.TabIndex = 40;
			this.btnIDEACK.Text = "IDEACK";
			this.btnIDEACK.UseVisualStyleBackColor = true;
			this.btnIDEACK.Click += new System.EventHandler(this.btnIDEACK_Click);
			// 
			// btnChange
			// 
			this.btnChange.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnChange.Location = new System.Drawing.Point(1039, 422);
			this.btnChange.Name = "btnChange";
			this.btnChange.Size = new System.Drawing.Size(72, 23);
			this.btnChange.TabIndex = 41;
			this.btnChange.Text = "Change";
			this.btnChange.UseVisualStyleBackColor = true;
			this.btnChange.Click += new System.EventHandler(this.btnChange_Click);
			// 
			// radioDF0
			// 
			this.radioDF0.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioDF0.AutoSize = true;
			this.radioDF0.Checked = true;
			this.radioDF0.Location = new System.Drawing.Point(1116, 424);
			this.radioDF0.Name = "radioDF0";
			this.radioDF0.Size = new System.Drawing.Size(14, 13);
			this.radioDF0.TabIndex = 42;
			this.radioDF0.TabStop = true;
			this.radioDF0.UseVisualStyleBackColor = true;
			this.radioDF0.CheckedChanged += new System.EventHandler(this.radioDFx_CheckedChanged);
			// 
			// radioDF1
			// 
			this.radioDF1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioDF1.AutoSize = true;
			this.radioDF1.Location = new System.Drawing.Point(1136, 424);
			this.radioDF1.Name = "radioDF1";
			this.radioDF1.Size = new System.Drawing.Size(14, 13);
			this.radioDF1.TabIndex = 43;
			this.radioDF1.TabStop = true;
			this.radioDF1.UseVisualStyleBackColor = true;
			this.radioDF1.CheckedChanged += new System.EventHandler(this.radioDFx_CheckedChanged);
			// 
			// radioDF2
			// 
			this.radioDF2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioDF2.AutoSize = true;
			this.radioDF2.Location = new System.Drawing.Point(1156, 424);
			this.radioDF2.Name = "radioDF2";
			this.radioDF2.Size = new System.Drawing.Size(14, 13);
			this.radioDF2.TabIndex = 44;
			this.radioDF2.TabStop = true;
			this.radioDF2.UseVisualStyleBackColor = true;
			this.radioDF2.CheckedChanged += new System.EventHandler(this.radioDFx_CheckedChanged);
			// 
			// radioDF3
			// 
			this.radioDF3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.radioDF3.AutoSize = true;
			this.radioDF3.Location = new System.Drawing.Point(1177, 424);
			this.radioDF3.Name = "radioDF3";
			this.radioDF3.Size = new System.Drawing.Size(14, 13);
			this.radioDF3.TabIndex = 45;
			this.radioDF3.TabStop = true;
			this.radioDF3.UseVisualStyleBackColor = true;
			this.radioDF3.CheckedChanged += new System.EventHandler(this.radioDFx_CheckedChanged);
			// 
			// btnGfxScan
			// 
			this.btnGfxScan.Location = new System.Drawing.Point(1120, 536);
			this.btnGfxScan.Name = "btnGfxScan";
			this.btnGfxScan.Size = new System.Drawing.Size(75, 23);
			this.btnGfxScan.TabIndex = 46;
			this.btnGfxScan.Text = "Gfx Scan";
			this.btnGfxScan.UseVisualStyleBackColor = true;
			this.btnGfxScan.Click += new System.EventHandler(this.btnGfxScan_Click);
			// 
			// RunAmiga
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1414, 605);
			this.Controls.Add(this.btnGfxScan);
			this.Controls.Add(this.radioDF3);
			this.Controls.Add(this.radioDF2);
			this.Controls.Add(this.radioDF1);
			this.Controls.Add(this.radioDF0);
			this.Controls.Add(this.btnChange);
			this.Controls.Add(this.btnIDEACK);
			this.Controls.Add(this.btnDumpTrace);
			this.Controls.Add(this.lbCustom);
			this.Controls.Add(this.btnINTENA);
			this.Controls.Add(this.btnStepOut);
			this.Controls.Add(this.lbCallStack);
			this.Controls.Add(this.cbTypes);
			this.Controls.Add(this.cbCIA);
			this.Controls.Add(this.cbIRQ);
			this.Controls.Add(this.btnIRQ);
			this.Controls.Add(this.btnCIAInt);
			this.Controls.Add(this.btnRemoveDisk);
			this.Controls.Add(this.btnInsertDisk);
			this.Controls.Add(this.txtExecBase);
			this.Controls.Add(this.addressFollowBox);
			this.Controls.Add(this.btnDisassemble);
			this.Controls.Add(this.picDisk);
			this.Controls.Add(this.picPower);
			this.Controls.Add(this.btnStepOver);
			this.Controls.Add(this.btnRefresh);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.btnReset);
			this.Controls.Add(this.btnGo);
			this.Controls.Add(this.btnStop);
			this.Controls.Add(this.btnStep);
			this.Controls.Add(this.lbRegisters);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "RunAmiga";
			this.Text = "RunAmiga";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.menuDisassembly.ResumeLayout(false);
			this.menuMemory.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.picPower)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.picDisk)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

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
	}
}

