using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Runtime.InteropServices;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Audio.Linux
{
	public class AudioALSA : Custom.Audio.Audio, IAudio
	{
		private const int SND_PCM_STREAM_PLAYBACK = 0;
		private const int SND_PCM_ACCESS_RW_INTERLEAVED = 3;
		private const int SND_PCM_FORMAT_S16_LE = 2;
		private const int EPIPE = -32;

		[DllImport("asound", CharSet = CharSet.Ansi)]
		private static extern int snd_pcm_open(out IntPtr handle, string name, int stream, int mode);

		[DllImport("asound")]
		private static extern int snd_pcm_set_params(IntPtr handle,
			int format, int access, int channels, int rate,
			int soft_resample, int latency);

		[DllImport("asound")]
		private static extern int snd_pcm_writei(IntPtr handle, byte[] buffer, int size);

		[DllImport("asound")]
		private static extern int snd_pcm_close(IntPtr handle);

		[DllImport("asound")]
		public static extern int snd_pcm_prepare(IntPtr pcm);

		[DllImport("asound")]
		public static extern int snd_pcm_drop(IntPtr pcm);

		[DllImport("asound")]
		public static extern int snd_pcm_start(IntPtr pcm);

		public AudioALSA(IChipsetClock clock, IChipRAM memory, IInterrupt interrupt, IOptions<EmulationSettings> settings, ILogger<AudioALSA> logger):
			base(clock, memory, interrupt, settings, logger)
		{
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
		public new void Emulate()
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

		private class AmigaChannel :IFilter
		{
			public byte[] audioBytes { get; set; }
			public int xaudioCIndex { get; set; }
			public FilterChannel filter { get; } = new FilterChannel();
		}

		private readonly AmigaChannel[] channels = new[] { new AmigaChannel(), new AmigaChannel(), new AmigaChannel(), new AmigaChannel() };

		private const int BUFFER_SIZE = 3120*SAMPLE_SIZE;
		
		private IntPtr pcmHandle;
		private byte[] mixBuffer;

		private void InitMixer()
		{
			int err = snd_pcm_open(out pcmHandle, "default", SND_PCM_STREAM_PLAYBACK, 0);

			err = snd_pcm_set_params(pcmHandle,
				SND_PCM_FORMAT_S16_LE,
				SND_PCM_ACCESS_RW_INTERLEAVED,
				2,        // channels
				31200,    // sample rate
				1,        // allow resampling
				500000);  // latency in microseconds

			for (int i = 0; i < 4; i++)
			{
				channels[i].audioBytes = new byte[BUFFER_SIZE];
				channels[i].xaudioCIndex = 0;
			}
			mixBuffer = new byte[BUFFER_SIZE * 2];
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
#pragma warning disable CS0162 // Unreachable code detected
					//(-128 * 64)>>6 = -128
					//(+127 * 64)>>6 = +127;
					s0 = (short)((s0 * volume) >> 6);
					s1 = (short)((s1 * volume) >> 6);
					//8-bit PCM is unsigned
					channels[i].audioBytes[channels[i].xaudioCIndex++] = (byte)(s0 + 128);
					channels[i].audioBytes[channels[i].xaudioCIndex++] = (byte)(s1 + 128);
#pragma warning restore CS0162 // Unreachable code detected
				}
				else
				{
					//(-128 * 64)<<2 = -32768
					//(+127 * 64)<<2 = +32767;
					s0 = (short)((s0 * volume) << 2); s0 |= (short)((s0 >> 14) & 3);
					s1 = (short)((s1 * volume) << 2); s1 |= (short)((s1 >> 14) & 3);

					//16-bit PCM is signed
					channels[i].audioBytes[channels[i].xaudioCIndex++] = (byte)s0;
					channels[i].audioBytes[channels[i].xaudioCIndex++] = (byte)(s0>>8);
					channels[i].audioBytes[channels[i].xaudioCIndex++] = (byte)s1;
					channels[i].audioBytes[channels[i].xaudioCIndex++] = (byte)(s1>>8);
				}
			}
			
			//time to mix?
			if (channels[0].xaudioCIndex == channels[0].audioBytes.Length)
			{
				for (int i = 0; i < 4; i++)
					LowPassFilter(channels[i]);

				for (int s = 0; s < channels[0].audioBytes.Length; s += 2)
				{
					int v0 = (int)(short)((ushort)channels[0].audioBytes[s] + (ushort)(channels[0].audioBytes[s + 1] << 8));
					int v1 = (int)(short)((ushort)channels[1].audioBytes[s] + (ushort)(channels[1].audioBytes[s + 1] << 8));
					int v2 = (int)(short)((ushort)channels[2].audioBytes[s] + (ushort)(channels[2].audioBytes[s + 1] << 8));
					int v3 = (int)(short)((ushort)channels[3].audioBytes[s] + (ushort)(channels[3].audioBytes[s + 1] << 8));

					int L = (v0 + v1) >> 1;
					int R = (v2 + v3) >> 1;

					mixBuffer[s * 2 + 0] = (byte)L;
					mixBuffer[s * 2 + 1] = (byte)(L>>8);
					mixBuffer[s * 2 + 2] = (byte)R;
					mixBuffer[s * 2 + 3] = (byte)(R>>8);
				}

				int err = snd_pcm_writei(pcmHandle, mixBuffer, mixBuffer.Length / 4);
				if (err == EPIPE)
				{
					int e0 = snd_pcm_drop(pcmHandle);
					int e1 = snd_pcm_prepare(pcmHandle);
					int e2 = snd_pcm_start(pcmHandle);
					logger.LogTrace($"ALSA audio buffer underrun {e0} {e1} {e2}");
				}
				else if (err < 0)
				{
					logger.LogTrace($"ALSA error {err}");
				}

				for (int i = 0; i < 4; i++)
					channels[i].xaudioCIndex = 0;
			}
		}
	}
}