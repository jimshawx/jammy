using Jammy.AmigaTypes;
using Jammy.Assembler;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
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
		private readonly IAssembler assembler;
		private ZorroConfiguration configuration;

		public DosExpansionDebugHandler(IDebugger debugger, IZorroExpansionRegistry registry,
			IObjectMapper objectMapper, ICPU cpu, IDebugMemoryMapper memory, IAssembler assembler,
			ILogger<DosExpansionDebugHandler> logger) : base(registry)
		{
			this.debugger = debugger;
			this.logger = logger;
			this.registry = registry;
			this.objectMapper = objectMapper;
			this.cpu = cpu;
			this.memory = memory;
			this.assembler = assembler;
			registry.RegisterHandler(DosExpansion.Serial, this);
		}
		bool volumeLinked = false;
		uint myVolumeNodeBPTR = 0;

		private class MyFileInfo
		{
			public string Name { get; set; }
			public uint Size { get; set; }
			public uint LockKey { get; set; }
		}

		private readonly Dictionary<uint, MyFileInfo> files = new Dictionary<uint, MyFileInfo>();

		public override void Init(ZorroConfiguration configuration)
		{
			uint baseAddress = configuration.BaseAddress;
			this.configuration = configuration;

			logger.LogTrace($"Configured {ZorroConfiguration.GetSerial(DosExpansion.Serial)} @ {baseAddress:X8}");

			// hard to track the execution of the code, much easier to use a trap which will end up at a fixed address

			//debugger.AddBreakpoint(baseAddress + 0x40, BreakpointType.Read, size: Size.Long, callback: (bp) =>
			//{
			//	debugger.RemoveBreakpoint(bp.Bp);
			//	var regs = debugger.GetRegs();
			//	logger.LogTrace($"DiagArea L copied @ {regs.PC:X8} to {regs.A[1] - 4:X8}");

			//	//FC2FCC  22D8                move.l    (a0)+,(a1)+
			//	//FC2FCE  51C9 FFFC           dbra d1,#$FC2FCC(pc)
			//	//It's copying the code to A1

			//	//break when it's executed at the new location
			//	debugger.AddBreakpoint(regs.A[1] - 4 + 14, callback: (bp2) =>
			//	{
			//		debugger.RemoveBreakpoint(bp2.Bp);
			//		//var regs2 = debugger.GetRegs();
			//		//logger.LogTrace($"DiagArea W copied @ {regs2.PC:X8}");
			//		return true;
			//	});

			//	// then it copies the whole block again to a correctly-sized allocation
			//	// A0 is EA0040

			//	//FC2FA8  48E7 7F3E           movem.l d1-d7 / a2 - a6,-(sp)
			//	//FC2FAC  4CD8 7CFE movem.l(a0) +,d1 - d7 / a2 - a6
			//	//FC2FB0  48D1 7CFE movem.l d1-d7 / a2 - a6,(a1)
			//	//FC2FB4  7230                moveq     #48,d1
			//	//FC2FB6  D3C1 adda.l d1, a1
			//	//FC2FB8  9081                sub.l d1, d0
			//	//FC2FBA B081                cmp.l     d1,d0
			//	//FC2FBC  64EE bcc.s     #$FC2FAC
			//	//FC2FBE  4CDF 7CFE movem.l(sp) +,d1 - d7 / a2 - a6

			//	debugger.AddBreakpoint(baseAddress + 0x40, BreakpointType.Read, size: Size.Long, callback: (bp) =>
			//	{
			//		var regs2 = debugger.GetRegs();
			//		logger.LogTrace("Copy DiagPoint");
			//		return true;
			//	});



			//	return true;
			//});

			//debugger.AddBreakpoint(baseAddress + 0x40 + 14, BreakpointType.Read, size: Size.Long, callback: (bp) =>
			//{
			//	debugger.RemoveBreakpoint(bp.Bp);
			//	var regs = debugger.GetRegs();
			//	logger.LogTrace($"DiagPoint copied @ {regs.PC:X8} to {regs.A[1] - 4:X8}");

			//	//FC2FCC  22D8                move.l    (a0)+,(a1)+
			//	//FC2FCE  51C9 FFFC           dbra d1,#$FC2FCC(pc)
			//	//It's copying the code to A1

			//	//break when it's executed at the new location
			//	//debugger.AddBreakpoint(regs.A[1] - 4 + 14, callback: (bp2) =>
			//	//{
			//	//	debugger.RemoveBreakpoint(bp2);
			//	//	//var regs2 = debugger.GetRegs();
			//	//	//logger.LogTrace($"DiagArea W copied @ {regs2.PC:X8}");
			//	//	return true;
			//	//});

			//	return true;
			//});

			debugger.AddBreakpoint(0x00fffffe, callback: (bp) =>
			{
				var regs = cpu.GetRegs();

				if (!volumeLinked)
				{
					volumeLinked = true;

					// 1. Allocate the Volume Node
					uint volMem = AllocMem(48, 1);
					memory.UnsafeWrite32(volMem + 0, 0);          // dol_Next
					memory.UnsafeWrite32(volMem + 4, 2);          // dol_Type = DLT_VOLUME (2)
					memory.UnsafeWrite32(volMem + 8, regs.A[3]);  // dol_Task = Your MsgPort
					memory.UnsafeWrite32(volMem + 12, 0);         // dol_Lock = 0

					// 2. Allocate the Name string separately and store it as a BPTR!
					uint nameMem = AllocMem(8, 1);
					memory.UnsafeWrite8(nameMem + 0, 5);         // BCPL Length
					memory.UnsafeWrite8(nameMem + 1, (byte)'M');
					memory.UnsafeWrite8(nameMem + 2, (byte)'Y');
					memory.UnsafeWrite8(nameMem + 3, (byte)'D');
					memory.UnsafeWrite8(nameMem + 4, (byte)'E');
					memory.UnsafeWrite8(nameMem + 5, (byte)'V');

					memory.UnsafeWrite32(volMem + 16, 0);
					memory.UnsafeWrite32(volMem + 20, 0);
					memory.UnsafeWrite32(volMem + 24, 0);

					memory.UnsafeWrite32(volMem + 28, 0);
					memory.UnsafeWrite32(volMem + 32, 0x4D594653);
					memory.UnsafeWrite32(volMem + 40, nameMem >> 2);

					myVolumeNodeBPTR = volMem >> 2; // Save this to use in LOCATE_OBJECT!

					// 3. THE ACTUAL INJECTION: Walk ExecBase->libList to find dos.library
					uint execBase = memory.UnsafeRead32(4);
					uint libListHead = execBase + 378; // Offset of libList.lh_Head in ExecBase
					uint node = memory.UnsafeRead32(libListHead);
					uint dosBase = 0;

					// Walk the library linked list
					while (memory.UnsafeRead32(node) != 0)
					{
						uint namePtr = memory.UnsafeRead32(node + 10); // ln_Name pointer
						string libName = "";
						uint p = namePtr;
						while (memory.UnsafeRead8(p) != 0)
						{
							libName += (char)memory.UnsafeRead8(p++);
						}

						if (libName == "dos.library")
						{
							dosBase = node;
							break;
						}
						node = memory.UnsafeRead32(node); // Next node
					}

					if (dosBase != 0)
					{
						uint rootNode = memory.UnsafeRead32(dosBase + 34);     // dos.library->dl_Root
						uint dosInfo = memory.UnsafeRead32(rootNode + 24) << 2; // RootNode->rn_Info (Convert BPTR)

						// Read the current head of the DosList
						uint headBPTR = memory.UnsafeRead32(dosInfo + 4);       // DosInfo->di_DevInfo

						// Prepend our VolumeNode to the linked list
						memory.UnsafeWrite32(volMem + 0, headBPTR);             // ourNode->Next = oldHead
						memory.UnsafeWrite32(dosInfo + 4, myVolumeNodeBPTR);    // Head = ourNode

						logger.LogTrace("MYDEV Volume Node successfully injected into OS DosList!");
					}
					else
					{
						logger.LogTrace("FAILED to find dos.library!");
					}
				}

				//nb. while it _is_ a StandardPacket, there's sometimes a gap between the two structures
				//it holds because of BCPL alignment
				//packet is in d0
				//var std = new StandardPacket();
				//objectMapper.MapObject(std, regs.A[2]);

				uint link = memory.UnsafeRead32(regs.A[4]);
				if (link != regs.A[2])
				{
					logger.LogTrace($"LINK mismatch {link:X8} {regs.A[2]:X8}");
					//return back to emulation
					regs.SR = memory.UnsafeRead16(regs.SSP); regs.SSP += 2;
					regs.PC = memory.UnsafeRead32(regs.SSP); regs.SSP += 4;
					cpu.SetRegs(regs);

					return false;
				}

				//dos packet is in A4
				var pkt = new DosPacket();
				objectMapper.MapObject(pkt, regs.A[4]);
				uint typ = memory.UnsafeRead32(regs.A[4] + 8);
				if (typ != pkt.dp_Type)
					logger.LogTrace($"MAPPING packet type mismatch {typ} {pkt.dp_Type}");

				//for (uint i = 0; i < 80; i += 4)
				//	logger.LogTrace($"{regs.A[2] + i:X8} {memory.UnsafeRead32(regs.A[2] + i):X8}");

				//for (uint i = 0; i < 80; i += 4)
				//	logger.LogTrace($"{regs.A[4]+i:X8} {memory.UnsafeRead32(regs.A[4]+i):X8}");

				logger.LogTrace($"Packet {pkt.dp_Type}");

				const int ACTION_INHIBIT = 0x1f;
				const int ACTION_HANDLER_INFO = 0x19;

				const int ACTION_FREE_LOCK = 15;

				const int ACTION_LOCATE_OBJECT = 0x8;
				const int ACTION_EXAMINE_OBJECT = 0x17;
				const int ACTION_EXAMINE_NEXT = 0x18;

				const int ACTION_FINDINPUT = 1005;
				const int ACTION_END = 1007;

				const int DOSTRUE = -1;
				const int DOSFAIL = 0;

				const int ERROR_NO_MORE_ENTRIES = 232;
				const int ERROR_OBJECT_NOT_FOUND = 205;

				switch (pkt.dp_Type)
				{
					case ACTION_INHIBIT:
						{
							logger.LogTrace($"inhibit {pkt.dp_Arg1}");
							pkt.dp_Res1 = DOSTRUE;
							pkt.dp_Res2 = 0;
							memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
						}
						break;

					case ACTION_HANDLER_INFO://aka ACTION_DISK_INFO
						{
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
							memory.UnsafeWrite32(address, myVolumeNodeBPTR); address += 4;
							memory.UnsafeWrite32(address, 0); address += 4;

							var t = new InfoData();
							t.id_NumSoftErrors = 0;
							t.id_UnitNumber = 1;
							t.id_DiskState = 2;
							t.id_NumBlocks = 0x40000000 / 512;
							t.id_NumBlocksUsed = 0;
							t.id_BytesPerBlock = 512;
							t.id_DiskType = 0x4D594653;
							t.id_VolumeNode = myVolumeNodeBPTR;
							t.id_InUse = 0;
							var c = ObjectWalk.Walk(t);
							logger.LogTrace(c);
							var w = ObjectWalk.Walk2(t);


							memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
						}
						break;

					case ACTION_LOCATE_OBJECT:
						{
							Node reference;
							if (pkt.dp_Arg1 != 0)
							{
								var @lock = new FileLock();
								objectMapper.MapObject(@lock, (uint)pkt.dp_Arg1 << 2);
								logger.LogTrace($"LOCATE FAILING {@lock.fl_Key:X8}");
								reference = new Node();//root node
							}
							else
							{
								reference = new Node();//root node
							}

							uint name = (uint)pkt.dp_Arg2;
							if (name == 0)
							{
								logger.LogTrace($"LOCATE empty name");
							}
							else
							{
								var sb = new StringBuilder();
								name <<= 2;
								byte l = memory.UnsafeRead8(name++);


								while (l-- != 0)
								{
									byte b = memory.UnsafeRead8(name++);
									sb.Append((char)b);
								}

								if (sb.ToString() != "" && sb.ToString().ToUpper() != "MYDEV:" && sb.ToString().ToUpper() != "MYDEV")
								{
									logger.LogTrace($"LOCATE FAILING {sb.ToString()}");

									memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
									memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
									break;
								}


								logger.LogTrace($"LOCATE {sb.ToString()}");

							}
							uint mode = (uint)pkt.dp_Arg3;
							logger.LogTrace($"MODE {mode:X8}");

							uint mem = AllocMem(20, 1);
							memory.UnsafeWrite32(mem, 0);
							memory.UnsafeWrite32(mem + 4, 0);//0x12345678);
							memory.UnsafeWrite32(mem + 8, mode);
							memory.UnsafeWrite32(mem + 12, regs.A[3]);
							memory.UnsafeWrite32(mem + 16, myVolumeNodeBPTR);//regs.A[5]);

							memory.UnsafeWrite32(regs.A[4] + 12, mem / 4);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
						}
						break;

					case ACTION_EXAMINE_OBJECT:
						{
							uint lockPtr = (uint)pkt.dp_Arg1 << 2;
							var lok = new FileLock();
							objectMapper.MapObject(lok, lockPtr);
							uint fib = (uint)pkt.dp_Arg2 << 2;
							memory.UnsafeWrite32(fib + 4, 2); // fib_DirEntryType = Directory
							memory.UnsafeWrite32(fib + 120, 2); // fib_EntryType
							memory.UnsafeWrite32(fib + 124, 0); // fib_Size = 0 for dirs

							memory.UnsafeWrite32(fib, (uint)lok.fl_Key);
							memory.UnsafeWrite32(fib + 116, 0);//rwed

							memory.UnsafeWrite32(fib + 132, 10000); // Days since 1978
							memory.UnsafeWrite32(fib + 136, 0);     // Minutes
							memory.UnsafeWrite32(fib + 140, 0);     // Ticks (1/50th of a sec)

							//memory.UnsafeWrite8(fib + 8, 0);

							memory.UnsafeWrite8(fib + 8, (byte)'M');
							memory.UnsafeWrite8(fib + 9, (byte)'Y');
							memory.UnsafeWrite8(fib + 10, (byte)'D');
							memory.UnsafeWrite8(fib + 11, (byte)'E');
							memory.UnsafeWrite8(fib + 12, (byte)'V');
							memory.UnsafeWrite8(fib + 13, 0);

							memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
						}
						break;

					case ACTION_EXAMINE_NEXT:

						//no more files
						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, ERROR_NO_MORE_ENTRIES);
						break;

					case ACTION_FREE_LOCK:
						{
							uint lockPtr = (uint)pkt.dp_Arg1 << 2;
							var lok = new FileLock();
							objectMapper.MapObject(lok, lockPtr);

							memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
						}
						break;


					case ACTION_FINDINPUT:
						{
							uint bstrAddr = (uint)pkt.dp_Arg2 << 2;
							if (bstrAddr == 0)
							{
								logger.LogTrace("ACTION_FINDINPUT with empty filename");
							}
							else
							{
								byte length = memory.UnsafeRead8(bstrAddr);
								var sb = new StringBuilder();
								for (int i = 0; i < length; i++)
									sb.Append((char)memory.UnsafeRead8(bstrAddr + 1 + (uint)i));
								logger.LogTrace($"ACTION_FINDINPUT for file: {sb.ToString()}");
							}
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						}
						break;

					case ACTION_END:
						memory.UnsafeWrite32(regs.A[4] + 12, 0xffffffff);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						break;

					case >= 1008:
						//send back unchanged
						logger.LogTrace($"IGNORED {pkt.dp_Type} {pkt.dp_Type:X8} {pkt.dp_Type << 2:X8}");
						break;

					default:

						logger.LogTrace($"UNHANDLED {pkt.dp_Type}");

						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, 120);
						break;
				}

				//return back to emulation
				regs.SR = memory.UnsafeRead16(regs.SSP); regs.SSP += 2;
				regs.PC = memory.UnsafeRead32(regs.SSP); regs.SSP += 4;
				cpu.SetRegs(regs);

				return false;
			});
		}

		private uint AllocMem(uint size, uint flags)
		{
			return CallExec(-198, size, flags);
		}

		//assuming this is extremely unsafe (interrupts, locks etc), but here we go
		//we're inside a call to the expansion ROM from dos.library, and inside a trap handler, so it can't be that bad
		private uint CallExec(int lvo, params uint[] p)
		{
			string asm = $@"
				move.l #0,-(sp)
				move.l  $4,a6

				move.l  #{p[0]},d0
				move.l  #{p[1]},d1

				jmp {lvo}(a6)
				";
			var r = assembler.Assemble(asm);

			//we know this space (copy of DiagArea) is unused after expansion.library is finished with it
			uint i = configuration.BaseAddress; ;
			foreach (var b in r.Program)
			{
				memory.UnsafeWrite16(i, b);
				i += 2;
			}

			var regs = new Regs();

			cpu.GetRegs(regs);

			var saved = regs.Clone();

			cpu.SetPC(configuration.BaseAddress);
			do
			{
				cpu.Emulate();
				cpu.GetRegs(regs);
			} while (regs.PC != 0);

			uint rv = regs.D[0];

			cpu.SetRegs(saved);

			return rv;
		}
	}
}
/*
        0       0x0000  ACTION_NIL
        1               <Reserved by Commodore>
        2       0x0002  ACTION_GET_BLOCK
        3               <Reserved by Commodore>
        4       0x0004  ACTION_SET_MAP
        5       0x0005  ACTION_DIE
        6       0x0006  ACTION_EVENT
        7       0x0007  ACTION_CURRENT_VOLUME
        8       0x0008  ACTION_LOCATE_OBJECT
        9       0x0009  ACTION_RENAME_DISK
        10-14           <Reserved by Commodore>
        15      0x000F  ACTION_FREE_LOCK
        16      0x0010  ACTION_DELETE_OBJECT
        17      0x0011  ACTION_RENAME_OBJECT
        18      0x0012  ACTION_MORE_CACHE
        19      0x0013  ACTION_COPY_DIR
        20      0x0014  ACTION_WAIT_CHAR
        21      0x0015  ACTION_SET_PROTECT
        22      0x0016  ACTION_CREATE_DIR
        23      0x0017  ACTION_EXAMINE_OBJECT
        24      0x0018  ACTION_EXAMINE_NEXT
        25      0x0019  ACTION_DISK_INFO
        26      0x001A  ACTION_INFO
        27      0x001B  ACTION_FLUSH
        28      0x001C  ACTION_SET_COMMENT
        29      0x001D  ACTION_PARENT
        30      0x001E  ACTION_TIMER
        31      0x001F  ACTION_INHIBIT
        32      0x0020  ACTION_DISK_TYPE
        33      0x0021  ACTION_DISK_CHANGE
        34      0x0022  ACTION_SET_DATE
        35-39           <Reserved by Commodore>
        40      0x0028  ACTION_SAME_LOCK
        41-81           <Reserved by Commodore>
        82      0x0052  ACTION_READ
        83-86           <Reserved by Commodore>
        87      0x0057  ACTION_WRITE
        88-993          <Reserved by Commodore>
        994     0x03E2  ACTION_SCREEN_MODE
        995     0x03E3  ACTION_CHANGE_SIGNAL
        996-1000        <Reserved by Commodore>
        1001    0x03E9  ACTION_READ_RETURN
        1002    0x03EA  ACTION_WRITE_RETURN
        1003            <Reserved by Commodore>
        1004    0x03EC  ACTION_FINDUPDATE
        1005    0x03ED  ACTION_FINDINPUT
        1006    0x03EE  ACTION_FINDOUTPUT
        1007    0x03EF  ACTION_END
        1008    0x03F0  ACTION_SEEK
        1009-1019       <Reserved by Commodore>
        1020    0x03FC  ACTION_FORMAT
        1021    0x03FD  ACTION_MAKE_LINK
        1022    0x03FE  ACTION_SET_FILE_SIZE
        1023    0x03FF  ACTION_WRITE_PROTECT
        1024    0x0400  ACTION_READ_LINK
        1025            <Reserved by Commodore>
        1026    0x0402  ACTION_FH_FROM_LOCK
        1027    0x0403  ACTION_IS_FILESYSTEM
        1028    0x0404  ACTION_CHANGE_MODE
        1029            <Reserved by Commodore>
        1030    0x0406  ACTION_COPY_DIR_FH
        1031    0x0407  ACTION_PARENT_FH
        1032            <Reserved by Commodore>
        1033    0x0409  ACTION_EXAMINE_ALL
        1034    0x040A  ACTION_EXAMINE_FH
        1035-2007       <Reserved by Commodore>
        2008    0x07D8  ACTION_LOCK_RECORD
        2009    0x07D9  ACTION_FREE_RECORD
        2010-2049       <Reserved by Commodore>
        2050-2999       <Reserved for 3rd Party Handlers>
        4097    0x1001  ACTION_ADD_NOTIFY
        4098    0x1002  ACTION_REMOVE_NOTIFY
        4099-           <Reserved by Commodore for Future Expansion>
*/
