using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Jammy.Core.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Jammy.UI.Settings.Avalonia
{
	public partial class Settings : Window
	{
		private string configPath = "../../../../config";

		public Settings()
		{
			InitializeComponent();
			rbSynchronous.IsEnabled = true;

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

			//ActiveControl = btnQuickStart;
			//CancelButton = btnExit;
			//AcceptButton = btnGo;

			var cfgs = Array.Empty<string>();
			try
			{
				cfgs = Directory.GetFiles(configPath, "*.cfg", SearchOption.TopDirectoryOnly);
			}
			catch { }
			foreach (var item in cfgs.Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).Cast<object>())
			cbQuickStart.Items.Add(item);

			//bind in the default settings
			BindSettings();

			LoadConfig("emulationSettings.json");
		}

		private bool Default(bool? value) { return value??false; }

		public bool ConfigOK { get; set; }


		private void btnQuickStart_Click(object sender, RoutedEventArgs e)
		{
			SaveConfig("emulationSettings.json");
			ConfigOK = true;
			Close();
		}

		private void cbCPU_SelectedValueChanged(object sender, SelectionChangedEventArgs e)
		{
			if (StringFromSelection(cbSku.SelectedItem) == "MC68000")
			{
				rbNative.IsEnabled = true;
			}
			else
			{
				rbNative.IsChecked = false;
				rbNative.IsEnabled = false;
			}

			if (StringFromSelection(cbSku.SelectedItem) == "MC68000" ||
				StringFromSelection(cbSku.SelectedItem) == "MC68EC020" ||
				StringFromSelection(cbSku.SelectedItem) == "MC68030")
			{
				rbMusashi.IsEnabled = true;
			}
			else
			{
				rbMusashi.IsChecked = false;
				rbMusashi.IsEnabled = false;
			}

			if (!Default(rbMusashi.IsChecked) && !Default(rbNative.IsChecked))
				rbMusashiCS.IsChecked = true;
		}

		private void cbChipset_SelectedValueChanged(object sender, EventArgs e)
		{
		}

		private void btnDF0Pick_Click(object sender, RoutedEventArgs e)
		{
			var openFielDialog1 = StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions {
					SuggestedFileName = txtDF0.Text,
					FileTypeFilter = new List<FilePickerFileType>{new FilePickerFileType("ADF Files") { Patterns = new []{"*.adf", "*.zip", "*.adz", "*.rp9" } } }
				}).Result;
			
			if (openFielDialog1.Any())
				txtDF0.Text = openFielDialog1.First().Path.AbsolutePath.ToString();
		}

		private void btnDF1Pick_Click(object sender, RoutedEventArgs e)
		{
			var openFielDialog1 = StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions
				{
					SuggestedFileName = txtDF1.Text,
					FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("ADF Files") { Patterns = new[] { "*.adf", "*.zip", "*.adz", "*.rp9" } } }
				}).Result;

			if (openFielDialog1.Any())
				txtDF1.Text = openFielDialog1.First().Path.ToString();
		}

		private void btnDF2Pick_Click(object sender, RoutedEventArgs e)
		{
			var openFielDialog1 = StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions
				{
					SuggestedFileName = txtDF2.Text,
					FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("ADF Files") { Patterns = new[] { "*.adf", "*.zip", "*.adz", "*.rp9" } } }
				}).Result;

			if (openFielDialog1.Any())
				txtDF2.Text = openFielDialog1.First().Path.ToString();
		}

		private void btnDF3Pick_Click(object sender, RoutedEventArgs e)
		{
			var openFielDialog1 = StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions
				{
					SuggestedFileName = txtDF3.Text,
					FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("ADF Files") { Patterns = new[] { "*.adf", "*.zip", "*.adz", "*.rp9" } } }
				}).Result;

			if (openFielDialog1.Any())
				txtDF3.Text = openFielDialog1.First().Path.ToString();
		}

		private void btnROMPick_Click(object sender, RoutedEventArgs e)
		{
			var openFielDialog1 = StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions
				{
					SuggestedFileName = txtKickstart.Text,
					FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("ROM Files") { Patterns = new[] {"*.rom"} } }
				}).Result;

			if (openFielDialog1.Any())
				txtKickstart.Text = openFielDialog1.First().Path.ToString();
		}

		private void btnExit_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void btnLoadConfig_Click(object sender, RoutedEventArgs e)
		{
			var openFielDialog1 = StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions
				{
					SuggestedFileName = string.Empty,
					FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("Config Files") { Patterns = new[] {"*.cfg" } } }
				}).Result;

			if (openFielDialog1.Any())
				LoadConfig(openFielDialog1.First().Path.ToString());
		}

		private void btnSaveConfig_Click(object sender, RoutedEventArgs e)
		{
			SaveConfig(null);
		}

		private void btnSaveAsConfig_Click(object sender, RoutedEventArgs e)
		{
			var openFielDialog1 = StorageProvider.OpenFilePickerAsync(
				new FilePickerOpenOptions
				{
					SuggestedFileName = string.Empty,
					FileTypeFilter = new List<FilePickerFileType> { new FilePickerFileType("Config Files") { Patterns = new[] { "*.cfg" } } }
				}).Result;

			if (openFielDialog1.Any())
				SaveConfig(openFielDialog1.First().Path.ToString());
		}

		private void btnGo_Click(object sender, RoutedEventArgs e)
		{
			SaveConfig("emulationSettings.json");
			ConfigOK = true;
			Close();
		}

		private void nudFloppyCount_ValueChanged(object sender, NumericUpDownValueChangedEventArgs e)
		{
			int f = (int)nudFloppyCount.Value;
			txtDF3.IsEnabled = btnDF3Pick.IsEnabled = f >= 4;
			txtDF2.IsEnabled = btnDF2Pick.IsEnabled = f >= 3;
			txtDF1.IsEnabled = btnDF1Pick.IsEnabled = f >= 2;
		}

		private void rbNative_CheckedChanged(object sender, RoutedEventArgs e)
		{
			if (Default(rbNative.IsChecked)) cbSku.SelectedIndex = 0;
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
				btnSaveConfig.IsEnabled = true;
			}
			else
			{
				currentSettingsFile = "";
				btnSaveConfig.IsEnabled = false;
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

		private ComboBoxItem SelectionFromString(ItemCollection items, string item)
		{
			return items.Cast<ComboBoxItem>().SingleOrDefault(x=>(string)x.Content == item);
		}
		private string StringFromSelection(object? item)
		{
			return (string)((ComboBoxItem)item).Content;
		}

		private void BindSettings()
		{
			//put currentSettings on to the UI

			//CPU
			cbSku.SelectedItem = SelectionFromString(cbSku.Items, currentSettings.Sku.ToString());
			rbMusashi.IsChecked = currentSettings.CPU == CPUType.Musashi;
			rbNative.IsChecked = currentSettings.CPU == CPUType.Native;
			rbMusashiCS.IsChecked = currentSettings.CPU == CPUType.MusashiCSharp;

			rbNative.IsEnabled = currentSettings.Sku == CPUSku.MC68000;
			rbMusashi.IsEnabled = currentSettings.Sku == CPUSku.MC68000 ||
								currentSettings.Sku == CPUSku.MC68EC020 ||
								currentSettings.Sku == CPUSku.MC68030;

			//Chipset
			cbChipset.SelectedItem = SelectionFromString(cbChipset.Items, currentSettings.ChipSet.ToString());
			rbPAL.IsChecked = currentSettings.VideoFormat == VideoFormat.PAL;
			rbNTSC.IsChecked = currentSettings.VideoFormat == VideoFormat.NTSC;

			if (currentSettings.Sku == CPUSku.MC68000)
			{
				//We're gonna say it's an A500
				if (currentSettings.VideoFormat == VideoFormat.PAL) currentSettings.CPUFrequency = 7093790;
				if (currentSettings.VideoFormat == VideoFormat.NTSC) currentSettings.CPUFrequency = 7159090;
			}
			if (currentSettings.Sku == CPUSku.MC68EC020)
			{
				//We're gonna say it's an A1200
				if (currentSettings.VideoFormat == VideoFormat.PAL) currentSettings.CPUFrequency = 14180000;
				if (currentSettings.VideoFormat == VideoFormat.NTSC) currentSettings.CPUFrequency = 14320000;
			}
			else if (currentSettings.Sku == CPUSku.MC68030 || currentSettings.Sku == CPUSku.MC68040)
			{
				//We're gonna say it's an A3000/4000 25MHz
				if (currentSettings.VideoFormat == VideoFormat.PAL) currentSettings.CPUFrequency = 25000000;
				if (currentSettings.VideoFormat == VideoFormat.NTSC) currentSettings.CPUFrequency = 25000000;
			}
			//todo: until all the code understands this (e.g. CIA timers think they run 1/10th CPU speed)
			currentSettings.CPUFrequency = 7093790;

			//Memory
			dudChipRAM.SelectedItem = SelectionFromString(dudChipRAM.Items, currentSettings.ChipMemory.ToString("0.0;0.0;0"));
			dudCPUSlot.SelectedItem = SelectionFromString(dudCPUSlot.Items, currentSettings.CPUSlotMemory.ToString());
			dudMotherboard.SelectedItem = SelectionFromString(dudMotherboard.Items, currentSettings.MotherboardMemory.ToString());
			dudTrapdoor.SelectedItem = SelectionFromString(dudTrapdoor.Items, currentSettings.TrapdoorMemory.ToString("0.0;0.0;0"));
			dudZ2.SelectedItem = SelectionFromString(dudZ2.Items, DecodeZorro2(currentSettings.ZorroIIMemory));
			dudZ3.SelectedItem = SelectionFromString(dudZ3.Items, DecodeZorro3(currentSettings.ZorroIIIMemory));

			//Floppies
			txtDF0.Text = currentSettings.DF0;
			txtDF1.Text = currentSettings.DF1;
			txtDF2.Text = currentSettings.DF2;
			txtDF3.Text = currentSettings.DF3;
			nudFloppyCount.Value = Math.Max(1, currentSettings.FloppyCount);
			txtDF3.IsEnabled = btnDF3Pick.IsEnabled = nudFloppyCount.Value >= 4;
			txtDF2.IsEnabled = btnDF2Pick.IsEnabled = nudFloppyCount.Value >= 3;
			txtDF1.IsEnabled = btnDF1Pick.IsEnabled = nudFloppyCount.Value >= 2;

			//Hard Disk
			cbDiskController.SelectedItem = SelectionFromString(cbDiskController.Items, currentSettings.DiskController.ToString());
			nudHardDiskCount.Value = currentSettings.HardDiskCount;

			//Kickstart
			txtKickstart.Text = currentSettings.KickStart;

			//Misc.
			cbAudio.IsChecked = currentSettings.Audio != AudioDriver.Null;

			//Debugging
			cbDebugging.IsChecked = currentSettings.Debugger == Feature.Enabled;

			//Blitter
			rbImmediate.IsChecked = false;//currentSettings.BlitterMode == BlitterMode.Immediate;
			rbSynchronous.IsChecked = true;//currentSettings.BlitterMode == BlitterMode.Synchronous;
		}

		private void UnbindSettings()
		{
			//fill currentSettings from the UI;

			//CPU
			currentSettings.Sku = Enum.Parse<CPUSku>(StringFromSelection(cbSku.SelectedItem));
			currentSettings.CPU = Default(rbMusashi.IsChecked) ? CPUType.Musashi : (Default(rbMusashiCS.IsChecked) ? CPUType.MusashiCSharp : CPUType.Native);
			currentSettings.AddressBits = (currentSettings.Sku == CPUSku.MC68030
										|| currentSettings.Sku == CPUSku.MC68040) ? 32 : 24;

			//Chipset
			currentSettings.ChipSet = Enum.Parse<ChipSet>(StringFromSelection(cbChipset.SelectedItem));
			currentSettings.VideoFormat = Default(rbNTSC.IsChecked) ? VideoFormat.NTSC : VideoFormat.PAL;

			//Memory
			currentSettings.ChipMemory = Convert.ToSingle(StringFromSelection(dudChipRAM.SelectedItem));
			currentSettings.CPUSlotMemory = Convert.ToSingle(StringFromSelection(dudCPUSlot.SelectedItem));
			currentSettings.MotherboardMemory = Convert.ToSingle(StringFromSelection(dudMotherboard.SelectedItem));
			currentSettings.TrapdoorMemory = Convert.ToSingle(StringFromSelection(dudTrapdoor.SelectedItem));
			currentSettings.ZorroIIMemory = EncodeZorro2(StringFromSelection(dudZ2.SelectedItem));
			currentSettings.ZorroIIIMemory = EncodeZorro3(StringFromSelection(dudZ3.SelectedItem));

			//Floppies
			currentSettings.DF0 = txtDF0.Text;
			currentSettings.DF1 = txtDF1.Text;
			currentSettings.DF2 = txtDF2.Text;
			currentSettings.DF3 = txtDF3.Text;
			currentSettings.FloppyCount = (int)nudFloppyCount.Value;

			//Hard Disk
			currentSettings.DiskController = Enum.Parse<DiskController>(StringFromSelection(cbDiskController.SelectedItem));
			currentSettings.HardDiskCount = (int)nudHardDiskCount.Value;

			//Kickstart
			currentSettings.KickStart = txtKickstart.Text;

			//Get the Kickstart CRC for identification
			try {
				var rom = File.ReadAllBytes(currentSettings.KickStart);
				var c = rom.Skip(rom.Length - 24).Take(4).Select(x => (uint)x).ToArray();
				uint crc = c[3] | (c[2] << 8) | (c[1] << 16) | (c[0] << 24);
				currentSettings.KickStartDisassembly = $"{crc:X8}";
			}
			catch
			{
				currentSettings.KickStart = null;
				currentSettings.KickStartDisassembly = null;
			}

			//Misc.
			currentSettings.Audio = Default(cbAudio.IsChecked) ? AudioDriver.XAudio2 : AudioDriver.Null;

			//Debugging
			currentSettings.Debugger = Default(cbDebugging.IsChecked) ? Feature.Enabled : Feature.Disabled;

			//Blitter
			currentSettings.BlitterMode = Default(rbImmediate.IsChecked) ? BlitterMode.Immediate : BlitterMode.Synchronous;
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
					btnSaveConfig.IsEnabled = false;
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
					btnSaveConfig.IsEnabled = false;
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
					btnSaveConfig.IsEnabled = false;
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
					btnSaveConfig.IsEnabled = false;
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
					btnSaveConfig.IsEnabled = false;
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
					btnSaveConfig.IsEnabled = false;
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
					btnSaveConfig.IsEnabled = false;
					break;

				default:
					string path = Path.Combine("../../../../config", StringFromSelection(cbQuickStart.SelectedItem));
					path = Path.ChangeExtension(path, "cfg");
					LoadConfig(path);
					btnSaveConfig.IsEnabled = true;
					break;
			}
		}
	}

	public static class SettingsUI
	{
		public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>() // `App` is child of `Application`
		.UsePlatformDetect()
		.LogToTrace(LogEventLevel.Verbose)
		.UseReactiveUI();

		public static void Run(string[] args)
		{
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}
	}
}