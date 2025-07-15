using Microsoft.Extensions.Logging;
using System.Management;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay.Overlays
{
	public class CpuUsageOverlay : BaseOverlay, ICpuUsageOverlay
	{
		private readonly Thread tid;
		public CpuUsageOverlay(INativeOverlay nativeOverlay, ILogger<CpuUsageOverlay> logger) : base(nativeOverlay, logger)
		{
			tid = new Thread(SnapshotCpuUsage);
			tid.Start();
		}

		private readonly Lock _lock = new();
		private uint[] cpuUsage = Array.Empty<uint>();

		private void SnapshotCpuUsage()
		{
			for (; ; )
			{
				//try
				//{
				//	logger.LogTrace("CPU Frequency and Throttling Info:\n");

				//	// Query CPU performance data
				//	using (var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_Processor"))
				//	{
				//		foreach (ManagementObject obj in searcher.Get())
				//		{
				//			string name = obj["Name"]?.ToString();
				//			uint currentClockSpeed = (uint)(obj["CurrentClockSpeed"] ?? 0);
				//			uint maxClockSpeed = (uint)(obj["MaxClockSpeed"] ?? 0);

				//			logger.LogTrace($"Processor: {name}");
				//			logger.LogTrace($"Current Clock Speed: {currentClockSpeed} MHz");
				//			logger.LogTrace($"Max Clock Speed: {maxClockSpeed} MHz");

				//			if (currentClockSpeed < maxClockSpeed)
				//			{
				//				logger.LogTrace("* Possible thermal or power throttling detected.");
				//			}
				//			else
				//			{
				//				logger.LogTrace("- Running at full speed.");
				//			}

				//			logger.LogTrace("");
				//		}
				//	}
				//}
				//catch (Exception ex)
				//{
				//	logger.LogTrace("Error retrieving CPU info: " + ex.Message);
				//}
#pragma warning disable CA1416 // Validate platform compatibility
				using (var searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor"))
				{
					var cpuInfo = searcher
							.Get()
							.Cast<ManagementObject>()
							.Where(x => int.TryParse((string)x["Name"], out _))
							.OrderBy(x => int.Parse((string)x["Name"]))
							.ToList();

					var tmpCpuUsage = cpuInfo.Select(mo => (uint)(ulong)mo["PercentProcessorTime"]).ToArray();
					lock (_lock)
					{
						cpuUsage = (uint[])tmpCpuUsage.Clone();
					}
				}
#pragma warning restore CA1416 // Validate platform compatibility
				Thread.Sleep(CPU_SNAPSHOT_FREQUENCY);
			}
		}

		private const int CPU_SNAPSHOT_FREQUENCY = 200;

		public void Render()
		{
			uint[] tmpCpuUsage;
			lock (_lock)
			{
				tmpCpuUsage = (uint[])cpuUsage.Clone();
			}

			if (tmpCpuUsage.Length == 0)
				return;

			const int barXMargin = 50;
			const int barYMargin = 50;

			//50 and 100%
			for (int x = barXMargin; x < screenWidth - barXMargin; x += 4)
			{
				screen[x + (screenHeight - barYMargin - 50) * screenWidth] = 0x00ffffff;
				screen[x + (screenHeight - barYMargin - 25) * screenWidth] = 0x00ffffff;
			}

			int barWidth = (screenWidth - (barXMargin * 2)) / (cpuUsage.Length/**2*/);
			int barBase = screenHeight - barYMargin;

			int barX = barXMargin + barWidth/4;
			for (int bar = 0; bar < cpuUsage.Length; bar++)
			{
				int barHeight = (int)tmpCpuUsage[bar] / 2;

				for (int xx = barX; xx < barX + barWidth / 2; xx++)
				{
					for (int yy = barBase - barHeight; yy < barBase; yy++)
					{
						screen[xx + yy * screenWidth] = 0x0089cff0;
					}
				}
				barX += barWidth;
			}

			//baseline
			for (int x = barXMargin; x < screenWidth - barXMargin; x ++)
			{
				screen[x + (screenHeight - barYMargin) * screenWidth] = 0x00ffffff;
			}
		}
	}
}
