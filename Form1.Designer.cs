
namespace RunAmiga
{
	partial class Form1
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.lbRegisters = new System.Windows.Forms.ListBox();
			this.txtDisassembly = new System.Windows.Forms.RichTextBox();
			this.btnStep = new System.Windows.Forms.Button();
			this.btnStop = new System.Windows.Forms.Button();
			this.btnGo = new System.Windows.Forms.Button();
			this.btnReset = new System.Windows.Forms.Button();
			this.txtMemory = new System.Windows.Forms.RichTextBox();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.btnRefresh = new System.Windows.Forms.Button();
			this.btnStepOver = new System.Windows.Forms.Button();
			this.picPower = new System.Windows.Forms.PictureBox();
			this.picDisk = new System.Windows.Forms.PictureBox();
			this.btnDisassemble = new System.Windows.Forms.Button();
			this.colour0 = new System.Windows.Forms.PictureBox();
			this.colour1 = new System.Windows.Forms.PictureBox();
			this.colour2 = new System.Windows.Forms.PictureBox();
			this.colour3 = new System.Windows.Forms.PictureBox();
			this.colour7 = new System.Windows.Forms.PictureBox();
			this.colour6 = new System.Windows.Forms.PictureBox();
			this.colour5 = new System.Windows.Forms.PictureBox();
			this.colour4 = new System.Windows.Forms.PictureBox();
			this.colour11 = new System.Windows.Forms.PictureBox();
			this.colour10 = new System.Windows.Forms.PictureBox();
			this.colour9 = new System.Windows.Forms.PictureBox();
			this.colour8 = new System.Windows.Forms.PictureBox();
			this.colour15 = new System.Windows.Forms.PictureBox();
			this.colour14 = new System.Windows.Forms.PictureBox();
			this.colour13 = new System.Windows.Forms.PictureBox();
			this.colour12 = new System.Windows.Forms.PictureBox();
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
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.picPower)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.picDisk)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour0)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour3)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour7)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour6)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour5)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour4)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour11)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour10)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour9)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour8)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour15)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour14)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour13)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.colour12)).BeginInit();
			this.SuspendLayout();
			// 
			// lbRegisters
			// 
			this.lbRegisters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lbRegisters.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.lbRegisters.FormattingEnabled = true;
			this.lbRegisters.IntegralHeight = false;
			this.lbRegisters.Location = new System.Drawing.Point(1091, 12);
			this.lbRegisters.Name = "lbRegisters";
			this.lbRegisters.SelectionMode = System.Windows.Forms.SelectionMode.None;
			this.lbRegisters.Size = new System.Drawing.Size(179, 160);
			this.lbRegisters.TabIndex = 0;
			// 
			// txtDisassembly
			// 
			this.txtDisassembly.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtDisassembly.DetectUrls = false;
			this.txtDisassembly.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.txtDisassembly.HideSelection = false;
			this.txtDisassembly.Location = new System.Drawing.Point(3, 3);
			this.txtDisassembly.Name = "txtDisassembly";
			this.txtDisassembly.ReadOnly = true;
			this.txtDisassembly.Size = new System.Drawing.Size(717, 227);
			this.txtDisassembly.TabIndex = 1;
			this.txtDisassembly.Text = "";
			this.txtDisassembly.WordWrap = false;
			// 
			// btnStep
			// 
			this.btnStep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStep.Location = new System.Drawing.Point(1091, 179);
			this.btnStep.Name = "btnStep";
			this.btnStep.Size = new System.Drawing.Size(75, 23);
			this.btnStep.TabIndex = 2;
			this.btnStep.Text = "Step";
			this.btnStep.UseVisualStyleBackColor = true;
			this.btnStep.Click += new System.EventHandler(this.btnStep_Click);
			// 
			// btnStop
			// 
			this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStop.Location = new System.Drawing.Point(1091, 208);
			this.btnStop.Name = "btnStop";
			this.btnStop.Size = new System.Drawing.Size(75, 23);
			this.btnStop.TabIndex = 3;
			this.btnStop.Text = "Stop";
			this.btnStop.UseVisualStyleBackColor = true;
			this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
			// 
			// btnGo
			// 
			this.btnGo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnGo.Location = new System.Drawing.Point(1091, 238);
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new System.Drawing.Size(75, 23);
			this.btnGo.TabIndex = 4;
			this.btnGo.Text = "Go";
			this.btnGo.UseVisualStyleBackColor = true;
			this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
			// 
			// btnReset
			// 
			this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnReset.Location = new System.Drawing.Point(1092, 268);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(75, 23);
			this.btnReset.TabIndex = 5;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// txtMemory
			// 
			this.txtMemory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.txtMemory.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.txtMemory.Location = new System.Drawing.Point(3, 3);
			this.txtMemory.Name = "txtMemory";
			this.txtMemory.ReadOnly = true;
			this.txtMemory.Size = new System.Drawing.Size(717, 301);
			this.txtMemory.TabIndex = 6;
			this.txtMemory.Text = "";
			this.txtMemory.WordWrap = false;
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
			this.splitContainer1.Size = new System.Drawing.Size(723, 544);
			this.splitContainer1.SplitterDistance = 233;
			this.splitContainer1.TabIndex = 7;
			// 
			// btnRefresh
			// 
			this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRefresh.Location = new System.Drawing.Point(1092, 332);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(75, 23);
			this.btnRefresh.TabIndex = 8;
			this.btnRefresh.Text = "Refresh";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
			// 
			// btnStepOver
			// 
			this.btnStepOver.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStepOver.Location = new System.Drawing.Point(1173, 179);
			this.btnStepOver.Name = "btnStepOver";
			this.btnStepOver.Size = new System.Drawing.Size(75, 23);
			this.btnStepOver.TabIndex = 9;
			this.btnStepOver.Text = "Step Over";
			this.btnStepOver.UseVisualStyleBackColor = true;
			this.btnStepOver.Click += new System.EventHandler(this.btnStepOver_Click);
			// 
			// picPower
			// 
			this.picPower.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.picPower.Location = new System.Drawing.Point(1197, 250);
			this.picPower.Name = "picPower";
			this.picPower.Size = new System.Drawing.Size(51, 10);
			this.picPower.TabIndex = 10;
			this.picPower.TabStop = false;
			// 
			// picDisk
			// 
			this.picDisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.picDisk.Location = new System.Drawing.Point(1197, 268);
			this.picDisk.Name = "picDisk";
			this.picDisk.Size = new System.Drawing.Size(51, 10);
			this.picDisk.TabIndex = 11;
			this.picDisk.TabStop = false;
			// 
			// btnDisassemble
			// 
			this.btnDisassemble.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDisassemble.Location = new System.Drawing.Point(1173, 332);
			this.btnDisassemble.Name = "btnDisassemble";
			this.btnDisassemble.Size = new System.Drawing.Size(81, 23);
			this.btnDisassemble.TabIndex = 12;
			this.btnDisassemble.Text = "Disassemble";
			this.btnDisassemble.UseVisualStyleBackColor = true;
			this.btnDisassemble.Click += new System.EventHandler(this.btnDisassemble_Click);
			// 
			// colour0
			// 
			this.colour0.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour0.Location = new System.Drawing.Point(1189, 485);
			this.colour0.Name = "colour0";
			this.colour0.Size = new System.Drawing.Size(16, 16);
			this.colour0.TabIndex = 13;
			this.colour0.TabStop = false;
			// 
			// colour1
			// 
			this.colour1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour1.Location = new System.Drawing.Point(1211, 485);
			this.colour1.Name = "colour1";
			this.colour1.Size = new System.Drawing.Size(16, 16);
			this.colour1.TabIndex = 14;
			this.colour1.TabStop = false;
			// 
			// colour2
			// 
			this.colour2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour2.Location = new System.Drawing.Point(1233, 485);
			this.colour2.Name = "colour2";
			this.colour2.Size = new System.Drawing.Size(16, 16);
			this.colour2.TabIndex = 15;
			this.colour2.TabStop = false;
			// 
			// colour3
			// 
			this.colour3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour3.Location = new System.Drawing.Point(1255, 485);
			this.colour3.Name = "colour3";
			this.colour3.Size = new System.Drawing.Size(16, 16);
			this.colour3.TabIndex = 16;
			this.colour3.TabStop = false;
			// 
			// colour7
			// 
			this.colour7.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour7.Location = new System.Drawing.Point(1255, 507);
			this.colour7.Name = "colour7";
			this.colour7.Size = new System.Drawing.Size(16, 16);
			this.colour7.TabIndex = 20;
			this.colour7.TabStop = false;
			// 
			// colour6
			// 
			this.colour6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour6.Location = new System.Drawing.Point(1233, 507);
			this.colour6.Name = "colour6";
			this.colour6.Size = new System.Drawing.Size(16, 16);
			this.colour6.TabIndex = 19;
			this.colour6.TabStop = false;
			// 
			// colour5
			// 
			this.colour5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour5.Location = new System.Drawing.Point(1211, 507);
			this.colour5.Name = "colour5";
			this.colour5.Size = new System.Drawing.Size(16, 16);
			this.colour5.TabIndex = 18;
			this.colour5.TabStop = false;
			// 
			// colour4
			// 
			this.colour4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour4.Location = new System.Drawing.Point(1189, 507);
			this.colour4.Name = "colour4";
			this.colour4.Size = new System.Drawing.Size(16, 16);
			this.colour4.TabIndex = 17;
			this.colour4.TabStop = false;
			// 
			// colour11
			// 
			this.colour11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour11.Location = new System.Drawing.Point(1255, 529);
			this.colour11.Name = "colour11";
			this.colour11.Size = new System.Drawing.Size(16, 16);
			this.colour11.TabIndex = 24;
			this.colour11.TabStop = false;
			// 
			// colour10
			// 
			this.colour10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour10.Location = new System.Drawing.Point(1233, 529);
			this.colour10.Name = "colour10";
			this.colour10.Size = new System.Drawing.Size(16, 16);
			this.colour10.TabIndex = 23;
			this.colour10.TabStop = false;
			// 
			// colour9
			// 
			this.colour9.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour9.Location = new System.Drawing.Point(1211, 529);
			this.colour9.Name = "colour9";
			this.colour9.Size = new System.Drawing.Size(16, 16);
			this.colour9.TabIndex = 22;
			this.colour9.TabStop = false;
			// 
			// colour8
			// 
			this.colour8.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour8.Location = new System.Drawing.Point(1189, 529);
			this.colour8.Name = "colour8";
			this.colour8.Size = new System.Drawing.Size(16, 16);
			this.colour8.TabIndex = 21;
			this.colour8.TabStop = false;
			// 
			// colour15
			// 
			this.colour15.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour15.Location = new System.Drawing.Point(1255, 551);
			this.colour15.Name = "colour15";
			this.colour15.Size = new System.Drawing.Size(16, 16);
			this.colour15.TabIndex = 20;
			this.colour15.TabStop = false;
			// 
			// colour14
			// 
			this.colour14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour14.Location = new System.Drawing.Point(1233, 551);
			this.colour14.Name = "colour14";
			this.colour14.Size = new System.Drawing.Size(16, 16);
			this.colour14.TabIndex = 19;
			this.colour14.TabStop = false;
			// 
			// colour13
			// 
			this.colour13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour13.Location = new System.Drawing.Point(1211, 551);
			this.colour13.Name = "colour13";
			this.colour13.Size = new System.Drawing.Size(16, 16);
			this.colour13.TabIndex = 18;
			this.colour13.TabStop = false;
			// 
			// colour12
			// 
			this.colour12.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.colour12.Location = new System.Drawing.Point(1189, 551);
			this.colour12.Name = "colour12";
			this.colour12.Size = new System.Drawing.Size(16, 16);
			this.colour12.TabIndex = 17;
			this.colour12.TabStop = false;
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
			this.addressFollowBox.Location = new System.Drawing.Point(1092, 362);
			this.addressFollowBox.Name = "addressFollowBox";
			this.addressFollowBox.Size = new System.Drawing.Size(121, 23);
			this.addressFollowBox.TabIndex = 25;
			this.addressFollowBox.SelectionChangeCommitted += new System.EventHandler(this.addressFollowBox_SelectionChangeCommitted);
			// 
			// txtExecBase
			// 
			this.txtExecBase.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtExecBase.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.txtExecBase.Location = new System.Drawing.Point(742, 15);
			this.txtExecBase.Multiline = true;
			this.txtExecBase.Name = "txtExecBase";
			this.txtExecBase.Size = new System.Drawing.Size(343, 538);
			this.txtExecBase.TabIndex = 26;
			this.txtExecBase.WordWrap = false;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1283, 568);
			this.Controls.Add(this.txtExecBase);
			this.Controls.Add(this.addressFollowBox);
			this.Controls.Add(this.colour15);
			this.Controls.Add(this.colour11);
			this.Controls.Add(this.colour14);
			this.Controls.Add(this.colour10);
			this.Controls.Add(this.colour13);
			this.Controls.Add(this.colour12);
			this.Controls.Add(this.colour9);
			this.Controls.Add(this.colour8);
			this.Controls.Add(this.colour7);
			this.Controls.Add(this.colour6);
			this.Controls.Add(this.colour5);
			this.Controls.Add(this.colour4);
			this.Controls.Add(this.colour3);
			this.Controls.Add(this.colour2);
			this.Controls.Add(this.colour1);
			this.Controls.Add(this.colour0);
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
			this.Name = "Form1";
			this.Text = "RunAmiga";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.picPower)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.picDisk)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour0)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour3)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour7)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour6)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour5)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour4)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour11)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour10)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour9)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour8)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour15)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour14)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour13)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.colour12)).EndInit();
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
		private System.Windows.Forms.PictureBox colour0;
		private System.Windows.Forms.PictureBox colour1;
		private System.Windows.Forms.PictureBox colour2;
		private System.Windows.Forms.PictureBox colour3;
		private System.Windows.Forms.PictureBox colour7;
		private System.Windows.Forms.PictureBox colour6;
		private System.Windows.Forms.PictureBox colour5;
		private System.Windows.Forms.PictureBox colour4;
		private System.Windows.Forms.PictureBox colour11;
		private System.Windows.Forms.PictureBox colour10;
		private System.Windows.Forms.PictureBox colour9;
		private System.Windows.Forms.PictureBox colour8;
		private System.Windows.Forms.PictureBox colour15;
		private System.Windows.Forms.PictureBox colour14;
		private System.Windows.Forms.PictureBox colour13;
		private System.Windows.Forms.PictureBox colour12;
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
	}
}

