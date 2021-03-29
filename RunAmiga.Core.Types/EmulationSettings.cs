namespace RunAmiga.Core.Types
{
	public class EmulationSettings
	{
		public string KickStart { get; set; }
		public string DF0 { get; set; }
		public string DF1 { get; set; }
		public string DF2 { get; set; }
		public string DF3 { get; set; }
		public int MemorySize { get;set; }
		public bool AlignmentExceptions { get; set; }
		public bool UnknownInstructionExceptions { get; set; }
		public bool UnknownEffectiveAddressExceptions { get; set; }
		public bool UnknownInstructionSizeExceptions { get; set; }

		public float ZorroIIMemory { get; set; }
		public float TrapdoorMemory { get; set; }
		public float ChipMemory { get; set; }

		public bool ProduceDisassemblies { get; set; }
	}
}