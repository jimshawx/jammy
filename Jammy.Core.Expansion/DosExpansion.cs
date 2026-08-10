using Jammy.AmigaTypes;
using Jammy.Assembler;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;
using Jammy.Disassembler.TypeMapper;
using Jammy.Interface;
using Microsoft.Extensions.Logging;
using System.Text;

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

		public static readonly uint Serial = ZorroConfiguration.MakeSerial("JAM0");

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
				Config = ZorroExpansion.ConfigForSize(ZorroExpansion.BaseConfig_Z2, v, Serial),
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
		private readonly IObjectMapper objectMapper;
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memory;

		public DosExpansionDebugHandler(IDebugger debugger, IZorroExpansionRegistry registry,
			IObjectMapper objectMapper, ICPU cpu, IDebugMemoryMapper memory,
			ILogger<DosExpansionDebugHandler> logger) : base(registry)
		{
			this.debugger = debugger;
			this.logger = logger;
			this.registry = registry;
			this.objectMapper = objectMapper;
			this.cpu = cpu;
			this.memory = memory;
			registry.RegisterHandler(DosExpansion.Serial, this);
		}

		public override void Init(ZorroConfiguration configuration)
		{
			uint baseAddress = configuration.BaseAddress;

			logger.LogTrace($"Configured {ZorroConfiguration.GetSerial(DosExpansion.Serial)} @ {baseAddress:X8}");

			debugger.AddBreakpoint(baseAddress + 0x40, BreakpointType.Read, size: Size.Long, callback: (bp) =>
			{
				debugger.RemoveBreakpoint(bp);
				var regs = debugger.GetRegs();
				logger.LogTrace($"DiagArea L copied @ {regs.PC:X8} to {regs.A[1]-4:X8}");

				//FC2FCC  22D8                move.l    (a0)+,(a1)+
				//FC2FCE  51C9 FFFC           dbra d1,#$FC2FCC(pc)
				//It's copying the code to A1

				//break when it's executed at the new location
				debugger.AddBreakpoint(regs.A[1]-4+14, callback: (bp2) =>
				{
					debugger.RemoveBreakpoint(bp2);
					//var regs2 = debugger.GetRegs();
					//logger.LogTrace($"DiagArea W copied @ {regs2.PC:X8}");
					return true;
				});

				return true;
			});

			debugger.AddBreakpoint(0x00fffffe, callback: (bp) =>
			{
				var regs = cpu.GetRegs();

				//nb. while it _is_ a StandardPacket, there's sometimes a gap between the two structures
				//it holds because of BCPL alignment
				//packet is in d0
				//var std = new StandardPacket();
				//objectMapper.MapObject(std, regs.A[2]);

				//dos packet is in A4
				var pkt = new DosPacket();
				objectMapper.MapObject(pkt, regs.A[4]);

				//for (uint i = 0; i < 80; i += 4)
				//	logger.LogTrace($"{regs.A[2] + i:X8} {memory.UnsafeRead32(regs.A[2] + i):X8}");

				//for (uint i = 0; i < 80; i += 4)
				//	logger.LogTrace($"{regs.A[4]+i:X8} {memory.UnsafeRead32(regs.A[4]+i):X8}");

				logger.LogTrace($"Packet {pkt.dp_Type}");

				const int ACTION_INHIBIT = 0x1f;
				const int ACTION_HANDLER_INFO = 0x19;

				const int ACTION_LOCATE_OBJECT = 0x8;
				const int ACTION_EXAMINE_OBJECT = 0x17;

				const int DOSTRUE = -1;
				const int DOSFAIL = 0;

				switch (pkt.dp_Type)
				{
					case ACTION_INHIBIT:
						logger.LogTrace($"inhibit {pkt.dp_Arg1}");
						pkt.dp_Res1 = DOSTRUE;
						pkt.dp_Res2 = 0;
						memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						break;

					case ACTION_HANDLER_INFO:

						uint address = (uint)pkt.dp_Arg1 << 2;//InfoData
						/*
						public class InfoData
						{
							public LONG id_NumSoftErrors { get; set; }
							public LONG id_UnitNumber { get; set; }
							public LONG id_DiskState { get; set; }
							public LONG id_NumBlocks { get; set; }
							public LONG id_NumBlocksUsed { get; set; }
							public LONG id_BytesPerBlock { get; set; }
							public LONG id_DiskType { get; set; }
							public BPTR id_VolumeNode { get; set; }
							public LONG id_InUse { get; set; }
						}
						*/
						const uint ID_VALIDATED = 82;
						const uint ID_NOT_REALLY_DOS = 0x4E444F53;  /* 'NDOS'  */

						memory.UnsafeWrite32(address, 0); address += 4;
						memory.UnsafeWrite32(address, 0); address += 4;
						memory.UnsafeWrite32(address, 2); address += 4;//ID_VALIDATED); address += 4;
						memory.UnsafeWrite32(address, 0x40000000); address += 4;//1GB
						memory.UnsafeWrite32(address, 0); address += 4;//nothing used
						memory.UnsafeWrite32(address, 512); address += 4;//512 byte blocks
						memory.UnsafeWrite32(address, 0x4D594653); address += 4;//MYFS        0x4A414D4D); address += 4;//JAMM
						memory.UnsafeWrite32(address, 0); address += 4;
						memory.UnsafeWrite32(address, 0); address += 4;

						var t = new InfoData();
						t.id_NumSoftErrors = 0;
						t.id_UnitNumber = 1;
						t.id_DiskState = 2;
						t.id_NumBlocks = 0x40000000 / 512;
						t.id_NumBlocksUsed = 0;
						t.id_BytesPerBlock = 512;
						t.id_DiskType = 0x4D594653;
						t.id_VolumeNode = 0;
						t.id_InUse = 0;
						var c = ObjectWalk.Walk(t);
						logger.LogTrace(c);
						var w = ObjectWalk.Walk2(t);


						memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						break;

					case ACTION_LOCATE_OBJECT:
						;
						Node reference;
						if (pkt.dp_Arg1 != 0)
						{
							var @lock = new FileLock();
							objectMapper.MapObject(@lock, (uint)pkt.dp_Arg1 << 2);
							reference = new Node();//root node
						}
						else
						{
							reference = new Node();//root node
						}
						Node result_object;
						uint name = (uint)pkt.dp_Arg2;
						if (name != 0)
						{
							result_object = reference;// find(reference, name<<2);
						}
						else
						{
							var sb = new StringBuilder();
							name <<= 2;
							for (; ; )
							{
								byte b = memory.UnsafeRead8(name++);
								if (b == 0) break;
								sb.Append((char)b);
							}
							logger.LogTrace($"LOCATE {sb.ToString()}");
							result_object = reference;
						}
						int mode = pkt.dp_Arg3;
						logger.LogTrace($"mode {mode:X8}");
						memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
						memory.UnsafeWrite32(regs.A[4] + 16, 205);//file not found
						break;

					case ACTION_EXAMINE_OBJECT:
						memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						break;

					case 0x8004e:
						//send back unchanged
						break;

					default:
						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						break;
				}

				//return back to emulation
				regs.SR = memory.UnsafeRead16(regs.SSP); regs.SSP += 2;
				regs.PC = memory.UnsafeRead32(regs.SSP); regs.SSP += 4;
				cpu.SetRegs(regs);

				return false;
			});
		}
	}
}
