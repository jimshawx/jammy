using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class BattClock : IEmulate, IMemoryMappedDevice
	{
		public void Emulate(ulong cycles)
		{
			
		}

		public void Reset()
		{
			
		}

		public bool IsMapped(uint address)
		{
			//return (address >= 0xdc0000 && address < 0xdd0000) ||
			//	   (address >= 0xd80000 && address < 0xd90000);
			return address >= 0xdc0000 && address < 0xd90000;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			Logger.WriteLine($"[BATTCLOCK] R {address:X8} @ {insaddr:X8}");
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			Logger.WriteLine($"[BATTCLOCK] W {address:X8} {value:X8} @ {insaddr:X8}");
		}
	}
}
