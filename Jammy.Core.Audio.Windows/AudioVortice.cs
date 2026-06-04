using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
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

		private class AmigaChannel
		{
			public IXAudio2SourceVoice xaudioVoice { get; set; }
			public AudioBuffer[] xaudioBuffer { get; set; }
			public byte[] xaudioCBuffer { get; set; }
			public int xaudioCIndex { get; set; }
			public int currentBuffer { get; set; }

			//low-pass filter
			public double i1 { get; set; }
			public double i2 { get; set; }
			public double o1 { get; set; }
			public double o2 { get; set; }

			//hardware filter
			public double y1 { get; set; }
		}

		private readonly AmigaChannel[] channels = new[] { new AmigaChannel(), new AmigaChannel(), new AmigaChannel(), new AmigaChannel() };

		private const int SAMPLE_RATE = 31200;
		private const int SAMPLE_SIZE = 2;//1 for 8bit, 2 for 16bit
		private const int BUFFER_SIZE = 3120*SAMPLE_SIZE;

		private const double LOW_PASS_FILTER_FREQUENCY = 3275.0;   // LED filter A500/A1200
		private const double A500_FIXED_FILTER_FREQUENCY = 4900.0; // Hardware filter A500
		private const double A1200_FIXED_FILTER_FREQUENCY = 28867.0; // Hardware filter A1200

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

					LowPassFilter(i);

					Marshal.Copy(channels[i].xaudioCBuffer, 0, channels[i].xaudioBuffer[channels[i].currentBuffer].AudioDataPointer, channels[i].xaudioCBuffer.Length);
					channels[i].xaudioCIndex = 0;
					channels[i].xaudioVoice.SubmitSourceBuffer(channels[i].xaudioBuffer[channels[i].currentBuffer], null);
					channels[i].xaudioVoice.Start();
					channels[i].currentBuffer ^= 1;
				}
			}
		}

		private bool filter = false;
		private bool raw = false;

		private double a1, a2, a3, b1, b2;
		private double a0;

		private void InitLowPassFilter()
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

		private void LowPassFilter(int i)
		{
			if (raw) return;

			if (SAMPLE_SIZE == 2)
			{
				double o1 = channels[i].o1;
				double o2 = channels[i].o2;
				double i1 = channels[i].i1;
				double i2 = channels[i].i2;

				for (int s = 0; s < channels[i].xaudioCBuffer.Length; s += 2)
				{
					double i0 = ((short)(channels[i].xaudioCBuffer[s] | (channels[i].xaudioCBuffer[s+1] << 8))) / 32768.0f;

					//hardware filter
					double output = i0 * a0 + channels[i].y1 * (1.0 - a0);
					channels[i].y1 = output;

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
					channels[i].xaudioCBuffer[s] = (byte)v;
					channels[i].xaudioCBuffer[s+1] = (byte)(v>>8);
				}

				channels[i].o1 = o1;
				channels[i].o2 = o2;
				channels[i].i1 = i1;
				channels[i].i2 = i2;
			}
		}
	}
}