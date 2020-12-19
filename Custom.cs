using runamiga.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace runamiga
{
	public class Custom : IEmulate, IMemoryMappedDevice
	{
		public void Emulate()
		{
		}

		public bool IsMapped(uint address)
		{
			return (address >> 16) == 0xdf;
		}

		public uint Read(uint address, Size size)
		{
			Trace.WriteLine($"Custom Read {address:X8} {size}");
			return 0;
		}

		public void Write(uint address, uint value, Size size)
		{
			Trace.WriteLine($"Custom Write {address:X8} {value:X8} {size}");
		}
	}
}
