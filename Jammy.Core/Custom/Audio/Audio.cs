using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.Audio
{
	public class Audio : IAudio
	{
		protected readonly IChipsetClock clock;
		protected readonly IContendedMemoryMappedDevice memory;
		protected readonly IInterrupt interrupt;
		protected readonly EmulationSettings settings;
		protected readonly ILogger logger;
		protected readonly uint[] intr = { Types.Interrupt.AUD0, Types.Interrupt.AUD1, Types.Interrupt.AUD2, Types.Interrupt.AUD3 };
		protected readonly ushort[] chanbit = { (ushort)DMA.AUD0EN, (ushort)DMA.AUD1EN, (ushort)DMA.AUD2EN, (ushort)DMA.AUD3EN };
		protected readonly AudioChannel[] ch = new AudioChannel[4] { new AudioChannel(), new AudioChannel(), new AudioChannel(), new AudioChannel()};

		private const double LOW_PASS_FILTER_FREQUENCY = 3275.0;   // LED filter A500/A1200
		private const double A500_FIXED_FILTER_FREQUENCY = 4900.0; // Hardware filter A500
		private const double A1200_FIXED_FILTER_FREQUENCY = 28867.0; // Hardware filter A1200

		protected const int SAMPLE_RATE = 31200;
		protected const int SAMPLE_SIZE = 2;//1 for 8bit, 2 for 16bit

		public Audio(IChipsetClock clock, IChipRAM memory, IInterrupt interrupt, IOptions<EmulationSettings> settings, ILogger<Audio> logger)
		{
			this.clock = clock;
			this.memory = (IContendedMemoryMappedDevice)memory;
			this.interrupt = interrupt;
			this.settings = settings.Value;
			this.logger = logger;

			InitLowPassFilter();
		}

		//audio frequency is CPUHz (7.14MHz) / 200, 35.7KHz

		//HRM p141
		//NTSC 2 samples/ line * 262.5 lines/frame * 59.94 frames/ second= 31,469 samples/ sec
		//PAL  2 samples/ line * 312 lines/frame * 50 frames/ second= 31,200 samples/ sec
		//hardware says it's designed to do a max of 28867

		//Thinking out loud:
		//The audio hardware can DMA 1 word per channel (2 8bit samples) per scanline
		//On PAL  there are 312 scanlines @ 50Hz, so the rate is 2*50*312Hz = 31.200KHz max
		//On NTSC there are 262 scanlines @ 60Hz, so the rate is 2*60*262Hz = 31.440KHz max

		//audio frequency is CPUHz (7.14MHz) / 200, 35.7KHz
		public void Emulate()
		{
			for (int i = 0; i < 4; i++)
			{
				if (ch[i].mode == AudioMode.DMA) PlayingDMA(i);
				else if (ch[i].mode == AudioMode.Interrupt) PlayingIRQ(i);
			}

			//sample audper for hardware mix
			//this would be 2 samples per line, 312 lines, 50 Hz = 31200Hz
			if (clock.HorizontalPos == 0 || clock.HorizontalPos == 113)
				AudioMix(ch);
		}

		private const int rate = 1;

		protected void Fetch(int channel)
		{
			//All DMA is off
			if ((dmacon & (int)DMA.DMAEN) == 0)
				return;

			//channel DMA is off
			if ((dmacon & chanbit[channel]) == 0)
				return;

			//read the sample into live audXdat
			ch[channel].auddat = (ushort)memory.ImmediateRead(0, ch[channel].working_audlc, Size.Word);
			ch[channel].working_audlc += 2;
		}

		protected void PlayingDMA(int channel)
		{
			ch[channel].working_audper -= rate;
			if (ch[channel].working_audper <= 0)
			{
				if ((adkcon & (0x11 << channel)) == 0)
				{ 
					if (!ch[channel].secondByte)
					{
						ch[channel].auddat <<= 8;
						ch[channel].secondByte = true;
						ch[channel].working_audper += ch[channel].audper;
						return;
					}
					else
					{
						Fetch(channel);
						ch[channel].secondByte = false;
					}
				}
				else
				{
					ch[channel].secondByte = false;
					Fetch(channel);
				}

				//update the pointers and reset the period

				ch[channel].working_audlen--;
				ch[channel].working_audper += ch[channel].audper;

				//is this channel modulating the next?
				if ((adkcon & (0x11 << channel)) != 0)
				{
					if (channel != 3)
					{
						if (ch[channel].modulating_vp)
							ch[channel + 1].working_audper = ch[channel].auddat;
						else
							ch[channel + 1].audvol = MapAudvol(ch[channel].auddat);
					}
					ch[channel].modulating_vp ^= ch[channel].modulate_toggle;
					ch[channel].auddat = 0;
				}

				//loop restart?
				if (ch[channel].working_audlen <= 0)
				{
					ch[channel].working_audlc = ch[channel].audlc;
					ch[channel].working_audlen = ch[channel].audlen;

					interrupt.AssertInterrupt(intr[channel]);
				}

				//DMA has been turned off, what's the right thing to do now?
				if ((dmacon & chanbit[channel]) == 0)
				{
					//todo: unsure asserting the interrupt is the right thing to do
					//but there are games that do
					//interrupts off, clear channel interrupt
					//channel period = 1, channel volume = 0
					//channel DMA off
					//wait for channel IRQ
					ch[channel].mode = AudioMode.Idle;
					interrupt.AssertInterrupt(intr[channel]);
				}
			}
		}

		protected void PlayingIRQ(int channel)
		{
			int audper = ch[channel].working_audper;
			audper -= rate;
			ch[channel].working_audper -= (ushort)rate;
			if (audper < 0)
			{
				//play the 2 bytes in audXdat, until we get here and the IRQ remains unacknowledged when period is out
				if ((intreq & (1 << (int)intr[channel])) != 0)
				{
					ch[channel].mode = AudioMode.Idle;
				}
				else
				{
					ch[channel].working_audper = ch[channel].audper;
					interrupt.AssertInterrupt(intr[channel]);
				}
			}
		}

		private void ChannelDMAOn(int channel)
		{
			ch[channel].working_audper = ch[channel].audper;
			ch[channel].working_audlen = ch[channel].audlen;
			ch[channel].working_audlc = ch[channel].audlc;

			ch[channel].mode = AudioMode.DMA;
			interrupt.AssertInterrupt(intr[channel]);
		}

		private void ChannelIRQOn(int channel)
		{
			ch[channel].mode = AudioMode.Interrupt;
			interrupt.AssertInterrupt(intr[channel]);
		}
		
		public void Reset()
		{
			for (int i = 0; i < 4; i++)
				ch[i].Clear();

			adkcon = 0;
			dmacon = 0;
			intreq = 0;
			intena = 0;

			lastMod = 0;
		}

		public enum AudioMode
		{
			Idle,
			DMA,
			Interrupt
		}

		public class AudioChannel : IFilter
		{
			public ushort audper { get; set; }
			public ushort audvol { get; set; }
			public ushort audlen { get; set; }
			public ushort auddat { get; set; }
			public uint audlc { get; set; }

			public int working_audlen { get;set; }
			public int working_audper { get; set; }
			public uint working_audlc { get; set; }

			public AudioMode mode { get; set; }

			public bool secondByte { get; set; }

			public bool modulating_vp { get; set; }//false = modulating volume, true = modulating period
			public bool modulate_toggle { get; set; }//false = modulate only volume or period, true = modulate both

			public byte[] audioBytes { get; set; }
			public int audioBytesIndex { get; set; }
			public FilterChannel filter { get; } = new FilterChannel();

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
				working_audper = 0;
				working_audlc = 0;
				working_audlen = 0;
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
				if ((dmaconchanges & dmacon & chanbit[i]) != 0)
					ChannelDMAOn(i);
			}
		}

		private ushort adkcon = 0;

		private ushort lastMod = 0;
		private void WriteADKCON(ushort v)
		{
			if ((v & 0x8000) != 0)
				adkcon |= (ushort)v;
			else
				adkcon &= (ushort)~v;

			v = (ushort)(adkcon & 0xff);

			SetModulation(0, v & 0x11);
			SetModulation(1, (v & 0x22) >> 1);
			SetModulation(2, (v & 0x44) >> 2);
			SetModulation(3, (v & 0x88) >> 3);

			if (v != lastMod)
			{
				if ((v & 1) != 0) logger.LogTrace("C0 modulates volume");
				if ((v & 2) != 0) logger.LogTrace("C1 modulates volume");
				if ((v & 4) != 0) logger.LogTrace("C2 modulates volume");
				if ((v & 8) != 0) logger.LogTrace("C3 modulates volume");
				v >>= 4;
				if ((v & 1) != 0) logger.LogTrace("C0 modulates frequency");
				if ((v & 2) != 0) logger.LogTrace("C1 modulates frequency");
				if ((v & 4) != 0) logger.LogTrace("C2 modulates frequency");
				if ((v & 8) != 0) logger.LogTrace("C3 modulates frequency");

				if (v == 0) logger.LogTrace("No modulation");
				lastMod = v;
			}
		}

		private void SetModulation(int channel, int mask)
		{
			switch (mask)
			{
				case 0x00: break;
				case 0x01: ch[channel].modulating_vp = false; ch[channel].modulate_toggle = false; break;
				case 0x10: ch[channel].modulating_vp = true;  ch[channel].modulate_toggle = false; break;
				case 0x11: ch[channel].modulating_vp = false; ch[channel].modulate_toggle = true; break;
			}
		}

		protected void AudioMix(IFilter[] channels)
		{ 
			//always mix in the audio, whether it's fetching from DMA or audXdat is being battered by the CPU
			for (int i = 0; i< 4; i++)
			{
				if (channels[i].audioBytes.Length <= channels[i].audioBytesIndex) continue;

				ushort volume = (ushort)((ch[i].audvol & (1 << 6)) != 0 ? 64 : (ch[i].audvol & 0x3f));

				//Amiga samples are two 8-bit signed values packed into a word, range -128 to +127
				short s0 = (sbyte)(ch[i].auddat >> 8);
				//short s1 = (sbyte)ch[i].auddat;

				if (SAMPLE_SIZE == 1)
				{
#pragma warning disable CS0162 // Unreachable code detected
					//(-128 * 64)>>6 = -128
					//(+127 * 64)>>6 = +127;
					s0 = (short) ((s0* volume) >> 6);
					//s1 = (short) ((s1* volume) >> 6);
					//8-bit PCM is unsigned
					channels[i].audioBytes[channels[i].audioBytesIndex++] = (byte) (s0 + 128);
					//channels[i].audioBytes[channels[i].audioBytesIndex++] = (byte) (s1 + 128);
#pragma warning restore CS0162 // Unreachable code detected
				}
				else
				{
					//(-128 * 64)<<2 = -32768
					//(+127 * 64)<<2 = +32767;
					s0 = (short) ((s0* volume) << 2); s0 |= (short) ((s0 >> 14) & 3);
					//s1 = (short) ((s1* volume) << 2); s1 |= (short) ((s1 >> 14) & 3);

					//16-bit PCM is signed
					channels[i].audioBytes[channels[i].audioBytesIndex++] = (byte) s0;
					channels[i].audioBytes[channels[i].audioBytesIndex++] = (byte) (s0>>8);
					//channels[i].audioBytes[channels[i].audioBytesIndex++] = (byte) s1;
					//channels[i].audioBytes[channels[i].audioBytesIndex++] = (byte) (s1>>8);
				}
			}
		}

		private ushort intreq = 0;

		public void WriteINTREQ(ushort v)
		{
			//ushort lastintreq = intreq;
			intreq = v;

			//var intreqchanges = (ushort)(intreq ^ lastintreq);
			//if ((intreqchanges & (1 << (int)Interrupt.AUD3)) != 0)
			//	logger.LogTrace($"IRQ3");
		}

		private ushort intena = 0;

		public void WriteINTENA(ushort v)
		{
			//ushort lastintena = intena;
			intena = v;

			//var intenachanges = (ushort)(intena ^ lastintena);
			//if ((intenachanges & (1 << (int)Interrupt.AUD3)) != 0 && (intena & (1 << (int)Interrupt.AUD3)) != 0)
			//	logger.LogTrace($"E3");
			//if ((intenachanges & (1 << (int)Interrupt.AUD3)) != 0 && (intena & (1 << (int)Interrupt.AUD3)) == 0)
			//	logger.LogTrace($"D3");
		}

		private ushort MapAudvol(ushort audvol)
		{
			//mask with 7f
			//if bit 6 is set, 0x40 else return the value
			audvol &= 0x7f;
			if ((audvol & 0x40) != 0) return 0x40;
			return audvol;
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.AUD0PER: ch[0].audper = value; break;
				case ChipRegs.AUD0VOL: ch[0].audvol = MapAudvol(value); break;
				case ChipRegs.AUD0LEN: ch[0].audlen = value; break;
				case ChipRegs.AUD0DAT: ch[0].auddat = value; ChannelIRQOn(0); break;
				case ChipRegs.AUD0LCH: ch[0].audlc = (ch[0].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD0LCL: ch[0].audlc = ((ch[0].audlc & 0xffff0000) | (uint)(value & 0xfffe)); break;

				case ChipRegs.AUD1PER: ch[1].audper = value; break;
				case ChipRegs.AUD1VOL: ch[1].audvol = MapAudvol(value); break;
				case ChipRegs.AUD1LEN: ch[1].audlen = value; break;
				case ChipRegs.AUD1DAT: ch[1].auddat = value; ChannelIRQOn(1); break;
				case ChipRegs.AUD1LCH: ch[1].audlc = (ch[1].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD1LCL: ch[1].audlc = ((ch[1].audlc & 0xffff0000) | (uint)(value & 0xfffe)); break;

				case ChipRegs.AUD2PER: ch[2].audper = value; break;
				case ChipRegs.AUD2VOL: ch[2].audvol = MapAudvol(value); break;
				case ChipRegs.AUD2LEN: ch[2].audlen = value; break;
				case ChipRegs.AUD2DAT: ch[2].auddat = value; ChannelIRQOn(2); break;
				case ChipRegs.AUD2LCH: ch[2].audlc = (ch[2].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD2LCL: ch[2].audlc = ((ch[2].audlc & 0xffff0000) | (uint)(value & 0xfffe)); break;

				case ChipRegs.AUD3PER: ch[3].audper = value; break;
				case ChipRegs.AUD3VOL: ch[3].audvol = MapAudvol(value); break;
				case ChipRegs.AUD3LEN: ch[3].audlen = value; break;
				case ChipRegs.AUD3DAT: ch[3].auddat = value; ChannelIRQOn(3); break;
				case ChipRegs.AUD3LCH: ch[3].audlc = (ch[3].audlc & 0x0000ffff) | ((uint)value << 16); break;
				case ChipRegs.AUD3LCL: ch[3].audlc = ((ch[3].audlc & 0xffff0000) | (uint)(value & 0xfffe)); break;

				case ChipRegs.ADKCON: WriteADKCON(value); break;
			}
			//DumpDiff();
		}

		public ushort Read(uint insaddr, uint address)
		{
			ushort value = 0;
			switch (address)
			{
				case ChipRegs.ADKCONR: value = (ushort)(adkcon & 0x00ff); break;
			}
			return value;
		}

		public uint DebugChipsetRead(uint address, Size size)
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

				case ChipRegs.ADKCONR: value = (ushort)(adkcon & 0x00ff); break;
			}
			return value;
		}

		private AudioChannel[] dc = new AudioChannel[4] { new AudioChannel(), new AudioChannel(), new AudioChannel(), new AudioChannel() };

		private void DumpDiff()
		{
			for (int i = 0; i < 4; i++)
			{
				if (dc[i].audper != ch[i].audper) logger.LogTrace($"AUD{i}PER {dc[i].audper}->{ch[i].audper}");
				if (dc[i].audvol != ch[i].audvol) logger.LogTrace($"AUD{i}VOL {dc[i].audvol}->{ch[i].audvol}");
				if (dc[i].audlen != ch[i].audlen) logger.LogTrace($"AUD{i}LEN {dc[i].audlen}->{ch[i].audlen}");
				if (dc[i].audlc != ch[i].audlc) logger.LogTrace($"AUD{i}LC {dc[i].audlc:X8}->{ch[i].audlc:X8}");
				//if (dc[i].auddat != ch[i].auddat) logger.LogTrace($"AUD{i}DAT {dc[i].auddat}->{ch[i].auddat}");
				ch[i].CopyTo(dc[i]);
			}
		}

		private bool filter = false;
		private bool raw = false;

		private double a1, a2, a3, b1, b2;
		private double a0;

		protected void InitLowPassFilter()
		{
			//low-pass filter A500/A1200
			{
				double sr = SAMPLE_RATE;
				double f = LOW_PASS_FILTER_FREQUENCY;
				double r = Math.Sqrt(2.0);
				double c = 1.0 / Math.Tan(Math.PI * f / sr);

				a1 = 1.0 / (1.0 + r * c + c * c);
				a2 = 2.0 * a1;
				a3 = a1;
				b1 = 2.0 * (1.0 - c * c) * a1;
				b2 = (1.0 - r * c + c * c) * a1;
			}

			//hardware filter A500/1200
			{
				double f = settings.ChipSet == ChipSet.AGA ? A1200_FIXED_FILTER_FREQUENCY : A500_FIXED_FILTER_FREQUENCY;
				double dt = 1.0 / SAMPLE_RATE;
				double rc = 1.0 / (2.0 * Math.PI * f);
				a0 = dt / (rc + dt);
			}
		}

		public class FilterChannel
		{
			//low-pass filter
			public double i1 { get; set; }
			public double i2 { get; set; }
			public double o1 { get; set; }
			public double o2 { get; set; }

			//hardware filter
			public double y1 { get; set; }
		}

		public interface IFilter
		{
			byte[] audioBytes { get; }
			int audioBytesIndex { get; set; }
			FilterChannel filter { get; }
		}

		protected void LowPassFilter(IFilter channel)
		{
			if (raw) return;

			if (SAMPLE_SIZE == 2)
			{
				double o1 = channel.filter.o1;
				double o2 = channel.filter.o2;
				double i1 = channel.filter.i1;
				double i2 = channel.filter.i2;

				for (int s = 0; s < channel.audioBytes.Length; s += 2)
				{
					double i0 = ((short)(channel.audioBytes[s] | (channel.audioBytes[s + 1] << 8))) / 32768.0f;

					//hardware filter
					double output = i0 * a0 + channel.filter.y1 * (1.0 - a0);
					channel.filter.y1 = output;

					//low-pass filter
					if (filter)
					{
						double noutput = a1 * output + a2 * i1 + a3 * i2 - b1 * o1 - b2 * o2;
						o2 = o1;
						o1 = noutput;
						i2 = i1;
						i1 = output;
						output = noutput;
					}

					short v = (short)Math.Clamp(output * 32768.0, -32768.0, 32767.0);
					channel.audioBytes[s] = (byte)v;
					channel.audioBytes[s + 1] = (byte)(v >> 8);
				}

				channel.filter.o1 = o1;
				channel.filter.o2 = o2;
				channel.filter.i1 = i1;
				channel.filter.i2 = i2;
			}
		}

		public void Save(JArray obj)
		{
		}

		public void Load(JObject obj)
		{
		}

	}
}