
/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.UI.Settings
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
			cbQuickStart = new System.Windows.Forms.ComboBox();
			btnQuickStart = new System.Windows.Forms.Button();
			cbSku = new System.Windows.Forms.ComboBox();
			rbNative = new System.Windows.Forms.RadioButton();
			rbMusashi = new System.Windows.Forms.RadioButton();
			panel1 = new System.Windows.Forms.Panel();
			groupBox1 = new System.Windows.Forms.GroupBox();
			panel2 = new System.Windows.Forms.Panel();
			groupBox2 = new System.Windows.Forms.GroupBox();
			cbChipset = new System.Windows.Forms.ComboBox();
			txtKickstart = new System.Windows.Forms.TextBox();
			panel3 = new System.Windows.Forms.Panel();
			groupBox3 = new System.Windows.Forms.GroupBox();
			btnROMPick = new System.Windows.Forms.Button();
			panel4 = new System.Windows.Forms.Panel();
			groupBox5 = new System.Windows.Forms.GroupBox();
			btnDF3Pick = new System.Windows.Forms.Button();
			txtDF0 = new System.Windows.Forms.TextBox();
			btnDF2Pick = new System.Windows.Forms.Button();
			nudFloppyCount = new System.Windows.Forms.NumericUpDown();
			btnDF1Pick = new System.Windows.Forms.Button();
			txtDF1 = new System.Windows.Forms.TextBox();
			btnDF0Pick = new System.Windows.Forms.Button();
			txtDF2 = new System.Windows.Forms.TextBox();
			txtDF3 = new System.Windows.Forms.TextBox();
			dudZ2 = new System.Windows.Forms.DomainUpDown();
			dudTrapdoor = new System.Windows.Forms.DomainUpDown();
			dudZ3 = new System.Windows.Forms.DomainUpDown();
			groupBox4 = new System.Windows.Forms.GroupBox();
			label6 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			dudChipRAM = new System.Windows.Forms.DomainUpDown();
			dudCPUSlot = new System.Windows.Forms.DomainUpDown();
			dudMotherboard = new System.Windows.Forms.DomainUpDown();
			openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			btnGo = new System.Windows.Forms.Button();
			btnExit = new System.Windows.Forms.Button();
			panel5 = new System.Windows.Forms.Panel();
			btnLoadConfig = new System.Windows.Forms.Button();
			btnSaveAsConfig = new System.Windows.Forms.Button();
			btnSaveConfig = new System.Windows.Forms.Button();
			panel6 = new System.Windows.Forms.Panel();
			groupBox6 = new System.Windows.Forms.GroupBox();
			cbDebugging = new System.Windows.Forms.CheckBox();
			cbAudio = new System.Windows.Forms.CheckBox();
			panel7 = new System.Windows.Forms.Panel();
			groupBox7 = new System.Windows.Forms.GroupBox();
			cbDiskController = new System.Windows.Forms.ComboBox();
			panel1.SuspendLayout();
			groupBox1.SuspendLayout();
			groupBox2.SuspendLayout();
			panel3.SuspendLayout();
			groupBox3.SuspendLayout();
			panel4.SuspendLayout();
			groupBox5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)nudFloppyCount).BeginInit();
			groupBox4.SuspendLayout();
			panel5.SuspendLayout();
			panel6.SuspendLayout();
			groupBox6.SuspendLayout();
			groupBox7.SuspendLayout();
			SuspendLayout();
			// 
			// cbQuickStart
			// 
			cbQuickStart.FormattingEnabled = true;
			cbQuickStart.Items.AddRange(new object[] { "current configuration", "A500, 512KB+512KB, OCS, KS1.3", "A500+, 1MB+1MB, ECS, KS2.04", "A600, 1MB, ECS, KS2.05", "A1200, 2MB, AGA, KS3.1", "A3000, 1MB+16MB+256MB, ECS, KS3.1", "A4000, 2MB+16MB+256MB, AGA, KS3.1" });
			cbQuickStart.Location = new System.Drawing.Point(12, 12);
			cbQuickStart.Name = "cbQuickStart";
			cbQuickStart.Size = new System.Drawing.Size(350, 23);
			cbQuickStart.TabIndex = 1;
			cbQuickStart.SelectedValueChanged += cbQuickStart_SelectedValueChanged;
			// 
			// btnQuickStart
			// 
			btnQuickStart.Location = new System.Drawing.Point(369, 13);
			btnQuickStart.Name = "btnQuickStart";
			btnQuickStart.Size = new System.Drawing.Size(75, 23);
			btnQuickStart.TabIndex = 2;
			btnQuickStart.Text = "Quick Start";
			btnQuickStart.UseVisualStyleBackColor = true;
			btnQuickStart.Click += btnQuickStart_Click;
			// 
			// cbSku
			// 
			cbSku.FormattingEnabled = true;
			cbSku.Items.AddRange(new object[] { "MC68000", "MC68EC020", "MC68030" });
			cbSku.Location = new System.Drawing.Point(6, 22);
			cbSku.Name = "cbSku";
			cbSku.Size = new System.Drawing.Size(121, 23);
			cbSku.TabIndex = 3;
			cbSku.SelectedValueChanged += cbCPU_SelectedValueChanged;
			// 
			// rbNative
			// 
			rbNative.AutoSize = true;
			rbNative.Location = new System.Drawing.Point(142, 19);
			rbNative.Name = "rbNative";
			rbNative.Size = new System.Drawing.Size(59, 19);
			rbNative.TabIndex = 4;
			rbNative.TabStop = true;
			rbNative.Text = "Native";
			rbNative.UseVisualStyleBackColor = true;
			rbNative.CheckedChanged += rbNative_CheckedChanged;
			// 
			// rbMusashi
			// 
			rbMusashi.AutoSize = true;
			rbMusashi.Location = new System.Drawing.Point(142, 37);
			rbMusashi.Name = "rbMusashi";
			rbMusashi.Size = new System.Drawing.Size(69, 19);
			rbMusashi.TabIndex = 5;
			rbMusashi.TabStop = true;
			rbMusashi.Text = "Musashi";
			rbMusashi.UseVisualStyleBackColor = true;
			// 
			// panel1
			// 
			panel1.Controls.Add(groupBox1);
			panel1.Location = new System.Drawing.Point(12, 42);
			panel1.Name = "panel1";
			panel1.Size = new System.Drawing.Size(238, 90);
			panel1.TabIndex = 6;
			// 
			// groupBox1
			// 
			groupBox1.Controls.Add(cbSku);
			groupBox1.Controls.Add(rbNative);
			groupBox1.Controls.Add(rbMusashi);
			groupBox1.ForeColor = System.Drawing.SystemColors.Highlight;
			groupBox1.Location = new System.Drawing.Point(0, 0);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new System.Drawing.Size(226, 72);
			groupBox1.TabIndex = 6;
			groupBox1.TabStop = false;
			groupBox1.Text = "CPU";
			// 
			// panel2
			// 
			panel2.Location = new System.Drawing.Point(12, 138);
			panel2.Name = "panel2";
			panel2.Size = new System.Drawing.Size(238, 54);
			panel2.TabIndex = 8;
			// 
			// groupBox2
			// 
			groupBox2.Controls.Add(cbChipset);
			groupBox2.ForeColor = System.Drawing.SystemColors.Highlight;
			groupBox2.Location = new System.Drawing.Point(12, 138);
			groupBox2.Name = "groupBox2";
			groupBox2.Size = new System.Drawing.Size(226, 54);
			groupBox2.TabIndex = 0;
			groupBox2.TabStop = false;
			groupBox2.Text = "Chipset";
			// 
			// cbChipset
			// 
			cbChipset.FormattingEnabled = true;
			cbChipset.Items.AddRange(new object[] { "OCS", "ECS", "AGA" });
			cbChipset.Location = new System.Drawing.Point(6, 22);
			cbChipset.Name = "cbChipset";
			cbChipset.Size = new System.Drawing.Size(121, 23);
			cbChipset.TabIndex = 0;
			cbChipset.SelectedValueChanged += cbChipset_SelectedValueChanged;
			// 
			// txtKickstart
			// 
			txtKickstart.Location = new System.Drawing.Point(6, 22);
			txtKickstart.Name = "txtKickstart";
			txtKickstart.Size = new System.Drawing.Size(307, 23);
			txtKickstart.TabIndex = 9;
			// 
			// panel3
			// 
			panel3.Controls.Add(groupBox3);
			panel3.Location = new System.Drawing.Point(256, 249);
			panel3.Name = "panel3";
			panel3.Size = new System.Drawing.Size(425, 60);
			panel3.TabIndex = 10;
			// 
			// groupBox3
			// 
			groupBox3.Controls.Add(txtKickstart);
			groupBox3.Controls.Add(btnROMPick);
			groupBox3.ForeColor = System.Drawing.SystemColors.Highlight;
			groupBox3.Location = new System.Drawing.Point(7, 1);
			groupBox3.Name = "groupBox3";
			groupBox3.Size = new System.Drawing.Size(418, 55);
			groupBox3.TabIndex = 11;
			groupBox3.TabStop = false;
			groupBox3.Text = "Kickstart";
			// 
			// btnROMPick
			// 
			btnROMPick.Location = new System.Drawing.Point(320, 21);
			btnROMPick.Name = "btnROMPick";
			btnROMPick.Size = new System.Drawing.Size(35, 23);
			btnROMPick.TabIndex = 10;
			btnROMPick.Text = "...";
			btnROMPick.UseVisualStyleBackColor = true;
			btnROMPick.Click += btnROMPick_Click;
			// 
			// panel4
			// 
			panel4.Controls.Add(groupBox5);
			panel4.Location = new System.Drawing.Point(256, 42);
			panel4.Name = "panel4";
			panel4.Size = new System.Drawing.Size(425, 141);
			panel4.TabIndex = 11;
			// 
			// groupBox5
			// 
			groupBox5.Controls.Add(btnDF3Pick);
			groupBox5.Controls.Add(txtDF0);
			groupBox5.Controls.Add(btnDF2Pick);
			groupBox5.Controls.Add(nudFloppyCount);
			groupBox5.Controls.Add(btnDF1Pick);
			groupBox5.Controls.Add(txtDF1);
			groupBox5.Controls.Add(btnDF0Pick);
			groupBox5.Controls.Add(txtDF2);
			groupBox5.Controls.Add(txtDF3);
			groupBox5.ForeColor = System.Drawing.SystemColors.Highlight;
			groupBox5.Location = new System.Drawing.Point(0, 1);
			groupBox5.Name = "groupBox5";
			groupBox5.Size = new System.Drawing.Size(418, 141);
			groupBox5.TabIndex = 16;
			groupBox5.TabStop = false;
			groupBox5.Text = "Floppy Disk";
			// 
			// btnDF3Pick
			// 
			btnDF3Pick.Location = new System.Drawing.Point(320, 106);
			btnDF3Pick.Name = "btnDF3Pick";
			btnDF3Pick.Size = new System.Drawing.Size(35, 23);
			btnDF3Pick.TabIndex = 8;
			btnDF3Pick.Text = "...";
			btnDF3Pick.UseVisualStyleBackColor = true;
			btnDF3Pick.Click += btnDF3Pick_Click;
			// 
			// txtDF0
			// 
			txtDF0.Location = new System.Drawing.Point(6, 21);
			txtDF0.Name = "txtDF0";
			txtDF0.Size = new System.Drawing.Size(307, 23);
			txtDF0.TabIndex = 1;
			// 
			// btnDF2Pick
			// 
			btnDF2Pick.Location = new System.Drawing.Point(320, 78);
			btnDF2Pick.Name = "btnDF2Pick";
			btnDF2Pick.Size = new System.Drawing.Size(35, 23);
			btnDF2Pick.TabIndex = 7;
			btnDF2Pick.Text = "...";
			btnDF2Pick.UseVisualStyleBackColor = true;
			btnDF2Pick.Click += btnDF2Pick_Click;
			// 
			// nudFloppyCount
			// 
			nudFloppyCount.Location = new System.Drawing.Point(359, 20);
			nudFloppyCount.Maximum = new decimal(new int[] { 4, 0, 0, 0 });
			nudFloppyCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
			nudFloppyCount.Name = "nudFloppyCount";
			nudFloppyCount.ReadOnly = true;
			nudFloppyCount.Size = new System.Drawing.Size(43, 23);
			nudFloppyCount.TabIndex = 0;
			nudFloppyCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
			nudFloppyCount.ValueChanged += nudFloppyCount_ValueChanged;
			// 
			// btnDF1Pick
			// 
			btnDF1Pick.Location = new System.Drawing.Point(319, 49);
			btnDF1Pick.Name = "btnDF1Pick";
			btnDF1Pick.Size = new System.Drawing.Size(35, 23);
			btnDF1Pick.TabIndex = 6;
			btnDF1Pick.Text = "...";
			btnDF1Pick.UseVisualStyleBackColor = true;
			btnDF1Pick.Click += btnDF1Pick_Click;
			// 
			// txtDF1
			// 
			txtDF1.Location = new System.Drawing.Point(6, 50);
			txtDF1.Name = "txtDF1";
			txtDF1.Size = new System.Drawing.Size(307, 23);
			txtDF1.TabIndex = 2;
			// 
			// btnDF0Pick
			// 
			btnDF0Pick.Location = new System.Drawing.Point(319, 20);
			btnDF0Pick.Name = "btnDF0Pick";
			btnDF0Pick.Size = new System.Drawing.Size(35, 23);
			btnDF0Pick.TabIndex = 5;
			btnDF0Pick.Text = "...";
			btnDF0Pick.UseVisualStyleBackColor = true;
			btnDF0Pick.Click += btnDF0Pick_Click;
			// 
			// txtDF2
			// 
			txtDF2.Location = new System.Drawing.Point(6, 79);
			txtDF2.Name = "txtDF2";
			txtDF2.Size = new System.Drawing.Size(307, 23);
			txtDF2.TabIndex = 3;
			// 
			// txtDF3
			// 
			txtDF3.Location = new System.Drawing.Point(6, 108);
			txtDF3.Name = "txtDF3";
			txtDF3.Size = new System.Drawing.Size(307, 23);
			txtDF3.TabIndex = 4;
			// 
			// dudZ2
			// 
			dudZ2.Items.Add("8.0");
			dudZ2.Items.Add("4.0");
			dudZ2.Items.Add("2.0");
			dudZ2.Items.Add("1.0");
			dudZ2.Items.Add("0.5");
			dudZ2.Items.Add("0");
			dudZ2.Location = new System.Drawing.Point(90, 77);
			dudZ2.Name = "dudZ2";
			dudZ2.ReadOnly = true;
			dudZ2.Size = new System.Drawing.Size(120, 23);
			dudZ2.TabIndex = 12;
			dudZ2.Text = "ZorroII RAM";
			// 
			// dudTrapdoor
			// 
			dudTrapdoor.Items.Add("1.75");
			dudTrapdoor.Items.Add("1.5");
			dudTrapdoor.Items.Add("1.0");
			dudTrapdoor.Items.Add("0.5");
			dudTrapdoor.Items.Add("0");
			dudTrapdoor.Location = new System.Drawing.Point(90, 48);
			dudTrapdoor.Name = "dudTrapdoor";
			dudTrapdoor.ReadOnly = true;
			dudTrapdoor.Size = new System.Drawing.Size(120, 23);
			dudTrapdoor.TabIndex = 13;
			dudTrapdoor.Text = "Trapdoor RAM";
			// 
			// dudZ3
			// 
			dudZ3.Items.Add("512+512+512");
			dudZ3.Items.Add("512+512");
			dudZ3.Items.Add("256+256");
			dudZ3.Items.Add("1024");
			dudZ3.Items.Add("512");
			dudZ3.Items.Add("256");
			dudZ3.Items.Add("128");
			dudZ3.Items.Add("0");
			dudZ3.Location = new System.Drawing.Point(90, 106);
			dudZ3.Name = "dudZ3";
			dudZ3.ReadOnly = true;
			dudZ3.Size = new System.Drawing.Size(120, 23);
			dudZ3.TabIndex = 14;
			dudZ3.Text = "ZorroIII RAM";
			// 
			// groupBox4
			// 
			groupBox4.Controls.Add(label6);
			groupBox4.Controls.Add(label5);
			groupBox4.Controls.Add(label4);
			groupBox4.Controls.Add(label3);
			groupBox4.Controls.Add(label2);
			groupBox4.Controls.Add(label1);
			groupBox4.Controls.Add(dudChipRAM);
			groupBox4.Controls.Add(dudCPUSlot);
			groupBox4.Controls.Add(dudMotherboard);
			groupBox4.Controls.Add(dudZ2);
			groupBox4.Controls.Add(dudZ3);
			groupBox4.Controls.Add(dudTrapdoor);
			groupBox4.ForeColor = System.Drawing.SystemColors.Highlight;
			groupBox4.Location = new System.Drawing.Point(0, 0);
			groupBox4.Name = "groupBox4";
			groupBox4.Size = new System.Drawing.Size(226, 197);
			groupBox4.TabIndex = 16;
			groupBox4.TabStop = false;
			groupBox4.Text = "Memory";
			// 
			// label6
			// 
			label6.AutoSize = true;
			label6.Location = new System.Drawing.Point(7, 164);
			label6.Name = "label6";
			label6.Size = new System.Drawing.Size(53, 15);
			label6.TabIndex = 23;
			label6.Text = "CPU Slot";
			// 
			// label5
			// 
			label5.AutoSize = true;
			label5.Location = new System.Drawing.Point(7, 135);
			label5.Name = "label5";
			label5.Size = new System.Drawing.Size(77, 15);
			label5.TabIndex = 22;
			label5.Text = "Motherboard";
			// 
			// label4
			// 
			label4.AutoSize = true;
			label4.Location = new System.Drawing.Point(6, 106);
			label4.Name = "label4";
			label4.Size = new System.Drawing.Size(48, 15);
			label4.TabIndex = 21;
			label4.Text = "Zorro III";
			// 
			// label3
			// 
			label3.AutoSize = true;
			label3.Location = new System.Drawing.Point(7, 77);
			label3.Name = "label3";
			label3.Size = new System.Drawing.Size(45, 15);
			label3.TabIndex = 20;
			label3.Text = "Zorro II";
			// 
			// label2
			// 
			label2.AutoSize = true;
			label2.Location = new System.Drawing.Point(7, 48);
			label2.Name = "label2";
			label2.Size = new System.Drawing.Size(54, 15);
			label2.TabIndex = 19;
			label2.Text = "Trapdoor";
			// 
			// label1
			// 
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point(7, 19);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(32, 15);
			label1.TabIndex = 18;
			label1.Text = "Chip";
			// 
			// dudChipRAM
			// 
			dudChipRAM.Items.Add("2.0");
			dudChipRAM.Items.Add("1.0");
			dudChipRAM.Items.Add("0.5");
			dudChipRAM.Location = new System.Drawing.Point(90, 19);
			dudChipRAM.Name = "dudChipRAM";
			dudChipRAM.ReadOnly = true;
			dudChipRAM.Size = new System.Drawing.Size(120, 23);
			dudChipRAM.TabIndex = 17;
			dudChipRAM.Text = "Chip RAM";
			// 
			// dudCPUSlot
			// 
			dudCPUSlot.Items.Add("128");
			dudCPUSlot.Items.Add("64");
			dudCPUSlot.Items.Add("32");
			dudCPUSlot.Items.Add("16");
			dudCPUSlot.Items.Add("8");
			dudCPUSlot.Items.Add("0");
			dudCPUSlot.Location = new System.Drawing.Point(90, 164);
			dudCPUSlot.Name = "dudCPUSlot";
			dudCPUSlot.ReadOnly = true;
			dudCPUSlot.Size = new System.Drawing.Size(120, 23);
			dudCPUSlot.TabIndex = 16;
			dudCPUSlot.Text = "CPU Slot RAM";
			// 
			// dudMotherboard
			// 
			dudMotherboard.Items.Add("64");
			dudMotherboard.Items.Add("32");
			dudMotherboard.Items.Add("16");
			dudMotherboard.Items.Add("8");
			dudMotherboard.Items.Add("0");
			dudMotherboard.Location = new System.Drawing.Point(90, 135);
			dudMotherboard.Name = "dudMotherboard";
			dudMotherboard.ReadOnly = true;
			dudMotherboard.Size = new System.Drawing.Size(120, 23);
			dudMotherboard.TabIndex = 15;
			dudMotherboard.Text = "Motherboard RAM";
			// 
			// btnGo
			// 
			btnGo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
			btnGo.Location = new System.Drawing.Point(600, 373);
			btnGo.Name = "btnGo";
			btnGo.Size = new System.Drawing.Size(75, 23);
			btnGo.TabIndex = 16;
			btnGo.Text = "Go!";
			btnGo.UseVisualStyleBackColor = true;
			btnGo.Click += btnGo_Click;
			// 
			// btnExit
			// 
			btnExit.Location = new System.Drawing.Point(266, 372);
			btnExit.Name = "btnExit";
			btnExit.Size = new System.Drawing.Size(75, 23);
			btnExit.TabIndex = 17;
			btnExit.Text = "Exit";
			btnExit.UseVisualStyleBackColor = true;
			btnExit.Click += btnExit_Click;
			// 
			// panel5
			// 
			panel5.Controls.Add(groupBox4);
			panel5.Location = new System.Drawing.Point(12, 198);
			panel5.Name = "panel5";
			panel5.Size = new System.Drawing.Size(238, 197);
			panel5.TabIndex = 18;
			// 
			// btnLoadConfig
			// 
			btnLoadConfig.Location = new System.Drawing.Point(357, 372);
			btnLoadConfig.Name = "btnLoadConfig";
			btnLoadConfig.Size = new System.Drawing.Size(75, 23);
			btnLoadConfig.TabIndex = 19;
			btnLoadConfig.Text = "Load ...";
			btnLoadConfig.UseVisualStyleBackColor = true;
			btnLoadConfig.Click += btnLoadConfig_Click;
			// 
			// btnSaveAsConfig
			// 
			btnSaveAsConfig.Location = new System.Drawing.Point(519, 373);
			btnSaveAsConfig.Name = "btnSaveAsConfig";
			btnSaveAsConfig.Size = new System.Drawing.Size(75, 23);
			btnSaveAsConfig.TabIndex = 20;
			btnSaveAsConfig.Text = "Save As...";
			btnSaveAsConfig.UseVisualStyleBackColor = true;
			btnSaveAsConfig.Click += btnSaveAsConfig_Click;
			// 
			// btnSaveConfig
			// 
			btnSaveConfig.Enabled = false;
			btnSaveConfig.Location = new System.Drawing.Point(438, 373);
			btnSaveConfig.Name = "btnSaveConfig";
			btnSaveConfig.Size = new System.Drawing.Size(75, 23);
			btnSaveConfig.TabIndex = 21;
			btnSaveConfig.Text = "Save";
			btnSaveConfig.UseVisualStyleBackColor = true;
			btnSaveConfig.Click += btnSaveConfig_Click;
			// 
			// panel6
			// 
			panel6.Controls.Add(groupBox6);
			panel6.Location = new System.Drawing.Point(256, 315);
			panel6.Name = "panel6";
			panel6.Size = new System.Drawing.Size(425, 52);
			panel6.TabIndex = 22;
			// 
			// groupBox6
			// 
			groupBox6.Controls.Add(cbDebugging);
			groupBox6.Controls.Add(cbAudio);
			groupBox6.ForeColor = System.Drawing.SystemColors.Highlight;
			groupBox6.Location = new System.Drawing.Point(1, 0);
			groupBox6.Name = "groupBox6";
			groupBox6.Size = new System.Drawing.Size(418, 51);
			groupBox6.TabIndex = 0;
			groupBox6.TabStop = false;
			groupBox6.Text = "Miscellaneous";
			// 
			// cbDebugging
			// 
			cbDebugging.AutoSize = true;
			cbDebugging.Location = new System.Drawing.Point(70, 22);
			cbDebugging.Name = "cbDebugging";
			cbDebugging.Size = new System.Drawing.Size(85, 19);
			cbDebugging.TabIndex = 1;
			cbDebugging.Text = "Debugging";
			cbDebugging.UseVisualStyleBackColor = true;
			// 
			// cbAudio
			// 
			cbAudio.AutoSize = true;
			cbAudio.Location = new System.Drawing.Point(6, 22);
			cbAudio.Name = "cbAudio";
			cbAudio.Size = new System.Drawing.Size(58, 19);
			cbAudio.TabIndex = 0;
			cbAudio.Text = "Audio";
			cbAudio.UseVisualStyleBackColor = true;
			// 
			// panel7
			// 
			panel7.Location = new System.Drawing.Point(256, 188);
			panel7.Name = "panel7";
			panel7.Size = new System.Drawing.Size(425, 56);
			panel7.TabIndex = 23;
			// 
			// groupBox7
			// 
			groupBox7.Controls.Add(cbDiskController);
			groupBox7.ForeColor = System.Drawing.SystemColors.Highlight;
			groupBox7.Location = new System.Drawing.Point(256, 188);
			groupBox7.Name = "groupBox7";
			groupBox7.Size = new System.Drawing.Size(418, 55);
			groupBox7.TabIndex = 0;
			groupBox7.TabStop = false;
			groupBox7.Text = "Hard Disk";
			// 
			// cbDiskController
			// 
			cbDiskController.FormattingEnabled = true;
			cbDiskController.Items.AddRange(new object[] { "None", "A600_A1200", "A3000", "A4000" });
			cbDiskController.Location = new System.Drawing.Point(13, 23);
			cbDiskController.Name = "cbDiskController";
			cbDiskController.Size = new System.Drawing.Size(175, 23);
			cbDiskController.TabIndex = 0;
			// 
			// Settings
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(688, 408);
			Controls.Add(groupBox2);
			Controls.Add(groupBox7);
			Controls.Add(panel7);
			Controls.Add(panel6);
			Controls.Add(btnSaveConfig);
			Controls.Add(btnSaveAsConfig);
			Controls.Add(btnLoadConfig);
			Controls.Add(panel5);
			Controls.Add(btnExit);
			Controls.Add(btnGo);
			Controls.Add(panel4);
			Controls.Add(panel3);
			Controls.Add(panel2);
			Controls.Add(panel1);
			Controls.Add(btnQuickStart);
			Controls.Add(cbQuickStart);
			Name = "Settings";
			ShowIcon = false;
			Text = "Emulation Settings";
			panel1.ResumeLayout(false);
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			groupBox2.ResumeLayout(false);
			panel3.ResumeLayout(false);
			groupBox3.ResumeLayout(false);
			groupBox3.PerformLayout();
			panel4.ResumeLayout(false);
			groupBox5.ResumeLayout(false);
			groupBox5.PerformLayout();
			((System.ComponentModel.ISupportInitialize)nudFloppyCount).EndInit();
			groupBox4.ResumeLayout(false);
			groupBox4.PerformLayout();
			panel5.ResumeLayout(false);
			panel6.ResumeLayout(false);
			groupBox6.ResumeLayout(false);
			groupBox6.PerformLayout();
			groupBox7.ResumeLayout(false);
			ResumeLayout(false);
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
		private System.Windows.Forms.CheckBox cbDebugging;
	}
}