
namespace RunAmiga.UI.Settings
{
	partial class Settings
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.cbQuickStart = new System.Windows.Forms.ComboBox();
			this.btnQuickStart = new System.Windows.Forms.Button();
			this.cbSku = new System.Windows.Forms.ComboBox();
			this.rbNative = new System.Windows.Forms.RadioButton();
			this.rbMusashi = new System.Windows.Forms.RadioButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.cbChipset = new System.Windows.Forms.ComboBox();
			this.txtKickstart = new System.Windows.Forms.TextBox();
			this.panel3 = new System.Windows.Forms.Panel();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.btnROMPick = new System.Windows.Forms.Button();
			this.panel4 = new System.Windows.Forms.Panel();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.btnDF3Pick = new System.Windows.Forms.Button();
			this.txtDF0 = new System.Windows.Forms.TextBox();
			this.btnDF2Pick = new System.Windows.Forms.Button();
			this.nudFloppyCount = new System.Windows.Forms.NumericUpDown();
			this.btnDF1Pick = new System.Windows.Forms.Button();
			this.txtDF1 = new System.Windows.Forms.TextBox();
			this.btnDF0Pick = new System.Windows.Forms.Button();
			this.txtDF2 = new System.Windows.Forms.TextBox();
			this.txtDF3 = new System.Windows.Forms.TextBox();
			this.dudZ2 = new System.Windows.Forms.DomainUpDown();
			this.dudTrapdoor = new System.Windows.Forms.DomainUpDown();
			this.dudZ3 = new System.Windows.Forms.DomainUpDown();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.dudChipRAM = new System.Windows.Forms.DomainUpDown();
			this.dudCPUSlot = new System.Windows.Forms.DomainUpDown();
			this.dudMotherboard = new System.Windows.Forms.DomainUpDown();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.btnGo = new System.Windows.Forms.Button();
			this.btnExit = new System.Windows.Forms.Button();
			this.panel5 = new System.Windows.Forms.Panel();
			this.btnLoadConfig = new System.Windows.Forms.Button();
			this.btnSaveAsConfig = new System.Windows.Forms.Button();
			this.btnSaveConfig = new System.Windows.Forms.Button();
			this.panel6 = new System.Windows.Forms.Panel();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.cbAudio = new System.Windows.Forms.CheckBox();
			this.panel7 = new System.Windows.Forms.Panel();
			this.groupBox7 = new System.Windows.Forms.GroupBox();
			this.cbDiskController = new System.Windows.Forms.ComboBox();
			this.panel1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.panel3.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.panel4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudFloppyCount)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.panel5.SuspendLayout();
			this.panel6.SuspendLayout();
			this.groupBox6.SuspendLayout();
			this.groupBox7.SuspendLayout();
			this.SuspendLayout();
			// 
			// cbQuickStart
			// 
			this.cbQuickStart.FormattingEnabled = true;
			this.cbQuickStart.Items.AddRange(new object[] {
            "current configuration",
            "A500, 512KB+512KB, OCS, KS1.3",
            "A500+, 1MB+1MB, ECS, KS2.04",
            "A600, 1MB, ECS, KS2.05",
            "A1200, 2MB, AGA, KS3.1",
            "A3000, 1MB+16MB+256MB, ECS, KS3.1",
            "A4000, 2MB+16MB+256MB, AGA, KS3.1"});
			this.cbQuickStart.Location = new System.Drawing.Point(12, 12);
			this.cbQuickStart.Name = "cbQuickStart";
			this.cbQuickStart.Size = new System.Drawing.Size(350, 23);
			this.cbQuickStart.TabIndex = 1;
			this.cbQuickStart.SelectedValueChanged += new System.EventHandler(this.cbQuickStart_SelectedValueChanged);
			// 
			// btnQuickStart
			// 
			this.btnQuickStart.Location = new System.Drawing.Point(369, 13);
			this.btnQuickStart.Name = "btnQuickStart";
			this.btnQuickStart.Size = new System.Drawing.Size(75, 23);
			this.btnQuickStart.TabIndex = 2;
			this.btnQuickStart.Text = "Quick Start";
			this.btnQuickStart.UseVisualStyleBackColor = true;
			this.btnQuickStart.Click += new System.EventHandler(this.btnQuickStart_Click);
			// 
			// cbSku
			// 
			this.cbSku.FormattingEnabled = true;
			this.cbSku.Items.AddRange(new object[] {
            "MC68000",
            "MC68EC020",
            "MC68030"});
			this.cbSku.Location = new System.Drawing.Point(6, 22);
			this.cbSku.Name = "cbSku";
			this.cbSku.Size = new System.Drawing.Size(121, 23);
			this.cbSku.TabIndex = 3;
			this.cbSku.SelectedValueChanged += new System.EventHandler(this.cbCPU_SelectedValueChanged);
			// 
			// rbNative
			// 
			this.rbNative.AutoSize = true;
			this.rbNative.Location = new System.Drawing.Point(142, 19);
			this.rbNative.Name = "rbNative";
			this.rbNative.Size = new System.Drawing.Size(59, 19);
			this.rbNative.TabIndex = 4;
			this.rbNative.TabStop = true;
			this.rbNative.Text = "Native";
			this.rbNative.UseVisualStyleBackColor = true;
			this.rbNative.CheckedChanged += new System.EventHandler(this.rbNative_CheckedChanged);
			// 
			// rbMusashi
			// 
			this.rbMusashi.AutoSize = true;
			this.rbMusashi.Location = new System.Drawing.Point(142, 37);
			this.rbMusashi.Name = "rbMusashi";
			this.rbMusashi.Size = new System.Drawing.Size(69, 19);
			this.rbMusashi.TabIndex = 5;
			this.rbMusashi.TabStop = true;
			this.rbMusashi.Text = "Musashi";
			this.rbMusashi.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.groupBox1);
			this.panel1.Location = new System.Drawing.Point(12, 42);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(238, 90);
			this.panel1.TabIndex = 6;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.cbSku);
			this.groupBox1.Controls.Add(this.rbNative);
			this.groupBox1.Controls.Add(this.rbMusashi);
			this.groupBox1.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox1.Location = new System.Drawing.Point(0, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(226, 72);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "CPU";
			// 
			// panel2
			// 
			this.panel2.Location = new System.Drawing.Point(12, 138);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(238, 54);
			this.panel2.TabIndex = 8;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.cbChipset);
			this.groupBox2.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox2.Location = new System.Drawing.Point(12, 138);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(226, 54);
			this.groupBox2.TabIndex = 0;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Chipset";
			// 
			// cbChipset
			// 
			this.cbChipset.FormattingEnabled = true;
			this.cbChipset.Items.AddRange(new object[] {
            "OCS",
            "ECS",
            "AGA"});
			this.cbChipset.Location = new System.Drawing.Point(6, 22);
			this.cbChipset.Name = "cbChipset";
			this.cbChipset.Size = new System.Drawing.Size(121, 23);
			this.cbChipset.TabIndex = 0;
			this.cbChipset.SelectedValueChanged += new System.EventHandler(this.cbChipset_SelectedValueChanged);
			// 
			// txtKickstart
			// 
			this.txtKickstart.Location = new System.Drawing.Point(6, 22);
			this.txtKickstart.Name = "txtKickstart";
			this.txtKickstart.Size = new System.Drawing.Size(307, 23);
			this.txtKickstart.TabIndex = 9;
			// 
			// panel3
			// 
			this.panel3.Controls.Add(this.groupBox3);
			this.panel3.Location = new System.Drawing.Point(256, 249);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(425, 60);
			this.panel3.TabIndex = 10;
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.txtKickstart);
			this.groupBox3.Controls.Add(this.btnROMPick);
			this.groupBox3.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox3.Location = new System.Drawing.Point(7, 1);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(418, 55);
			this.groupBox3.TabIndex = 11;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Kickstart";
			// 
			// btnROMPick
			// 
			this.btnROMPick.Location = new System.Drawing.Point(320, 21);
			this.btnROMPick.Name = "btnROMPick";
			this.btnROMPick.Size = new System.Drawing.Size(35, 23);
			this.btnROMPick.TabIndex = 10;
			this.btnROMPick.Text = "...";
			this.btnROMPick.UseVisualStyleBackColor = true;
			this.btnROMPick.Click += new System.EventHandler(this.btnROMPick_Click);
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.groupBox5);
			this.panel4.Location = new System.Drawing.Point(256, 42);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(425, 141);
			this.panel4.TabIndex = 11;
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.btnDF3Pick);
			this.groupBox5.Controls.Add(this.txtDF0);
			this.groupBox5.Controls.Add(this.btnDF2Pick);
			this.groupBox5.Controls.Add(this.nudFloppyCount);
			this.groupBox5.Controls.Add(this.btnDF1Pick);
			this.groupBox5.Controls.Add(this.txtDF1);
			this.groupBox5.Controls.Add(this.btnDF0Pick);
			this.groupBox5.Controls.Add(this.txtDF2);
			this.groupBox5.Controls.Add(this.txtDF3);
			this.groupBox5.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox5.Location = new System.Drawing.Point(0, 1);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(418, 141);
			this.groupBox5.TabIndex = 16;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Floppy Disk";
			// 
			// btnDF3Pick
			// 
			this.btnDF3Pick.Location = new System.Drawing.Point(320, 106);
			this.btnDF3Pick.Name = "btnDF3Pick";
			this.btnDF3Pick.Size = new System.Drawing.Size(35, 23);
			this.btnDF3Pick.TabIndex = 8;
			this.btnDF3Pick.Text = "...";
			this.btnDF3Pick.UseVisualStyleBackColor = true;
			this.btnDF3Pick.Click += new System.EventHandler(this.btnDF3Pick_Click);
			// 
			// txtDF0
			// 
			this.txtDF0.Location = new System.Drawing.Point(6, 21);
			this.txtDF0.Name = "txtDF0";
			this.txtDF0.Size = new System.Drawing.Size(307, 23);
			this.txtDF0.TabIndex = 1;
			// 
			// btnDF2Pick
			// 
			this.btnDF2Pick.Location = new System.Drawing.Point(320, 78);
			this.btnDF2Pick.Name = "btnDF2Pick";
			this.btnDF2Pick.Size = new System.Drawing.Size(35, 23);
			this.btnDF2Pick.TabIndex = 7;
			this.btnDF2Pick.Text = "...";
			this.btnDF2Pick.UseVisualStyleBackColor = true;
			this.btnDF2Pick.Click += new System.EventHandler(this.btnDF2Pick_Click);
			// 
			// nudFloppyCount
			// 
			this.nudFloppyCount.Location = new System.Drawing.Point(359, 20);
			this.nudFloppyCount.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
			this.nudFloppyCount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudFloppyCount.Name = "nudFloppyCount";
			this.nudFloppyCount.ReadOnly = true;
			this.nudFloppyCount.Size = new System.Drawing.Size(43, 23);
			this.nudFloppyCount.TabIndex = 0;
			this.nudFloppyCount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudFloppyCount.ValueChanged += new System.EventHandler(this.nudFloppyCount_ValueChanged);
			// 
			// btnDF1Pick
			// 
			this.btnDF1Pick.Location = new System.Drawing.Point(319, 49);
			this.btnDF1Pick.Name = "btnDF1Pick";
			this.btnDF1Pick.Size = new System.Drawing.Size(35, 23);
			this.btnDF1Pick.TabIndex = 6;
			this.btnDF1Pick.Text = "...";
			this.btnDF1Pick.UseVisualStyleBackColor = true;
			this.btnDF1Pick.Click += new System.EventHandler(this.btnDF1Pick_Click);
			// 
			// txtDF1
			// 
			this.txtDF1.Location = new System.Drawing.Point(6, 50);
			this.txtDF1.Name = "txtDF1";
			this.txtDF1.Size = new System.Drawing.Size(307, 23);
			this.txtDF1.TabIndex = 2;
			// 
			// btnDF0Pick
			// 
			this.btnDF0Pick.Location = new System.Drawing.Point(319, 20);
			this.btnDF0Pick.Name = "btnDF0Pick";
			this.btnDF0Pick.Size = new System.Drawing.Size(35, 23);
			this.btnDF0Pick.TabIndex = 5;
			this.btnDF0Pick.Text = "...";
			this.btnDF0Pick.UseVisualStyleBackColor = true;
			this.btnDF0Pick.Click += new System.EventHandler(this.btnDF0Pick_Click);
			// 
			// txtDF2
			// 
			this.txtDF2.Location = new System.Drawing.Point(6, 79);
			this.txtDF2.Name = "txtDF2";
			this.txtDF2.Size = new System.Drawing.Size(307, 23);
			this.txtDF2.TabIndex = 3;
			// 
			// txtDF3
			// 
			this.txtDF3.Location = new System.Drawing.Point(6, 108);
			this.txtDF3.Name = "txtDF3";
			this.txtDF3.Size = new System.Drawing.Size(307, 23);
			this.txtDF3.TabIndex = 4;
			// 
			// dudZ2
			// 
			this.dudZ2.Items.Add("8.0");
			this.dudZ2.Items.Add("4.0");
			this.dudZ2.Items.Add("2.0");
			this.dudZ2.Items.Add("1.0");
			this.dudZ2.Items.Add("0.5");
			this.dudZ2.Items.Add("0");
			this.dudZ2.Location = new System.Drawing.Point(90, 77);
			this.dudZ2.Name = "dudZ2";
			this.dudZ2.ReadOnly = true;
			this.dudZ2.Size = new System.Drawing.Size(120, 23);
			this.dudZ2.TabIndex = 12;
			this.dudZ2.Text = "ZorroII RAM";
			// 
			// dudTrapdoor
			// 
			this.dudTrapdoor.Items.Add("1.75");
			this.dudTrapdoor.Items.Add("1.5");
			this.dudTrapdoor.Items.Add("1.0");
			this.dudTrapdoor.Items.Add("0.5");
			this.dudTrapdoor.Items.Add("0");
			this.dudTrapdoor.Location = new System.Drawing.Point(90, 48);
			this.dudTrapdoor.Name = "dudTrapdoor";
			this.dudTrapdoor.ReadOnly = true;
			this.dudTrapdoor.Size = new System.Drawing.Size(120, 23);
			this.dudTrapdoor.TabIndex = 13;
			this.dudTrapdoor.Text = "Trapdoor RAM";
			// 
			// dudZ3
			// 
			this.dudZ3.Items.Add("512+512+512");
			this.dudZ3.Items.Add("512+512");
			this.dudZ3.Items.Add("256+256");
			this.dudZ3.Items.Add("1024");
			this.dudZ3.Items.Add("512");
			this.dudZ3.Items.Add("256");
			this.dudZ3.Items.Add("128");
			this.dudZ3.Items.Add("0");
			this.dudZ3.Location = new System.Drawing.Point(90, 106);
			this.dudZ3.Name = "dudZ3";
			this.dudZ3.ReadOnly = true;
			this.dudZ3.Size = new System.Drawing.Size(120, 23);
			this.dudZ3.TabIndex = 14;
			this.dudZ3.Text = "ZorroIII RAM";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.label6);
			this.groupBox4.Controls.Add(this.label5);
			this.groupBox4.Controls.Add(this.label4);
			this.groupBox4.Controls.Add(this.label3);
			this.groupBox4.Controls.Add(this.label2);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Controls.Add(this.dudChipRAM);
			this.groupBox4.Controls.Add(this.dudCPUSlot);
			this.groupBox4.Controls.Add(this.dudMotherboard);
			this.groupBox4.Controls.Add(this.dudZ2);
			this.groupBox4.Controls.Add(this.dudZ3);
			this.groupBox4.Controls.Add(this.dudTrapdoor);
			this.groupBox4.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox4.Location = new System.Drawing.Point(0, 0);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(226, 197);
			this.groupBox4.TabIndex = 16;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Memory";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(7, 164);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(53, 15);
			this.label6.TabIndex = 23;
			this.label6.Text = "CPU Slot";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(7, 135);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(77, 15);
			this.label5.TabIndex = 22;
			this.label5.Text = "Motherboard";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 106);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(48, 15);
			this.label4.TabIndex = 21;
			this.label4.Text = "Zorro III";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(7, 77);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(45, 15);
			this.label3.TabIndex = 20;
			this.label3.Text = "Zorro II";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(7, 48);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(54, 15);
			this.label2.TabIndex = 19;
			this.label2.Text = "Trapdoor";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(7, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(32, 15);
			this.label1.TabIndex = 18;
			this.label1.Text = "Chip";
			// 
			// dudChipRAM
			// 
			this.dudChipRAM.Items.Add("2.0");
			this.dudChipRAM.Items.Add("1.0");
			this.dudChipRAM.Items.Add("0.5");
			this.dudChipRAM.Location = new System.Drawing.Point(90, 19);
			this.dudChipRAM.Name = "dudChipRAM";
			this.dudChipRAM.ReadOnly = true;
			this.dudChipRAM.Size = new System.Drawing.Size(120, 23);
			this.dudChipRAM.TabIndex = 17;
			this.dudChipRAM.Text = "Chip RAM";
			// 
			// dudCPUSlot
			// 
			this.dudCPUSlot.Items.Add("128");
			this.dudCPUSlot.Items.Add("64");
			this.dudCPUSlot.Items.Add("32");
			this.dudCPUSlot.Items.Add("16");
			this.dudCPUSlot.Items.Add("8");
			this.dudCPUSlot.Items.Add("0");
			this.dudCPUSlot.Location = new System.Drawing.Point(90, 164);
			this.dudCPUSlot.Name = "dudCPUSlot";
			this.dudCPUSlot.ReadOnly = true;
			this.dudCPUSlot.Size = new System.Drawing.Size(120, 23);
			this.dudCPUSlot.TabIndex = 16;
			this.dudCPUSlot.Text = "CPU Slot RAM";
			// 
			// dudMotherboard
			// 
			this.dudMotherboard.Items.Add("64");
			this.dudMotherboard.Items.Add("32");
			this.dudMotherboard.Items.Add("16");
			this.dudMotherboard.Items.Add("8");
			this.dudMotherboard.Items.Add("0");
			this.dudMotherboard.Location = new System.Drawing.Point(90, 135);
			this.dudMotherboard.Name = "dudMotherboard";
			this.dudMotherboard.ReadOnly = true;
			this.dudMotherboard.Size = new System.Drawing.Size(120, 23);
			this.dudMotherboard.TabIndex = 15;
			this.dudMotherboard.Text = "Motherboard RAM";
			// 
			// btnGo
			// 
			this.btnGo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
			this.btnGo.Location = new System.Drawing.Point(600, 373);
			this.btnGo.Name = "btnGo";
			this.btnGo.Size = new System.Drawing.Size(75, 23);
			this.btnGo.TabIndex = 16;
			this.btnGo.Text = "Go!";
			this.btnGo.UseVisualStyleBackColor = true;
			this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
			// 
			// btnExit
			// 
			this.btnExit.Location = new System.Drawing.Point(266, 372);
			this.btnExit.Name = "btnExit";
			this.btnExit.Size = new System.Drawing.Size(75, 23);
			this.btnExit.TabIndex = 17;
			this.btnExit.Text = "Exit";
			this.btnExit.UseVisualStyleBackColor = true;
			this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
			// 
			// panel5
			// 
			this.panel5.Controls.Add(this.groupBox4);
			this.panel5.Location = new System.Drawing.Point(12, 198);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(238, 197);
			this.panel5.TabIndex = 18;
			// 
			// btnLoadConfig
			// 
			this.btnLoadConfig.Location = new System.Drawing.Point(357, 372);
			this.btnLoadConfig.Name = "btnLoadConfig";
			this.btnLoadConfig.Size = new System.Drawing.Size(75, 23);
			this.btnLoadConfig.TabIndex = 19;
			this.btnLoadConfig.Text = "Load ...";
			this.btnLoadConfig.UseVisualStyleBackColor = true;
			this.btnLoadConfig.Click += new System.EventHandler(this.btnLoadConfig_Click);
			// 
			// btnSaveAsConfig
			// 
			this.btnSaveAsConfig.Location = new System.Drawing.Point(519, 373);
			this.btnSaveAsConfig.Name = "btnSaveAsConfig";
			this.btnSaveAsConfig.Size = new System.Drawing.Size(75, 23);
			this.btnSaveAsConfig.TabIndex = 20;
			this.btnSaveAsConfig.Text = "Save As...";
			this.btnSaveAsConfig.UseVisualStyleBackColor = true;
			this.btnSaveAsConfig.Click += new System.EventHandler(this.btnSaveAsConfig_Click);
			// 
			// btnSaveConfig
			// 
			this.btnSaveConfig.Enabled = false;
			this.btnSaveConfig.Location = new System.Drawing.Point(438, 373);
			this.btnSaveConfig.Name = "btnSaveConfig";
			this.btnSaveConfig.Size = new System.Drawing.Size(75, 23);
			this.btnSaveConfig.TabIndex = 21;
			this.btnSaveConfig.Text = "Save";
			this.btnSaveConfig.UseVisualStyleBackColor = true;
			this.btnSaveConfig.Click += new System.EventHandler(this.btnSaveConfig_Click);
			// 
			// panel6
			// 
			this.panel6.Controls.Add(this.groupBox6);
			this.panel6.Location = new System.Drawing.Point(256, 315);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(425, 52);
			this.panel6.TabIndex = 22;
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.cbAudio);
			this.groupBox6.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox6.Location = new System.Drawing.Point(1, 0);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(418, 51);
			this.groupBox6.TabIndex = 0;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Miscellaneous";
			// 
			// cbAudio
			// 
			this.cbAudio.AutoSize = true;
			this.cbAudio.Location = new System.Drawing.Point(6, 22);
			this.cbAudio.Name = "cbAudio";
			this.cbAudio.Size = new System.Drawing.Size(58, 19);
			this.cbAudio.TabIndex = 0;
			this.cbAudio.Text = "Audio";
			this.cbAudio.UseVisualStyleBackColor = true;
			// 
			// panel7
			// 
			this.panel7.Location = new System.Drawing.Point(256, 188);
			this.panel7.Name = "panel7";
			this.panel7.Size = new System.Drawing.Size(425, 56);
			this.panel7.TabIndex = 23;
			// 
			// groupBox7
			// 
			this.groupBox7.Controls.Add(this.cbDiskController);
			this.groupBox7.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox7.Location = new System.Drawing.Point(256, 188);
			this.groupBox7.Name = "groupBox7";
			this.groupBox7.Size = new System.Drawing.Size(418, 55);
			this.groupBox7.TabIndex = 0;
			this.groupBox7.TabStop = false;
			this.groupBox7.Text = "Hard Disk";
			// 
			// cbDiskController
			// 
			this.cbDiskController.FormattingEnabled = true;
			this.cbDiskController.Items.AddRange(new object[] {
            "None",
            "A600_A1200",
            "A3000",
            "A4000"});
			this.cbDiskController.Location = new System.Drawing.Point(13, 23);
			this.cbDiskController.Name = "cbDiskController";
			this.cbDiskController.Size = new System.Drawing.Size(175, 23);
			this.cbDiskController.TabIndex = 0;
			// 
			// Settings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(688, 408);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox7);
			this.Controls.Add(this.panel7);
			this.Controls.Add(this.panel6);
			this.Controls.Add(this.btnSaveConfig);
			this.Controls.Add(this.btnSaveAsConfig);
			this.Controls.Add(this.btnLoadConfig);
			this.Controls.Add(this.panel5);
			this.Controls.Add(this.btnExit);
			this.Controls.Add(this.btnGo);
			this.Controls.Add(this.panel4);
			this.Controls.Add(this.panel3);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.btnQuickStart);
			this.Controls.Add(this.cbQuickStart);
			this.Name = "Settings";
			this.ShowIcon = false;
			this.Text = "Emulation Settings";
			this.panel1.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.panel3.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.panel4.ResumeLayout(false);
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudFloppyCount)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.panel5.ResumeLayout(false);
			this.panel6.ResumeLayout(false);
			this.groupBox6.ResumeLayout(false);
			this.groupBox6.PerformLayout();
			this.groupBox7.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox cbQuickStart;
		private System.Windows.Forms.Button btnQuickStart;
		private System.Windows.Forms.ComboBox cbSku;
		private System.Windows.Forms.RadioButton rbNative;
		private System.Windows.Forms.RadioButton rbMusashi;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.ComboBox cbChipset;
		private System.Windows.Forms.TextBox txtKickstart;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Button btnROMPick;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Button btnDF3Pick;
		private System.Windows.Forms.Button btnDF2Pick;
		private System.Windows.Forms.Button btnDF1Pick;
		private System.Windows.Forms.Button btnDF0Pick;
		private System.Windows.Forms.TextBox txtDF3;
		private System.Windows.Forms.TextBox txtDF2;
		private System.Windows.Forms.TextBox txtDF1;
		private System.Windows.Forms.TextBox txtDF0;
		private System.Windows.Forms.NumericUpDown nudFloppyCount;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.DomainUpDown dudZ2;
		private System.Windows.Forms.DomainUpDown dudTrapdoor;
		private System.Windows.Forms.DomainUpDown dudZ3;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Button btnGo;
		private System.Windows.Forms.Button btnExit;
		private System.Windows.Forms.DomainUpDown dudCPUSlot;
		private System.Windows.Forms.DomainUpDown dudMotherboard;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.Button btnLoadConfig;
		private System.Windows.Forms.Button btnSaveAsConfig;
		private System.Windows.Forms.Button btnSaveConfig;
		private System.Windows.Forms.Panel panel6;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.CheckBox cbAudio;
		private System.Windows.Forms.Panel panel7;
		private System.Windows.Forms.GroupBox groupBox7;
		private System.Windows.Forms.ComboBox cbDiskController;
		private System.Windows.Forms.DomainUpDown dudChipRAM;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
	}
}