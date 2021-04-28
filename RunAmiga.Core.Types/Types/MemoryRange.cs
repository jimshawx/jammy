namespace RunAmiga.Core.Types.Types
{
	public class MemoryRange
	{
		public MemoryRange(uint start, long length)
		{
			Start = start;
			Length = length;
		}

		public uint Start { get; set; }
		public long Length { get; set; }

		public bool Contains(uint address)
		{
			return address >= Start && address < Start+Length;
		}
	}

	public class BulkMemoryRange
	{
		public uint StartAddress { get; set; }
		public byte[] Memory { get; set; } = new byte[0];
		public uint EndAddress => (uint)(StartAddress + Memory.Length);
	}
}
