using RunAmiga.Types;

namespace RunAmiga.Custom
{
	//https://www.amigacoding.com/index.php?title=CIA_Memory_Map

	public class CIA : IEmulate, IMemoryMappedDevice
	{
			public const int PRA = 0;
			public const int PRB = 1;
			public const int DDRA = 2;
			public const int DDRB = 3;
			public const int TALO = 4;
			public const int TAHI = 5;
			public const int TBLO = 6;
			public const int TBHI = 7;
			public const int TODLO = 8;
			public const int TODMID = 9;
			public const int TODHI = 10;
			public const int NA = 11;
			public const int SDR = 12;
			public const int ICR = 13;
			public const int CRA = 14;
			public const int CRB = 15;

		private CIAAOdd ciaA;
		private CIABEven ciaB;

		public CIA(Debugger debugger, DiskDrives diskDrives, Mouse mouse, Interrupt interrupt)
		{
			ciaA = new CIAAOdd(debugger, diskDrives, mouse, interrupt);
			ciaB = new CIABEven(debugger, diskDrives, interrupt);
		}

		public void Emulate(ulong ns)
		{
			ciaA.Emulate(ns);
			ciaB.Emulate(ns);
		}

		public void Reset()
		{
			ciaA.Reset();
			ciaB.Reset();
		}

		public bool IsMapped(uint address)
		{
			return (address>>16)==0xbf;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (ciaA.IsMapped(address)) return ciaA.Read(insaddr, address, size);
			if (ciaB.IsMapped(address)) return ciaB.Read(insaddr, address, size);
			return 0;
		}
		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (ciaA.IsMapped(address)) { ciaA.Write(insaddr, address, value, size); return; }
			if (ciaB.IsMapped(address)) { ciaB.Write(insaddr, address, value, size); return; }
		}
	}
}
