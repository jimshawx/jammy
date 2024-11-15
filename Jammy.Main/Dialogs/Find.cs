using System;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main.Dialogs
{
	public partial class Find : Form
	{
		public Find()
		{
			InitializeComponent();
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool SearchBackwards { get; set; } = false;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string SearchText { get; set; } = null;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public byte[] SearchSeq { get; set; } = null;
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public uint SearchValue { get; set; } = 0;

		private void CollectResults()
		{
			SearchText = radioFindText.Checked ? txtFindText.Text : null;

			if (radioFindByte.Checked || radioFindWord.Checked || radioFindLong.Checked)
			{
				var bits = txtFindText.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				try
				{
					if (radioFindByte.Checked)
						SearchSeq = bits.Select(x => byte.Parse(x, System.Globalization.NumberStyles.HexNumber)).ToArray();

					if (radioFindWord.Checked)
						SearchSeq = bits.Select(x => ushort.Parse(x, System.Globalization.NumberStyles.HexNumber))
								.Select(x => new byte[] { (byte)(x >> 8), (byte)x }).SelectMany(x => x).ToArray();

					if (radioFindLong.Checked)
						SearchSeq = bits.Select(x => uint.Parse(x, System.Globalization.NumberStyles.HexNumber))
								.Select(x => new byte[] { (byte)(x >> 24), (byte)(x >> 16), (byte)(x >> 8), (byte)x }).SelectMany(x => x).ToArray();
				}
				catch
				{
					SearchSeq = null;
				}
			}
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
			CheckChangeOff();
			radioFindText.Checked = false;
			radioFindByte.Checked = true;
			radioFindWord.Checked = false;
			radioFindLong.Checked = false;
			CheckChangeOn();
		}

		private void radioFindWord_CheckedChanged(object sender, EventArgs e)
		{
			CheckChangeOff();
			radioFindText.Checked = false;
			radioFindByte.Checked = false;
			radioFindWord.Checked = true;
			radioFindLong.Checked = false;
			CheckChangeOn();
		}

		private void radioFindLong_CheckedChanged(object sender, EventArgs e)
		{
			CheckChangeOff();
			radioFindText.Checked = false;
			radioFindByte.Checked = false;
			radioFindWord.Checked = false;
			radioFindLong.Checked = true;
			CheckChangeOn();
		}

		private void radioFindText_CheckedChanged(object sender, EventArgs e)
		{
			CheckChangeOff();
			radioFindText.Checked = true;
			radioFindByte.Checked = false;
			radioFindWord.Checked = false;
			radioFindLong.Checked = false;
			CheckChangeOn();
		}

		private void radioFindText_KeyPress(object sender, KeyPressEventArgs e)
		{
			CheckChangeOff();
			radioFindText.Checked = true;
			radioFindByte.Checked = false;
			radioFindWord.Checked = false;
			radioFindLong.Checked = false;
			CheckChangeOn();
		}

		private void CheckChangeOff()
		{
			radioFindText.CheckedChanged -= radioFindText_CheckedChanged;
			radioFindByte.CheckedChanged -= radioFindByte_CheckedChanged;
			radioFindWord.CheckedChanged -= radioFindWord_CheckedChanged;
			radioFindLong.CheckedChanged -= radioFindLong_CheckedChanged;
		}

		private void CheckChangeOn()
		{
			radioFindText.CheckedChanged += radioFindText_CheckedChanged;
			radioFindByte.CheckedChanged += radioFindByte_CheckedChanged;
			radioFindWord.CheckedChanged += radioFindWord_CheckedChanged;
			radioFindLong.CheckedChanged += radioFindLong_CheckedChanged;
		}

		private void txtFindText_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				CollectResults();
				DialogResult = DialogResult.OK;
				Close();
				e.SuppressKeyPress = true;
			}
		}
	}
}
