using System;
using System.Collections.Generic;
using System.Diagnostics;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	//https://www.amigacoding.com/index.php?title=CIA_Memory_Map

	

	
	public class CIA : IEmulate, IMemoryMappedDevice
	{
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
