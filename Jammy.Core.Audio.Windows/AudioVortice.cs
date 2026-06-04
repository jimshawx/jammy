using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Multimedia;
using Vortice.XAudio2;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Audio.Windows
{
	public class AudioVortice : Custom.Audio.Audio, IAudio
	{
		public AudioVortice(IChipsetClock clock, IChipRAM memory, IInterrupt interrupt,
			IOptions<EmulationSettings> settings, ILogger<AudioVortice> logger) :
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

		private IXAudio2 xaudio;
		private IXAudio2MasteringVoice masteringVoice;

		private class AmigaChannel : IFilter
		{
			public IXAudio2SourceVoice xaudioVoice { get; set; }
			public AudioBuffer[] xaudioBuffer { get; set; }
			public byte[] audioBytes { get; set; }
			public int xaudioCIndex { get; set; }
			public int currentBuffer { get; set; }
			public FilterChannel filter { get; } = new FilterChannel();
		}

		private readonly AmigaChannel[] channels = new[] { new AmigaChannel(), new AmigaChannel(), new AmigaChannel(), new AmigaChannel() };


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
				channels[i].xaudioBuffer[0] = new AudioBuffer {AudioBytes = BUFFER_SIZE, AudioDataPointer = AllocateMemory(BUFFER_SIZE), PlayLength = BUFFER_SIZE/SAMPLE_SIZE};
				channels[i].xaudioBuffer[1] = new AudioBuffer {AudioBytes = BUFFER_SIZE, AudioDataPointer = AllocateMemory(BUFFER_SIZE), PlayLength = BUFFER_SIZE/SAMPLE_SIZE};
				channels[i].audioBytes = new byte[BUFFER_SIZE];
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

		private nint AllocateMemory(int size)
		{
			return Marshal.AllocHGlobal(size);
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

				if (channels[i].xaudioCIndex == channels[i].audioBytes.Length)
				{
					var state = channels[i].xaudioVoice.State;
					if (state.BuffersQueued >= 2)
					{
						do
						{
							Thread.Yield();
						} while (channels[i].xaudioVoice.State.BuffersQueued >= 2);
					}

					LowPassFilter(channels[i]);

					Marshal.Copy(channels[i].audioBytes, 0, channels[i].xaudioBuffer[channels[i].currentBuffer].AudioDataPointer, channels[i].audioBytes.Length);
					channels[i].xaudioCIndex = 0;
					channels[i].xaudioVoice.SubmitSourceBuffer(channels[i].xaudioBuffer[channels[i].currentBuffer], null);
					channels[i].xaudioVoice.Start();
					channels[i].currentBuffer ^= 1;
				}
			}
		}
	}
}