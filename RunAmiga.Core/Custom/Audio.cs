using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class Audio : IAudio
	{
		private readonly IMemory memory;
		private readonly IChips custom;
		private readonly IInterrupt interrupt;
		private readonly uint[] intr = { Interrupt.AUD0, Interrupt.AUD1, Interrupt.AUD2, Interrupt.AUD3 };
		private readonly AudioChannel[] ch = new AudioChannel[4];
		private readonly AudioChannel[] shadowch = new AudioChannel[4];

		public Audio(IMemory memory, IChips custom, IInterrupt interrupt)
		{
			this.memory = memory;
			this.custom = custom;
			this.interrupt = interrupt;
			for (int i = 0; i < 4; i++)
			{
				ch[i] = new AudioChannel();
				shadowch[i] = new AudioChannel();
			}
		}

		private ulong audioTime;

		//audio frequency is CPUHz (7.14MHz) / 200, 35.7KHz
		private ushort lastdmacon = 0;
		public void Emulate(ulong cycles)
		{
			audioTime += cycles;

			if (audioTime > 200)
			{
				audioTime -= 200;

				ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
				ushort adkcon = (ushort)custom.Read(0, ChipRegs.ADKCONR, Size.Word);

				var dmaconchanges = (ushort)(dmacon ^ lastdmacon);
				lastdmacon = dmacon;

				ChannelToggle(0, (dmaconchanges & dmacon & (uint)ChipRegs.DMA.AUD0) != 0);
				ChannelToggle(1, (dmaconchanges & dmacon & (uint)ChipRegs.DMA.AUD1) != 0);
				ChannelToggle(2, (dmaconchanges & dmacon & (uint)ChipRegs.DMA.AUD2) != 0);
				ChannelToggle(3, (dmaconchanges & dmacon & (uint)ChipRegs.DMA.AUD3) != 0);
				
				if ((dmacon & (uint)ChipRegs.DMA.AUD0) != 0) Playing(0);
				if ((dmacon & (uint)ChipRegs.DMA.AUD1) != 0) Playing(1);
				if ((dmacon & (uint)ChipRegs.DMA.AUD2) != 0) Playing(2);
				if ((dmacon & (uint)ChipRegs.DMA.AUD3) != 0) Playing(3);
			}
		}

		private void Playing(int channel)
		{
			//loop restart?
			if (shadowch[channel].audlen == 0)
				ChannelToggle(channel, true);

			//read the sample into live audXdat
			ch[channel].auddat = memory.Read16(shadowch[channel].audlc);

			//these should tick more slowly based on audper
			shadowch[channel].audlc += 2;
			shadowch[channel].audlen--;
		}

		private void ChannelToggle(int channel, bool onOff)
		{
			if (onOff)
			{
				ch[channel].CopyTo(shadowch[channel]);
				interrupt.AssertInterrupt(intr[channel]);
			}
		}

		public void Reset()
		{
			for (int i = 0; i < 4; i++)
				ch[i].Clear();
		}

		public class AudioChannel
		{
			public ushort audper { get; set; }
			public ushort audvol { get; set; }
			public ushort audlen { get; set; }
			public ushort auddat { get; set; }
			public uint audlc { get; set; }

			public void CopyTo(AudioChannel cp)
			{
				cp.audper = this.audper;
				cp.audvol = this.audvol;
				cp.audlen = this.audlen;
				cp.auddat = this.auddat;
				cp.audlc = this.audlc;
			}

			public void Clear()
			{
				audper = 0;
				audvol = 0;
				audlen = 0;
				auddat = 0;
				audlc = 0;
			}
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.AUD0PER: ch[0].audper = value; break;
				case ChipRegs.AUD0VOL: ch[0].audvol = value; break;
				case ChipRegs.AUD0LEN: ch[0].audlen = value; break;
				case ChipRegs.AUD0DAT: ch[0].auddat = value; break;
				case ChipRegs.AUD0LCH: ch[0].audlc = (ch[0].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD0LCL: ch[0].audlc = ((ch[0].audlc & 0xffff0000) |value) & ChipRegs.ChipAddressMask; break;

				case ChipRegs.AUD1PER: ch[1].audper = value; break;
				case ChipRegs.AUD1VOL: ch[1].audvol = value; break;
				case ChipRegs.AUD1LEN: ch[1].audlen = value; break;
				case ChipRegs.AUD1DAT: ch[1].auddat = value; break;
				case ChipRegs.AUD1LCH: ch[1].audlc = (ch[1].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD1LCL: ch[1].audlc = ((ch[1].audlc & 0xffff0000) | value) & ChipRegs.ChipAddressMask; break;

				case ChipRegs.AUD2PER: ch[2].audper = value; break;
				case ChipRegs.AUD2VOL: ch[2].audvol = value; break;
				case ChipRegs.AUD2LEN: ch[2].audlen = value; break;
				case ChipRegs.AUD2DAT: ch[2].auddat = value; break;
				case ChipRegs.AUD2LCH: ch[2].audlc = (ch[2].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD2LCL: ch[2].audlc = ((ch[2].audlc & 0xffff0000) | value) & ChipRegs.ChipAddressMask; break;

				case ChipRegs.AUD3PER: ch[3].audper = value; break;
				case ChipRegs.AUD3VOL: ch[3].audvol = value; break;
				case ChipRegs.AUD3LEN: ch[3].audlen = value; break;
				case ChipRegs.AUD3DAT: ch[3].auddat = value; break;
				case ChipRegs.AUD3LCH: ch[3].audlc = (ch[3].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD3LCL: ch[3].audlc = ((ch[3].audlc & 0xffff0000) | value) & ChipRegs.ChipAddressMask; break;

			}
		}

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			switch (address)
			{
				case ChipRegs.AUD0PER: value = ch[0].audper; break;
				case ChipRegs.AUD0VOL: value = ch[0].audvol; break;
				case ChipRegs.AUD0LEN: value = ch[0].audlen; break;
				case ChipRegs.AUD0DAT: value = ch[0].auddat; break;
				case ChipRegs.AUD0LCH: value = (ushort)(ch[0].audlc>>16); break;
				case ChipRegs.AUD0LCL: value = (ushort)ch[0].audlc; break;
				
				case ChipRegs.AUD1PER: value = ch[1].audper; break;
				case ChipRegs.AUD1VOL: value = ch[1].audvol; break;
				case ChipRegs.AUD1LEN: value = ch[1].audlen; break;
				case ChipRegs.AUD1DAT: value = ch[1].auddat; break;
				case ChipRegs.AUD1LCH: value = (ushort)(ch[1].audlc >> 16); break;
				case ChipRegs.AUD1LCL: value = (ushort)ch[1].audlc; break;

				case ChipRegs.AUD2PER: value = ch[2].audper; break;
				case ChipRegs.AUD2VOL: value = ch[2].audvol; break;
				case ChipRegs.AUD2LEN: value = ch[2].audlen; break;
				case ChipRegs.AUD2DAT: value = ch[2].auddat; break;
				case ChipRegs.AUD2LCH: value = (ushort)(ch[2].audlc >> 16); break;
				case ChipRegs.AUD2LCL: value = (ushort)ch[2].audlc; break;

				case ChipRegs.AUD3PER: value = ch[3].audper; break;
				case ChipRegs.AUD3VOL: value = ch[3].audvol; break;
				case ChipRegs.AUD3LEN: value = ch[3].audlen; break;
				case ChipRegs.AUD3DAT: value = ch[3].auddat; break;
				case ChipRegs.AUD3LCH: value = (ushort)(ch[3].audlc >> 16); break;
				case ChipRegs.AUD3LCL: value = (ushort)ch[3].audlc; break;
			}
			return value;
		}
	}
}