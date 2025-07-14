using Microsoft.Extensions.Logging;
using System.Management;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.NativeOverlay.Overlays
{
	public class CpuUsageOverlay : BaseOverlay, ICpuUsageOverlay
	{
		public CpuUsageOverlay(INativeOverlay nativeOverlay, ILogger<CpuUsageOverlay> logger) : base(nativeOverlay, logger)
		{
		}

		private void SnapshotCpuUsage()
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
			using (var searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor")) { 
			var cpuInfo = searcher.Get().Cast<ManagementObject>().ToList();
			foreach (var mo in cpuInfo.OrderBy(x => { if (int.TryParse((string)x["Name"], out var s)) return s; return int.MaxValue; }))
			{
				//.Select(mo =>// new
				//			 //{
				//			 //	Name = mo["Name"],
				//			 //	Usage = mo["PercentProcessorTime"]
				//			 //}
				logger.LogTrace($"{mo["Name"]} {mo["PercentProcessorTime"]}");
				//)
				//.ToList();	
			} }

			using (var searcher = new ManagementObjectSearcher("select * from Win32_Perf"))
			{
				var cpuInfo = searcher.Get().Cast<ManagementObject>().ToList();
				//foreach (var mo in cpuInfo.OrderBy(x => { if (int.TryParse((string)x["Name"], out var s)) return s; return int.MaxValue; }))
				//{
				//	//.Select(mo =>// new
				//	//			 //{
				//	//			 //	Name = mo["Name"],
				//	//			 //	Usage = mo["PercentProcessorTime"]
				//	//			 //}
				//	logger.LogTrace($"{mo["Name"]} {mo["PercentProcessorTime"]}");
				//	//)
				//	//.ToList();	
				//}
			}
			using (var searcher = new ManagementObjectSearcher("select * from Win32_PerfRawData"))
			{
				var cpuInfo = searcher.Get().Cast<ManagementObject>().ToList();
				//foreach (var mo in cpuInfo.OrderBy(x => { if (int.TryParse((string)x["Name"], out var s)) return s; return int.MaxValue; }))
				//{
				//	//.Select(mo =>// new
				//	//			 //{
				//	//			 //	Name = mo["Name"],
				//	//			 //	Usage = mo["PercentProcessorTime"]
				//	//			 //}
				//	logger.LogTrace($"{mo["Name"]} {mo["PercentProcessorTime"]}");
				//	//)
				//	//.ToList();	
				//}
			}
		}

		private DateTime lastTime = DateTime.Now;
		private const int CPU_SNAPSHOT_FREQUENCY = 2000;

		public void Render()
		{
			var now = DateTime.Now;
			if (now - lastTime > TimeSpan.FromMilliseconds(CPU_SNAPSHOT_FREQUENCY)) 
			{
				SnapshotCpuUsage();
				lastTime = now;
			}
		}
	}
}
