
namespace RunAmiga.Core.Dialogs
{
	partial class Find
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
			this.txtFindText = new System.Windows.Forms.TextBox();
			this.btnFindPrev = new System.Windows.Forms.Button();
			this.btnFindNext = new System.Windows.Forms.Button();
			this.btnFindCancel = new System.Windows.Forms.Button();
			this.txtFindNumber = new System.Windows.Forms.TextBox();
			this.radioFindByte = new System.Windows.Forms.RadioButton();
			this.radioFindWord = new System.Windows.Forms.RadioButton();
			this.radioFindLong = new System.Windows.Forms.RadioButton();
			this.radioFindText = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// txtFindText
			// 
			this.txtFindText.Location = new System.Drawing.Point(32, 13);
			this.txtFindText.Name = "txtFindText";
			this.txtFindText.Size = new System.Drawing.Size(197, 23);
			this.txtFindText.TabIndex = 0;
			// 
			// btnFindPrev
			// 
			this.btnFindPrev.Location = new System.Drawing.Point(34, 106);
			this.btnFindPrev.Name = "btnFindPrev";
			this.btnFindPrev.Size = new System.Drawing.Size(75, 23);
			this.btnFindPrev.TabIndex = 1;
			this.btnFindPrev.Text = "Find Prev";
			this.btnFindPrev.UseVisualStyleBackColor = true;
			this.btnFindPrev.Click += new System.EventHandler(this.btnFindPrev_Click);
			// 
			// btnFindNext
			// 
			this.btnFindNext.Location = new System.Drawing.Point(112, 106);
			this.btnFindNext.Name = "btnFindNext";
			this.btnFindNext.Size = new System.Drawing.Size(75, 23);
			this.btnFindNext.TabIndex = 2;
			this.btnFindNext.Text = "Find Next";
			this.btnFindNext.UseVisualStyleBackColor = true;
			this.btnFindNext.Click += new System.EventHandler(this.btnFindNext_Click);
			// 
			// btnFindCancel
			// 
			this.btnFindCancel.Location = new System.Drawing.Point(193, 106);
			this.btnFindCancel.Name = "btnFindCancel";
			this.btnFindCancel.Size = new System.Drawing.Size(72, 23);
			this.btnFindCancel.TabIndex = 3;
			this.btnFindCancel.Text = "Cancel";
			this.btnFindCancel.UseVisualStyleBackColor = true;
			this.btnFindCancel.Click += new System.EventHandler(this.btnFindCancel_Click);
			// 
			// txtFindNumber
			// 
			this.txtFindNumber.Location = new System.Drawing.Point(32, 43);
			this.txtFindNumber.Name = "txtFindNumber";
			this.txtFindNumber.Size = new System.Drawing.Size(100, 23);
			this.txtFindNumber.TabIndex = 4;
			// 
			// radioFindByte
			// 
			this.radioFindByte.AutoSize = true;
			this.radioFindByte.Location = new System.Drawing.Point(175, 43);
			this.radioFindByte.Name = "radioFindByte";
			this.radioFindByte.Size = new System.Drawing.Size(48, 19);
			this.radioFindByte.TabIndex = 5;
			this.radioFindByte.TabStop = true;
			this.radioFindByte.Text = "Byte";
			this.radioFindByte.UseVisualStyleBackColor = true;
			this.radioFindByte.CheckedChanged += new System.EventHandler(this.radioFindByte_CheckedChanged);
			// 
			// radioFindWord
			// 
			this.radioFindWord.AutoSize = true;
			this.radioFindWord.Location = new System.Drawing.Point(175, 62);
			this.radioFindWord.Name = "radioFindWord";
			this.radioFindWord.Size = new System.Drawing.Size(54, 19);
			this.radioFindWord.TabIndex = 6;
			this.radioFindWord.TabStop = true;
			this.radioFindWord.Text = "Word";
			this.radioFindWord.UseVisualStyleBackColor = true;
			this.radioFindWord.CheckedChanged += new System.EventHandler(this.radioFindWord_CheckedChanged);
			// 
			// radioFindLong
			// 
			this.radioFindLong.AutoSize = true;
			this.radioFindLong.Location = new System.Drawing.Point(175, 81);
			this.radioFindLong.Name = "radioFindLong";
			this.radioFindLong.Size = new System.Drawing.Size(52, 19);
			this.radioFindLong.TabIndex = 7;
			this.radioFindLong.TabStop = true;
			this.radioFindLong.Text = "Long";
			this.radioFindLong.UseVisualStyleBackColor = true;
			this.radioFindLong.CheckedChanged += new System.EventHandler(this.radioFindLong_CheckedChanged);
			// 
			// radioFindText
			// 
			this.radioFindText.AutoSize = true;
			this.radioFindText.Checked = true;
			this.radioFindText.Location = new System.Drawing.Point(237, 16);
			this.radioFindText.Name = "radioFindText";
			this.radioFindText.Size = new System.Drawing.Size(46, 19);
			this.radioFindText.TabIndex = 8;
			this.radioFindText.TabStop = true;
			this.radioFindText.Text = "Text";
			this.radioFindText.UseVisualStyleBackColor = true;
			this.radioFindText.CheckedChanged += new System.EventHandler(this.radioFindText_CheckedChanged);
			this.radioFindText.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.radioFindText_KeyPress);
			// 
			// Find
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(303, 141);
			this.Controls.Add(this.radioFindText);
			this.Controls.Add(this.radioFindLong);
			this.Controls.Add(this.radioFindWord);
			this.Controls.Add(this.radioFindByte);
			this.Controls.Add(this.txtFindNumber);
			this.Controls.Add(this.btnFindCancel);
			this.Controls.Add(this.btnFindNext);
			this.Controls.Add(this.btnFindPrev);
			this.Controls.Add(this.txtFindText);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Find";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Find";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox txtFindText;
		private System.Windows.Forms.Button btnFindPrev;
		private System.Windows.Forms.Button btnFindNext;
		private System.Windows.Forms.Button btnFindCancel;
		private System.Windows.Forms.TextBox txtFindNumber;
		private System.Windows.Forms.RadioButton radioFindByte;
		private System.Windows.Forms.RadioButton radioFindWord;
		private System.Windows.Forms.RadioButton radioFindLong;
		private System.Windows.Forms.RadioButton radioFindText;
	}
}