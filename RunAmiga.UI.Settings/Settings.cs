using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using RunAmiga.Core.Types;

namespace RunAmiga.UI.Settings
{
	public partial class Settings : Form
	{
		private string configPath = "../../../../config";
		public Settings()
		{
			InitializeComponent();

			//should store this somewhere
			cbQuickStart.SelectedIndex = 0;
			LoadConfig("emulationSettings.json");
		}

		public bool ConfigOK { get; set; }

		private void btnQuickStart_Click(object sender, EventArgs e)
		{
			ConfigOK = true;
			Close();
		}

		private void cbCPU_SelectedValueChanged(object sender, EventArgs e)
		{
			if (cbSku.SelectedText != "MC68000")
			{
				rbNative.Checked = false;
				rbNative.Enabled = false;
				rbMusashi.Checked = true;
			}
			else
			{
				rbNative.Enabled = true;
			}
		}

		private void cbChipset_SelectedValueChanged(object sender, EventArgs e)
		{

		}

		private void btnDF0Pick_Click(object sender, EventArgs e)
		{
			openFileDialog1.DefaultExt = ".adf";
			openFileDialog1.FileName = txtDF0.Text;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				txtDF0.Text = openFileDialog1.FileName;
		}

		private void btnDF1Pick_Click(object sender, EventArgs e)
		{
			openFileDialog1.DefaultExt = ".adf";
			openFileDialog1.FileName = txtDF1.Text;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				txtDF1.Text = openFileDialog1.FileName;
		}

		private void btnDF2Pick_Click(object sender, EventArgs e)
		{
			openFileDialog1.DefaultExt = ".adf";
			openFileDialog1.FileName = txtDF2.Text;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				txtDF2.Text = openFileDialog1.FileName;
		}

		private void btnDF3Pick_Click(object sender, EventArgs e)
		{
			openFileDialog1.DefaultExt = ".adf";
			openFileDialog1.FileName = txtDF2.Text;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				txtDF2.Text = openFileDialog1.FileName;
		}

		private void btnROMPick_Click(object sender, EventArgs e)
		{
			openFileDialog1.DefaultExt = ".adf";
			openFileDialog1.FileName = txtKickstart.Text;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				txtKickstart.Text = openFileDialog1.FileName;

		}

		private void btnExit_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void btnLoadConfig_Click(object sender, EventArgs e)
		{
			openFileDialog1.DefaultExt = ".cfg";
			openFileDialog1.FileName = "";
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				LoadConfig(openFileDialog1.FileName);
		}

		private void btnSaveConfig_Click(object sender, EventArgs e)
		{
			SaveConfig(null);
		}

		private void btnSaveAsConfig_Click(object sender, EventArgs e)
		{
			openFileDialog1.DefaultExt = ".cfg";
			openFileDialog1.FileName = "";
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				SaveConfig(openFileDialog1.FileName);
		}

		private void btnGo_Click(object sender, EventArgs e)
		{
			SaveConfig("emulationSettings.json");
			ConfigOK = true;
			Close();
		}

		private void nudFloppyCount_ValueChanged(object sender, EventArgs e)
		{
			int f = (int)nudFloppyCount.Value;
			txtDF3.Enabled = btnDF3Pick.Enabled = f>=4;
			txtDF2.Enabled = btnDF2Pick.Enabled = f>=3;
			txtDF1.Enabled = btnDF1Pick.Enabled = f>=2;
		}

		private void rbNative_CheckedChanged(object sender, EventArgs e)
		{
			if (rbNative.Checked) cbSku.SelectedIndex = 0;
		}

		private EmulationSettings currentSettings = DefaultSettings();

		private string currentSettingsFile = "";

		private void LoadConfig(string fileName)
		{
			if (File.Exists(fileName))
			{
				string cfg = File.ReadAllText(fileName);
				currentSettings = JsonConvert.DeserializeObject<EmulationSettings>(cfg);
				BindSettings();
				UpdateCurrentSettingFilename(fileName);
			}
		}

		private void SaveConfig(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				fileName = currentSettingsFile;
			UnbindSettings();
			string cfg = JsonConvert.SerializeObject(currentSettings);
			File.WriteAllText(currentSettingsFile, cfg);
			UpdateCurrentSettingFilename(fileName);
		}

		private void UpdateCurrentSettingFilename(string fileName)
		{
			if (Path.GetFileName(fileName).ToLower() != "emulationsettings.json")
			{
				currentSettingsFile = fileName;
				btnSaveConfig.Enabled = true;
			}
		}

		private void BindSettings()
		{
			//put currentSettings on to the UI

			//Quickstart
			var cfgs = Directory.GetFiles(configPath, "*.cfg", SearchOption.TopDirectoryOnly);
			cbQuickStart.Items.AddRange(cfgs.Select(x=>Path.GetFileNameWithoutExtension(x)).OrderBy(x=>x).Cast<object>().ToArray());

			//CPU
			cbSku.SelectedValue = currentSettings.Sku.ToString();
			rbMusashi.Checked = currentSettings.CPU == CPUType.Musashi;
			rbNative.Checked = currentSettings.CPU == CPUType.Native;
			rbNative.Enabled = currentSettings.Sku == CPUSku.MC68000;

			//Chipset
			cbChipset.SelectedValue = currentSettings.ChipSet.ToString();
			
			//Memory
			dudCPUSlot.SelectedItem = currentSettings.CPUSlotMemory.ToString();
			dudMotherboard.SelectedItem = currentSettings.MotherboardMemory.ToString();
			dudTrapdoor.SelectedItem = currentSettings.TrapdoorMemory.ToString();
			dudZ2.SelectedItem = currentSettings.ZorroIIMemory;
			dudZ3.SelectedItem = currentSettings.ZorroIIIMemory;
			
			//Floppies
			txtDF0.Text = currentSettings.DF0;
			txtDF1.Text = currentSettings.DF1;
			txtDF2.Text = currentSettings.DF2;
			txtDF3.Text = currentSettings.DF3;
			nudFloppyCount.Value = Math.Max(1, currentSettings.FloppyCount);
			txtDF3.Enabled = btnDF3Pick.Enabled = nudFloppyCount.Value >= 4;
			txtDF2.Enabled = btnDF2Pick.Enabled = nudFloppyCount.Value >= 3;
			txtDF1.Enabled = btnDF1Pick.Enabled = nudFloppyCount.Value >= 2;

			//Kickstart
			txtKickstart.Text = currentSettings.KickStart;

			//Misc.
			cbAudio.Checked = currentSettings.Audio != AudioDriver.Null;
		}

		private void UnbindSettings()
		{
			//fill currentSettings from the UI;

			//CPU
			currentSettings.Sku = Enum.Parse<CPUSku>(cbSku.SelectedText);
			currentSettings.CPU = rbMusashi.Checked? CPUType.Musashi : CPUType.Native;
			currentSettings.AddressBits = currentSettings.Sku == CPUSku.MC68030 ? 32 : 24;

			//Chipset
			currentSettings.ChipSet = Enum.Parse<ChipSet>(cbChipset.SelectedText);

			//Memory
			currentSettings.CPUSlotMemory = Convert.ToSingle(dudCPUSlot.SelectedItem);
			currentSettings.MotherboardMemory = Convert.ToSingle(dudMotherboard.SelectedItem);
			currentSettings.TrapdoorMemory = Convert.ToSingle(dudTrapdoor.SelectedItem);
			currentSettings.ZorroIIMemory = (string)dudZ2.SelectedItem;
			currentSettings.ZorroIIIMemory = (string)dudZ3.SelectedItem;

			//Floppies
			currentSettings.DF0 = txtDF0.Text;
			currentSettings.DF1 = txtDF1.Text;
			currentSettings.DF2 = txtDF2.Text;
			currentSettings.DF3 = txtDF3.Text;
			currentSettings.FloppyCount = (int)nudFloppyCount.Value;

			//Kickstart
			currentSettings.KickStart = txtKickstart.Text;

			//Misc.
			currentSettings.Audio = cbAudio.Checked ? AudioDriver.XAudio2 : AudioDriver.Null;
		}

		private static EmulationSettings DefaultSettings()
		{
			return new EmulationSettings();
		}
	}
}
