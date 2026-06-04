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

		private class AmigaChannel
		{
			public byte[] xaudioCBuffer { get; set; }
			public int xaudioCIndex { get; set; }
		}

		private readonly AmigaChannel[] channels = new[] { new AmigaChannel(), new AmigaChannel(), new AmigaChannel(), new AmigaChannel() };

		private const int SAMPLE_RATE = 31200;
		private const int SAMPLE_SIZE = 2;//1 for 8bit, 2 for 16bit
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
				channels[i].xaudioCBuffer = new byte[BUFFER_SIZE];
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
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)(s0 + 128);
					channels[i].xaudioCBuffer[channels[i].xaudioCIndex++] = (byte)(s1 + 128);
#pragma warning restore CS0162 // Unreachable code detected
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
			}
			
			//time to mix?
			if (channels[0].xaudioCIndex == channels[0].xaudioCBuffer.Length)
			{
				//for (int i = 0; i < 4; i++)
				//	LowPassFilter(i);

				for (int s = 0; s < channels[0].xaudioCBuffer.Length; s += 2)
				{
					int v0 = (int)(short)((ushort)channels[0].xaudioCBuffer[s] + (ushort)(channels[0].xaudioCBuffer[s + 1] << 8));
					int v1 = (int)(short)((ushort)channels[1].xaudioCBuffer[s] + (ushort)(channels[1].xaudioCBuffer[s + 1] << 8));
					int v2 = (int)(short)((ushort)channels[2].xaudioCBuffer[s] + (ushort)(channels[2].xaudioCBuffer[s + 1] << 8));
					int v3 = (int)(short)((ushort)channels[3].xaudioCBuffer[s] + (ushort)(channels[3].xaudioCBuffer[s + 1] << 8));

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