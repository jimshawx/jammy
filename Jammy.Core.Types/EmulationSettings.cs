namespace Jammy.Core.Types
{
	public enum CPUType
	{
		Native,
		Musashi
	}

	public enum CPUSku
	{
		MC68000,
		MC68EC020,
		MC68030
	}

	public enum AudioDriver
	{
		Null,
		XAudio2
	}

	public enum Feature
	{
		Disabled,
		Enabled
	}

	public enum ChipSet
	{
		OCS,
		ECS,
		AGA,
	}

	public enum SerialConsole
	{
		Null,
		ANSI,
		Emulation
	}

	public enum DiskController
	{
		None,
		A600_A1200,
		A3000,
		A4000
	}

	public class EmulationSettings
	{
		public string KickStart { get; set; }
		public string DF0 { get; set; }
		public string DF1 { get; set; }
		public string DF2 { get; set; }
		public string DF3 { get; set; }

		public int AddressBits { get;set; }

		public bool UnknownInstructionSizeExceptions { get; set; }

		public int FloppyCount { get; set; }
		public int HardDiskCount { get; set; }

		public string ZorroIIMemory { get; set; }
		public string ZorroIIIMemory { get; set; }
		public float TrapdoorMemory { get; set; }
		public float ChipMemory { get; set; }
		public float MotherboardMemory { get; set; }
		public float CPUSlotMemory { get; set; }

		public Feature Disassemblies { get; set; }

		public CPUType CPU { get; set; }
		public CPUSku Sku { get; set; }
		public AudioDriver Audio { get; set; }
		public Feature Tracer { get; set; }
		public ChipSet ChipSet { get; set; }
		public SerialConsole Console { get; set; }
		public DiskController DiskController { get; set; }
	}
}