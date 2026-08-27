using Jammy.AmigaTypes;
using Jammy.Assembler;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;
using Jammy.Interface;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text;
using DateTime = System.DateTime;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Expansion
{
	public interface IDosExpansion { }

	public class DosExpansion : IExpansionROM
	{
		private readonly IZorroExpansionRegistry zorroExpansionRegistry;

		private readonly IAssembler assembler;
		private readonly ILogger<TestExpansion> logger;

		//public static readonly uint Serial = ZorroConfiguration.MakeSerial("JAM0");
		public readonly string serial;
		private readonly string deviceName;

		public DosExpansion(IZorroExpansionRegistry zorroExpansionRegistry, IAssembler assembler, ILogger<TestExpansion> logger, string serial, string deviceName)
		{
			this.zorroExpansionRegistry = zorroExpansionRegistry;
			this.assembler = assembler;
			this.logger = logger;
			this.serial = serial;
			this.deviceName = deviceName;
		}

		public ZorroConfiguration GetConfiguration()
		{
			//add a 64KB Zorro II expansion with an autoboot ROM
			float v = 0.0625f;
			var cfg = new ZorroConfiguration
			{
				Config = ZorroExpansion.ConfigForSize(ZorroExpansion.BaseConfig_Z2, v, ZorroConfiguration.MakeSerial(serial)),
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

			string asm = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DosExpansion.s"), Encoding.UTF8);
			asm = asm.Replace("MYDEV", deviceName);
			int unitNo = deviceName[deviceName.Length-1] - '0';
			asm = asm.Replace("$00fffffe", $"${0xfffffe-unitNo*2:X8}");

			logger.LogTrace(asm);
			var r = assembler.Assemble(asm);
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
		private readonly string serial;
		private readonly string deviceName;
		private readonly IObjectMapper objectMapper;
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memory;
		private readonly IAssembler assembler;
		private ZorroConfiguration configuration;
		private readonly string rootDir;
		private readonly uint unitNo;

		public DosExpansionDebugHandler(IDebugger debugger, IZorroExpansionRegistry registry,
			IObjectMapper objectMapper, ICPU cpu, IDebugMemoryMapper memory, IAssembler assembler,
			ILogger<DosExpansionDebugHandler> logger, string serial, string deviceName, string rootDir, int unitNo) : base(registry, serial)
		{
			this.debugger = debugger;
			this.logger = logger;
			this.serial = serial;
			this.deviceName = deviceName.ToUpper();
			this.objectMapper = objectMapper;
			this.cpu = cpu;
			this.memory = memory;
			this.assembler = assembler;
			this.rootDir = rootDir;
			this.unitNo = (uint)unitNo;
		}

		private bool volumeLinked = false;
		private uint myVolumeNodeBPTR = 0;
		private bool deviceIsDead = false;

		private class MyLockInfo
		{
			public string FullPath { get; set; }
			public uint Size { get; set; }
			public uint LockKey { get; set; }
			//public MyLockInfo Parent { get; set; }
			public bool Freed { get; set; }

			public override string ToString()
			{
				return $"{LockKey:X8} {Size:X8} {FullPath} {(Freed ? "FREE" : "")}";
			}
			//public List<MyLockInfo> Children { get; } = new List<MyLockInfo>();
		}

		private class MyFileInfo
		{
			public Stream stream;
		}

		//turn an Amiga path like df0:dir1/dir2 into a host path, like c:\source\jammy\hd\dir1\dir2
		private string MakeHostPath(string amigaPath)
		{
			//the bit up to the first colon
			int t = amigaPath.IndexOf(':');
			if (t != -1)
			{
				amigaPath = amigaPath.Substring(t + 1);
			}
			
			//if (amigaPath.StartsWith('\\'))
			//	amigaPath = amigaPath.Substring(1);

			//have to eliminate '..' from Host path
			amigaPath = amigaPath.Replace("..", "__");

			////special file names on Windows
			//string[] bad3 = {"CON", "PRN", "AUX", "NUL" };
			//string[] bad4 = {"COM", "LPT" };
			//if (bad3.Contains(amigaPath.ToUpper())) amigaPath = "_" + amigaPath;
			//if (amigaPath.Length == 4 && bad4.Contains(amigaPath.Substring(0, 3).ToUpper()) && char.IsAsciiDigit(amigaPath[3])) amigaPath = "_" + amigaPath;

			char[] badChars = { '\\', '*', '?', '"', '<', '>', '|', ':' };
			foreach (var c in badChars)
			{
				if (amigaPath.Contains(c))
					amigaPath = amigaPath.Replace(c, '_');
			}

			//finally attach the Host root path from the settings
			amigaPath = Path.Combine(rootDir, amigaPath);

			//path would escape the sandbox?
			if (!IsSandboxEscape(amigaPath))
				amigaPath = rootDir;

			return amigaPath;
		}

		private bool IsSandboxEscape(string combinedPath)
		{
			string basePath = Path.GetFullPath(rootDir);

			if (!basePath.EndsWith(Path.DirectorySeparatorChar))
				basePath += Path.DirectorySeparatorChar;

			string resolvedPath = Path.GetFullPath(combinedPath);

			return resolvedPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
		}

		private string SanitiseAmigaPath(string amigaPath)
		{
			amigaPath = amigaPath.Replace('\\', '_');
			return amigaPath;
		}

		private class MyDirCache
		{
			public MyDirCache(string basePath, ILogger logger)
			{
				//basePath = MakeHostPath(basePath);

				logger.LogTrace($"DirCache {basePath}");

				var dirs = Directory.GetDirectories(basePath);
				var fils = Directory.GetFiles(basePath);

				foreach (var d in dirs)
				{
					logger.LogTrace($"D {d}");
					DirEntries.Add(new MyDirEntry { Name = MungeName(d), IsDirectory = true, Stamp = DirDate(d) });
				}
				foreach (var f in fils)
				{
					logger.LogTrace($"F {f}");
					DirEntries.Add(new MyDirEntry { Name = MungeName(f), IsDirectory = false, Size = FileLen(f), Stamp = FileDate(f) });
				}
			}

			private uint FileLen(string p)
			{
				var f = new FileInfo(p);
				return (uint)f.Length;
			}

			private DateTime FileDate(string p)
			{
				var f = new FileInfo(p);
				return f.CreationTimeUtc.AddYears(-40);
			}

			private DateTime DirDate(string p)
			{
				var f = new DirectoryInfo(p);
				return f.CreationTimeUtc.AddYears(-40);
			}

			private string MungeName(string p)
			{
				p = Path.GetFileName(p);
				p = p.Replace('\\', '/');
				return p;
			}

			public class MyDirEntry
			{
				public string Name { get; set; }
				public bool IsDirectory { get; set; }
				public uint Size { get; set; }
				public DateTime Stamp { get; set; }
			}

			private readonly List<MyDirEntry> DirEntries = new List<MyDirEntry>();

			public MyDirEntry Next()
			{
				if (DirEntries.Count == 0) return null;
				var d = DirEntries[0];
				DirEntries.RemoveAt(0);
				return d;
			}

			//public bool IsEmpty()
			//{
			//	return DirEntries.Count == 0;
			//}
		}

		private const int ACTION_NIL = 0;
		private const int ACTION_GET_BLOCK = 2;
		private const int ACTION_SET_MAP = 4;
		private const int ACTION_DIE = 5;
		private const int ACTION_EVENT = 6;
		private const int ACTION_CURRENT_VOLUME = 7;
		private const int ACTION_LOCATE_OBJECT = 8;
		private const int ACTION_RENAME_DISK = 9;
		private const int ACTION_FREE_LOCK = 15;
		private const int ACTION_DELETE_OBJECT = 16;
		private const int ACTION_RENAME_OBJECT = 17;
		private const int ACTION_MORE_CACHE = 18;
		private const int ACTION_COPY_DIR = 19;
		private const int ACTION_WAIT_CHAR = 20;
		private const int ACTION_SET_PROTECT = 21;
		private const int ACTION_CREATE_DIR = 22;
		private const int ACTION_EXAMINE_OBJECT = 23;
		private const int ACTION_EXAMINE_NEXT = 24;
		private const int ACTION_DISK_INFO = 25;
		private const int ACTION_INFO = 26;
		private const int ACTION_FLUSH = 27;
		private const int ACTION_SET_COMMENT = 28;
		private const int ACTION_PARENT = 29;
		private const int ACTION_TIMER = 30;
		private const int ACTION_INHIBIT = 31;
		private const int ACTION_DISK_TYPE = 32;
		private const int ACTION_DISK_CHANGE = 33;
		private const int ACTION_SET_DATE = 34;
		private const int ACTION_SAME_LOCK = 40;
		private const int ACTION_READ = 82;
		private const int ACTION_WRITE = 87;
		private const int ACTION_SCREEN_MODE = 994;
		private const int ACTION_CHANGE_SIGNAL = 995;
		private const int ACTION_READ_RETURN = 1001;
		private const int ACTION_WRITE_RETURN = 1002;
		private const int ACTION_FINDUPDATE = 1004;
		private const int ACTION_FINDINPUT = 1005;
		private const int ACTION_FINDOUTPUT = 1006;
		private const int ACTION_END = 1007;
		private const int ACTION_SEEK = 1008;
		private const int ACTION_FORMAT = 1020;
		private const int ACTION_MAKE_LINK = 1021;
		private const int ACTION_SET_FILE_SIZE = 1022;
		private const int ACTION_WRITE_PROTECT = 1023;
		private const int ACTION_READ_LINK = 1024;
		private const int ACTION_FH_FROM_LOCK = 1026;
		private const int ACTION_IS_FILESYSTEM = 1027;
		private const int ACTION_CHANGE_MODE = 1028;
		private const int ACTION_COPY_DIR_FH = 1030;
		private const int ACTION_PARENT_FH = 1031;
		private const int ACTION_EXAMINE_ALL = 1033;
		private const int ACTION_EXAMINE_FH = 1034;
		private const int ACTION_LOCK_RECORD = 2008;
		private const int ACTION_FREE_RECORD = 2009;
		private const int ACTION_ADD_NOTIFY = 4097;
		private const int ACTION_REMOVE_NOTIFY = 4098;

		private const int ACTION_MAXIMUM_VALUE = ACTION_IS_FILESYSTEM;

		private const uint DOSTRUE = 0xffffffff;
		private const int DOSFAIL = 0;

		private const int ERROR_NO_FREE_STORE = 103;
		private const int ERROR_TASK_TABLE_FULL = 105;
		private const int ERROR_LINE_TOO_LONG = 120;
		private const int ERROR_FILE_NOT_OBJECT = 121;
		private const int ERROR_INVALID_RESIDENT_LIBRARY = 122;
		private const int ERROR_OBJECT_IN_USE = 202;
		private const int ERROR_OBJECT_EXISTS = 203;
		private const int ERROR_OBJECT_NOT_FOUND = 205;
		private const int ERROR_ACTION_NOT_KNOWN = 209;
		private const int ERROR_INVALID_COMPONENT_NAME = 210;
		private const int ERROR_INVALID_LOCK = 211;
		private const int ERROR_OBJECT_WRONG_TYPE = 212;
		private const int ERROR_DISK_NOT_VALIDATED = 213;
		private const int ERROR_DISK_WRITE_PROTECTED = 214;
		private const int ERROR_RENAME_ACROSS_DEVICES = 215;
		private const int ERROR_DIRECTORY_NOT_EMPTY = 216;
		private const int ERROR_DEVICE_NOT_MOUNTED = 218;
		private const int ERROR_SEEK_ERROR = 219;
		private const int ERROR_COMMENT_TOO_BIG = 220;
		private const int ERROR_DISK_FULL = 221;
		private const int ERROR_DELETE_PROTECTED = 222;
		private const int ERROR_WRITE_PROTECTED = 223;
		private const int ERROR_READ_PROTECTED = 224;
		private const int ERROR_NOT_A_DOS_DISK = 225;
		private const int ERROR_NO_DISK = 226;
		private const int ERROR_NO_MORE_ENTRIES = 232;

		private const int ST_ROOT = 1;
		private const int ST_USERDIR = 2;
		private const int ST_SOFTLINK = 3;
		private const int ST_LINKDIR = 4;
		private const int ST_FILE = -3;
		private const int ST_LINKFILE = -4;

		private const uint ID_VALIDATED = 82;
		private const uint ID_NOT_REALLY_DOS = 0x4E444F53;  /* 'NDOS'  */
		private const uint ID_DOS_DISK = ('D' << 24) | ('O' << 16) | ('S' << 8);

		private readonly Dictionary<uint, MyLockInfo> locks = new Dictionary<uint, MyLockInfo>();

		private readonly Dictionary<uint, MyFileInfo> files = new Dictionary<uint, MyFileInfo>();

		private readonly Dictionary<uint, MyDirCache> dircache = new Dictionary<uint, MyDirCache>();

		public override void Init(ZorroConfiguration configuration)
		{
			uint baseAddress = configuration.BaseAddress;
			this.configuration = configuration;

			logger.LogTrace($"Configured {serial} @ {baseAddress:X8}");

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

			debugger.AddBreakpoint(0x00fffffe-unitNo*2, callback: (bp) => ProcessAmigaDOSAction(bp));

		}

		private bool ProcessAmigaDOSAction(BreakpointHitInfo bp)
		{
			var regs = cpu.GetRegs();

			if (!volumeLinked)
			{
				volumeLinked = true;

				/*
				struct DosList {
					BPTR             dol_Next;      // +0  
					LONG dol_Type;      // +4  

					struct MsgPort * dol_Task;      // +8  
					BPTR dol_Lock;      // +12 

						union {
						// --- DLT_DEVICE (Handler) fields would go here ---
        
						// --- DLT_VOLUME fields ---
						struct {
							struct DateStamp dol_VolumeDate; // +16 (Takes 12 bytes: Days, Mins, Ticks) 
							BPTR dol_LockList;   // +28 
							LONG dol_DiskType;   // +32 <-- Here is your MYFS / DOS0! 
							LONG dol_pad3;       // +36 (Reserved/padding) 
							BSTR dol_Name;       // +40 (The volume name BPTR) 
						}
						dol_volume;
        
						// --- DLT_ASSIGN fields would go here ---
					}
					dol_misc;
				};
				*/

				// 1. Allocate the Volume Node
				uint volMem = AllocMem(48, 0x10001);
				memory.UnsafeWrite32(volMem + 0, 0);          // dol_Next
				memory.UnsafeWrite32(volMem + 4, 2);          // dol_Type = DLT_VOLUME (2)
				memory.UnsafeWrite32(volMem + 8, regs.A[3]);  // dol_Task = Your MsgPort
				memory.UnsafeWrite32(volMem + 12, 0);         // dol_Lock = 0

				// 2. Allocate the Name string separately and store it as a BPTR!
				//uint nameMem = AllocMem(8, 0x10001);
				//memory.UnsafeWrite8(nameMem + 0, 5);         // BCPL Length
				//memory.UnsafeWrite8(nameMem + 1, (byte)'M');
				//memory.UnsafeWrite8(nameMem + 2, (byte)'Y');
				//memory.UnsafeWrite8(nameMem + 3, (byte)'D');
				//memory.UnsafeWrite8(nameMem + 4, (byte)'E');
				//memory.UnsafeWrite8(nameMem + 5, (byte)'V');

				uint nameMem = AllocMem((uint)deviceName.Length+1, 0x10001);
				memory.UnsafeWrite8(nameMem + 0, (byte)deviceName.Length);         // BCPL Length
				for (uint i = 0; i < deviceName.Length; i++)
					memory.UnsafeWrite8(nameMem + i + 1, (byte)deviceName[(int)i]);

				memory.UnsafeWrite32(volMem + 16, 0);
				memory.UnsafeWrite32(volMem + 20, 0);
				memory.UnsafeWrite32(volMem + 24, 0);

				memory.UnsafeWrite32(volMem + 28, 0);
				memory.UnsafeWrite32(volMem + 32, ID_DOS_DISK);//0x4D594653);//MYFS (should be DOS0?)
				memory.UnsafeWrite32(volMem + 40, nameMem >> 2);

				myVolumeNodeBPTR = volMem >> 2;

				// walk ExecBase->libList to find dos.library
				uint execBase = memory.UnsafeRead32(4);
				uint libListHead = execBase + 378; // libList.lh_Head
				uint node = memory.UnsafeRead32(libListHead);
				uint dosBase = 0;

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

					logger.LogTrace($"{deviceName} Volume Node successfully injected into OS DosList!");
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
				//regs.SR = memory.UnsafeRead16(regs.SSP); regs.SSP += 2;
				//regs.PC = memory.UnsafeRead32(regs.SSP); regs.SSP += 4;
				regs.PC = memory.UnsafeRead32(regs.SP); regs.SP += 4;
				cpu.SetRegs(regs);
				return false;
			}

			if (deviceIsDead)
			{
				logger.LogTrace("DEAD DEVICE - RECEIVE MSG IGNORED");
				memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
				memory.UnsafeWrite32(regs.A[4] + 16, 0);
				regs.PC = memory.UnsafeRead32(regs.SP); regs.SP += 4;
				cpu.SetRegs(regs);
				return false;
			}

			//dos packet is in A4
			var pkt = new DosPacket();
			objectMapper.Deserialize(regs.A[4], pkt);
			uint typ = memory.UnsafeRead32(regs.A[4] + 8);

			//sanity check the MapObject
			if (typ != pkt.dp_Type)
				throw new ArgumentException($"MAPPING packet type mismatch {typ} {pkt.dp_Type}");

			switch (pkt.dp_Type)
			{
				case ACTION_INHIBIT:
					{
						logger.LogTrace($"ACTION_INHIBIT {pkt.dp_Arg1}");
						memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_DISK_INFO://aka ACTION_HANDLER_INFO
					{
						logger.LogTrace($"ACTION_DISK_INFO");
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

						//the hard way


						memory.UnsafeWrite32(address, 0); address += 4;
						memory.UnsafeWrite32(address, unitNo); address += 4;
						memory.UnsafeWrite32(address, ID_VALIDATED); address += 4;//ID_VALIDATED); address += 4;
						memory.UnsafeWrite32(address, 0x40000000 / 512); address += 4;//1GB
						memory.UnsafeWrite32(address, 0x20000000 / 512); address += 4;//half used
						memory.UnsafeWrite32(address, 512); address += 4;//512 byte blocks
						memory.UnsafeWrite32(address, ID_DOS_DISK); address += 4;//MYFS        0x4A414D4D);0x4D594653 address += 4;//JAMM
						memory.UnsafeWrite32(address, myVolumeNodeBPTR); address += 4;
						memory.UnsafeWrite32(address, 0); address += 4;

						////the easy way
						//var t = new InfoData();
						//t.id_NumSoftErrors = 0;
						//t.id_UnitNumber = 0;
						//t.id_DiskState = 2;
						//t.id_NumBlocks = 0x40000000 / 512;
						//t.id_NumBlocksUsed = 0;
						//t.id_BytesPerBlock = 512;
						//t.id_DiskType = 0x4D594653;
						//t.id_VolumeNode = myVolumeNodeBPTR;
						//t.id_InUse = 0;
						//var c = ObjectWalk.Walk(t);
						//logger.LogTrace(c);
						//var w = ObjectWalk.Walk2(t);

						memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_INFO:
					{
						logger.LogTrace($"ACTION_INFO {pkt.dp_Arg1:X8} {pkt.dp_Arg2 << 2:X8}");

						uint address = (uint)pkt.dp_Arg2 << 2;//InfoData
						memory.UnsafeWrite32(address, 0); address += 4;
						memory.UnsafeWrite32(address, unitNo); address += 4;
						memory.UnsafeWrite32(address, ID_VALIDATED); address += 4;//ID_VALIDATED); address += 4;
						memory.UnsafeWrite32(address, 0x40000000 / 512); address += 4;//1GB
						memory.UnsafeWrite32(address, 0x20000000 / 512); address += 4;//half used
						memory.UnsafeWrite32(address, 512); address += 4;//512 byte blocks
						memory.UnsafeWrite32(address, ID_DOS_DISK); address += 4;//MYFS        0x4A414D4D); address += 4;//JAMM
						memory.UnsafeWrite32(address, myVolumeNodeBPTR); address += 4;
						memory.UnsafeWrite32(address, 0); address += 4;

						memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_LOCATE_OBJECT:
					{
						logger.LogTrace($"ACTION_LOCATE_OBJECT");
						string pathName = string.Empty;
						string parentPath = string.Empty;

						int mode = pkt.dp_Arg3;
						if (mode == -2)
							logger.LogTrace($"MODE SHARED_LOCK/ACCESS_READ");
						else if (mode == -1)
							logger.LogTrace("MODE EXCLUSIVE_LOCK/ACCESS_WRITE");
						else
						{
							logger.LogTrace("MODE UNKNOWN => ACCESS_READ");
							mode = -2;
						}

						var parent = LockFromBPtr(regs, pkt.dp_Arg1);
						if (parent != null)
							parentPath = parent.FullPath;
						
						//MyLockInfo parent = null;
						//if (pkt.dp_Arg1 != 0)
						//{
						//	var @lock = new FileLock();
						//	objectMapper.Deserialize((uint)pkt.dp_Arg1 << 2, @lock);
						//	logger.LogTrace($"LOCATE FAILING (parent) {@lock.fl_Key:X8}");

						//	if (locks.TryGetValue((uint)@lock.fl_Key, out parent))
						//	{
						//		logger.LogTrace($"FOUND {parent.FullPath}");
						//		parentPath = parent.FullPath;
						//	}
						//	else
						//	{
						//		logger.LogTrace("FOUND NOTHING");
						//	}
						//}

						uint namePtr = (uint)pkt.dp_Arg2 << 2;
						if (namePtr == 0)
						{
							logger.LogTrace($"LOCATE empty name");
						}
						else
						{
							pathName = ReadDOSString(namePtr);
							pathName = SanitiseAmigaPath(pathName);
							logger.LogTrace($"LOCATE {parentPath} {pathName}");
						}

						string searchPath = null;

						//SPECIAL CASE PATH NAMES '/' means go up one directory from parentPath
						//SPECIAL CASE PATH NAMES '//' means go up two directories from parentPath (etc)
						//SPECIAL CASE PATH NAMES ':' means relative to root directory

						if (pathName.StartsWith(':') || pathName.ToUpper().StartsWith($"{deviceName}:"))
							parentPath = string.Empty;

						while (pathName.StartsWith('/') && parentPath != null)
						{
							parentPath = AmigaParentPath(parentPath);
							pathName = pathName.Substring(1);
						}
						if (parentPath == null)
						{
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}

						searchPath = AmigaPathCombine(parentPath, pathName);

						//what to do here, with // inside the combined path
						//cd dir//dirsibling is definitely a thing
						while (searchPath != null && searchPath.Contains("//"))
						{
							int i = searchPath.IndexOf("//");
							string p0 = searchPath.Substring(0, i);
							string p1 = searchPath.Substring(i + 1);
							logger.LogTrace($"RELATIVE {searchPath} {p0} {p1}");
							searchPath = AmigaParentPath(p0);
							if (searchPath != null) searchPath += p1;
						}
						if (searchPath == null)
						{
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}

						string hostPath = MakeHostPath(searchPath);
						logger.LogTrace($"SEARCH {searchPath} {hostPath}");

						if (Path.Exists(hostPath))
						{
							uint mem = AllocMem(20, 0x10001);
							memory.UnsafeWrite32(mem, 0);
							memory.UnsafeWrite32(mem + 4, mem);
							memory.UnsafeWrite32(mem + 8, (uint)mode);
							memory.UnsafeWrite32(mem + 12, regs.A[3]);
							memory.UnsafeWrite32(mem + 16, myVolumeNodeBPTR);

							locks.Add(mem, new MyLockInfo { FullPath = searchPath, Size = 0, LockKey = mem/*, Parent = parent*/ });
							//WalkLocks();
							logger.LogTrace($"LOCATE lock {mem:X8}");

							memory.UnsafeWrite32(regs.A[4] + 12, mem / 4);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
							break;
						}

						logger.LogTrace($"PATH DOES NOT EXIST {hostPath}");

						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
					}
					break;

				case ACTION_COPY_DIR:
					{
						logger.LogTrace($"ACTION_COPY_DIR (AKA COPY_LOCK)");

						var lok = new FileLock();
						//objectMapper.Deserialize((uint)pkt.dp_Arg1 << 2, lok);

						//if (!locks.TryGetValue((uint)lok.fl_Key, out var parent))
						//{
						//	logger.LogTrace($"parent lock not found {lok.fl_Key:X8}");
						//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						//	break;
						//}
						var parent = LockFromBPtr(regs, pkt.dp_Arg1, lok);
						if (parent == null)
							break;

						//create a new lock, same place
						uint mem = AllocMem(20, 0x10001);
						memory.UnsafeWrite32(mem, 0);
						memory.UnsafeWrite32(mem + 4, mem);
						memory.UnsafeWrite32(mem + 8, (uint)lok.fl_Access);
						memory.UnsafeWrite32(mem + 12, lok.fl_Task.Address);
						memory.UnsafeWrite32(mem + 16, lok.fl_Volume);

						locks.Add(mem, new MyLockInfo { FullPath = parent.FullPath, Size = parent.Size, LockKey = mem/*, Parent = parent*/ });
						//WalkLocks();

						logger.LogTrace($"LOCATE lock {mem:X8}");

						memory.UnsafeWrite32(regs.A[4] + 12, mem / 4);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_EXAMINE_OBJECT:
					{
						logger.LogTrace($"ACTION_EXAMINE_OBJECT {pkt.dp_Arg1 << 2:X8}");

						//uint lockPtr = (uint)pkt.dp_Arg1 << 2;
						var lok = new FileLock();
						//objectMapper.Deserialize(lockPtr, lok);

						//logger.LogTrace($"{lok.fl_Link:X8}");
						//logger.LogTrace($"{lok.fl_Key:X8}");
						//logger.LogTrace($"{lok.fl_Access:X8}");
						//logger.LogTrace($"{lok.fl_Task.Address:X8}");
						//logger.LogTrace($"{lok.fl_Volume:X8}");

						string basePath = "";
						//if (!locks.TryGetValue((uint)lok.fl_Key, out var parent))
						//{
						//	logger.LogTrace($"parent lock not found {lok.fl_Key:X8}");
						//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						//	break;
						//}
						var parent = LockFromBPtr(regs, pkt.dp_Arg1, lok);
						if (parent == null)
							break;
						//else
						//{
						logger.LogTrace($"PATH \"{parent.FullPath}\" {parent.LockKey:X8}");
						basePath = parent.FullPath;
						//}
						logger.LogTrace($"PATH {basePath}");

						if (basePath == "")
						{
							logger.LogTrace("NO PATH");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}
						if (!Path.Exists(MakeHostPath(basePath)))
						{
							logger.LogTrace($"NO PATH HOST {MakeHostPath(basePath)}");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}

						MyDirCache dircach = null;
						if (Directory.Exists(MakeHostPath(basePath)))
						{
							dircach = new MyDirCache(MakeHostPath(basePath), logger);
							//overwrite any existing entry
							dircache[(uint)pkt.dp_Arg1 << 2] = dircach;
						}

						//var s = new FileInfoBlock();
						/*
						  0 public LONG fib_DiskKey { get; set; }
						  4 public LONG fib_DirEntryType { get; set; }
							[AmigaArraySize(108)]
						  8 public char[] fib_FileName { get; set; }
						116 public LONG fib_Protection { get; set; }
						120	public LONG fib_EntryType { get; set; }
						124	public LONG fib_Size { get; set; }
						128	public LONG fib_NumBlocks { get; set; }
						132	public DateStamp fib_Date { get; set; }
							[AmigaArraySize(80)]
						144	public char[] fib_Comment { get; set; }
							[AmigaArraySize(36)]
						224	public char[] fib_Reserved { get; set; }
						260
						*/

						var dirEntry = new MyDirCache.MyDirEntry { Name = basePath, IsDirectory = dircach != null };
						logger.LogTrace($"{dirEntry.Name} {dirEntry.Size} {(dirEntry.IsDirectory ? 'D' : 'F')}");

						uint fib = (uint)pkt.dp_Arg2 << 2;
						memory.UnsafeWrite32(fib + 4, (uint)(dirEntry.IsDirectory ? ST_USERDIR : ST_FILE));
						memory.UnsafeWrite32(fib + 120, (uint)(dirEntry.IsDirectory ? ST_USERDIR : ST_FILE));

						if (parent.FullPath.EndsWith(':'))
						{
							logger.LogTrace("IS ROOT");
							memory.UnsafeWrite32(fib + 4, ST_ROOT);
							memory.UnsafeWrite32(fib + 120, ST_ROOT);
						}

						memory.UnsafeWrite32(fib + 124, dirEntry.IsDirectory ? 0 : dirEntry.Size);

						memory.UnsafeWrite32(fib, (uint)lok.fl_Key);
						memory.UnsafeWrite32(fib + 116, 0);//rwed

						int days, minutes, ticks;
						DateTimeToAmiga(dirEntry.Stamp, out days, out minutes, out ticks);

						memory.UnsafeWrite32(fib + 132, (uint)days);
						memory.UnsafeWrite32(fib + 136, (uint)minutes);
						memory.UnsafeWrite32(fib + 140, (uint)ticks);

						uint s = fib + 8;
						string name = dirEntry.Name;
						int j = name.IndexOf(':');
						if (j != -1) name = name.Substring(j + 1);
						memory.UnsafeWrite8(s++, (byte)Math.Min(107, name.Length));
						for (int i = 0; i < Math.Min(107, name.Length); i++)
							memory.UnsafeWrite8(s++, (byte)name[i]);

						memory.UnsafeWrite32(fib + 144, 0);//no comment
						memory.UnsafeWrite32(fib + 128, (dirEntry.Size + 511) / 512);

						memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_EXAMINE_NEXT:
					{
						logger.LogTrace($"ACTION_EXAMINE_NEXT {pkt.dp_Arg1 << 2:X8}");

						uint lockPtr = (uint)pkt.dp_Arg1 << 2;
						var lok = new FileLock();
						objectMapper.Deserialize(lockPtr, lok);

						if (dircache.TryGetValue((uint)pkt.dp_Arg1 << 2, out var dircach2))
						{
							//if (dircach2.IsEmpty())
							//{
							//	logger.LogTrace("NO MORE");
							//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_NO_MORE_ENTRIES);
							//	break;
							//}

							var thing = dircach2.Next();
							if (thing == null)
							{
								logger.LogTrace("NO MORE FILES");
								memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
								memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
								break;
							}

							logger.LogTrace($"{thing.Name} {thing.Size} {(thing.IsDirectory ? 'D' : 'F')}");

							uint fib = (uint)pkt.dp_Arg2 << 2;
							memory.UnsafeWrite32(fib + 4, (uint)(thing.IsDirectory ? ST_USERDIR : ST_FILE));
							memory.UnsafeWrite32(fib + 120, (uint)(thing.IsDirectory ? ST_USERDIR : ST_FILE));
							memory.UnsafeWrite32(fib + 124, thing.IsDirectory ? 0 : thing.Size);

							memory.UnsafeWrite32(fib, (uint)lok.fl_Key);
							memory.UnsafeWrite32(fib + 116, 0);//rwed

							int days, minutes, ticks;
							DateTimeToAmiga(thing.Stamp, out days, out minutes, out ticks);

							memory.UnsafeWrite32(fib + 132, (uint)days); 
							memory.UnsafeWrite32(fib + 136, (uint)minutes);
							memory.UnsafeWrite32(fib + 140, (uint)ticks);

							uint s = fib + 8;
							memory.UnsafeWrite8(s++, (byte)Math.Min(107, thing.Name.Length));
							for (int i = 0; i < Math.Min(107, thing.Name.Length); i++)
								memory.UnsafeWrite8(s++, (byte)thing.Name[i]);

							memory.UnsafeWrite32(fib + 144, 0);//no comment
							memory.UnsafeWrite32(fib + 128, (thing.Size + 511) / 512);

							memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
							break;
						}

						logger.LogTrace($"dir cache not found {(uint)pkt.dp_Arg1 << 2:X8}");

						//no more files
						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, ERROR_NO_MORE_ENTRIES);
					}
					break;

				case ACTION_FREE_LOCK:
					{
						logger.LogTrace($"ACTION_FREE_LOCK {pkt.dp_Arg1 << 2:X8}");

						uint lockPtr = (uint)pkt.dp_Arg1 << 2;
						var lok = new FileLock();
						objectMapper.Deserialize(lockPtr, lok);

						var @lock = locks[lockPtr];
						logger.LogTrace($"FREE LOCK {@lock}");
						if (@lock.Freed) logger.LogTrace($"DOUBLE FREE LOCK {@lock}");
						FreeMem(lockPtr, 20);
						locks.Remove(lockPtr);
						//remove parent lock link
						//foreach (var l in locks.Values)
						//	if (l.Parent == @lock) l.Parent = null;
						//WalkLocks();
						//locks[lockPtr].Freed = true;

						memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_PARENT:
					{
						logger.LogTrace($"ACTION_PARENT {pkt.dp_Arg1 << 2:X8}");

						//uint lockPtr = (uint)pkt.dp_Arg1 << 2;
						var lok = new FileLock();
						//objectMapper.Deserialize(lockPtr, lok);

						//if (!locks.TryGetValue((uint)lok.fl_Key, out var child))
						//{
						//	logger.LogTrace($"parent lock not found {lok.fl_Key:X8}");
						//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						//	break;
						//}
						var child = LockFromBPtr(regs, pkt.dp_Arg1, lok);
						if (child == null)
							break;

						/*
						//at the root already?
						if (child.Parent != null)
						{
							uint mem = AllocMem(20, 0x10001);
							memory.UnsafeWrite32(mem, 0);
							memory.UnsafeWrite32(mem + 4, mem);
							memory.UnsafeWrite32(mem + 8, (uint)lok.fl_Access);
							memory.UnsafeWrite32(mem + 12, lok.fl_Task.Address);
							memory.UnsafeWrite32(mem + 16, lok.fl_Volume);

							if (child.Parent.Parent != null)
								locks.Add(mem, new MyLockInfo { FullPath = child.Parent.Parent.FullPath, Size = 0, LockKey = mem, Parent = child.Parent.Parent });
							else
								locks.Add(mem, new MyLockInfo { FullPath = ":", Size = 0, LockKey = mem, Parent = null });
							WalkLocks();

							logger.LogTrace($"LOCATE lock {mem:X8} {locks[mem].FullPath} {AmigaParentPath(child.FullPath)}");

							memory.UnsafeWrite32(regs.A[4] + 12, mem / 4);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
							break;
						}
						*/
						logger.LogTrace($"NO PARENT {child.FullPath} {AmigaParentPath(child.FullPath)}");

						string parentPath = AmigaParentPath(child.FullPath);
						if (parentPath != null)
						{ 
							uint mem = AllocMem(20, 0x10001);
							memory.UnsafeWrite32(mem, 0);
							memory.UnsafeWrite32(mem + 4, mem);
							memory.UnsafeWrite32(mem + 8, (uint)lok.fl_Access);
							memory.UnsafeWrite32(mem + 12, lok.fl_Task.Address);
							memory.UnsafeWrite32(mem + 16, lok.fl_Volume);

							locks.Add(mem, new MyLockInfo { FullPath = ":", Size = 0, LockKey = mem/*, Parent = null*/ });
							//WalkLocks();

							logger.LogTrace($"LOCATE NAME lock {mem:X8}");

							memory.UnsafeWrite32(regs.A[4] + 12, mem / 4);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
							break;
						}

						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
					}
					break;

				case ACTION_FINDINPUT:
				case ACTION_FINDOUTPUT:
				case ACTION_FINDUPDATE:
					{
						if (pkt.dp_Type == ACTION_FINDINPUT) logger.LogTrace($"ACTION_FINDINPUT {pkt.dp_Arg2 << 2:X8}");
						if (pkt.dp_Type == ACTION_FINDOUTPUT) logger.LogTrace($"ACTION_FINDOUTPUT {pkt.dp_Arg2 << 2:X8}");
						if (pkt.dp_Type == ACTION_FINDUPDATE) logger.LogTrace($"ACTION_FINDUPDATE {pkt.dp_Arg2 << 2:X8}");

						//Arg1 points to a FileHandle
						//Arg2 is the lock
						//Arg3 is the filename

						string basePath = "";

						//var @lock = new FileLock();
						//objectMapper.Deserialize((uint)pkt.dp_Arg2 << 2, @lock);

						//if (!locks.TryGetValue((uint)@lock.fl_Key, out var parent))
						//{
						//	logger.LogTrace($"parent lock not found {pkt.dp_Arg2 << 2:X8}");
						//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						//	break;
						//}
						var parent = LockFromBPtr(regs, pkt.dp_Arg2);
						if (parent == null)
							break;
						//else
						//{
						logger.LogTrace($"PATH \"{parent.FullPath}\" {parent.LockKey:X8}");
						basePath = parent.FullPath;
						//}

						uint bstrAddr = (uint)pkt.dp_Arg3 << 2;
						if (bstrAddr == 0)
						{
							logger.LogTrace("ACTION_FINDINPUT empty name");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}
						//else
						//{
						string pathName = ReadDOSString(bstrAddr);
						logger.LogTrace($"ACTION_FINDINPUT {pathName}");
						basePath = AmigaPathCombine(basePath, pathName);
						//}

						basePath = MakeHostPath(basePath);
						logger.LogTrace($"Looking for {basePath}");
						if (Path.Exists(basePath))
						{
							logger.LogTrace("FOUND");
							//need to fill in FileHandle at Arg1
							/*
							var x = new FileHandle();
							public MessagePtr fh_Link { get; set; }
							public MsgPortPtr fh_Port { get; set; }
							public MsgPortPtr fh_Type { get; set; }
							public LONG fh_Buf { get; set; }
							public LONG fh_Pos { get; set; }
							public LONG fh_End { get; set; }
							public LONG fh_Funcs { get; set; }
							public LONG fh_Func2 { get; set; }
							public LONG fh_Func3 { get; set; }
							public LONG fh_Args { get; set; }
							public LONG fh_Arg2 { get; set; }
							*/

							var fi = new MyFileInfo();

							FileAccess access = FileAccess.Read;
							FileMode mode = FileMode.Open;
							if (pkt.dp_Type == ACTION_FINDINPUT) { mode = FileMode.Open; access = FileAccess.Read; }
							if (pkt.dp_Type == ACTION_FINDOUTPUT) { mode = FileMode.Create; access = FileAccess.ReadWrite; }
							if (pkt.dp_Type == ACTION_FINDUPDATE) { mode = FileMode.OpenOrCreate; access = FileAccess.ReadWrite; }

							try
							{
								fi.stream = File.Open(basePath, mode, access, FileShare.Read);
								uint id = UniqueFileId();
								files.Add(id, fi);

								//set fh_Pos/fh_End = -1
								uint address = (uint)pkt.dp_Arg1 << 2;
								memory.UnsafeWrite32(address + 16, 0xffffffff);
								memory.UnsafeWrite32(address + 20, 0xffffffff);
								memory.UnsafeWrite32(address + 36, id);

								memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
								memory.UnsafeWrite32(regs.A[4] + 16, 0);
							}
							catch (Exception ex)
							{
								logger.LogTrace($"Exception: {ex}");
								memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
								memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							}
							break;
						}
						else if (pkt.dp_Type == ACTION_FINDOUTPUT)
						{
							logger.LogTrace($"NEW {pkt.dp_Arg1 << 2:X8}");
							var fi = new MyFileInfo();

							FileAccess access = FileAccess.ReadWrite;
							FileMode mode = FileMode.Create;
							////if (pkt.dp_Type == ACTION_FINDINPUT) { mode = FileMode.Open; access = FileAccess.Read; }
							//if (pkt.dp_Type == ACTION_FINDOUTPUT) { mode = FileMode.Create; access = FileAccess.ReadWrite; }
							////if (pkt.dp_Type == ACTION_FINDUPDATE) { mode = FileMode.Open; access = FileAccess.ReadWrite; }

							fi.stream = File.Open(basePath, mode, access, FileShare.Read);
							uint id = UniqueFileId();
							files.Add(id, fi);

							//set fh_Pos/fh_End = -1
							uint address = (uint)pkt.dp_Arg1 << 2;
							memory.UnsafeWrite32(address + 16, 0xffffffff);
							memory.UnsafeWrite32(address + 20, 0xffffffff);
							memory.UnsafeWrite32(address + 36, id);

							memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
							break;
						}

						logger.LogTrace("NOT FOUND");
						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
					}
					break;

				case ACTION_READ:
					{
						logger.LogTrace($"ACTION_READ {pkt.dp_Arg1} {pkt.dp_Arg2:X8} {pkt.dp_Arg3}");
						if (!files.TryGetValue((uint)pkt.dp_Arg1, out var file))
						{
							logger.LogTrace("CAN'T FIND FILE");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}
						logger.LogTrace("READ FILE");
						var tmp = new byte[pkt.dp_Arg3];
						uint r = (uint)file.stream.Read(tmp, 0, pkt.dp_Arg3);
						for (uint i = 0; i < r; i++)
							memory.UnsafeWrite8((uint)pkt.dp_Arg2 + i, tmp[i]);
						logger.LogTrace($"READ {r}");
						memory.UnsafeWrite32(regs.A[4] + 12, r);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_WRITE:
					{
						logger.LogTrace($"ACTION_WRITE {pkt.dp_Arg1} {pkt.dp_Arg2:X8} {pkt.dp_Arg3}");
						if (!files.TryGetValue((uint)pkt.dp_Arg1, out var file))
						{
							logger.LogTrace("CAN'T FIND FILE");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}
						var tmp = new byte[pkt.dp_Arg3];
						for (uint i = 0; i < pkt.dp_Arg3; i++)
							tmp[i] = memory.UnsafeRead8((uint)pkt.dp_Arg2 + i);
						file.stream.Write(tmp, 0, pkt.dp_Arg3);

						memory.UnsafeWrite32(regs.A[4] + 12, (uint)pkt.dp_Arg3);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_SEEK:
					{
						logger.LogTrace($"ACTION_SEEK {pkt.dp_Arg1} {pkt.dp_Arg2} {pkt.dp_Arg3}");
						if (!files.TryGetValue((uint)pkt.dp_Arg1, out var file))
						{
							logger.LogTrace("CAN'T FIND FILE");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}

						var origin = pkt.dp_Arg3 == -1 ? SeekOrigin.Begin :
									(pkt.dp_Arg3 == 1 ? SeekOrigin.End : SeekOrigin.Current);

						file.stream.Seek(pkt.dp_Arg2, origin);

						memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_END:
					{
						logger.LogTrace($"ACTION_END {pkt.dp_Arg1}");
						if (!files.TryGetValue((uint)pkt.dp_Arg1, out var file))
						{
							logger.LogTrace("CAN'T FIND FILE");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
							break;
						}
						file.stream.Close();
						file.stream.Dispose();
						files.Remove((uint)pkt.dp_Arg1);

						memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
					}
					break;

				case ACTION_DELETE_OBJECT:
					{
						logger.LogTrace($"ACTION_DELETE_OBJECT {pkt.dp_Arg1 << 2:X8}");

						string filename = string.Empty;

						//uint lockPtr = (uint)pkt.dp_Arg1 << 2;
						//var lok = new FileLock();
						//objectMapper.Deserialize(lockPtr, lok);

						//if (!locks.TryGetValue((uint)lok.fl_Key, out var parent))
						//{
						//	logger.LogTrace($"parent lock not found {lok.fl_Key:X8}");
						//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						//	break;
						//}
						var parent = LockFromBPtr(regs, pkt.dp_Arg1);
						if (parent == null)
							break;
						//else
						//{
						logger.LogTrace($"DELETE {parent.FullPath}");

						filename = AmigaPathCombine(parent.FullPath, ReadDOSString((uint)pkt.dp_Arg2 << 2));
						logger.LogTrace($"{filename}");
						logger.LogTrace($"{MakeHostPath(filename)}");
						//}

						try
						{
							string hostPath = MakeHostPath(filename);
							if (File.Exists(hostPath))
							{
								try
								{ 
									File.Delete(hostPath);
								}
								catch (IOException)
								{
									memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
									memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_IN_USE);
									break;
								}
							}
							else if (Directory.Exists(hostPath))
							{
								try
								{ 
									Directory.Delete(hostPath);
								}
								catch (IOException)
								{
									memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
									memory.UnsafeWrite32(regs.A[4] + 16, ERROR_DIRECTORY_NOT_EMPTY);
									break;
								}
							}
							else
							{
								memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
								memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
								break;
							}
							memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
						}
						catch (Exception ex)
						{
							logger.LogTrace($"Exception: {ex}");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						}
					}
					break;

				case ACTION_CREATE_DIR:
					{
						logger.LogTrace($"ACTION_CREATE_DIR {pkt.dp_Arg1 << 2:X8}");

						string filename = string.Empty;

						//uint lockPtr = (uint)pkt.dp_Arg1 << 2;
						var lok = new FileLock();
						//objectMapper.Deserialize(lockPtr, lok);

						//if (!locks.TryGetValue((uint)lok.fl_Key, out var parent))
						//{
						//	logger.LogTrace($"parent lock not found {lok.fl_Key:X8}");
						//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						//	break;
						//}
						var parent = LockFromBPtr(regs, pkt.dp_Arg1, lok);
						if (parent == null)
							break;
						//else
						//{
						logger.LogTrace($"CREATE DIR {parent.FullPath}");

						filename = AmigaPathCombine(parent.FullPath, ReadDOSString((uint)pkt.dp_Arg2 << 2));
						logger.LogTrace($"{filename}");
						logger.LogTrace($"{MakeHostPath(filename)}");
						//}

						try
						{
							string hostPath = MakeHostPath(filename);
							if (Path.Exists(hostPath))
							{
								memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
								memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_EXISTS);
								break;
							}

							Directory.CreateDirectory(hostPath);

							uint mem = AllocMem(20, 0x10001);
							memory.UnsafeWrite32(mem, 0);
							memory.UnsafeWrite32(mem + 4, (uint)lok.fl_Key);
							memory.UnsafeWrite32(mem + 8, (uint)lok.fl_Access);
							memory.UnsafeWrite32(mem + 12, lok.fl_Task.Address);
							memory.UnsafeWrite32(mem + 16, lok.fl_Volume);

							locks.Add(mem, new MyLockInfo { FullPath = filename, Size = 0, LockKey = mem/*, Parent = parent*/ });
							//WalkLocks();

							logger.LogTrace($"LOCATE lock {mem:X8}");

							memory.UnsafeWrite32(regs.A[4] + 12, mem / 4);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
						}
						catch (Exception ex)
						{
							logger.LogTrace($"Exception: {ex}");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						}
					}
					break;

				case ACTION_RENAME_OBJECT:
					{
						logger.LogTrace($"ACTION_RENAME_OBJECT {pkt.dp_Arg1 << 2:X8}");

						string filename = string.Empty;
						string dstFilename = string.Empty;

						//uint lockPtr = (uint)pkt.dp_Arg1 << 2;
						//var lok = new FileLock();
						//objectMapper.Deserialize(lockPtr, lok);


						//uint lockDst = (uint)pkt.dp_Arg3 << 2;
						//var lokDst = new FileLock();
						//objectMapper.Deserialize(lockDst, lokDst);

						//if (!locks.TryGetValue((uint)lok.fl_Key, out var parent))
						//{
						//	logger.LogTrace($"parent lock not found {lok.fl_Key:X8}");
						//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						//	break;
						//}
						var parent = LockFromBPtr(regs, pkt.dp_Arg1);
						if (parent == null)
							break;
						//else
						//{
						logger.LogTrace($"RENAME FROM {parent.FullPath}");

						filename = AmigaPathCombine(parent.FullPath, ReadDOSString((uint)pkt.dp_Arg2 << 2));
						logger.LogTrace($"{filename}");
						logger.LogTrace($"{MakeHostPath(filename)}");
						//}

						//if (!locks.TryGetValue((uint)lokDst.fl_Key, out var parent2))
						//{
						//	logger.LogTrace($"parent lock not found {lokDst.fl_Key:X8}");
						//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						//	break;
						//}
						var parent2 = LockFromBPtr(regs, pkt.dp_Arg3);
						if (parent2 == null)
							break;
						//else
						//{
						logger.LogTrace($"RENAME TO {parent2.FullPath}");

						dstFilename = AmigaPathCombine(parent2.FullPath, ReadDOSString((uint)pkt.dp_Arg4 << 2));
						logger.LogTrace($"{dstFilename}");
						logger.LogTrace($"{MakeHostPath(dstFilename)}");
						//}

						try
						{
							if (File.Exists(MakeHostPath(filename)))
								File.Move(MakeHostPath(filename), MakeHostPath(dstFilename));
							else if (Directory.Exists(MakeHostPath(filename)))
								Directory.Move(MakeHostPath(filename), MakeHostPath(dstFilename));
							memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
						}
						catch (Exception ex)
						{
							logger.LogTrace($"Exception: {ex}");
							memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
							memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						}
					}
					break;

				case ACTION_SAME_LOCK:
					logger.LogTrace($"ACTION_SAME_LOCK");
					var lock1 = LockFromBPtr(regs, pkt.dp_Arg1);
					var lock2 = LockFromBPtr(regs, pkt.dp_Arg2);
					if (lock1 == null || lock2 == null)
					{
						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
						break;
					}
					if (lock1.FullPath.ToUpper() != lock2.FullPath.ToUpper())
					{
						memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						break;
					}
					memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
					memory.UnsafeWrite32(regs.A[4] + 16, 0);
					break;

				case ACTION_SET_PROTECT:
					logger.LogTrace($"ACTION_SET_PROTECT");
					memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
					memory.UnsafeWrite32(regs.A[4] + 16, 0);
					break;

				case ACTION_SET_COMMENT:
					logger.LogTrace($"ACTION_SET_COMMENT");
					memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
					memory.UnsafeWrite32(regs.A[4] + 16, 0);
					break;

				case ACTION_SET_DATE:
					logger.LogTrace($"ACTION_SET_DATE");
					memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
					memory.UnsafeWrite32(regs.A[4] + 16, 0);
					break;

				case ACTION_FLUSH:
				case ACTION_MORE_CACHE:
				case ACTION_IS_FILESYSTEM:
					logger.LogTrace($"ACTION_FLUSH/ACTION_MORE_CACHE/ACTION_IS_FILESYSTEM");
					memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
					memory.UnsafeWrite32(regs.A[4] + 16, 0);
					break;

				case ACTION_DIE:
					logger.LogTrace($"ACTION_DIE");
					//close all open files
					foreach (var file in files.Values)
					{
						try
						{ 
							file.stream.Close();
							file.stream.Dispose();
						}
						catch { /* ignore any errors */ }
					}
					files.Clear();
					deviceIsDead = true;
					memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
					memory.UnsafeWrite32(regs.A[4] + 16, 0);
					break;

				case > ACTION_MAXIMUM_VALUE:

					logger.LogTrace($"ACTION_IGNORED** {pkt.dp_Type} {pkt.dp_Type:X8} {pkt.dp_Type << 2:X8}");

					//possible mapping for first message
					//var f = new FileSysStartupMsg();
					//objectMapper.Deserialize(regs.A[4], f);

					break;

				default:
					logger.LogTrace($"ACTION_UNHANDLED** {pkt.dp_Type} {pkt.dp_Type:X8} {pkt.dp_Type << 2:X8}");

					memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
					memory.UnsafeWrite32(regs.A[4] + 16, ERROR_ACTION_NOT_KNOWN);
					break;
			}

			//return back to emulation
			//regs.SR = memory.UnsafeRead16(regs.SSP); regs.SSP += 2;
			//regs.PC = memory.UnsafeRead32(regs.SSP); regs.SSP += 4;
			regs.PC = memory.UnsafeRead32(regs.SP); regs.SP += 4;
			cpu.SetRegs(regs);

			return false;
		}

		private string AmigaPathCombine(string root, string fragment)
		{
			if (root == string.Empty) return fragment;
			if (root.EndsWith(':'))
				return root + fragment;
			if (fragment == string.Empty) return root;
			return root + '/' + fragment;
		}

		private string AmigaParentPath(string path)
		{
			if (path.EndsWith(':'))
			{
				logger.LogTrace("PARENT OF ROOT");
				return null;
			}

			int i;
			i = path.LastIndexOf('/');
			if (i != -1)
				return path.Substring(0, i);
			i = path.LastIndexOf(':');
			if (i != -1)
				return path.Substring(0, i + 1);

			logger.LogTrace($"NO PARENT OF {path}");
			return null;
		}

		//private string PathFromLock(int arg)
		//{
		//	if (arg != 0)
		//	{
		//		var @lock = new FileLock();
		//		objectMapper.Deserialize((uint)arg << 2, @lock);
		//		logger.LogTrace($"LOCATE FAILING (parent) {@lock.fl_Key:X8}");

		//		if (locks.TryGetValue((uint)@lock.fl_Key, out var ll))
		//		{
		//			logger.LogTrace($"FOUND {ll.FullPath}");
		//			return ll.FullPath;
		//		}
		//		else
		//		{
		//			logger.LogTrace("FOUND NOTHING");
		//		}
		//	}
		//	return null;
		//}

		//private MyLockInfo LockFromKey(Regs regs, int key)
		//{
		//	if (locks.TryGetValue((uint)key, out var parent))
		//		return parent;

		//	logger.LogTrace($"parent lock not found {key:X8}");
		//	memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
		//	memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
		//	return null;
		//}

		private MyLockInfo LockFromBPtr(Regs regs, int arg)
		{
			var @lock = new FileLock();
			return LockFromBPtr(regs, arg, @lock);
		}

		private MyLockInfo LockFromBPtr(Regs regs, int arg, FileLock @lock)
		{
			if (arg == 0)
			{
				logger.LogTrace("Lock 0");

				memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
				memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
				return null;
			}

			objectMapper.Deserialize((uint)arg << 2, @lock);

			if (locks.TryGetValue((uint)@lock.fl_Key, out var parent))
				return parent;

			logger.LogTrace($"Lock not found {(uint)arg << 2:X8} {@lock.fl_Key:X8}");

			memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
			memory.UnsafeWrite32(regs.A[4] + 16, ERROR_OBJECT_NOT_FOUND);
			return null;
		}

		//private void WalkLocks()
		//{
		//	logger.LogTrace($"WALK LOCKS {locks.Count}");
		//	var childs = new Dictionary<MyLockInfo, List<MyLockInfo>>();

		//	var children = new List<MyLockInfo>();
		//	var orphans = locks.Values.ToList();

		//	int maxloops = orphans.Count;

		//	while (maxloops-- > 0)
		//	{
		//		children.AddRange(orphans);
		//		orphans.Clear();

		//		foreach (var my in children)
		//		{
		//			if (my.Parent == null)
		//			{
		//				childs.Add(my, new List<MyLockInfo>());
		//			}
		//			else if (childs.TryGetValue(my.Parent, out var p))
		//			{
		//				p.Add(my);
		//				childs.Add(my, new List<MyLockInfo>());
		//			}
		//			else
		//			{
		//				orphans.Add(my);
		//			}
		//		}
		//		if (orphans.Count == 0) break;
		//		children.Clear();
		//	}

		//	logger.LogTrace("LOCK TREE");
		//	foreach (var root in childs.Where(x => x.Key.Parent == null))
		//		WalkLocks2(root, 0);
		//	foreach (var orp in orphans)
		//		logger.LogTrace($"ORPHAN {orp} Parent: {(orp.Parent != null ? orp.Parent : "")}");

		//	void WalkLocks2(KeyValuePair<MyLockInfo, List<MyLockInfo>> kvp, int depth)
		//	{
		//		logger.LogTrace($"{new string(' ', depth * 2)} {kvp.Key}");
		//		foreach (var c in kvp.Value)
		//			WalkLocks2(new KeyValuePair<MyLockInfo, List<MyLockInfo>>(c, childs[c]), depth + 1);
		//	}
		//}

		private uint uniqueFileId = 1;
		private uint UniqueFileId()
		{
			return uniqueFileId++;
		}

		private uint AllocMem(uint size, uint flags)
		{
			//D0,D1
			var regs = @$"
							move.l  #{size},d0
							move.l  #{flags},d1";

			return CallExec(-198, regs);
		}

		private uint FreeMem(uint ptr, uint size)
		{
			//A1, D0
			var regs = @$"
							move.l  #{ptr},a1
							move.l  #{size},d0";

			return CallExec(-210, regs);
		}

		private string ReadDOSString(uint namePtr)
		{
			var sb = new StringBuilder();
			byte l = memory.UnsafeRead8(namePtr++);

			while (l-- != 0)
			{
				byte b = memory.UnsafeRead8(namePtr++);
				sb.Append((char)b);
			}
			return sb.ToString();
		}

		// Amiga file date epoch January 1, 1978
		private static readonly DateTime AmigaEpoch = new DateTime(1978, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		//nb. 60 on NTSC
		private const int TicksPerSecond = 50;

		public static DateTime AmigaToDateTime(int days, int minute, int tick)
		{
			long totalSeconds = (days * 86400L) + (minute * 60L) + (tick / TicksPerSecond);
			return AmigaEpoch.AddSeconds(totalSeconds).ToLocalTime();
		}

		public static void DateTimeToAmiga(DateTime dateTime, out int days, out int minute, out int tick)
		{
			DateTime utcDateTime = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
			TimeSpan span = utcDateTime - AmigaEpoch;

			long totalSeconds = (long)span.TotalSeconds;
			if (totalSeconds < 0) totalSeconds = 0;

			days = (int)(totalSeconds / 86400);
			long remainingSeconds = totalSeconds % 86400;
			minute = (int)(remainingSeconds / 60);

			long subMinuteSeconds = remainingSeconds % 60;
			tick = (int)(subMinuteSeconds * TicksPerSecond);
		}

		//assuming this is extremely unsafe (interrupts, locks etc), but here we go
		//we're inside a call to the expansion ROM from dos.library, and inside a trap handler, so it can't be that bad
		private uint CallExec(int lvo, string parms)
		{
			string asm =
				$@"
					move.l #0,-(sp)
					move.l  $4,a6
				"
					+ parms +
				$@"
					jmp {lvo}(a6)
				";
			var r = assembler.Assemble(asm);

			//we know this space (copy of DiagArea) is unused after expansion.library is finished with it
			uint i = configuration.BaseAddress;
			if (r.Program == null || r.Program.Length > 0x20)
				throw new ArgumentOutOfRangeException("Program too long");

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
