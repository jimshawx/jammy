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

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			Trace.WriteLine($"Blitter Read {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");
			return value;
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			Trace.WriteLine($"W {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.BLTCON0:
					uint lf = (uint)value & 0xff;
					uint ash = (uint)value >> 12;
					uint use = (uint)(value >> 8) & 0xf;
					Trace.WriteLine($"minterm:{lf:X2} ash:{ash} use:{Convert.ToString(use, 2).PadLeft(4, '0')}");
					break;
				case ChipRegs.BLTCON1:
					uint bsh = (uint)value >> 12;
					uint doff = (uint)(value >> 7) & 1;
					uint efe = (uint)(value >> 4) & 1;
					uint ife = (uint)(value >> 3) & 1;
					uint fci = (uint)(value >> 2) & 1;
					uint desc = (uint)(value >> 1) & 1;
					uint line = (uint)value & 1;
					Trace.WriteLine($"bsh:{bsh} doff:{doff} efe:{efe} ife:{ife} fci:{fci} desc:{desc} line:{line}");
					break;
				case ChipRegs.BLTAFWM: break;
				case ChipRegs.BLTALWM: break;

				case ChipRegs.BLTCPTH: break;
				case ChipRegs.BLTCPTL: break;
				case ChipRegs.BLTBPTH: break;
				case ChipRegs.BLTBPTL: break;
				case ChipRegs.BLTAPTH: break;
				case ChipRegs.BLTAPTL: break;
				case ChipRegs.BLTDPTH: break;
				case ChipRegs.BLTDPTL: break;

				case ChipRegs.BLTSIZE:
					uint width = (uint)value & 0x1f;
					uint height = (uint)value >> 5;
					Trace.WriteLine($"size:{width}x{height}");
					break;

				case ChipRegs.BLTCON0L:
					uint minterm = (uint)value & 0xff;
					Trace.WriteLine($"minterm:{minterm:X2}");
					break;

				case ChipRegs.BLTSIZV: break;
				case ChipRegs.BLTSIZH: break;
				case ChipRegs.BLTCMOD: break;
				case ChipRegs.BLTBMOD: break;
				case ChipRegs.BLTAMOD: break;
				case ChipRegs.BLTDMOD: break;
				case ChipRegs.BLTCDAT: break;
				case ChipRegs.BLTBDAT: break;
				case ChipRegs.BLTADAT: break;

				case ChipRegs.BLTDDAT: break;
			}
		}
	}
}