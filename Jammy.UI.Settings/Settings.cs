using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Jammy.Core.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.UI.Settings
{
	public partial class Settings : Form
	{
		private string configPath = "../../../../config";

		public Settings()
		{
			InitializeComponent();
			rbSynchronous.Enabled = true;

			JsonConvert.DefaultSettings = () =>
			{
				var s = new JsonSerializerSettings { Formatting = Formatting.Indented };
				s.Converters.Add(new StringEnumConverter());
				s.Error = (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs e) =>
				{
					//ignore all the errors
					e.ErrorContext.Handled = true;
				};
				return s;
			};

			//Quickstart
			cbQuickStart.SelectedIndex = 0;
			ActiveControl = btnQuickStart;
			CancelButton = btnExit;
			AcceptButton = btnGo;

			var cfgs = Array.Empty<string>();
			try
			{
				cfgs = Directory.GetFiles(configPath, "*.cfg", SearchOption.TopDirectoryOnly);
			} catch {}
			cbQuickStart.Items.AddRange(cfgs.Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).Cast<object>().ToArray());

			//bind in the default settings
			BindSettings();

			LoadConfig("emulationSettings.json");
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool ConfigOK { get; set; }

		private void btnQuickStart_Click(object sender, EventArgs e)
		{
			SaveConfig("emulationSettings.json");
			ConfigOK = true;
			Close();
		}

		private void cbCPU_SelectedValueChanged(object sender, EventArgs e)
		{
			if ((string)cbSku.SelectedItem == "MC68000")
			{
				rbNative.Enabled = true;
			}
			else
			{
				rbNative.Checked = false;
				rbNative.Enabled = false;
			}

			if ((string)cbSku.SelectedItem == "MC68000" ||
				(string)cbSku.SelectedItem == "MC68EC020" ||
				(string)cbSku.SelectedItem == "MC68030")
			{
				rbMusashi.Enabled = true;
			}
			else
			{
				rbMusashi.Checked = false;
				rbMusashi.Enabled = false;
			}

			if (!rbMusashi.Checked && !rbNative.Checked)
				rbMusashiCS.Checked = true;
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
			openFileDialog1.FileName = txtDF3.Text;
			if (openFileDialog1.ShowDialog() == DialogResult.OK)
				txtDF3.Text = openFileDialog1.FileName;
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
			txtDF3.Enabled = btnDF3Pick.Enabled = f >= 4;
			txtDF2.Enabled = btnDF2Pick.Enabled = f >= 3;
			txtDF1.Enabled = btnDF1Pick.Enabled = f >= 2;
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
			string cfg = JsonConvert.SerializeObject(new Emulation { Settings = currentSettings });
			File.WriteAllText(fileName, cfg);
			UpdateCurrentSettingFilename(fileName);
		}

		private void UpdateCurrentSettingFilename(string fileName)
		{
			if (cbQuickStart.SelectedIndex > 6)
			{
				currentSettingsFile = fileName;
				btnSaveConfig.Enabled = true;
			}
			else
			{
				currentSettingsFile = "";
				btnSaveConfig.Enabled = false;
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
			return string.Join('+', s.Split(',').Select(x => Convert.ToSingle(x).ToString("0.0;0.0;0")));
		}

		private void BindSettings()
		{
			//put currentSettings on to the UI

			//CPU
			cbSku.SelectedItem = currentSettings.Sku.ToString();
			rbMusashi.Checked = currentSettings.CPU == CPUType.Musashi;
			rbNative.Checked = currentSettings.CPU == CPUType.Native;
			rbMusashiCS.Checked = currentSettings.CPU == CPUType.MusashiCSharp;

			rbNative.Enabled = currentSettings.Sku == CPUSku.MC68000;
			rbMusashi.Enabled = currentSettings.Sku == CPUSku.MC68000 || 
								currentSettings.Sku == CPUSku.MC68EC020 || 
								currentSettings.Sku == CPUSku.MC68030;

			//Chipset
			cbChipset.SelectedItem = currentSettings.ChipSet.ToString();
			rbPAL.Checked = currentSettings.VideoFormat == VideoFormat.PAL;
			rbNTSC.Checked = currentSettings.VideoFormat == VideoFormat.NTSC;

			if (currentSettings.Sku == CPUSku.MC68000)
			{
				//We're gonna say it's an A500
				if (currentSettings.VideoFormat == VideoFormat.PAL) currentSettings.CPUFrequency = 7093790;
				if (currentSettings.VideoFormat == VideoFormat.NTSC) currentSettings.CPUFrequency= 7159090;
			}
			if (currentSettings.Sku == CPUSku.MC68EC020)
			{
				//We're gonna say it's an A1200
				if (currentSettings.VideoFormat == VideoFormat.PAL) currentSettings.CPUFrequency = 14180000;
				if (currentSettings.VideoFormat == VideoFormat.NTSC) currentSettings.CPUFrequency= 14320000;
			}
			else if (currentSettings.Sku == CPUSku.MC68030 || currentSettings.Sku == CPUSku.MC68040)
			{
				//We're gonna say it's an A3000/4000 25MHz
				if (currentSettings.VideoFormat == VideoFormat.PAL) currentSettings.CPUFrequency = 25000000;
				if (currentSettings.VideoFormat == VideoFormat.NTSC) currentSettings.CPUFrequency= 25000000;
			}
			//todo: until all the code understands this (e.g. CIA timers think they run 1/10th CPU speed)
			currentSettings.CPUFrequency = 7093790;

			//Memory
			dudChipRAM.SelectedItem = currentSettings.ChipMemory.ToString("0.0;0.0;0");
			dudCPUSlot.SelectedItem = currentSettings.CPUSlotMemory.ToString();
			dudMotherboard.SelectedItem = currentSettings.MotherboardMemory.ToString();
			dudTrapdoor.SelectedItem = currentSettings.TrapdoorMemory.ToString("0.0;0.0;0");
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
			nudHardDiskCount.Value = currentSettings.HardDiskCount;

			//Kickstart
			txtKickstart.Text = currentSettings.KickStart;

			//Misc.
			cbAudio.Checked = currentSettings.Audio != AudioDriver.Null;

			//Debugging
			cbDebugging.Checked = currentSettings.Debugger == Feature.Enabled;

			//Blitter
			rbImmediate.Checked = false;//currentSettings.BlitterMode == BlitterMode.Immediate;
			rbSynchronous.Checked = true;//currentSettings.BlitterMode == BlitterMode.Synchronous;
		}

		private void UnbindSettings()
		{
			//fill currentSettings from the UI;

			//CPU
			currentSettings.Sku = Enum.Parse<CPUSku>((string)cbSku.SelectedItem);
			currentSettings.CPU = rbMusashi.Checked ? CPUType.Musashi : (rbMusashiCS.Checked ? CPUType.MusashiCSharp : CPUType.Native);
			currentSettings.AddressBits = (currentSettings.Sku == CPUSku.MC68030
										|| currentSettings.Sku == CPUSku.MC68040) ? 32 : 24;

			//Chipset
			currentSettings.ChipSet = Enum.Parse<ChipSet>((string)cbChipset.SelectedItem);
			currentSettings.VideoFormat = rbNTSC.Checked ? VideoFormat.NTSC : VideoFormat.PAL;

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
			currentSettings.HardDiskCount = (int)nudHardDiskCount.Value;

			//Kickstart
			currentSettings.KickStart = txtKickstart.Text;

			//Get the Kickstart CRC for identification
			try
			{
				var rom = File.ReadAllBytes(currentSettings.KickStart);
				var c = rom.Skip(rom.Length - 24).Take(4).Select(x=>(uint)x).ToArray();
				uint crc = c[3] | (c[2] << 8) | (c[1] << 16) | (c[0] << 24);
				currentSettings.KickStartDisassembly = $"{crc:X8}";
			}
			catch
			{
				currentSettings.KickStart = null;
				currentSettings.KickStartDisassembly = null;
			}

			//Misc.
			currentSettings.Audio = cbAudio.Checked ? AudioDriver.XAudio2 : AudioDriver.Null;

			//Debugging
			currentSettings.Debugger = cbDebugging.Checked ? Feature.Enabled : Feature.Disabled;

			//Blitter
			currentSettings.BlitterMode = rbImmediate.Checked ? BlitterMode.Immediate : BlitterMode.Synchronous;
		}

		private static EmulationSettings DefaultSettings()
		{
			return new EmulationSettings { ChipMemory = 0.5f };
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
				case 0:
					LoadConfig("emulationSettings.json");
					btnSaveConfig.Enabled = false;
					break;

				case 1:
					currentSettings = new EmulationSettings();
					currentSettings.ChipMemory = 0.5f;
					currentSettings.AddressBits = 24;
					currentSettings.TrapdoorMemory = 0.5f;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench1.3.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick13.rom";
					BindSettings();
					btnSaveConfig.Enabled = false;
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
					BindSettings();
					btnSaveConfig.Enabled = false;
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
					BindSettings();
					btnSaveConfig.Enabled = false;
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
					BindSettings();
					btnSaveConfig.Enabled = false;
					break;

				case 5:
					currentSettings = new EmulationSettings();
					currentSettings.CPU = CPUType.Musashi;
					currentSettings.Sku = CPUSku.MC68030;
					currentSettings.ChipMemory = 1.0f;
					currentSettings.MotherboardMemory = 16;
					currentSettings.ZorroIIIMemory = "256";
					currentSettings.AddressBits = 32;
					currentSettings.ChipSet = ChipSet.ECS;
					currentSettings.DiskController = DiskController.A3000;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench3.1.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick31_a3000.rom";
					BindSettings();
					btnSaveConfig.Enabled = false;
					break;

				case 6:
					currentSettings = new EmulationSettings();
					currentSettings.CPU = CPUType.Musashi;
					currentSettings.Sku = CPUSku.MC68030;
					currentSettings.ChipMemory = 2.0f;
					currentSettings.MotherboardMemory = 16;
					currentSettings.ZorroIIIMemory = "256";
					currentSettings.AddressBits = 32;
					currentSettings.ChipSet = ChipSet.AGA;
					currentSettings.DiskController = DiskController.A4000;
					currentSettings.Audio = AudioDriver.XAudio2;
					currentSettings.DF0 = "workbench3.1.adf";
					currentSettings.FloppyCount = 1;
					currentSettings.KickStart = "kick31_a4000.rom";
					currentSettings.HardDiskCount = 2;
					BindSettings();
					btnSaveConfig.Enabled = false;
					break;

				default:
					string path = Path.Combine("../../../../config", (string)cbQuickStart.SelectedItem);
					path = Path.ChangeExtension(path, "cfg");
					LoadConfig(path);
					btnSaveConfig.Enabled = true;
					break;
			}
		}
	}
}
