using System;
using System.Windows.Forms;
using System.ComponentModel;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main.Dialogs
{
	public partial class GoTo : Form
	{
		public GoTo()
		{
			InitializeComponent();
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public uint GotoLocation { get; set; } = 0;

		private void btnGoToGoTo_Click(object sender, EventArgs e)
		{
			GotoLocation = Convert.ToUInt32(this.txtGotoAddress.Text, 16);
			if (GotoLocation == 0)
				this.txtGotoAddress.Text = "0";
			DialogResult = DialogResult.OK;
		}

		private void btnGotoCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}
	}
}
