using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;

namespace RunAmiga.Core.Custom
{
	public class Audio : IAudio
	{
		private readonly IMemory memory;
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;
		private readonly uint[] intr = { Interrupt.AUD0, Interrupt.AUD1, Interrupt.AUD2, Interrupt.AUD3 };
		private readonly ushort[] chanbit = { (ushort)ChipRegs.DMA.AUD0EN, (ushort)ChipRegs.DMA.AUD1EN, (ushort)ChipRegs.DMA.AUD2EN, (ushort)ChipRegs.DMA.AUD3EN };
		private readonly AudioChannel[] ch = new AudioChannel[4];
		private readonly AudioChannel[] shadowch = new AudioChannel[4];

		public Audio(IMemory memory, IInterrupt interrupt, ILogger<Audio> logger)
		{
			this.memory = memory;
			this.interrupt = interrupt;
			this.logger = logger;
			for (int i = 0; i < 4; i++)
			{
				ch[i] = new AudioChannel();
				shadowch[i] = new AudioChannel();
			}
		}

		private ulong audioTime;

		//audio frequency is CPUHz (7.14MHz) / 200, 35.7KHz
		public void Emulate(ulong cycles)
		{
			audioTime += cycles;

			if (audioTime > 140_000 / 312)
			{
				audioTime -= 140_000 / 312;

				for (int i = 0; i < 4; i++)
				{
					if (ch[i].mode == AudioMode.DMA) PlayingDMA(i);
					else if (ch[i].mode == AudioMode.Interrupt) PlayingIRQ(i);
				}
			}
		}

		private int rate = 124;

		private void PlayingDMA(int channel)
		{
			//All DMA is off
			if ((dmacon & (int)ChipRegs.DMA.DMAEN) == 0)
				return;

			int audper = shadowch[channel].audper;
			audper -= rate;
			shadowch[channel].audper -= (ushort)rate;
			if (audper < 0)
			{
				//read the sample into live audXdat
				ch[channel].auddat = memory.Read16(shadowch[channel].audlc);
				shadowch[channel].audlc += 2;
				shadowch[channel].audlen--;

				//DMA has been turned off, what's the right thing to do now?
				if ((dmacon & chanbit[channel]) == 0)
				{
					//todo: unsure asseting the interrupt is the right thing to do
					ch[channel].mode = AudioMode.Idle;
					interrupt.AssertInterrupt(intr[channel]);
					return;
				}

				//loop restart?
				if (shadowch[channel].audlen == 1)
				{
					//ChannelDMAToggle(channel, true);
					shadowch[channel].audlc = ch[channel].audlc;
					shadowch[channel].audlen = ch[channel].audlen;

					interrupt.AssertInterrupt(intr[channel]);
					if (channel == 3)
						logger.LogTrace($"I3x {channel} {ch[channel].mode} {shadowch[channel].audper}");
				}

				//reset the period
				shadowch[channel].audper = ch[channel].audper;

				if (channel == 3)
					logger.LogTrace($"D {channel} {ch[channel].mode} {shadowch[channel].audper}");
			}
		}

		private void PlayingIRQ(int channel)
		{
			int audper = shadowch[channel].audper;
			audper -= rate;
			shadowch[channel].audper -= (ushort)rate;
			if (audper < 0)
			{
				//play the 2 bytes in audXdat, until we get here and the IRQ remains unacknowledged when period is out
				if ((intreq & (1 << (int)intr[channel])) != 0)
				{
					ch[channel].mode = AudioMode.Idle;
				}
				else
				{
					shadowch[channel].audper = ch[channel].audper;
					interrupt.AssertInterrupt(intr[channel]);
				}
				if (channel == 3)
					logger.LogTrace($"I {channel} {ch[channel].mode} {shadowch[channel].audper}");
			}
		}

		private void ChannelDMAOn(int channel)
		{
			ch[channel].CopyTo(shadowch[channel]);
			ch[channel].mode = AudioMode.DMA;
		}

		private void ChannelIRQOn(int channel)
		{
			ch[channel].mode = AudioMode.Interrupt;
			interrupt.AssertInterrupt(intr[channel]);
		}

		private void Dump(string msg, int channel)
		{
			//var cp = ch[channel];
			//logger.LogTrace($"{msg} A {cp.audlc:X6} L:{cp.audlen} V:{cp.audvol} P:{cp.audper}");
			//cp = shadowch[channel];
			//logger.LogTrace($"{msg} S {cp.audlc:X6} L:{cp.audlen} V:{cp.audvol} P:{cp.audper}");
			//ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
			//logger.LogTrace($"{msg} D 0-3:{(dmacon&(ushort)ChipRegs.DMA.AUD0EN)!=0}{(dmacon & (ushort)ChipRegs.DMA.AUD1EN) != 0}{(dmacon & (ushort)ChipRegs.DMA.AUD2EN) != 0}{(dmacon & (ushort)ChipRegs.DMA.AUD3EN) != 0} DMA {(dmacon & (ushort)ChipRegs.DMA.DMAEN) != 0}");
		}

		public void Reset()
		{
			for (int i = 0; i < 4; i++)
				ch[i].Clear();

			adkcon = 0;
			dmacon = 0;
			intreq = 0;
			intena = 0;
		}

		public enum AudioMode
		{
			Idle,
			DMA,
			Interrupt
		}

		public class AudioChannel
		{
			public ushort audper { get; set; }
			public ushort audvol { get; set; }
			public ushort audlen { get; set; }
			public ushort auddat { get; set; }
			public uint audlc { get; set; }

			public AudioMode mode { get; set; }

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
				mode = AudioMode.Idle;
			}
		}

		private ushort dmacon = 0;

		public void WriteDMACON(ushort v)
		{
			ushort lastdmacon = dmacon;
			dmacon = v;
			ushort dmaconchanges = (ushort)(dmacon ^ lastdmacon);

			for (int i = 0; i < 4; i++)
			{
				if ((dmaconchanges & dmacon & (int)chanbit[i]) != 0)
					ChannelDMAOn(i);
			}
		}

		private ushort adkcon = 0;

		public void WriteADKCON(ushort v)
		{
			adkcon = v;
		}

		private ushort intreq = 0;

		public void WriteINTREQ(ushort v)
		{
			ushort lastintreq = intreq;
			intreq = v;

			var intreqchanges = (ushort)(intreq ^ lastintreq);
			if ((intreqchanges & (1 << (int)Interrupt.AUD3)) != 0)
				logger.LogTrace($"IRQ3");
		}

		private ushort intena = 0;

		public void WriteINTENA(ushort v)
		{
			ushort lastintena = intena;
			intena = v;

			var intenachanges = (ushort)(intena ^ lastintena);
			if ((intenachanges & (1 << (int)Interrupt.AUD3)) != 0 && (intena & (1 << (int)Interrupt.AUD3)) != 0)
				logger.LogTrace($"E3");
			if ((intenachanges & (1 << (int)Interrupt.AUD3)) != 0 && (intena & (1 << (int)Interrupt.AUD3)) == 0)
				logger.LogTrace($"D3");
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.AUD0PER: ch[0].audper = value; break;
				case ChipRegs.AUD0VOL: ch[0].audvol = value; break;
				case ChipRegs.AUD0LEN: ch[0].audlen = value; break;
				case ChipRegs.AUD0DAT: ch[0].auddat = value; ChannelIRQOn(0); break;
				case ChipRegs.AUD0LCH: ch[0].audlc = (ch[0].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD0LCL: ch[0].audlc = ((ch[0].audlc & 0xffff0000) | value) & ChipRegs.ChipAddressMask; break;

				case ChipRegs.AUD1PER: ch[1].audper = value; break;
				case ChipRegs.AUD1VOL: ch[1].audvol = value; break;
				case ChipRegs.AUD1LEN: ch[1].audlen = value; break;
				case ChipRegs.AUD1DAT: ch[1].auddat = value; ChannelIRQOn(1); break;
				case ChipRegs.AUD1LCH: ch[1].audlc = (ch[1].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD1LCL: ch[1].audlc = ((ch[1].audlc & 0xffff0000) | value) & ChipRegs.ChipAddressMask; break;

				case ChipRegs.AUD2PER: ch[2].audper = value; break;
				case ChipRegs.AUD2VOL: ch[2].audvol = value; break;
				case ChipRegs.AUD2LEN: ch[2].audlen = value; break;
				case ChipRegs.AUD2DAT: ch[2].auddat = value; ChannelIRQOn(2); break;
				case ChipRegs.AUD2LCH: ch[2].audlc = (ch[2].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD2LCL: ch[2].audlc = ((ch[2].audlc & 0xffff0000) | value) & ChipRegs.ChipAddressMask; break;

				case ChipRegs.AUD3PER: ch[3].audper = value; break;
				case ChipRegs.AUD3VOL: ch[3].audvol = value; break;
				case ChipRegs.AUD3LEN: ch[3].audlen = value; break;
				case ChipRegs.AUD3DAT: ch[3].auddat = value; ChannelIRQOn(3); break;
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
				case ChipRegs.AUD0LCH: value = (ushort)(ch[0].audlc >> 16); break;
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