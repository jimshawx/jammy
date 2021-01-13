namespace RunAmiga.Custom
{
	public class Beam : IEmulate
	{
		private ulong beamTime;

		//HRM 3rd Ed, PP24
		private uint beamHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		private uint beamVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313

		private uint vposr;
		private uint vhposr;

		public void Emulate(ulong ns)
		{
			beamTime += ns;

			//every 50Hz, reset the copper list
			if (beamTime > 140_000)
			{
				beamTime -= 140_000;
			}

			//roughly
			beamVert = (uint)((beamTime * 312) / 140_000);
			beamHorz = (uint)(beamTime % (140_000 / 312));
		}

		public void Reset()
		{
			beamTime = 0;
		}

		public ushort Read(uint address)
		{
			ushort value=0;

			switch (address)
			{
				case ChipRegs.VPOSR: 
					value = (ushort)((beamVert >> 8)&1);
					break;
				case ChipRegs.VHPOSR:
					value = (ushort)((beamVert<<8)|(beamHorz&0x00ff));
					break;
			}

			return value;
		}
	}
}