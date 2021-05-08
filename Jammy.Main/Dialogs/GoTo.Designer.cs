
/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main.Dialogs
{
	partial class GoTo
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
			this.txtGotoAddress = new System.Windows.Forms.TextBox();
			this.btnGoToGoTo = new System.Windows.Forms.Button();
			this.btnGotoCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// txtGotoAddress
			// 
			this.txtGotoAddress.Location = new System.Drawing.Point(13, 13);
			this.txtGotoAddress.Name = "txtGotoAddress";
			this.txtGotoAddress.Size = new System.Drawing.Size(160, 23);
			this.txtGotoAddress.TabIndex = 0;
			// 
			// btnGoToGoTo
			// 
			this.btnGoToGoTo.Location = new System.Drawing.Point(13, 42);
			this.btnGoToGoTo.Name = "btnGoToGoTo";
			this.btnGoToGoTo.Size = new System.Drawing.Size(75, 23);
			this.btnGoToGoTo.TabIndex = 1;
			this.btnGoToGoTo.Text = "Go To";
			this.btnGoToGoTo.UseVisualStyleBackColor = true;
			this.btnGoToGoTo.Click += new System.EventHandler(this.btnGoToGoTo_Click);
			// 
			// btnGotoCancel
			// 
			this.btnGotoCancel.Location = new System.Drawing.Point(94, 41);
			this.btnGotoCancel.Name = "btnGotoCancel";
			this.btnGotoCancel.Size = new System.Drawing.Size(78, 23);
			this.btnGotoCancel.TabIndex = 2;
			this.btnGotoCancel.Text = "Cancel";
			this.btnGotoCancel.UseVisualStyleBackColor = true;
			this.btnGotoCancel.Click += new System.EventHandler(this.btnGotoCancel_Click);
			// 
			// GoTo
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(194, 76);
			this.Controls.Add(this.btnGotoCancel);
			this.Controls.Add(this.btnGoToGoTo);
			this.Controls.Add(this.txtGotoAddress);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GoTo";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "GoTo";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox txtGotoAddress;
		private System.Windows.Forms.Button btnGoToGoTo;
		private System.Windows.Forms.Button btnGotoCancel;
	}
}