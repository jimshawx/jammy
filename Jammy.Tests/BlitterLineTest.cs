using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jammy.Core.Custom;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Tests
{
	public class LoggedChipRAM : ChipRAM, IMemoryMappedDevice
	{
		public LoggedChipRAM(IOptions<EmulationSettings> settings, ILogger<ChipRAM> logger) : base(settings, logger)
		{
		}

		public class ChipLog : IEqualityComparer<ChipLog>
		{
			public uint address;
			public ushort value;

			public bool Equals(ChipLog x, ChipLog y)
			{
				if (x == null & y == null) return true;
				if (x == null ^ y == null) return false;
				return x.address == y.address && x.value == y.value;
			}

			public int GetHashCode(ChipLog obj)
			{
				return obj.address.GetHashCode() ^ obj.value.GetHashCode();
			}
		}

		private readonly List<ChipLog> log = new List<ChipLog>();

		public new void Write(uint insaddr, uint address, uint value, Size size)
		{
			log.Add(new ChipLog{address = address, value = (ushort)value});
			base.Write(insaddr, address, value, size);
		}

		//public void Log()
		//{
		//	//var logs = log
		//	//	.Where(x=>x.value != 0)
		//	//	.Distinct(new ChipLog())
		//	//	.OrderBy(x => x.address)
		//	//	.ThenBy(x => x.value);
		//	var logs = log;
		//	foreach (var l in logs)
		//		TestContext.WriteLine($"{l.address:X6} {Convert.ToString(l.value, 2).PadLeft(16, '0')}");
		//}

		public void LogOut(List<ChipLog> logs)
		{
			var logs0 = logs
				.Where(x => x.value != 0)
				.Distinct(new ChipLog())
				.OrderBy(x => x.address)
				.ThenBy(x => x.value);

			foreach (var l in logs0)
				TestContext.WriteLine($"{l.address:X6} {Convert.ToString(l.value, 2).PadLeft(16, '0')}");
		}

		public List<ChipLog> GetLog()
		{
			return log.ToList();
		}

		public bool LogsDiffer(List<ChipLog> l0, List<ChipLog> l1)
		{
			var logs0 = l0
				.Where(x => x.value != 0)
				.Distinct(new ChipLog())
				.OrderBy(x => x.address)
				.ThenBy(x => x.value);
			var logs1 = l1
				.Where(x => x.value != 0)
				.Distinct(new ChipLog())
				.OrderBy(x => x.address)
				.ThenBy(x => x.value);

			return logs0.Except(logs1, new ChipLog()).Any();
		}

		public void Clear()
		{
			Array.Clear(memory, 0, memory.Length);
			log.Clear();
		}
	}

	[TestFixture]
	public class BlitterLineTest
	{
		private ServiceProvider serviceProvider0;

		[OneTimeSetUp]
		public void CPUTestInit()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
				.AddJsonFile("appsettings.json", false)
				.Build();

			var interrupt = new Mock<IInterrupt>();
			var custom = new Mock<IChips>();

			serviceProvider0 = new ServiceCollection()
				.AddLogging(x =>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					x.AddDebug();
				})
				.AddSingleton<IInterrupt>(x=> interrupt.Object)
				.AddSingleton<IChips>(x=>custom.Object)
				.AddSingleton<IChipRAM, LoggedChipRAM>()
				.AddSingleton<IBlitter, Blitter>()
				.Configure<EmulationSettings>(o => configuration.GetSection("Emulation").Bind(o))
				.BuildServiceProvider();
		}

		[Test]
		public void TestBlitterLine()
		{
			var blitter = serviceProvider0.GetRequiredService<IBlitter>();
			var chipRAM = (LoggedChipRAM)serviceProvider0.GetRequiredService<IChipRAM>();

			int i = 0;
			var testcases = BlitterLineTestCases.TestCases();
			int passes = 0;
			foreach (var c in testcases)
			{
				TestContext.WriteLine($"\n------- Test Case {++i,4} -------");
				if (RunBlitterLineTestCase(c, blitter, chipRAM))
					passes++;
			}
			TestContext.WriteLine($"PASSES: {passes}/{testcases.Count}");
		}

		[Test]
		public void TestBlitterLineSingle()
		{
			var blitter = serviceProvider0.GetRequiredService<IBlitter>();
			var chipRAM = (LoggedChipRAM)serviceProvider0.GetRequiredService<IChipRAM>();

			int i = 6;
			var testcases = BlitterLineTestCases.TestCases();
			TestContext.WriteLine($"\n------- Test Case {i,4} -------");
			RunBlitterLineTestCase(testcases[i-1], blitter, chipRAM);
		}

		private bool RunBlitterLineTestCase(BlitterLineTestCases.BlitterLineTestCase c, IBlitter blitter, LoggedChipRAM chipRAM)
		{
			List<LoggedChipRAM.ChipLog>[] log = new List<LoggedChipRAM.ChipLog>[2];

			for (int a = 0; a < 2; a++)
			{
				blitter.SetLineMode(a);

				chipRAM.Clear();

				blitter.Write(0, ChipRegs.BLTCON0, (ushort)c.bltcon0);
				blitter.Write(0, ChipRegs.BLTCON1, (ushort)c.bltcon1);

				blitter.Write(0, ChipRegs.BLTAPTH, (ushort)(c.bltapt >> 16));
				blitter.Write(0, ChipRegs.BLTAPTL, (ushort)c.bltapt);
				blitter.Write(0, ChipRegs.BLTBPTH, (ushort)(c.bltbpt >> 16));
				blitter.Write(0, ChipRegs.BLTBPTL, (ushort)c.bltbpt);
				blitter.Write(0, ChipRegs.BLTCPTH, (ushort)(c.bltcpt >> 16));
				blitter.Write(0, ChipRegs.BLTCPTL, (ushort)c.bltcpt);
				blitter.Write(0, ChipRegs.BLTDPTH, (ushort)(c.bltdpt >> 16));
				blitter.Write(0, ChipRegs.BLTDPTL, (ushort)c.bltdpt);

				blitter.Write(0, ChipRegs.BLTADAT, (ushort)c.bltadat);
				blitter.Write(0, ChipRegs.BLTBDAT, (ushort)c.bltbdat);
				blitter.Write(0, ChipRegs.BLTCDAT, (ushort)c.bltcdat);
				blitter.Write(0, ChipRegs.BLTDDAT, (ushort)c.bltddat);

				blitter.Write(0, ChipRegs.BLTAMOD, (ushort)c.bltamod);
				blitter.Write(0, ChipRegs.BLTBMOD, (ushort)c.bltbmod);
				blitter.Write(0, ChipRegs.BLTCMOD, (ushort)c.bltcmod);
				blitter.Write(0, ChipRegs.BLTDMOD, (ushort)c.bltdmod);

				blitter.Write(0, ChipRegs.BLTAFWM, (ushort)c.bltafwm);
				blitter.Write(0, ChipRegs.BLTALWM, (ushort)c.bltalwm);

				if (c.bltsize != 0)
				{
					TestContext.WriteLine($"BLTSIZE {c.bltsize & 0x3f} x {c.bltsize >> 6} mod: {(int)c.bltcmod} oct:{(c.bltcon1 >> 2) & 7}");
					blitter.Write(0, ChipRegs.BLTSIZE, (ushort)c.bltsize);
				}
				else
				{
					TestContext.WriteLine($"BLTSIZE {c.bltsizh} x {c.bltsizv}");
					blitter.Write(0, ChipRegs.BLTSIZV, (ushort)c.bltsizv);
					blitter.Write(0, ChipRegs.BLTSIZH, (ushort)c.bltsizh);
				}

				log[a] = chipRAM.GetLog();
			}

			if (chipRAM.LogsDiffer(log[0], log[1]))
			{
				for (int a = 0; a < 2; a++)
				{
					TestContext.WriteLine($"{(a == 0 ? "Benchmark" : "Incoming")}");
					chipRAM.LogOut(log[a]);
				}

				return false;
			}
			else
			{
				TestContext.WriteLine("PASS");
			}

			return true;
		}
	}

	public static class BlitterLineTestCases 
	{
		public class BlitterLineTestCase
		{
			public uint bltcon0;
			public uint bltcon1;

			public uint bltapt;
			public uint bltbpt;
			public uint bltcpt;
			public uint bltdpt;
			
			public uint bltadat;
			public uint bltbdat;
			public uint bltcdat;
			public uint bltddat;
			
			public uint bltamod;
			public uint bltbmod;
			public uint bltcmod;
			public uint bltdmod;
			
			public uint bltafwm;
			public uint bltalwm;
			
			public uint bltsize;
			public uint bltsizh;
			public uint bltsizv;
		}

		public static List<BlitterLineTestCase> TestCases()
		{
			var json = $"[{File.ReadAllText("blitter-2021-04-08-144513.txt")}]";
			return JsonConvert.DeserializeObject<List<BlitterLineTestCase>>(json);
		}
	}

}