
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
			this.cbCPU = new System.Windows.Forms.ComboBox();
			this.rbNative = new System.Windows.Forms.RadioButton();
			this.rbMusashi = new System.Windows.Forms.RadioButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.cbChipset = new System.Windows.Forms.ComboBox();
			this.txtKickstart = new System.Windows.Forms.TextBox();
			this.panel3 = new System.Windows.Forms.Panel();
			this.panel4 = new System.Windows.Forms.Panel();
			this.nudFloppyCount = new System.Windows.Forms.NumericUpDown();
			this.txtDF0 = new System.Windows.Forms.TextBox();
			this.txtDF1 = new System.Windows.Forms.TextBox();
			this.txtDF3 = new System.Windows.Forms.TextBox();
			this.txtDF4 = new System.Windows.Forms.TextBox();
			this.btnDF0Pick = new System.Windows.Forms.Button();
			this.btnDF1Pick = new System.Windows.Forms.Button();
			this.btnDF2Pick = new System.Windows.Forms.Button();
			this.btnDF3Pick = new System.Windows.Forms.Button();
			this.btnROMPick = new System.Windows.Forms.Button();
			this.dudZ2 = new System.Windows.Forms.DomainUpDown();
			this.dudTrapdoor = new System.Windows.Forms.DomainUpDown();
			this.dudZ3 = new System.Windows.Forms.DomainUpDown();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.btnGo = new System.Windows.Forms.Button();
			this.btnExit = new System.Windows.Forms.Button();
			this.dudMotherboard = new System.Windows.Forms.DomainUpDown();
			this.dudCPUSlot = new System.Windows.Forms.DomainUpDown();
			this.panel5 = new System.Windows.Forms.Panel();
			this.btnLoadConfig = new System.Windows.Forms.Button();
			this.btnSaveAsConfig = new System.Windows.Forms.Button();
			this.btnSaveConfig = new System.Windows.Forms.Button();
			this.panel1.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.panel4.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudFloppyCount)).BeginInit();
			this.groupBox3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.panel5.SuspendLayout();
			this.SuspendLayout();
			// 
			// cbQuickStart
			// 
			this.cbQuickStart.FormattingEnabled = true;
			this.cbQuickStart.Items.AddRange(new object[] {
            "A500, 512KB+512KB, OCS, KS1.3",
            "A500+, 1MB+1MB, ECS, KS2.04",
            "A600, 1MB, ECS, KS2.05",
            "A1200, 2MB, AGA, KS3.1",
            "A3000, 1MB+256MB, ECS, KS3.1",
            "A4000, 2MB+16MB+128MB, AGA, KS3.1"});
			this.cbQuickStart.Location = new System.Drawing.Point(12, 12);
			this.cbQuickStart.Name = "cbQuickStart";
			this.cbQuickStart.Size = new System.Drawing.Size(350, 23);
			this.cbQuickStart.TabIndex = 1;
			// 
			// btnQuickStart
			// 
			this.btnQuickStart.Location = new System.Drawing.Point(369, 13);
			this.btnQuickStart.Name = "btnQuickStart";
			this.btnQuickStart.Size = new System.Drawing.Size(75, 23);
			this.btnQuickStart.TabIndex = 2;
			this.btnQuickStart.Text = "Quick Start";
			this.btnQuickStart.UseVisualStyleBackColor = true;
			// 
			// cbCPU
			// 
			this.cbCPU.FormattingEnabled = true;
			this.cbCPU.Items.AddRange(new object[] {
            "68000",
            "68EC020",
            "68030"});
			this.cbCPU.Location = new System.Drawing.Point(6, 22);
			this.cbCPU.Name = "cbCPU";
			this.cbCPU.Size = new System.Drawing.Size(121, 23);
			this.cbCPU.TabIndex = 3;
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
			this.groupBox1.Controls.Add(this.cbCPU);
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
			this.panel2.Controls.Add(this.groupBox2);
			this.panel2.Location = new System.Drawing.Point(12, 138);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(238, 70);
			this.panel2.TabIndex = 8;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.cbChipset);
			this.groupBox2.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox2.Location = new System.Drawing.Point(0, 0);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(226, 57);
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
			this.panel3.Location = new System.Drawing.Point(256, 214);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(425, 74);
			this.panel3.TabIndex = 10;
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.groupBox5);
			this.panel4.Location = new System.Drawing.Point(256, 42);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(425, 166);
			this.panel4.TabIndex = 11;
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
			this.nudFloppyCount.Size = new System.Drawing.Size(43, 23);
			this.nudFloppyCount.TabIndex = 0;
			this.nudFloppyCount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// txtDF0
			// 
			this.txtDF0.Location = new System.Drawing.Point(6, 21);
			this.txtDF0.Name = "txtDF0";
			this.txtDF0.Size = new System.Drawing.Size(307, 23);
			this.txtDF0.TabIndex = 1;
			// 
			// txtDF1
			// 
			this.txtDF1.Location = new System.Drawing.Point(6, 50);
			this.txtDF1.Name = "txtDF1";
			this.txtDF1.Size = new System.Drawing.Size(307, 23);
			this.txtDF1.TabIndex = 2;
			// 
			// txtDF3
			// 
			this.txtDF3.Location = new System.Drawing.Point(6, 79);
			this.txtDF3.Name = "txtDF3";
			this.txtDF3.Size = new System.Drawing.Size(307, 23);
			this.txtDF3.TabIndex = 3;
			// 
			// txtDF4
			// 
			this.txtDF4.Location = new System.Drawing.Point(6, 108);
			this.txtDF4.Name = "txtDF4";
			this.txtDF4.Size = new System.Drawing.Size(307, 23);
			this.txtDF4.TabIndex = 4;
			// 
			// btnDF0Pick
			// 
			this.btnDF0Pick.Location = new System.Drawing.Point(319, 20);
			this.btnDF0Pick.Name = "btnDF0Pick";
			this.btnDF0Pick.Size = new System.Drawing.Size(35, 23);
			this.btnDF0Pick.TabIndex = 5;
			this.btnDF0Pick.Text = "...";
			this.btnDF0Pick.UseVisualStyleBackColor = true;
			// 
			// btnDF1Pick
			// 
			this.btnDF1Pick.Location = new System.Drawing.Point(319, 49);
			this.btnDF1Pick.Name = "btnDF1Pick";
			this.btnDF1Pick.Size = new System.Drawing.Size(35, 23);
			this.btnDF1Pick.TabIndex = 6;
			this.btnDF1Pick.Text = "...";
			this.btnDF1Pick.UseVisualStyleBackColor = true;
			// 
			// btnDF2Pick
			// 
			this.btnDF2Pick.Location = new System.Drawing.Point(320, 78);
			this.btnDF2Pick.Name = "btnDF2Pick";
			this.btnDF2Pick.Size = new System.Drawing.Size(35, 23);
			this.btnDF2Pick.TabIndex = 7;
			this.btnDF2Pick.Text = "...";
			this.btnDF2Pick.UseVisualStyleBackColor = true;
			// 
			// btnDF3Pick
			// 
			this.btnDF3Pick.Location = new System.Drawing.Point(320, 106);
			this.btnDF3Pick.Name = "btnDF3Pick";
			this.btnDF3Pick.Size = new System.Drawing.Size(35, 23);
			this.btnDF3Pick.TabIndex = 8;
			this.btnDF3Pick.Text = "...";
			this.btnDF3Pick.UseVisualStyleBackColor = true;
			// 
			// btnROMPick
			// 
			this.btnROMPick.Location = new System.Drawing.Point(315, 21);
			this.btnROMPick.Name = "btnROMPick";
			this.btnROMPick.Size = new System.Drawing.Size(35, 23);
			this.btnROMPick.TabIndex = 10;
			this.btnROMPick.Text = "...";
			this.btnROMPick.UseVisualStyleBackColor = true;
			// 
			// dudZ2
			// 
			this.dudZ2.Items.Add("0");
			this.dudZ2.Items.Add("0.5");
			this.dudZ2.Items.Add("1.0");
			this.dudZ2.Items.Add("2.0");
			this.dudZ2.Items.Add("4.0");
			this.dudZ2.Items.Add("8.0");
			this.dudZ2.Location = new System.Drawing.Point(9, 51);
			this.dudZ2.Name = "dudZ2";
			this.dudZ2.Size = new System.Drawing.Size(120, 23);
			this.dudZ2.TabIndex = 12;
			this.dudZ2.Text = "ZorroII RAM";
			// 
			// dudTrapdoor
			// 
			this.dudTrapdoor.Items.Add("0");
			this.dudTrapdoor.Items.Add("0.5");
			this.dudTrapdoor.Items.Add("1.0");
			this.dudTrapdoor.Items.Add("1.5");
			this.dudTrapdoor.Items.Add("1.75");
			this.dudTrapdoor.Location = new System.Drawing.Point(9, 22);
			this.dudTrapdoor.Name = "dudTrapdoor";
			this.dudTrapdoor.Size = new System.Drawing.Size(120, 23);
			this.dudTrapdoor.TabIndex = 13;
			this.dudTrapdoor.Text = "Trapdoor RAM";
			// 
			// dudZ3
			// 
			this.dudZ3.Items.Add("0");
			this.dudZ3.Items.Add("256");
			this.dudZ3.Items.Add("512");
			this.dudZ3.Items.Add("1024");
			this.dudZ3.Items.Add("256+256");
			this.dudZ3.Items.Add("512+512");
			this.dudZ3.Items.Add("512+512+512");
			this.dudZ3.Location = new System.Drawing.Point(9, 80);
			this.dudZ3.Name = "dudZ3";
			this.dudZ3.Size = new System.Drawing.Size(120, 23);
			this.dudZ3.TabIndex = 14;
			this.dudZ3.Text = "ZorroIII RAM";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.txtKickstart);
			this.groupBox3.Controls.Add(this.btnROMPick);
			this.groupBox3.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox3.Location = new System.Drawing.Point(266, 214);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(408, 61);
			this.groupBox3.TabIndex = 11;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Kickstart";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.dudCPUSlot);
			this.groupBox4.Controls.Add(this.dudMotherboard);
			this.groupBox4.Controls.Add(this.dudZ2);
			this.groupBox4.Controls.Add(this.dudZ3);
			this.groupBox4.Controls.Add(this.dudTrapdoor);
			this.groupBox4.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox4.Location = new System.Drawing.Point(0, 0);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(226, 174);
			this.groupBox4.TabIndex = 16;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Memory";
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
			this.groupBox5.Controls.Add(this.txtDF3);
			this.groupBox5.Controls.Add(this.txtDF4);
			this.groupBox5.ForeColor = System.Drawing.SystemColors.Highlight;
			this.groupBox5.Location = new System.Drawing.Point(0, 1);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(418, 141);
			this.groupBox5.TabIndex = 16;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Floppy Disk";
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
			// 
			// btnExit
			// 
			this.btnExit.Location = new System.Drawing.Point(266, 372);
			this.btnExit.Name = "btnExit";
			this.btnExit.Size = new System.Drawing.Size(75, 23);
			this.btnExit.TabIndex = 17;
			this.btnExit.Text = "Exit";
			this.btnExit.UseVisualStyleBackColor = true;
			// 
			// dudMotherboard
			// 
			this.dudMotherboard.Items.Add("0");
			this.dudMotherboard.Items.Add("16");
			this.dudMotherboard.Items.Add("32");
			this.dudMotherboard.Items.Add("64");
			this.dudMotherboard.Location = new System.Drawing.Point(9, 109);
			this.dudMotherboard.Name = "dudMotherboard";
			this.dudMotherboard.Size = new System.Drawing.Size(120, 23);
			this.dudMotherboard.TabIndex = 15;
			this.dudMotherboard.Text = "Motherboard RAM";
			// 
			// dudCPUSlot
			// 
			this.dudCPUSlot.Items.Add("0");
			this.dudCPUSlot.Items.Add("8");
			this.dudCPUSlot.Items.Add("16");
			this.dudCPUSlot.Items.Add("32");
			this.dudCPUSlot.Items.Add("64");
			this.dudCPUSlot.Items.Add("128");
			this.dudCPUSlot.Location = new System.Drawing.Point(9, 138);
			this.dudCPUSlot.Name = "dudCPUSlot";
			this.dudCPUSlot.Size = new System.Drawing.Size(120, 23);
			this.dudCPUSlot.TabIndex = 16;
			this.dudCPUSlot.Text = "CPU Slot RAM";
			// 
			// panel5
			// 
			this.panel5.Controls.Add(this.groupBox4);
			this.panel5.Location = new System.Drawing.Point(12, 214);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(238, 181);
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
			// 
			// btnSaveAsConfig
			// 
			this.btnSaveAsConfig.Location = new System.Drawing.Point(519, 373);
			this.btnSaveAsConfig.Name = "btnSaveAsConfig";
			this.btnSaveAsConfig.Size = new System.Drawing.Size(75, 23);
			this.btnSaveAsConfig.TabIndex = 20;
			this.btnSaveAsConfig.Text = "Save As...";
			this.btnSaveAsConfig.UseVisualStyleBackColor = true;
			// 
			// btnSaveConfig
			// 
			this.btnSaveConfig.Location = new System.Drawing.Point(438, 373);
			this.btnSaveConfig.Name = "btnSaveConfig";
			this.btnSaveConfig.Size = new System.Drawing.Size(75, 23);
			this.btnSaveConfig.TabIndex = 21;
			this.btnSaveConfig.Text = "Save";
			this.btnSaveConfig.UseVisualStyleBackColor = true;
			// 
			// Settings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(688, 408);
			this.Controls.Add(this.btnSaveConfig);
			this.Controls.Add(this.btnSaveAsConfig);
			this.Controls.Add(this.btnLoadConfig);
			this.Controls.Add(this.groupBox3);
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
			this.panel2.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.panel4.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.nudFloppyCount)).EndInit();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox4.ResumeLayout(false);
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			this.panel5.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox cbQuickStart;
		private System.Windows.Forms.Button btnQuickStart;
		private System.Windows.Forms.ComboBox cbCPU;
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
		private System.Windows.Forms.TextBox txtDF4;
		private System.Windows.Forms.TextBox txtDF3;
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
	}
}