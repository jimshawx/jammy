namespace RunAmiga.Core.Types.Types
{
	public class MemoryRange
	{
		public MemoryRange(uint start, uint length)
		{
			Start = start;
			Length = length;
		}

		public uint Start { get; set; }
		public uint Length { get; set; }

		public bool Contains(uint address)
		{
			return address >= Start && address-Start < Length;
		}
	}
}
