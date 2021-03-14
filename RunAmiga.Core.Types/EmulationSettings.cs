namespace RunAmiga.Core.Types
{
	public class EmulationSettings
	{
		public string KickStart { get; set; }
		public string DF0 { get; set; }
		public int MemorySize { get;set; }
		public bool AlignmentExceptions { get; set; }
		public bool UnknownInstructionExceptions { get; set; }
		public bool UnknownEffectiveAddressExceptions { get; set; }
		public bool UnknownInstructionSizeExceptions { get; set; }
	}
}