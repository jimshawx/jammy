namespace RunAmiga.Custom
{
	public class Audio : IAudio
	{
		private readonly IMemory memory;

		public Audio(IMemory memory)
		{
			this.memory = memory;
		}

		public void Emulate(ulong cycles)
		{
			
		}

		public void Reset()
		{
			
		}

		private ushort[] audper = new ushort[4];
		private ushort[] audvol = new ushort[4];

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.AUD0PER: audper[0] = value; break;
				case ChipRegs.AUD1PER: audper[1] = value; break;
				case ChipRegs.AUD2PER: audper[2] = value; break;
				case ChipRegs.AUD3PER: audper[3] = value; break;
				case ChipRegs.AUD0VOL: audvol[0] = value; break;
				case ChipRegs.AUD1VOL: audvol[1] = value; break;
				case ChipRegs.AUD2VOL: audvol[2] = value; break;
				case ChipRegs.AUD3VOL: audvol[3] = value; break;
			}
		}

		public ushort Read(uint insaddr, uint address)
		{
			uint value = 0;
			switch (address)
			{
				case ChipRegs.AUD0PER: value = audper[0]; break;
				case ChipRegs.AUD1PER: value = audper[1]; break;
				case ChipRegs.AUD2PER: value = audper[2]; break;
				case ChipRegs.AUD3PER: value = audper[3]; break;
				case ChipRegs.AUD0VOL: value = audvol[0]; break;
				case ChipRegs.AUD1VOL: value = audvol[1]; break;
				case ChipRegs.AUD2VOL: value = audvol[2]; break;
				case ChipRegs.AUD3VOL: value = audvol[3]; break;
			}
			return 0;
		}
	}
}