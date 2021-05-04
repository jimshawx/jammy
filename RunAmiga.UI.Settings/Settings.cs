using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RunAmiga.Core.Types;

namespace RunAmiga.UI.Settings
{
	public partial class Settings : Form
	{
		private string configPath = "../../../../config";
		public Settings()
		{
			InitializeComponent();

			JsonConvert.DefaultSettings = () =>
			{
				var s = new JsonSerializerSettings {Formatting = Formatting.Indented,  };
				s.Converters.Add(new StringEnumConverter());
				return s;
			};

			//should store this somewhere
			cbQuickStart.SelectedIndex = 0;

			//Quickstart
			var cfgs = Directory.GetFiles(configPath, "*.cfg", SearchOption.TopDirectoryOnly);
			cbQuickStart.Items.AddRange(cfgs.Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).Cast<object>().ToArray());

			LoadConfig("emulationSettings.json");
		}

		public bool ConfigOK { get; set; }

		private void btnQuickStart_Click(object sender, EventArgs e)
		{
			SaveConfig("emulationSettings.json");
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
				currentSettings = JsonConvert.DeserializeObject<Emulation>(cfg).Settings;
				BindSettings();
				UpdateCurrentSettingFilename(fileName);
			}
		}

		private void SaveConfig(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
				fileName = currentSettingsFile;
			UnbindSettings();
			string cfg = JsonConvert.SerializeObject(new Emulation{ Settings = currentSettings});
			File.WriteAllText(fileName, cfg);
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

		private string EncodeZorro3(string s)
		{
			if (string.IsNullOrEmpty(s)) return "0";
			return s.Replace('+', ',');
		}

		private string DecodeZorro3(string s)
		{
			if (string.IsNullOrEmpty(s)) return "0";
			return string.Join('+', s.Split(',').Select(x => ((int)Convert.ToSingle(x)).ToString()));
		}

		private string EncodeZorro2(string s)
		{
			if (string.IsNullOrEmpty(s)) return "0";
			return s.Replace('+', ',');
		}

		private string DecodeZorro2(string s)
		{
			if (string.IsNullOrEmpty(s)) return "0";
			return string.Join('+', s.Split(',').Select(x => Convert.ToSingle(x).ToString("0.0")));
		}

		private void BindSettings()
		{
			//put currentSettings on to the UI

			//CPU
			cbSku.SelectedItem = currentSettings.Sku.ToString();
			rbMusashi.Checked = currentSettings.CPU == CPUType.Musashi;
			rbNative.Checked = currentSettings.CPU == CPUType.Native;
			rbNative.Enabled = currentSettings.Sku == CPUSku.MC68000;

			//Chipset
			cbChipset.SelectedItem = currentSettings.ChipSet.ToString();
			
			//Memory
			dudChipRAM.SelectedItem = currentSettings.ChipMemory.ToString();
			dudCPUSlot.SelectedItem = currentSettings.CPUSlotMemory.ToString();
			dudMotherboard.SelectedItem = currentSettings.MotherboardMemory.ToString();
			dudTrapdoor.SelectedItem = currentSettings.TrapdoorMemory.ToString();
			dudZ2.SelectedItem = DecodeZorro2(currentSettings.ZorroIIMemory);
			dudZ3.SelectedItem = DecodeZorro3(currentSettings.ZorroIIIMemory);
			
			//Floppies
			txtDF0.Text = currentSettings.DF0;
			txtDF1.Text = currentSettings.DF1;
			txtDF2.Text = currentSettings.DF2;
			txtDF3.Text = currentSettings.DF3;
			nudFloppyCount.Value = Math.Max(1, currentSettings.FloppyCount);
			txtDF3.Enabled = btnDF3Pick.Enabled = nudFloppyCount.Value >= 4;
			txtDF2.Enabled = btnDF2Pick.Enabled = nudFloppyCount.Value >= 3;
			txtDF1.Enabled = btnDF1Pick.Enabled = nudFloppyCount.Value >= 2;

			//Hard Disk
			cbDiskController.SelectedItem = currentSettings.DiskController.ToString();

			//Kickstart
			txtKickstart.Text = currentSettings.KickStart;

			//Misc.
			cbAudio.Checked = currentSettings.Audio != AudioDriver.Null;
		}

		private void UnbindSettings()
		{
			//fill currentSettings from the UI;

			//CPU
			currentSettings.Sku = Enum.Parse<CPUSku>((string)cbSku.SelectedItem);
			currentSettings.CPU = rbMusashi.Checked? CPUType.Musashi : CPUType.Native;
			currentSettings.AddressBits = currentSettings.Sku == CPUSku.MC68030 ? 32 : 24;

			//Chipset
			currentSettings.ChipSet = Enum.Parse<ChipSet>((string)cbChipset.SelectedItem);

			//Memory
			currentSettings.ChipMemory = Convert.ToSingle(dudChipRAM.SelectedItem);
			currentSettings.CPUSlotMemory = Convert.ToSingle(dudCPUSlot.SelectedItem);
			currentSettings.MotherboardMemory = Convert.ToSingle(dudMotherboard.SelectedItem);
			currentSettings.TrapdoorMemory = Convert.ToSingle(dudTrapdoor.SelectedItem);
			currentSettings.ZorroIIMemory = EncodeZorro2((string)dudZ2.SelectedItem);
			currentSettings.ZorroIIIMemory = EncodeZorro3((string)dudZ3.SelectedItem);

			//Floppies
			currentSettings.DF0 = txtDF0.Text;
			currentSettings.DF1 = txtDF1.Text;
			currentSettings.DF2 = txtDF2.Text;
			currentSettings.DF3 = txtDF3.Text;
			currentSettings.FloppyCount = (int)nudFloppyCount.Value;

			//Hard Disk
			currentSettings.DiskController = Enum.Parse<DiskController>((string)cbDiskController.SelectedItem);

			//Kickstart
			currentSettings.KickStart = txtKickstart.Text;

			//Misc.
			currentSettings.Audio = cbAudio.Checked ? AudioDriver.XAudio2 : AudioDriver.Null;
		}

		private static EmulationSettings DefaultSettings()
		{
			return new EmulationSettings();
		}

		public class Emulation
		{
			[JsonProperty("Emulation")]
			public EmulationSettings Settings { get; set; }
		}

		private void cbQuickStart_SelectedValueChanged(object sender, EventArgs e)
		{
			SetQuickStart();
		}

		private void SetQuickStart()
		{
			/*
A500, 512KB+512KB, OCS, KS1.3
A500+, 1MB+1MB, ECS, KS2.04
A600, 1MB, ECS, KS2.05
A1200, 2MB, AGA, KS3.1
A3000, 1MB+256MB, ECS, KS3.1
A4000, 2MB+16MB+128MB, AGA, KS3.1
*/
			switch (cbQuickStart.SelectedIndex)
			{
				case 0: break;

				case 1:
					currentSettings = new EmulationSettings();
					currentSettings.ChipMemory = 0.5f;
					currentSettings.AddressBits = 24;
					currentSettings.TrapdoorMemory = 0.5f;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench1.3.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick13.rom";
					break;

				case 2:
					currentSettings = new EmulationSettings();
					currentSettings.ChipMemory = 1.0f;
					currentSettings.AddressBits = 24;
					currentSettings.TrapdoorMemory = 1.0f;
					currentSettings.ChipSet = ChipSet.ECS;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench2.04.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick204.rom";
					break;

				case 3:
					currentSettings = new EmulationSettings();
					currentSettings.ChipMemory = 1.0f;
					currentSettings.AddressBits = 24;
					currentSettings.ChipSet = ChipSet.ECS;
					currentSettings.DiskController = DiskController.A600_A1200;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench2.04.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick205.rom";
					currentSettings.HardDiskCount = 2;
					break;

				case 4:
					currentSettings = new EmulationSettings();
					currentSettings.CPU = CPUType.Musashi;
					currentSettings.Sku = CPUSku.MC68EC020;
					currentSettings.ChipMemory = 2.0f;
					currentSettings.AddressBits = 24;
					currentSettings.ChipSet = ChipSet.AGA;
					currentSettings.DiskController = DiskController.A600_A1200;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench3.1.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick31_a1200.rom";
					currentSettings.HardDiskCount = 2;
					break;

				case 5:
					currentSettings = new EmulationSettings();
					currentSettings.CPU = CPUType.Musashi;
					currentSettings.Sku = CPUSku.MC68030;
					currentSettings.ChipMemory = 1.0f;
					currentSettings.ZorroIIIMemory = "256";
					currentSettings.AddressBits = 32;
					currentSettings.ChipSet = ChipSet.ECS;
					currentSettings.DiskController = DiskController.A3000;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench3.1.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick31_a3000.rom";
					break;

				case 6:
					currentSettings = new EmulationSettings();
					currentSettings.CPU = CPUType.Musashi;
					currentSettings.Sku = CPUSku.MC68030;
					currentSettings.ChipMemory = 2.0f;
					currentSettings.MotherboardMemory = 16;
					currentSettings.ZorroIIIMemory = "128";
					currentSettings.AddressBits = 32;
					currentSettings.ChipSet = ChipSet.AGA;
					currentSettings.DiskController = DiskController.A4000;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench3.1.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick31_a4000.rom";
					currentSettings.HardDiskCount = 2;
					break;

				default:
					string path = Path.Combine("../../../../config", (string)cbQuickStart.SelectedItem);
					path = Path.ChangeExtension(path, "cfg");
					LoadConfig(path);
					break;
			}
			BindSettings();
		}
	}
}
