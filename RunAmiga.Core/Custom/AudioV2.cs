using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;
using SharpDX;
using SharpDX.Multimedia;
using SharpDX.XAudio2;

namespace RunAmiga.Core.Custom
{
	public class AudioV2 : IAudio
	{
		private readonly IMemory memory;
		private readonly IChips custom;
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;
		private readonly uint[] intr = { Interrupt.AUD0, Interrupt.AUD1, Interrupt.AUD2, Interrupt.AUD3 };
		private readonly AudioChannel[] ch = new AudioChannel[4];
		private readonly AudioChannel[] shadowch = new AudioChannel[4];

		[Flags]
		public enum XAUDIO2_LOG
		{
			ERRORS = 0x0001, // For handled errors with serious effects.
			WARNINGS = 0x0002, // For handled errors that may be recoverable.
			INFO = 0x0004, // Informational chit-chat (e.g. state changes).
			DETAIL = 0x0008, // More detailed chit-chat.
			API_CALLS = 0x0010, // Public API function entries and exits.
			FUNC_CALLS = 0x0020, // Internal function entries and exits.
			TIMING = 0x0040, // Delays detected and other timing data.
			LOCKS = 0x0080, // Usage of critical sections and mutexes.
			MEMORY = 0x0100, // Memory heap usage information.
			STREAMING = 0x1000, // Audio streaming information.
		}
		private XAudio2 xaudio;
		private MasteringVoice masteringVoice;

		private class AmigaChannel
		{
			public SourceVoice xaudioVoice { get; set; }
			public AudioBuffer[] xaudioBuffer { get; set; }
			public byte[] xaudioCBuffer { get; set; }
			public int xaudioCIndex { get; set; }
			public int currentBuffer { get; set; }
		}

		private AmigaChannel[] channels = new[] {new AmigaChannel(), new AmigaChannel(), new AmigaChannel(), new AmigaChannel()};

		private const int SAMPLE_RATE = 31200;
		private const int BUFFER_SIZE = 3120;

		public AudioV2(IMemory memory, IChips custom, IInterrupt interrupt, ILogger<Audio> logger)
		{
			this.memory = memory;
			this.custom = custom;
			this.interrupt = interrupt;
			this.logger = logger;
			for (int i = 0; i < 4; i++)
			{
				ch[i] = new AudioChannel();
				shadowch[i] = new AudioChannel();
			}

			xaudio = new XAudio2();
			masteringVoice = new MasteringVoice(xaudio);

			xaudio.SetDebugConfiguration(new DebugConfiguration
			{
				TraceMask = (int)(XAUDIO2_LOG.ERRORS | XAUDIO2_LOG.WARNINGS | XAUDIO2_LOG.DETAIL | XAUDIO2_LOG.API_CALLS | XAUDIO2_LOG.FUNC_CALLS),
				BreakMask = 0,
				LogThreadID = true,
				LogFileline = true,
				LogFunctionName = true,
				LogTiming = true
			}, IntPtr.Zero);

			for (int i = 0; i < 4; i++)
			{
				channels[i].xaudioVoice = new SourceVoice(xaudio, new WaveFormat(SAMPLE_RATE, 8, 1), VoiceFlags.None);
				channels[i].xaudioVoice.Start();

				channels[i].xaudioBuffer = new AudioBuffer [2];
				channels[i].xaudioBuffer[0] = new AudioBuffer { AudioBytes = BUFFER_SIZE, AudioDataPointer = Utilities.AllocateMemory(BUFFER_SIZE), PlayLength = BUFFER_SIZE };
				channels[i].xaudioBuffer[1] = new AudioBuffer { AudioBytes = BUFFER_SIZE, AudioDataPointer = Utilities.AllocateMemory(BUFFER_SIZE), PlayLength = BUFFER_SIZE };
				channels[i].xaudioCBuffer = new byte[BUFFER_SIZE];
				channels[i].xaudioCIndex = 0;
				channels[i].currentBuffer = 0;
			}
			//1,2 left
			//0,3 right
			//channels[0].xaudioVoice.SetOutputMatrix(1, 1, new float[2] {});
			//channels[1].xaudioVoice.SetOutputMatrix(1, 1,);
			//channels[2].xaudioVoice.SetOutputMatrix(1, 1,);
			//channels[3].xaudioVoice.SetOutputMatrix(1, 1,);
		}

		private ulong audioTime;

		//audio frequency is CPUHz (7.14MHz) / 200, 35.7KHz

		//HRM p141
		//NTSC 2 samples/ line * 262.5 lines/frame * 59.94 frames/ second= 31,469 samples/ sec
		//PAL  2 samples/ line * 312 lines/frame * 50 frames/ second= 31,200 samples/ sec
		//hardware says it's designed to do a max of 28867

		//Thinking out loud:
		//The audio hardware can DMA 1 word per channel (2 8bit samples) per scanline
		//On PAL  there are 312 scanlines @ 50Hz, so the rate is 2*50*312Hz = 31.200KHz max
		//On NTSC there are 262 scanlines @ 60Hz, so the rate is 2*60*262Hz = 31.440KHz max

		private ushort lastdmacon = 0;

		public void Emulate(ulong cycles)
		{
			audioTime += cycles;

			if (audioTime > 140_000 / 312)
			{
				audioTime -= 140_000 / 312;

				ushort dmacon = (ushort)custom.Read(0, ChipRegs.DMACONR, Size.Word);
				ushort adkcon = (ushort)custom.Read(0, ChipRegs.ADKCONR, Size.Word);

				var dmaconchanges = (ushort)(dmacon ^ lastdmacon);
				lastdmacon = dmacon;

				ChannelToggle(0, (dmaconchanges & dmacon & (uint)ChipRegs.DMA.AUD0EN) != 0);
				ChannelToggle(1, (dmaconchanges & dmacon & (uint)ChipRegs.DMA.AUD1EN) != 0);
				ChannelToggle(2, (dmaconchanges & dmacon & (uint)ChipRegs.DMA.AUD2EN) != 0);
				ChannelToggle(3, (dmaconchanges & dmacon & (uint)ChipRegs.DMA.AUD3EN) != 0);

				if ((dmacon & (int)ChipRegs.DMA.DMAEN) != 0)
				{
					if ((dmacon & (uint)ChipRegs.DMA.AUD0EN) != 0) Playing(0);
					if ((dmacon & (uint)ChipRegs.DMA.AUD1EN) != 0) Playing(1);
					if ((dmacon & (uint)ChipRegs.DMA.AUD2EN) != 0) Playing(2);
					if ((dmacon & (uint)ChipRegs.DMA.AUD3EN) != 0) Playing(3);
				}

				//always mix in the audio, whether it's fetching from DMA or audXdat is being battered by the CPU
				for (int i = 0; i < 4; i++)
				{
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)((sbyte)(ch[i].auddat >> 8)+128);
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)((sbyte)ch[i].auddat+128);

					if (channels[i].xaudioCIndex == channels[i].xaudioCBuffer.Length)
					{
						var state = channels[i].xaudioVoice.State;
						logger.LogTrace($"{i} Q:{state.BuffersQueued} S;{state.SamplesPlayed}");
						if (state.BuffersQueued >= 2)
						{
							do
							{
								Thread.Yield();
							} while (channels[i].xaudioVoice.State.BuffersQueued >= 2);
						}

						Marshal.Copy(channels[i].xaudioCBuffer, 0, channels[i].xaudioBuffer[channels[i].currentBuffer].AudioDataPointer, channels[i].xaudioCBuffer.Length);
						channels[i].xaudioCIndex = 0;
						channels[i].xaudioVoice.SubmitSourceBuffer(channels[i].xaudioBuffer[channels[i].currentBuffer], null);
						channels[i].xaudioVoice.Start();
						channels[i].currentBuffer ^= 1;
					}

					channels[i].xaudioVoice.SetVolume(ch[i].audvol / 64.0f);
				}
			}
		}

		private int rate = 124;

		private void Playing(int channel)
		{

			//todo: what happens if CPU is bashing audper after sample is started?
			int audper = shadowch[channel].audpercurr;
			audper -= rate;
			shadowch[channel].audpercurr -= (ushort)rate;
			if (audper < 0)
			{
				//read the sample into live audXdat
				ch[channel].auddat = memory.Read16(shadowch[channel].audlc);

				shadowch[channel].audlc += 2;
				shadowch[channel].audlen--;

				//loop restart?
				if (shadowch[channel].audlen == 0)
				{
					ChannelToggle(channel, true);
					interrupt.AssertInterrupt(intr[channel]);
				}

				//say audper is -100, we want the new audper to be the reset value of audper + 100

				audper = -audper;//+100
				shadowch[channel].audpercurr = (ushort)(shadowch[channel].audper + (ushort)audper);
				shadowch[channel].audpercurr = shadowch[channel].audper;
			}
		}

		private void ChannelToggle(int channel, bool onOff)
		{
			if (onOff)
			{
				Dump($"AUD{channel} ON", channel);

				ch[channel].CopyTo(shadowch[channel]);
			}
			//else
			//{
			//	logger.LogTrace($"AUD{channel} OFF");
			//}
		}

		private void Dump(string msg, int channel)
		{
			var cp = ch[channel];
			logger.LogTrace($"{msg} {cp.audlc:X6} L:{cp.audlen} V:{cp.audvol} P:{cp.audper}");
		}

		public void Reset()
		{
			for (int i = 0; i < 4; i++)
				ch[i].Clear();
		}

		public class AudioChannel
		{
			public ushort audper { get; set; }
			public ushort audpercurr { get; set; }

			public ushort audvol { get; set; }
			public ushort audlen { get; set; }
			public ushort auddat { get; set; }
			public uint audlc { get; set; }

			public void CopyTo(AudioChannel cp)
			{
				cp.audper = this.audper;
				cp.audpercurr = 0;
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
				case ChipRegs.AUD0LCL: ch[0].audlc = ((ch[0].audlc & 0xffff0000) | value) & ChipRegs.ChipAddressMask; break;

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