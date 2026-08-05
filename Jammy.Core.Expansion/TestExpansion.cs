using Jammy.Assembler;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Expansion
{
	public class TestExpansion : IExpansionROM
	{
		private readonly IAssembler assembler;
		private readonly ILogger<TestExpansion> logger;

		public TestExpansion(IAssembler assembler, ILogger<TestExpansion> logger)
		{
			this.assembler = assembler;
			this.logger = logger;
		}

		public ZorroConfiguration GetConfiguration()
		{
			//add a 64KB Zorro II expansion with an autoboot ROM
			float v = 0.0625f;
			var cfg = new ZorroConfiguration
			{
				Config = ZorroExpansion.ConfigForSize(ZorroExpansion.BaseConfig_Z2, v),
				Name = $"{v}MB ZII ROM Expansion",
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

		public void PopulateROM(IDebuggableMemory zorroRAM, uint baseAddress)
		{
			const byte DAC_WORDWIDE = 0x80;
			const byte DAC_NYBBLEWIDE = 0x00;
			const byte DAC_BYTEWIDE = 0x40;
			const byte DAC_CONFIGTIME = 0x10;

			zorroRAM.DebugWrite(0x40, DAC_WORDWIDE | DAC_CONFIGTIME, Size.Byte); //da_Config
			zorroRAM.DebugWrite(0x41, 0, Size.Byte); //da_Flags
			zorroRAM.DebugWrite(0x42, 0, Size.Word); //da_Size
			zorroRAM.DebugWrite(0x44, 0, Size.Word); //da_DiagPoint 
			zorroRAM.DebugWrite(0x46, 0, Size.Word); //da_BootPoint 
			zorroRAM.DebugWrite(0x48, 0, Size.Word); //da_Name
			zorroRAM.DebugWrite(0x4A, 0, Size.Word); //da_Reserved01
			zorroRAM.DebugWrite(0x4C, 0, Size.Word); //da_Reserved02

			string name = "jammy.expansion\0";
			uint address = 0x4e;

			zorroRAM.DebugWrite(0x48, address - 0x40, Size.Word);//da_Name

			for (uint i = 0; i < name.Length; i++)
				zorroRAM.DebugWrite(address++, name[(int)i], Size.Byte); ;

			if ((address & 1) != 0) address++;

			//all initialisation happens in DiagPoint
			zorroRAM.DebugWrite(0x44, address - 0x40, Size.Word);//da_DiagPoint

			//41F9 00DF F000      lea       $DFF000,a0
			//zorroRAM.DebugWrite(address, 0x41f9, Size.Word); address += 2;
			//zorroRAM.DebugWrite(address, 0x00df, Size.Word); address += 2;
			//zorroRAM.DebugWrite(address, 0xf000, Size.Word); address += 2;

			////317C 0ff0 0180      move.w    #$ff0,$180(a0)
			//zorroRAM.DebugWrite(address, 0x317c, Size.Word); address += 2;
			//zorroRAM.DebugWrite(address, 0x0ff0, Size.Word); address += 2;
			//zorroRAM.DebugWrite(address, 0x0180, Size.Word); address += 2;
			
			//BootPoint is never called
			zorroRAM.DebugWrite(0x46, address - 0x40, Size.Word);//da_BootPoint

			//7001                moveq     #1,d0
			zorroRAM.DebugWrite(address, 0x7001, Size.Word); address += 2;

			//4e7f                rts
			zorroRAM.DebugWrite(address, 0x4e75, Size.Word); address += 2;

			zorroRAM.DebugWrite(0x42, address - 0x40, Size.Word);//da_size

			//string s = " lea $dff000,a0\n move.w #$ff0,$180(a0)\n moveq #1,d0\n rts\n";
			//var r = assembler.Assemble(s);
			//logger.LogTrace(r.ToString());
		}
	}

	public class DosExpansion : IExpansionROM
	{
		//private readonly ICPU cpu;
		private readonly IBreakpointCollection breakpoints;
		private readonly IAssembler assembler;
		private readonly ILogger<TestExpansion> logger;

		public DosExpansion(/*ICPU cpu, */IBreakpointCollection breakpoints, IAssembler assembler, ILogger<TestExpansion> logger)
		{
			//this.cpu = cpu;
			this.breakpoints = breakpoints;
			this.assembler = assembler;
			this.logger = logger;
		}

		public ZorroConfiguration GetConfiguration()
		{
			//add a 64KB Zorro II expansion with an autoboot ROM
			float v = 0.0625f;
			var cfg = new ZorroConfiguration
			{
				Config = ZorroExpansion.ConfigForSize(ZorroExpansion.BaseConfig_Z2, v),
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

		public void PopulateROM(IDebuggableMemory zorroRAM, uint baseAddress)
		{
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
			breakpoints.AddBreakpoint(baseAddress+0x40, Types.Types.Breakpoints.BreakpointType.Read, size: Size.Word, callback: (bp) =>
			{
				breakpoints.RemoveBreakpoint(bp);
				//var regs = cpu.GetRegs();
				var regs = new Regs();
				logger.LogTrace($"DiagArea W copied @ {regs.PC:X8}");

				return false;
			});
			breakpoints.AddBreakpoint(baseAddress + 0x40, Types.Types.Breakpoints.BreakpointType.Read, size: Size.Long, callback: (bp) =>
			{
				breakpoints.RemoveBreakpoint(bp);
				//var regs = cpu.GetRegs();
				var regs = new Regs();
				logger.LogTrace($"DiagArea L copied @ {regs.PC:X8}");

				return false;
			});
		}
	}
}
