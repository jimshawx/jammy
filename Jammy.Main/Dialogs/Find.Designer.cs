
/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main.Dialogs
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
			txtFindText = new System.Windows.Forms.TextBox();
			btnFindPrev = new System.Windows.Forms.Button();
			btnFindNext = new System.Windows.Forms.Button();
			btnFindCancel = new System.Windows.Forms.Button();
			radioFindByte = new System.Windows.Forms.RadioButton();
			radioFindWord = new System.Windows.Forms.RadioButton();
			radioFindLong = new System.Windows.Forms.RadioButton();
			radioFindText = new System.Windows.Forms.RadioButton();
			SuspendLayout();
			// 
			// txtFindText
			// 
			txtFindText.Location = new System.Drawing.Point(59, 28);
			txtFindText.Margin = new System.Windows.Forms.Padding(6);
			txtFindText.Name = "txtFindText";
			txtFindText.Size = new System.Drawing.Size(362, 39);
			txtFindText.TabIndex = 0;
			txtFindText.KeyDown += txtFindText_KeyDown;
			// 
			// btnFindPrev
			// 
			btnFindPrev.Enabled = false;
			btnFindPrev.Location = new System.Drawing.Point(59, 89);
			btnFindPrev.Margin = new System.Windows.Forms.Padding(6);
			btnFindPrev.Name = "btnFindPrev";
			btnFindPrev.Size = new System.Drawing.Size(139, 49);
			btnFindPrev.TabIndex = 1;
			btnFindPrev.Text = "Find &Prev";
			btnFindPrev.UseVisualStyleBackColor = true;
			btnFindPrev.Click += btnFindPrev_Click;
			// 
			// btnFindNext
			// 
			btnFindNext.Location = new System.Drawing.Point(210, 89);
			btnFindNext.Margin = new System.Windows.Forms.Padding(6);
			btnFindNext.Name = "btnFindNext";
			btnFindNext.Size = new System.Drawing.Size(139, 49);
			btnFindNext.TabIndex = 2;
			btnFindNext.Text = "&Find Next";
			btnFindNext.UseVisualStyleBackColor = true;
			btnFindNext.Click += btnFindNext_Click;
			// 
			// btnFindCancel
			// 
			btnFindCancel.Location = new System.Drawing.Point(59, 147);
			btnFindCancel.Margin = new System.Windows.Forms.Padding(6);
			btnFindCancel.Name = "btnFindCancel";
			btnFindCancel.Size = new System.Drawing.Size(134, 49);
			btnFindCancel.TabIndex = 3;
			btnFindCancel.Text = "&Cancel";
			btnFindCancel.UseVisualStyleBackColor = true;
			btnFindCancel.Click += btnFindCancel_Click;
			// 
			// radioFindByte
			// 
			radioFindByte.AutoSize = true;
			radioFindByte.Location = new System.Drawing.Point(440, 72);
			radioFindByte.Margin = new System.Windows.Forms.Padding(6);
			radioFindByte.Name = "radioFindByte";
			radioFindByte.Size = new System.Drawing.Size(92, 36);
			radioFindByte.TabIndex = 5;
			radioFindByte.TabStop = true;
			radioFindByte.Text = "Byte";
			radioFindByte.UseVisualStyleBackColor = true;
			radioFindByte.CheckedChanged += radioFindByte_CheckedChanged;
			// 
			// radioFindWord
			// 
			radioFindWord.AutoSize = true;
			radioFindWord.Location = new System.Drawing.Point(440, 112);
			radioFindWord.Margin = new System.Windows.Forms.Padding(6);
			radioFindWord.Name = "radioFindWord";
			radioFindWord.Size = new System.Drawing.Size(102, 36);
			radioFindWord.TabIndex = 6;
			radioFindWord.TabStop = true;
			radioFindWord.Text = "Word";
			radioFindWord.UseVisualStyleBackColor = true;
			radioFindWord.CheckedChanged += radioFindWord_CheckedChanged;
			// 
			// radioFindLong
			// 
			radioFindLong.AutoSize = true;
			radioFindLong.Location = new System.Drawing.Point(440, 153);
			radioFindLong.Margin = new System.Windows.Forms.Padding(6);
			radioFindLong.Name = "radioFindLong";
			radioFindLong.Size = new System.Drawing.Size(98, 36);
			radioFindLong.TabIndex = 7;
			radioFindLong.TabStop = true;
			radioFindLong.Text = "Long";
			radioFindLong.UseVisualStyleBackColor = true;
			radioFindLong.CheckedChanged += radioFindLong_CheckedChanged;
			// 
			// radioFindText
			// 
			radioFindText.AutoSize = true;
			radioFindText.Checked = true;
			radioFindText.Location = new System.Drawing.Point(440, 34);
			radioFindText.Margin = new System.Windows.Forms.Padding(6);
			radioFindText.Name = "radioFindText";
			radioFindText.Size = new System.Drawing.Size(88, 36);
			radioFindText.TabIndex = 4;
			radioFindText.TabStop = true;
			radioFindText.Text = "Text";
			radioFindText.UseVisualStyleBackColor = true;
			radioFindText.CheckedChanged += radioFindText_CheckedChanged;
			radioFindText.KeyPress += radioFindText_KeyPress;
			// 
			// Find
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(565, 208);
			Controls.Add(radioFindText);
			Controls.Add(radioFindLong);
			Controls.Add(radioFindWord);
			Controls.Add(radioFindByte);
			Controls.Add(btnFindCancel);
			Controls.Add(btnFindNext);
			Controls.Add(btnFindPrev);
			Controls.Add(txtFindText);
			Margin = new System.Windows.Forms.Padding(6);
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "Find";
			ShowIcon = false;
			ShowInTaskbar = false;
			Text = "Find";
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private System.Windows.Forms.TextBox txtFindText;
		private System.Windows.Forms.Button btnFindPrev;
		private System.Windows.Forms.Button btnFindNext;
		private System.Windows.Forms.Button btnFindCancel;
		private System.Windows.Forms.RadioButton radioFindByte;
		private System.Windows.Forms.RadioButton radioFindWord;
		private System.Windows.Forms.RadioButton radioFindLong;
		private System.Windows.Forms.RadioButton radioFindText;
	}
}