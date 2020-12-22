using RunAmiga.Types;

namespace RunAmiga
{
	public interface IMemoryMappedDevice
	{
		public bool IsMapped(uint address);
		public uint Read(uint address, Size size);
		public void Write(uint address, uint value, Size size);
	}
}
