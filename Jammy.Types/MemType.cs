namespace Jammy.Types
{
	public enum MemType : byte
	{
		Unknown,
		Code,
		Byte,
		Word,
		Long,
		Str
	}

	public class MemTypeCollection
	{
		public MemType[] memTypes;

		public MemTypeCollection(MemType[] memTypes)
		{
			this.memTypes = memTypes;
		}

		public MemType this[int i] => (i>=0 && i < memTypes.Length)  ? memTypes[i]: MemType.Unknown;
		public MemType this[uint i] => this[(int)i];
	}
}
