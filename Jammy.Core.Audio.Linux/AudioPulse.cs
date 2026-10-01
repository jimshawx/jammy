using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Runtime.InteropServices;
using System.Threading;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Audio.Linux
{
	public class AudioPulse : Custom.Audio.Audio, IAudio
	{
		public AudioPulse(IChipsetClock clock, IChipRAM memory, IInterrupt interrupt, IDMA dma,
			IOptions<EmulationSettings> settings, ILogger<AudioPulse> logger) :
			base(clock, memory, interrupt, dma, settings, logger)
		{
			InitHardwareMixer();
		}

		public new void Emulate()
		{
			base.Emulate();
			HardwareMix();
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct pa_sample_spec
		{
			public int format;
			public uint rate;
			public byte channels;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct pa_buffer_attr
		{
			public uint maxlength;
			public uint tlength;
			public uint prebuf;
			public uint minreq;
			public uint fragsize;
		}

		[DllImport("libpulse-simple.so.0")]
		private static extern IntPtr pa_simple_new(
			string server,
			string name,
			int dir,
			string dev,
			string stream_name,
			ref pa_sample_spec ss,
			IntPtr channel_map,
			ref pa_buffer_attr attr,
			//IntPtr attr,
			out int error);

		[DllImport("libpulse-simple.so.0")]
		private static extern int pa_simple_write(
			IntPtr s,
			byte[] data,
			nuint bytes,
			out int error);

		[DllImport("libpulse-simple.so.0")]
		private static extern ulong pa_simple_get_latency(IntPtr s, out int error);

		private IntPtr pulseHandle;
		private byte[] mixBuffer;

		private void InitHardwareMixer()
		{
			var ss = new pa_sample_spec
			{
				format = 3, // PA_SAMPLE_S16LE
				rate = 31200,
				channels = 2
			};

			uint frameBytes = (uint)((31200 / 60) * 4);
			uint targetBytes = frameBytes * 2;// 2 frames

			//var attr = new pa_buffer_attr
			//{
			//	maxlength = targetBytes * 2,
			//	tlength = targetBytes,
			//	prebuf = 0,
			//	minreq = frameBytes/2,
			//	fragsize = uint.MaxValue
			//};
			var attr = new pa_buffer_attr
			{
				maxlength = uint.MaxValue,
				tlength = uint.MaxValue,
				prebuf = 0,
				minreq = uint.MaxValue,
				fragsize = uint.MaxValue
			};

			pulseHandle = pa_simple_new(
				null,               // Default server
				"Jammy",       // App name
				1,                  // PA_STREAM_PLAYBACK
				null,               // Default device
				"Audio Out",        // Stream name
				ref ss,
				IntPtr.Zero,        // Default channel map
				ref attr,        // Above buffering attributes
				//IntPtr.Zero,
				out int error);

			mixBuffer = new byte[ch[0].audioBytes.Length * 2];
		}

		private void HardwareMix()
		{
			if (ch[0].audioBytesIndex != ch[0].audioBytes.Length) return;

			//2 frames
			ulong targetLatencyUs = 33333;

			for (;;)
			{
				ulong currentLatencyUs = pa_simple_get_latency(pulseHandle, out int err);
				if (currentLatencyUs <= targetLatencyUs)
					break;
				Thread.SpinWait(100);
			}

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
				mixBuffer[s * 2 + 1] = (byte)(L >> 8);
				mixBuffer[s * 2 + 2] = (byte)R;
				mixBuffer[s * 2 + 3] = (byte)(R >> 8);
			}

			pa_simple_write(pulseHandle, mixBuffer, (nuint)mixBuffer.Length, out int error);

			for (int i = 0; i < 4; i++)
				ch[i].audioBytesIndex = 0;
		}
	}
}