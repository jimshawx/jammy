
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
			this.lbRegisters.Location = new System.Drawing.Point(717, 12);
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
			this.txtDisassembly.Size = new System.Drawing.Size(693, 233);
			this.txtDisassembly.TabIndex = 1;
			this.txtDisassembly.Text = "";
			this.txtDisassembly.WordWrap = false;
			// 
			// btnStep
			// 
			this.btnStep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStep.Location = new System.Drawing.Point(717, 179);
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
			this.btnStop.Location = new System.Drawing.Point(717, 208);
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
			this.btnGo.Location = new System.Drawing.Point(717, 238);
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
			this.btnReset.Location = new System.Drawing.Point(718, 268);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(75, 23);
			this.btnReset.TabIndex = 5;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// txtMemory
			// 
			this.txtMemory.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtMemory.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.txtMemory.Location = new System.Drawing.Point(3, 3);
			this.txtMemory.Name = "txtMemory";
			this.txtMemory.ReadOnly = true;
			this.txtMemory.Size = new System.Drawing.Size(693, 306);
			this.txtMemory.TabIndex = 6;
			this.txtMemory.Text = "";
			this.txtMemory.WordWrap = false;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
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
			this.splitContainer1.Size = new System.Drawing.Size(699, 555);
			this.splitContainer1.SplitterDistance = 239;
			this.splitContainer1.TabIndex = 7;
			// 
			// btnRefresh
			// 
			this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnRefresh.Location = new System.Drawing.Point(718, 332);
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
			this.btnStepOver.Location = new System.Drawing.Point(799, 179);
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
			this.picPower.Location = new System.Drawing.Point(823, 250);
			this.picPower.Name = "picPower";
			this.picPower.Size = new System.Drawing.Size(51, 10);
			this.picPower.TabIndex = 10;
			this.picPower.TabStop = false;
			// 
			// picDisk
			// 
			this.picDisk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.picDisk.Location = new System.Drawing.Point(823, 268);
			this.picDisk.Name = "picDisk";
			this.picDisk.Size = new System.Drawing.Size(51, 10);
			this.picDisk.TabIndex = 11;
			this.picDisk.TabStop = false;
			// 
			// btnDisassemble
			// 
			this.btnDisassemble.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDisassemble.Location = new System.Drawing.Point(799, 332);
			this.btnDisassemble.Name = "btnDisassemble";
			this.btnDisassemble.Size = new System.Drawing.Size(81, 23);
			this.btnDisassemble.TabIndex = 12;
			this.btnDisassemble.Text = "Disassemble";
			this.btnDisassemble.UseVisualStyleBackColor = true;
			this.btnDisassemble.Click += new System.EventHandler(this.btnDisassemble_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(909, 579);
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
			this.ResumeLayout(false);

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
	}
}

