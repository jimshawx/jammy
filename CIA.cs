using runamiga.Types;
using System.Diagnostics;

namespace runamiga
{
	public class CIA : IEmulate, IMemoryMappedDevice
	{
		public void Emulate()
		{

		}

		public bool IsMapped(uint address)
		{
			return (address>>16)==0xbf;
		}
		public uint Read(uint address, Size size)
		{
			Trace.WriteLine($"CIA Read {address:X8} {size}");
			return 0;
		}
		public void Write(uint address, uint value, Size size)
		{
			Trace.WriteLine($"CIA Write {address:X8} {value:X8} {size}");
		}
	}
}
