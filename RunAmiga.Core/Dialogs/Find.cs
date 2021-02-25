using System;
using System.Windows.Forms;

namespace RunAmiga.Core.Dialogs
{
	public partial class Find : Form
	{
		public Find()
		{
			InitializeComponent();
		}

		public bool SearchBackwards { get; set; } = false;
		public string SearchText { get; set; } = null;
		public uint SearchSize { get; set; } = 0;
		public uint SearchValue { get; set; } = 0;

		private void CollectResults()
		{
			if (radioFindText.Checked)
				SearchText = this.txtFindText.Text;
			else
				SearchText = null;
			SearchSize = radioFindByte.Checked ? 1 : (radioFindWord.Checked ? 2 : (radioFindLong.Checked ? 4 : 0u));
			if (!string.IsNullOrWhiteSpace(txtFindNumber.Text))
				SearchValue = Convert.ToUInt32(txtFindNumber.Text, 16);
			if (SearchValue == 0) txtFindNumber.Text = "0";
		}

		private void btnFindPrev_Click(object sender, EventArgs e)
		{
			SearchBackwards = true;
			CollectResults();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnFindNext_Click(object sender, EventArgs e)
		{
			CollectResults();
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnFindCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void radioFindByte_CheckedChanged(object sender, EventArgs e)
		{
			radioFindText.Checked = false;
			radioFindByte.Checked = true;
			radioFindWord.Checked = false;
			radioFindLong.Checked = false;
		}

		private void radioFindWord_CheckedChanged(object sender, EventArgs e)
		{
			radioFindText.Checked = false;
			radioFindByte.Checked = false;
			radioFindWord.Checked = true;
			radioFindLong.Checked = false;
		}

		private void radioFindLong_CheckedChanged(object sender, EventArgs e)
		{
			radioFindText.Checked = false;
			radioFindByte.Checked = false;
			radioFindWord.Checked = false;
			radioFindLong.Checked = true;
		}

		private void radioFindText_CheckedChanged(object sender, EventArgs e)
		{
			radioFindText.Checked = true;
			radioFindByte.Checked = false;
			radioFindWord.Checked = false;
			radioFindLong.Checked = false;
		}

		private void radioFindText_KeyPress(object sender, KeyPressEventArgs e)
		{
			radioFindText.Checked = true;
			radioFindByte.Checked = false;
			radioFindWord.Checked = false;
			radioFindLong.Checked = false;
		}
	}
}
