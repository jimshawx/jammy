namespace RunAmiga.Core.Types.Types
{
	public class ZorroConfiguration
	{
		public string Name { get; set; }
		public bool IsConfigured { get; set; }
		public uint BaseAddress { get; set; }
		public byte[] Config { get; set; }
	}
}