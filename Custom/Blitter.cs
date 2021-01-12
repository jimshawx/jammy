using System;
using System.Diagnostics;

namespace RunAmiga.Custom
{
	public class Blitter : IEmulate
	{
		private Chips custom;

		public Blitter(Chips custom)
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
			Trace.WriteLine($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");
			return value;
		}

		private uint bltapt;
		private uint bltbpt;
		private uint bltcpt;
		private uint bltdpt;

		private uint bltamod;
		private uint bltbmod;
		private uint bltcmod;
		private uint bltdmod;

		private uint bltafwm;
		private uint bltalwm;

		private uint bltsizv;

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

				case ChipRegs.BLTAFWM:
					bltafwm = value;
					break;
				case ChipRegs.BLTALWM:
					bltalwm = value;
					break;

				case ChipRegs.BLTCPTH:
					bltcpt = (bltcpt & 0x0000ffff) | ((uint)value << 16);
					break;
				case ChipRegs.BLTCPTL:
					bltcpt = (bltcpt & 0xffff0000) | value;
					break;
				case ChipRegs.BLTBPTH:
					bltbpt = (bltbpt & 0x0000ffff) | ((uint)value << 16);
					break;
				case ChipRegs.BLTBPTL:
					bltbpt = (bltbpt & 0xffff0000) | value;
					break;
				case ChipRegs.BLTAPTH:
					bltapt = (bltapt & 0x0000ffff) | ((uint)value << 16);
					break;
				case ChipRegs.BLTAPTL:
					bltapt = (bltapt & 0xffff0000) | value;
					break;
				case ChipRegs.BLTDPTH:
					bltdpt = (bltdpt & 0x0000ffff) | ((uint)value << 16);
					break;
				case ChipRegs.BLTDPTL:
					bltdpt = (bltdpt & 0xffff0000) | value;
					break;

				case ChipRegs.BLTSIZE:
					uint width = (uint)value & 0x1f;
					uint height = (uint)value >> 5;
					Trace.WriteLine($"BLIT! size:{width}x{height}");
					break;

				case ChipRegs.BLTCON0L:
					uint minterm = (uint)value & 0xff;
					Trace.WriteLine($"minterm:{minterm:X2}");
					break;

				case ChipRegs.BLTSIZV:
					bltsizv = value;
					break;
				case ChipRegs.BLTSIZH:
					Trace.WriteLine($"BLIT! size:{value}x{bltsizv}");
					break;

				case ChipRegs.BLTCMOD:
					bltcmod = value;
					break;
				case ChipRegs.BLTBMOD:
					bltbmod = value; 
					break;
				case ChipRegs.BLTAMOD:
					bltamod = value; 
					break;
				case ChipRegs.BLTDMOD:
					bltdmod = value; 
					break;

				case ChipRegs.BLTCDAT: break;
				case ChipRegs.BLTBDAT: break;
				case ChipRegs.BLTADAT: break;
				case ChipRegs.BLTDDAT: break;
			}
		}
	}
}