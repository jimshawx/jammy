using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
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

		public new void Emulate()
		{
			base.Emulate();
			HardwareMix();
		}

		private IXAudio2 xaudio;
		private IXAudio2MasteringVoice masteringVoice;

		private class VorticeChannel
		{
			public IXAudio2SourceVoice xaudioVoice { get; set; }
			public AudioBuffer[] xaudioBuffer { get; set; }
			public int currentBuffer { get; set; }
		}

		private readonly VorticeChannel[] channels = new[] { new VorticeChannel(), new VorticeChannel(), new VorticeChannel(), new VorticeChannel() };

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
				ch[i].audioBytes = new byte[BUFFER_SIZE];
				ch[i].audioBytesIndex = 0;
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

		private void HardwareMix()
		{
			//time to hardware mix?
			for (int i = 0; i < 4; i++)
			{
				if (ch[i].audioBytesIndex == ch[i].audioBytes.Length)
				{
					var state = channels[i].xaudioVoice.State;
					if (state.BuffersQueued >= 2)
					{
						do
						{
							Thread.Yield();
						} while (channels[i].xaudioVoice.State.BuffersQueued >= 2);
					}

					LowPassFilter(ch[i]);

					Marshal.Copy(ch[i].audioBytes, 0, channels[i].xaudioBuffer[channels[i].currentBuffer].AudioDataPointer, ch[i].audioBytes.Length);
					ch[i].audioBytesIndex = 0;
					channels[i].xaudioVoice.SubmitSourceBuffer(channels[i].xaudioBuffer[channels[i].currentBuffer], null);
					channels[i].xaudioVoice.Start();
					channels[i].currentBuffer ^= 1;
				}
			}
		}
	}
}