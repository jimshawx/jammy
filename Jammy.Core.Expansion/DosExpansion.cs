using Jammy.Assembler;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types.Types;
using Jammy.Interface;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Expansion
{
	public class DosExpansion : IExpansionROM
	{
		private readonly IZorroExpansionRegistry zorroExpansionRegistry;

		private readonly IAssembler assembler;
		private readonly ILogger<TestExpansion> logger;

		public DosExpansion(IZorroExpansionRegistry zorroExpansionRegistry, IAssembler assembler, ILogger<TestExpansion> logger)
		{
			this.zorroExpansionRegistry = zorroExpansionRegistry;
			this.assembler = assembler;
			this.logger = logger;
		}

		public ZorroConfiguration GetConfiguration()
		{
			//add a 64KB Zorro II expansion with an autoboot ROM
			float v = 0.0625f;
			var cfg = new ZorroConfiguration
			{
				Config = ZorroExpansion.ConfigForSize(ZorroExpansion.BaseConfig_Z2, v, ZorroConfiguration.MakeSerial("JAM0")),
				Name = $"{v}MB ZII DOS ROM Expansion",
				Size = (uint)(v * 1024.0f * 1024.0f),
			};

			//indicate there's an autoboot ROM, and not to link to free pool
			cfg.Config[0] |= 0b00010000;
			cfg.Config[0] &= 0b11011111;

			//indicate there's a DiagArea
			cfg.Config[0xA] = 0;
			cfg.Config[0xB] = 0x40;

			return cfg;
		}

		public void PopulateROM(IDebuggableMemory zorroRAM, ZorroConfiguration configuration)
		{
			zorroExpansionRegistry.RegisterExpansion(configuration);

			var r = assembler.AssembleFile("DosExpansion.s");
			logger.LogTrace(r.ToString());

			if (!r.HasErrors())
			{
				uint address = 0;
				foreach (var w in r.Program)
				{
					zorroRAM.DebugWrite(address, w, Size.Word);
					address += 2;
				}
			}
			else
			{
				logger.LogTrace("Assembly Failed");
				foreach (var e in r.Errors)
					logger.LogTrace(e.Text);
			}
		}
	}

	public class DosExpansionDebugHandler : ZorroDebugHandler
	{
		private readonly IDebugger debugger;
		private readonly ILogger<DosExpansionDebugHandler> logger;
		private readonly IZorroExpansionRegistry registry;

		public DosExpansionDebugHandler(IDebugger debugger, IZorroExpansionRegistry registry, ILogger<DosExpansionDebugHandler> logger) : base(registry)
		{
			this.debugger = debugger;
			this.logger = logger;
			this.registry = registry;
			registry.RegisterHandler(ZorroConfiguration.MakeSerial("JAM0"), this);
		}

		public override void Init(ZorroConfiguration configuration)
		{
			uint baseAddress = configuration.BaseAddress;

			debugger.AddBreakpoint(baseAddress + 0x40, Types.Types.Breakpoints.BreakpointType.Read, size: Size.Word, callback: (bp) =>
			{
				debugger.RemoveBreakpoint(bp);
				var regs = debugger.GetRegs();
				logger.LogTrace($"DiagArea W copied @ {regs.PC:X8}");

				return false;
			});
			debugger.AddBreakpoint(baseAddress + 0x40, Types.Types.Breakpoints.BreakpointType.Read, size: Size.Long, callback: (bp) =>
			{
				debugger.RemoveBreakpoint(bp);
				var regs = debugger.GetRegs();
				logger.LogTrace($"DiagArea L copied @ {regs.PC:X8}");

				return false;
			});
		}
	}
}
