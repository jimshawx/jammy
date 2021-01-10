using System;
using System.Diagnostics;

namespace RunAmiga.Custom
{
	public class Blitter : IEmulate
	{
		private RunAmiga.Custom.Chips custom;

		public Blitter(RunAmiga.Custom.Chips custom)
		{
			this.custom = custom;
		}

		public void Emulate(ulong ns)
		{
			
		}

		public void Reset()
		{
			
		}

		public void Write(uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.BLTCON0:
					uint lf = (uint)value & 0xff;
					uint ash = (uint)value >>12;
					uint use = (uint)(value>>8) & 0xf;
					Trace.WriteLine($"minterm:{lf:X2} ash:{ash} use:{Convert.ToString(use,2).PadLeft(4,'0')}");
					break;
				case ChipRegs.BLTCON0L:
					uint minterm = (uint) value & 0xff;
					Trace.WriteLine($"minterm:{minterm:X2}");
					break;
				case ChipRegs.BLTCON1:
					uint bsh = (uint)value >> 12;
					uint doff = (uint) (value >> 7) & 1;
					uint efe = (uint)(value >> 4) & 1;
					uint ife = (uint)(value >> 3) & 1;
					uint fci = (uint)(value >> 2) & 1;
					uint desc = (uint)(value >> 1) & 1;
					uint line = (uint)value & 1;
					Trace.WriteLine($"bsh:{bsh} doff:{doff} efe:{efe} ife:{ife} fci:{fci} desc:{desc} line:{line}");
					break;
				case ChipRegs.BLTSIZE:
					uint width = (uint)value & 0x1f;
					uint height = (uint)value >> 5;
					Trace.WriteLine($"size:{width}x{height}");
					break;
			}
		}
	}
}