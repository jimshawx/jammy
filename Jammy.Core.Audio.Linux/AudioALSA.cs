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

		public new void Emulate()
		{
			base.Emulate();
			HardwareMix();
		}

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
				ch[i].audioBytes = new byte[BUFFER_SIZE];
				ch[i].audioBytesIndex = 0;
			}
			mixBuffer = new byte[BUFFER_SIZE * 2];
		}

		private void HardwareMix()
		{
			//time to hardware mix?
			if (ch[0].audioBytesIndex == ch[0].audioBytes.Length)
			{
				for (int i = 0; i < 4; i++)
					LowPassFilter(ch[i]);

				for (int s = 0; s < ch[0].audioBytes.Length; s += 2)
				{
					int v0 = (int)(short)((ushort)ch[0].audioBytes[s] + (ushort)(ch[0].audioBytes[s + 1] << 8));
					int v1 = (int)(short)((ushort)ch[1].audioBytes[s] + (ushort)(ch[1].audioBytes[s + 1] << 8));
					int v2 = (int)(short)((ushort)ch[2].audioBytes[s] + (ushort)(ch[2].audioBytes[s + 1] << 8));
					int v3 = (int)(short)((ushort)ch[3].audioBytes[s] + (ushort)(ch[3].audioBytes[s + 1] << 8));

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
					ch[i].audioBytesIndex = 0;
			}
		}
	}
}