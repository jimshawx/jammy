using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using Vortice.Multimedia;
using Vortice.XAudio2;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Audio.Windows
{
	public class AudioVortice : IAudio
	{
		private readonly IChipsetClock clock;
		private readonly IMemoryMappedDevice memory;
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;
		private readonly uint[] intr = { Interrupt.AUD0, Interrupt.AUD1, Interrupt.AUD2, Interrupt.AUD3 };
		private readonly ushort[] chanbit = { (ushort)DMA.AUD0EN, (ushort)DMA.AUD1EN, (ushort)DMA.AUD2EN, (ushort)DMA.AUD3EN };
		private readonly AudioChannel[] ch = new AudioChannel[4] { new AudioChannel(), new AudioChannel(), new AudioChannel(), new AudioChannel()};

		public AudioVortice(IChipsetClock clock, IChipRAM memory, IInterrupt interrupt, IOptions<EmulationSettings> settings, ILogger<AudioVortice> logger)
		{
			this.clock = clock;
			this.memory = memory;
			this.interrupt = interrupt;
			this.logger = logger;

			InitMixer();
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
			if ((clock.ClockState & ChipsetClockState.EndOfLine) != 0)
			{
				for (int i = 0; i < 4; i++)
				{
					if (ch[i].mode == AudioMode.DMA) PlayingDMA(i);
					else if (ch[i].mode == AudioMode.Interrupt) PlayingIRQ(i);
				}

				AudioMix();
			}
		}

		private int rate = 100;

		private void PlayingDMA(int channel)
		{
			//All DMA is off
			if ((dmacon & (int)DMA.DMAEN) == 0)
				return;

			ch[channel].working_audper -= rate;
			if (ch[channel].working_audper <= 0)
			{
				//read the sample into live audXdat
				ch[channel].auddat = (ushort)memory.Read(0, ch[channel].working_audlc, Size.Word);
				//update the pointers and reset the period
				ch[channel].working_audlc += 2;
				ch[channel].working_audlen--;
				ch[channel].working_audper += ch[channel].audper;

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

		private void PlayingIRQ(int channel)
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

		public class AudioChannel
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

			v = (ushort)(adkcon&0xff);
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
		private IXAudio2 xaudio;
		private IXAudio2MasteringVoice masteringVoice;

		private class AmigaChannel
		{
			public IXAudio2SourceVoice xaudioVoice { get; set; }
			public AudioBuffer[] xaudioBuffer { get; set; }
			public byte[] xaudioCBuffer { get; set; }
			public int xaudioCIndex { get; set; }
			public int currentBuffer { get; set; }
		}

		private readonly AmigaChannel[] channels = new[] { new AmigaChannel(), new AmigaChannel(), new AmigaChannel(), new AmigaChannel() };

		private const int SAMPLE_RATE = 31200;
		private const int SAMPLE_SIZE = 2;//1 for 8bit, 2 for 16bit
		private const int BUFFER_SIZE = 3120*SAMPLE_SIZE;

		private void InitMixer()
		{
			xaudio = XAudio2.XAudio2Create(ProcessorSpecifier.DefaultProcessor);
			masteringVoice = xaudio.CreateMasteringVoice();
			var masteringChannelDetails = masteringVoice.VoiceDetails;

			xaudio.SetDebugConfiguration(new DebugConfiguration
			{
				TraceMask = LogType.Errors | LogType.Warnings | LogType.Detail | LogType.ApiCalls | LogType.FuncCalls,
				BreakMask = 0,
				LogThreadID = true,
				LogFileline = true,
				LogFunctionName = true,
				LogTiming = true
			});

			for (int i = 0; i < 4; i++)
			{
				channels[i].xaudioVoice = xaudio.CreateSourceVoice(new WaveFormat(SAMPLE_RATE, 8*SAMPLE_SIZE, 1), VoiceFlags.None);
				channels[i].xaudioVoice.Start();

				channels[i].xaudioBuffer = new AudioBuffer[2];
				channels[i].xaudioBuffer[0] = new AudioBuffer {AudioBytes = BUFFER_SIZE, AudioDataPointer = SharpDX.Utilities.AllocateMemory(BUFFER_SIZE), PlayLength = BUFFER_SIZE/SAMPLE_SIZE};
				channels[i].xaudioBuffer[1] = new AudioBuffer {AudioBytes = BUFFER_SIZE, AudioDataPointer = SharpDX.Utilities.AllocateMemory(BUFFER_SIZE), PlayLength = BUFFER_SIZE/SAMPLE_SIZE};
				channels[i].xaudioCBuffer = new byte[BUFFER_SIZE];
				channels[i].xaudioCIndex = 0;
				channels[i].currentBuffer = 0;

				//panning 1,2 left   0,3 right
				var channelDetails = channels[i].xaudioVoice.VoiceDetails;
				float[] outputMatrix = new float[channelDetails.InputChannels * masteringChannelDetails.InputChannels];

				//if (i == 0 || i == 3) { outputMatrix[0] = 0.0f; outputMatrix[1] = 1.0f; }//hard right
				//else { outputMatrix[0] = 1.0f; outputMatrix[1] = 0.0f; }//hard left
				if (i == 0 || i == 3) { outputMatrix[0] = 0.2f; outputMatrix[1] = 0.8f; }//soft right
				else { outputMatrix[0] = 0.8f; outputMatrix[1] = 0.2f; }//soft left

				channels[i].xaudioVoice.SetOutputMatrix(null, channelDetails.InputChannels, masteringChannelDetails.InputChannels, outputMatrix);
			}
		}

		private void AudioMix()
		{
			//always mix in the audio, whether it's fetching from DMA or audXdat is being battered by the CPU
			for (int i = 0; i < 4; i++)
			{
				ushort volume = (ushort)((ch[i].audvol & (1 << 6)) != 0 ? 64 : (ch[i].audvol & 0x3f));

				//Amiga samples are two 8-bit signed values packed into a word, range -128 to +127
				short s0 = (sbyte)(ch[i].auddat >> 8);
				short s1 = (sbyte)ch[i].auddat;

				if (SAMPLE_SIZE == 1)
				{
					//(-128 * 64)>>6 = -128
					//(+127 * 64)>>6 = +127;
					s0 = (short)((s0 * volume) >> 6);
					s1 = (short)((s1 * volume) >> 6);
					//8-bit PCM is unsigned
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)(s0 + 128);
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)(s1 + 128);
				}
				else
				{
					//(-128 * 64)<<2 = -32768
					//(+127 * 64)<<2 = +32767;
					s0 = (short)((s0 * volume) << 2); s0 |= (short)((s0 >> 14) & 3);
					s1 = (short)((s1 * volume) << 2); s1 |= (short)((s1 >> 14) & 3);

					//16-bit PCM is signed
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)s0;
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)(s0>>8);
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)s1;
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)(s1>>8);
				}

				if (channels[i].xaudioCIndex == channels[i].xaudioCBuffer.Length)
				{
					var state = channels[i].xaudioVoice.State;
					if (state.BuffersQueued >= 2)
					{
						do
						{
							Thread.Yield();
						} while (channels[i].xaudioVoice.State.BuffersQueued >= 2);
					}

					//LowPassFilter(i);

					Marshal.Copy(channels[i].xaudioCBuffer, 0, channels[i].xaudioBuffer[channels[i].currentBuffer].AudioDataPointer, channels[i].xaudioCBuffer.Length);
					channels[i].xaudioCIndex = 0;
					channels[i].xaudioVoice.SubmitSourceBuffer(channels[i].xaudioBuffer[channels[i].currentBuffer], null);
					channels[i].xaudioVoice.Start();
					channels[i].currentBuffer ^= 1;
				}
			}
		}

		private void LowPassFilter(int i)
		{
			if (SAMPLE_SIZE == 2)
			{
				double o2, o1;
				double i0, i1, i2;

				o2 = o1 = 0.0;
				i2 = i1 = 0.0;

				//double sr = 3546895.0;
				double sr = SAMPLE_RATE;
				double r = Math.Sqrt(2.0);
				//double f = 3275.0;
				double f = 32000.0;
				double c = 1.0 / Math.Tan(Math.PI * f / sr);
				double a1, a2, a3;
				double b1, b2;

				a1 = 1.0 / (1.0 + r * c + c * c);
				a2 = 2.0 * a1;
				a3 = a1;
				b1 = 2.0 * (1.0 - c * c) * a1;
				b2 = (1.0 - r * c + c * c) * a1;

				double[] outputs = new double[channels[i].xaudioCBuffer.Length / 2];
				for (int s = 0; s < channels[i].xaudioCBuffer.Length; s += 2)
				{
					i0 = (channels[i].xaudioCBuffer[s] + (channels[i].xaudioCBuffer[s+1] << 8)) / 32768.0f;

					outputs[s / 2] = a1 * i0 + a2 * i1 + a3 * i2 - b1 * o1 - b2 * o2;
					o2 = o1;
					i2 = i1;
					i1 = i0;
				}

				for (int s = 0; s < outputs.Length; s++)
				{
					short v = (short)Math.Clamp(outputs[s]*32768.0, -32768.0, 32767.0);
					channels[i].xaudioCBuffer[s * 2] = (byte)v;
					channels[i].xaudioCBuffer[s * 2+1] = (byte)(v>>8);
				}
			}
		}
	}
}