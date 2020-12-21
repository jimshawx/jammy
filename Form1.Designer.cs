
namespace runamiga
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
			this.lbRegisters = new System.Windows.Forms.ListBox();
			this.txtDisassembly = new System.Windows.Forms.RichTextBox();
			this.btnStep = new System.Windows.Forms.Button();
			this.btnStop = new System.Windows.Forms.Button();
			this.btnGo = new System.Windows.Forms.Button();
			this.btnReset = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// lbRegisters
			// 
			this.lbRegisters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lbRegisters.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
			this.lbRegisters.FormattingEnabled = true;
			this.lbRegisters.IntegralHeight = false;
			this.lbRegisters.Location = new System.Drawing.Point(462, 12);
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
			this.txtDisassembly.Location = new System.Drawing.Point(12, 12);
			this.txtDisassembly.Name = "txtDisassembly";
			this.txtDisassembly.ReadOnly = true;
			this.txtDisassembly.Size = new System.Drawing.Size(444, 426);
			this.txtDisassembly.TabIndex = 1;
			this.txtDisassembly.Text = "";
			this.txtDisassembly.WordWrap = false;
			// 
			// btnStep
			// 
			this.btnStep.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnStep.Location = new System.Drawing.Point(462, 179);
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
			this.btnStop.Location = new System.Drawing.Point(462, 208);
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
			this.btnGo.Location = new System.Drawing.Point(462, 238);
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
			this.btnReset.Location = new System.Drawing.Point(463, 268);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(75, 23);
			this.btnReset.TabIndex = 5;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(654, 450);
			this.Controls.Add(this.btnReset);
			this.Controls.Add(this.btnGo);
			this.Controls.Add(this.btnStop);
			this.Controls.Add(this.btnStep);
			this.Controls.Add(this.txtDisassembly);
			this.Controls.Add(this.lbRegisters);
			this.Name = "Form1";
			this.Text = "RunAmiga";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListBox lbRegisters;
		private System.Windows.Forms.RichTextBox txtDisassembly;
		private System.Windows.Forms.Button btnStep;
		private System.Windows.Forms.Button btnStop;
		private System.Windows.Forms.Button btnGo;
		private System.Windows.Forms.Button btnReset;
	}
}

