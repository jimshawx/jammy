using Jammy.AmigaTypes;
using Jammy.Assembler;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Memory;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;
using Jammy.Extensions.Extensions;
using Jammy.Interface;
using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.Security.Cryptography;
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
		private readonly IAssembler assembler;
		private readonly ILogger<TestExpansion> logger;

		public readonly string serial;
		private readonly string volumeName;

		public DosExpansion(IAssembler assembler, ILogger<TestExpansion> logger, string serial, string volumeName)
		{
			this.assembler = assembler;
			this.logger = logger;
			this.serial = serial;
			this.volumeName = volumeName;
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
			string asm = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DosExpansion.s"), Encoding.UTF8);
			asm = asm.Replace("MYDEV", volumeName);
			int unitNo = volumeName[volumeName.Length-1] - '0';
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
		private string volumeName;
		private readonly IObjectMapper objectMapper;
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memory;
		private readonly IAssembler assembler;
		private ZorroConfiguration configuration;
		private readonly string rootDir;
		private readonly uint unitNo;

		public DosExpansionDebugHandler(IDebugger debugger, IZorroExpansionRegistry registry,
			IObjectMapper objectMapper, ICPU cpu, IDebugMemoryMapper memory, IAssembler assembler,
			ILogger<DosExpansionDebugHandler> logger, string serial, string volumeName, string rootDir, int unitNo) : base(registry, serial)
		{
			this.debugger = debugger;
			this.logger = logger;
			this.serial = serial;
			this.volumeName = volumeName.ToUpper();
			this.objectMapper = objectMapper;
			this.cpu = cpu;
			this.memory = memory;
			this.assembler = assembler;
			this.rootDir = Paths.GetCandidateDirectory(rootDir, "");
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
			public bool Freed { get; set; }

			public override string ToString()
			{
				return $"{LockKey:X8} {Size:X8} {FullPath} {(Freed ? "FREE" : "")}";
			}
		}

		private class MyFileInfo
		{
			public Stream stream;
		}

		private string UnwindExtraSlashes(string searchPath)
		{
			//what to do here, with // or /// etc inside the combined path
			//cd dir//dirsibling is definitely a thing
			//remove one level of hierarchy at a time
			while (searchPath != null && searchPath.Contains("//"))
			{
				int i = searchPath.IndexOf("//");
				string p0 = searchPath.Substring(0, i);
				string p1 = searchPath.Substring(i + 1);
				logger.LogTrace($"RELATIVE {searchPath} {p0} {p1}");
				searchPath = AmigaParentPath(p0);
				if (searchPath != null) searchPath += p1;
			}
			return searchPath;
		}

		//turn an Amiga path like df0:dir1/dir2 into a host path, like c:\source\jammy\hd\dir1\dir2
		private string MakeHostPath(string amigaPath)
		{
			string fromPath = amigaPath;

			amigaPath = UnwindExtraSlashes(amigaPath);
			if (amigaPath == null) return null;

			//the bit up to the last colon
			int t = amigaPath.LastIndexOf(':');
			if (t != -1)
			{
				amigaPath = amigaPath.Substring(t + 1);
			}
			
			char[] badChars = { '\\', '*', '?', '"', '<', '>', '|', ':' };
			foreach (var c in badChars)
			{
				if (amigaPath.Contains(c))
					return null;
			}

			//need to search the directory caches for
			//the amiga filename to get the host filename

			logger.LogTrace($"AMIGA PATH {amigaPath}");

			//start with the Host root path from the settings
			string hostPathBits = rootDir;
			string[] amigaBits = amigaPath.Split('/');
			foreach (string amigaBit in amigaBits)
			{
				var dc = new MyDirCache(hostPathBits, logger);

				string hostBit = dc.AmigaToHost(amigaBit);
				if (hostBit == null)
					hostPathBits = Path.Combine(hostPathBits, amigaBit);
				else
					hostPathBits = Path.Combine(hostPathBits, hostBit);
			}

			amigaPath = hostPathBits;
			logger.LogTrace($"->HOST PATH {amigaPath}");

			//path would escape the sandbox?
			if (IsSandboxEscape(amigaPath))
				return null;

			logger.LogTrace($"{fromPath}->{amigaPath}");

			return amigaPath;
		}

		private bool IsSandboxEscape(string combinedPath)
		{
			string basePath = Path.GetFullPath(rootDir);

			if (!basePath.EndsWith(Path.DirectorySeparatorChar))
				basePath += Path.DirectorySeparatorChar;

			string resolvedPath = Path.GetFullPath(combinedPath);

			if (!resolvedPath.EndsWith(Path.DirectorySeparatorChar))
				resolvedPath += Path.DirectorySeparatorChar;

			return !resolvedPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
		}

		private string SanitiseAmigaPath(string amigaPath)
		{
			amigaPath = amigaPath.Replace('\\', '_');
			return amigaPath;
		}

		private class MyDirCache
		{
			public MyDirCache(string hostPath, ILogger logger)
			{
				try
				{ 
					logger.LogTrace($"DirCache {hostPath}");

					var dirs = Directory.GetDirectories(hostPath);
					var fils = Directory.GetFiles(hostPath);

					foreach (var d in dirs)
					{
						//logger.LogTrace($"D {d}");
						dirEntries.Add(new MyDirEntry { HostName = MungeName(d), IsDirectory = true, Stamp = DirDate(d) });
					}
					foreach (var f in fils)
					{
						//logger.LogTrace($"F {f}");
						dirEntries.Add(new MyDirEntry { HostName = MungeName(f), IsDirectory = false, Size = FileLen(f), Stamp = FileDate(f) });
					}
					BuildDirMap();
				}
				catch (Exception ex)
				{
					logger.LogTrace($"{ex}");
					logger.LogTrace($"Can't build dir cache {hostPath}");
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
				return f.CreationTimeUtc.ToLocalTime();
			}

			private DateTime DirDate(string p)
			{
				var f = new DirectoryInfo(p);
				return f.CreationTimeUtc.ToLocalTime();
			}

			private string MungeName(string p)
			{
				p = Path.GetFileName(p);
				//p = p.Replace('\\', '/');
				//p = p.Length <= 30 ? p : p.Substring(0, 30);
				return p;
			}

			private static byte[] ToAmigaFileNameBytes(string name)
			{
				byte[] bytes = Encoding.Latin1.GetBytes(name);

				foreach (ref byte b in bytes.AsSpan())
					if (b is (byte)'?' or (byte)':' or < 0x20 or (>= 0x7F and <= 0x9F))
						b = (byte)'_';

				return bytes;
			}

			private static string ToAmigaFileName(string name) => Encoding.Latin1.GetString(ToAmigaFileNameBytes(name));

			private const string B32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

			private const int HashLen = 4;		// 5 bits per character; 32 bits of hash used, so up to 6 fits in the uint
			private const int TailMax = 10;		// maximum size of the kept tail
			private const int HeadMin = 7;		// minimum size of the kept head

			private static string HashStr(string hostName)
			{
				byte[] d = SHA1.HashData(Encoding.UTF8.GetBytes(hostName));
				uint v = BinaryPrimitives.ReadUInt32LittleEndian(d);
				var sb = new StringBuilder(HashLen);
				for (int i = 0; i < HashLen; i++) { sb.Append(B32[(int)(v & 31)]); v >>= 5; }
				return sb.ToString();
			}

			private static string ToAmigaName(string hostName, out bool mangled, int limit = 30)
			{
				//remove any invalid characters
				string s = ToAmigaFileName(hostName);

				//short enough to fit?
				mangled = s != hostName;
				if (s.Length < limit) return s;

				//too long, insert a hash token in the filename
				mangled = true;

				int dot = s.LastIndexOf('.');
				string ext = (dot > 0 && s.Length - dot <= 8) ? s.Substring(dot) : "";
				string stem = s.Substring(0, s.Length - ext.Length);

				int budget = limit - ext.Length - (1 + HashLen); // "~" + hash
				int tailLen = Math.Max(0, Math.Min(TailMax, budget - HeadMin));
				int headLen = budget - tailLen;

				string kept;
				if (stem.Length <= budget)
					kept = stem;
				else if (tailLen == 0)
					kept = stem.Substring(0, headLen);
				else
					kept = stem.Substring(0, headLen-1) + "-" + stem.Substring(stem.Length - tailLen);

				return kept + "~" + HashStr(hostName) + ext;
			}

			private void BuildDirMap()
			{
				//every file in dirEntries needs to get put in nameMapping
				//they must be unique, less than 30 long, no invalid characters, and case insensitive
				
				var distinct = dirEntries.Select(x=>
					{ 
						string amigaName = ToAmigaName(x.HostName, out _);
						return new { hostName = x.HostName, hashed = amigaName.ToLower(), caseHashed = amigaName };
					})
					.GroupBy(x=>x.hashed)
					.ToDictionary(x=>x.Key, x=>x.Select(z=> new { hostName = z.hostName, caseHashed = z.caseHashed } ).ToList());

				//the indistinct ones will be those hashed names that map to multiple files
				//the only way that happens is if two names were the same after hashing and lower case
				//so the only thing to do is to separate them by forcibly hashing them
				foreach (var tieBreak in distinct.Where(x => x.Value.Count > 1).ToList())
				{
					//remove the original hashed name
					distinct.Remove(tieBreak.Key);

					//for each filename that matched to the non-unique key, re-key them (force them to hash)
					foreach (var tie in tieBreak.Value)
					{
						if (!distinct.TryAdd(ToAmigaName(tie.hostName, out _, int.MaxValue).ToLower(), [ tie ]))
							throw new InvalidOperationException($"Filename HASH Collision {tie}");
					}
				}
				//should now be totally distinct

				var hostToAmiga = new Dictionary<string, string>();
				foreach (var f in distinct)
				{ 
					amigaToHost.Add(f.Key, f.Value.Single().hostName);
					hostToAmiga.Add(f.Value.Single().hostName, f.Value.Single().caseHashed);
				}

				foreach (var entry in dirEntries)
					entry.AmigaName = hostToAmiga[entry.HostName];
			}

			public class MyDirEntry
			{
				public string AmigaName { get; set; }
				public string HostName { get; set; }
				public bool IsDirectory { get; set; }
				public uint Size { get; set; }
				public DateTime Stamp { get; set; }
			}

			private readonly List<MyDirEntry> dirEntries = new List<MyDirEntry>();
			private readonly Dictionary<string, string> amigaToHost = new Dictionary<string, string>();

			public MyDirEntry Next()
			{
				if (dirEntries.Count == 0) return null;
				var d = dirEntries[0];
				dirEntries.RemoveAt(0);
				return d;
			}

			public string AmigaToHost(string amiga)
			{
				if (amigaToHost.TryGetValue(amiga.ToLower(), out string host))
					return host;
				return null;
			}
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

		private const int ERROR_OBJECT_LINKED = 234;
		private const int ERROR_BAD_HUN = 235;
		private const int ERROR_NOT_IMPLEMENTED = 236;
		private const int ERROR_RECORD_NOT_LOCKED = 240;
		private const int ERROR_LOCK_COLLISION = 241;
		private const int ERROR_LOCK_TIMEOUT = 242;
		private const int ERROR_UNLOCK_ERROR = 243;

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

		private readonly Dictionary<int, MyDirCache> dircache = new Dictionary<int, MyDirCache>();

		public override void Reset()
		{
			volumeLinked = false;
			Cleanup();
		}

		public override void Init(ZorroConfiguration configuration)
		{
			uint baseAddress = configuration.BaseAddress;
			this.configuration = configuration;

			logger.LogTrace($"Configured {serial} @ {baseAddress:X8}");
			debugger.AddBreakpoint(0x00fffffe-unitNo*2, callback: (bp) => ProcessAmigaDOSAction(bp));
		}

		private void InsertVolume(Regs regs)
		{
			if (volumeLinked) return;

			LockDOSLists();

			// walk ExecBase->libList to find dos.library
			uint execBase = memory.UnsafeRead32(4);
			uint libListHead = execBase + 378; // libList.lh_Head
			uint node = memory.UnsafeRead32(libListHead);
			uint dosBase = 0;

			while (memory.UnsafeRead32(node) != 0)
			{
				uint namePtr = memory.UnsafeRead32(node + 10); // ln_Name pointer
				string libName = ReadCString(namePtr);
				if (libName == "dos.library")
				{
					dosBase = node;
					break;
				}
				node = memory.UnsafeRead32(node); // Next node
			}
			if (dosBase == 0)
			{ 
				logger.LogTrace("FAILED to find dos.library");
				return;
			}

			volumeLinked = true;
			deviceIsDead = false;

			//is our device already in the list? (snapshot restored)
			uint rootNode = memory.UnsafeRead32(dosBase + 34);          // dl_Root
			uint dosInfo = CPtr(memory.UnsafeRead32(rootNode + 24));    // rootNode->rn_Info
			uint devInfo = CPtr(memory.UnsafeRead32(dosInfo + 4));      // di_DevInfo

			while (devInfo != 0)
			{
				uint namePtr = CPtr(memory.UnsafeRead32(devInfo + 40));     // dol_Name

				if (memory.UnsafeRead32(devInfo + 4) == 2 &&                // DLT_VOLUME
					memory.UnsafeRead32(devInfo + 8) == regs.A[3] &&		// dol_Task == our port
					namePtr != 0 &&
					ReadDOSString(namePtr) == volumeName)
				{
					logger.LogTrace($"{serial} {volumeName} Volume Node already injected");
					UnlockDOSLists();
					myVolumeNodeBPTR = BPtr(devInfo);
					return;
				}

				devInfo = CPtr(memory.UnsafeRead32(devInfo));				// dol_Next
			}

			uint volMem = AllocMem(48, 0x10001);
			uint volNameMem = AllocMem((uint)volumeName.Length + 2, 0x10001);
			WriteDOSString(volNameMem, volumeName);

			memory.UnsafeWrite32(volMem + 0, 0);                // dol_Next (set below)
			memory.UnsafeWrite32(volMem + 4, 2);                // DLT_VOLUME
			memory.UnsafeWrite32(volMem + 8, regs.A[3]);        // dol_Task = handler's MsgPort
			memory.UnsafeWrite32(volMem + 12, 0);               // dol_Lock

			DateTime now = DateTime.Now;                        // local time, as AmigaDOS expects
			uint days = (uint)(now.Date - new DateTime(1978, 1, 1)).TotalDays;
			uint minutes = (uint)(now.Hour * 60 + now.Minute);
			uint ticks = (uint)(now.Second * 50 + now.Millisecond / 20);

			memory.UnsafeWrite32(volMem + 16, days);
			memory.UnsafeWrite32(volMem + 20, minutes);
			memory.UnsafeWrite32(volMem + 24, ticks);

			memory.UnsafeWrite32(volMem + 28, 0);					// dol_LockList
			memory.UnsafeWrite32(volMem + 32, ID_DOS_DISK);			// dol_DiskType
			memory.UnsafeWrite32(volMem + 40, BPtr(volNameMem));	// dol_Name

			myVolumeNodeBPTR = BPtr(volMem);

			uint devNode = memory.UnsafeRead32(regs.A[4] + 28) << 2;
			memory.UnsafeWrite32(devNode + 8, regs.A[3]);

			//insert the node at the head of the dos device list
			uint headBPTR = memory.UnsafeRead32(dosInfo + 4);       // DosInfo->di_DevInfo
			memory.UnsafeWrite32(volMem + 0, headBPTR);             // node->Next = oldHead
			memory.UnsafeWrite32(dosInfo + 4, BPtr(volMem));		// Head = node

			logger.LogTrace($"{serial} {volumeName} Volume Node injected");

			UnlockDOSLists();
		}

		private bool ProcessAmigaDOSAction(BreakpointHitInfo bp)
		{
			var regs = cpu.GetRegs();

			InsertVolume(regs);

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
				DosFail(regs, 0);
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

			ProcessPacket(pkt, regs);

			//return back to emulation
			//regs.SR = memory.UnsafeRead16(regs.SSP); regs.SSP += 2;
			//regs.PC = memory.UnsafeRead32(regs.SSP); regs.SSP += 4;
			regs.PC = memory.UnsafeRead32(regs.SP); regs.SP += 4;
			cpu.SetRegs(regs);

			return false;
		}

		private bool ProcessPacket(DosPacket pkt, Regs regs)
		{ 
			switch (pkt.dp_Type)
			{
				case ACTION_INHIBIT:
					{
						logger.LogTrace($"ACTION_INHIBIT {pkt.dp_Arg1}");
						return DosTrue(regs);
						
						//if (pkt.dp_Arg1 == 0)//uninhibit
						//{ 
						//	return DosTrue(regs);
						//	
						//}
						//else
						//{ 
						//	return DosFail(regs, ERROR_OBJECT_IN_USE);
						//}
					}

				case ACTION_DISK_INFO://aka ACTION_HANDLER_INFO
					{
						logger.LogTrace($"ACTION_DISK_INFO");
						uint address = (uint)pkt.dp_Arg1 << 2;//InfoData
						/*
						//the easy way
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

						var t = new InfoData();
						t.id_NumSoftErrors = 0;
						t.id_UnitNumber = 0;
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
						*/

						//the hard way
						memory.UnsafeWrite32(address, 0); address += 4;
						memory.UnsafeWrite32(address, unitNo); address += 4;
						memory.UnsafeWrite32(address, ID_VALIDATED); address += 4;
						memory.UnsafeWrite32(address, 0x40000000 / 512); address += 4;//1GB
						memory.UnsafeWrite32(address, 0x20000000 / 512); address += 4;//half used
						memory.UnsafeWrite32(address, 512); address += 4;//512 byte blocks
						memory.UnsafeWrite32(address, ID_DOS_DISK); address += 4;
						memory.UnsafeWrite32(address, myVolumeNodeBPTR); address += 4;
						memory.UnsafeWrite32(address, 0); address += 4;
						return DosTrue(regs);
					}

				case ACTION_INFO:
					{
						logger.LogTrace($"ACTION_INFO {pkt.dp_Arg1:X8} {pkt.dp_Arg2 << 2:X8}");

						uint address = (uint)pkt.dp_Arg2 << 2;//InfoData
						memory.UnsafeWrite32(address, 0); address += 4;
						memory.UnsafeWrite32(address, unitNo); address += 4;
						memory.UnsafeWrite32(address, ID_VALIDATED); address += 4;
						memory.UnsafeWrite32(address, 0x40000000 / 512); address += 4;//1GB
						memory.UnsafeWrite32(address, 0x20000000 / 512); address += 4;//half used
						memory.UnsafeWrite32(address, 512); address += 4;//512 byte blocks
						memory.UnsafeWrite32(address, ID_DOS_DISK); address += 4;
						memory.UnsafeWrite32(address, myVolumeNodeBPTR); address += 4;
						memory.UnsafeWrite32(address, 0); address += 4;
						return DosTrue(regs);
					}

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

						if (pathName.StartsWith(':') || pathName.ToUpper().StartsWith($"{volumeName}:"))
							parentPath = string.Empty;

						while (pathName.StartsWith('/') && parentPath != null)
						{
							parentPath = AmigaParentPath(parentPath);
							pathName = pathName.Substring(1);
						}
						if (parentPath == null)
						{
							logger.LogTrace("NOT FOUND PARENT");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}

						searchPath = AmigaPathCombine(parentPath, pathName);
						if (searchPath == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						string hostPath = MakeHostPath(searchPath);
						if (hostPath == null) return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

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

							memory.UnsafeWrite32(regs.A[4] + 12, BPtr(mem));
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
							return true;
						}

						logger.LogTrace($"PATH DOES NOT EXIST {hostPath}");

						return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
					}

				case ACTION_COPY_DIR:
					{
						logger.LogTrace($"ACTION_COPY_DIR (AKA COPY_LOCK)");

						var lok = new FileLock();
						var parent = LockFromBPtr(regs, pkt.dp_Arg1, lok);
						if (parent == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

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

						memory.UnsafeWrite32(regs.A[4] + 12, BPtr(mem));
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						return true;
					}

				case ACTION_EXAMINE_OBJECT:
					{
						logger.LogTrace($"ACTION_EXAMINE_OBJECT {pkt.dp_Arg1 << 2:X8}");

						var lok = new FileLock();
						string basePath = "";
						var parent = LockFromBPtr(regs, pkt.dp_Arg1, lok);
						if (parent == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						logger.LogTrace($"PATH \"{parent.FullPath}\" {parent.LockKey:X8}");
						basePath = parent.FullPath;
						logger.LogTrace($"PATH {basePath}");

						if (basePath == "")
						{
							logger.LogTrace("NO PATH");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}
						
						string hostPath = MakeHostPath(basePath);
						if (hostPath == null) return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						if (!Path.Exists(hostPath))
						{
							logger.LogTrace($"NO PATH HOST");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}

						MyDirCache dircach = null;
						if (Directory.Exists(hostPath))
						{
							dircach = new MyDirCache(hostPath, logger);
							//overwrite any existing entry
							dircache[pkt.dp_Arg1] = dircach;
						}

						var dirEntry = new MyDirCache.MyDirEntry { AmigaName = basePath, IsDirectory = dircach != null };
						logger.LogTrace($"{dirEntry.AmigaName} {dirEntry.Size} {(dirEntry.IsDirectory ? 'D' : 'F')}");

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
						string name = dirEntry.AmigaName;
						int j = name.IndexOf(':');
						if (j != -1) name = name.Substring(j + 1);
						memory.UnsafeWrite8(s++, (byte)Math.Min(107, name.Length));
						for (int i = 0; i < Math.Min(107, name.Length); i++)
							memory.UnsafeWrite8(s++, (byte)name[i]);

						memory.UnsafeWrite32(fib + 144, 0);//no comment
						memory.UnsafeWrite32(fib + 128, (dirEntry.Size + 511) / 512);

						return DosTrue(regs);
					}

				case ACTION_EXAMINE_NEXT:
					{
						logger.LogTrace($"ACTION_EXAMINE_NEXT {pkt.dp_Arg1 << 2:X8}");

						uint lockPtr = (uint)pkt.dp_Arg1 << 2;
						var lok = new FileLock();
						objectMapper.Deserialize(lockPtr, lok);

						if (dircache.TryGetValue(pkt.dp_Arg1, out var dircach2))
						{
							var thing = dircach2.Next();
							if (thing == null)
							{
								logger.LogTrace("NO MORE FILES");
								dircache.Remove(pkt.dp_Arg1);
								return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
							}

							logger.LogTrace($"{thing.AmigaName} {thing.Size} {(thing.IsDirectory ? 'D' : 'F')}");

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
							memory.UnsafeWrite8(s++, (byte)Math.Min(107, thing.AmigaName.Length));
							for (int i = 0; i < Math.Min(107, thing.AmigaName.Length); i++)
								memory.UnsafeWrite8(s++, (byte)thing.AmigaName[i]);

							memory.UnsafeWrite32(fib + 144, 0);//no comment
							memory.UnsafeWrite32(fib + 128, (thing.Size + 511) / 512);

							return DosTrue(regs);
						}

						logger.LogTrace($"dir cache not found {(uint)pkt.dp_Arg1 << 2:X8}");

						//no more files
						return DosFail(regs, ERROR_NO_MORE_ENTRIES);
					}

				case ACTION_FREE_LOCK:
					{
						logger.LogTrace($"ACTION_FREE_LOCK {pkt.dp_Arg1 << 2:X8}");

						uint lockPtr = (uint)pkt.dp_Arg1 << 2;

						if (locks.ContainsKey(lockPtr))
						{
							var @lock = locks[lockPtr];
							logger.LogTrace($"FREE LOCK {@lock}");
							if (@lock.Freed) logger.LogTrace($"DOUBLE FREE LOCK {@lock}");
							locks.Remove(lockPtr);
						}
						else
						{
							logger.LogTrace($"WARNING: LOCK NOT FOUND {lockPtr:X8} (snapshot?)");
						}
						FreeMem(lockPtr, 20);

						return DosTrue(regs);
					}

				case ACTION_PARENT:
					{
						logger.LogTrace($"ACTION_PARENT {pkt.dp_Arg1 << 2:X8}");

						var lok = new FileLock();
						var child = LockFromBPtr(regs, pkt.dp_Arg1, lok);
						if (child == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						logger.LogTrace($"PARENT {child.FullPath} {AmigaParentPath(child.FullPath)}");

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

							memory.UnsafeWrite32(regs.A[4] + 12, BPtr(mem));
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
							return true;
						}

						logger.LogTrace($"ROOT {child.FullPath}");
						return DosFail(regs, 0);//zero lock at root of file system
					}

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

						var parent = LockFromBPtr(regs, pkt.dp_Arg2);
						if (parent == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						logger.LogTrace($"PATH \"{parent.FullPath}\" {parent.LockKey:X8}");
						basePath = parent.FullPath;

						uint bstrAddr = (uint)pkt.dp_Arg3 << 2;
						if (bstrAddr == 0)
						{
							logger.LogTrace("ACTION_FINDINPUT empty name");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}

						string pathName = ReadDOSString(bstrAddr);
						logger.LogTrace($"ACTION_FINDINPUT {pathName}");
						basePath = AmigaPathCombine(basePath, pathName);
						if (basePath == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						basePath = MakeHostPath(basePath);
						if (basePath == null) return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						if (Path.Exists(basePath))
						{
							logger.LogTrace("FOUND");

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

								return DosTrue(regs);
							}
							catch (Exception ex)
							{
								logger.LogTrace($"Exception: {ex}");
								return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
							}
						}
						else if (pkt.dp_Type == ACTION_FINDOUTPUT)
						{
							logger.LogTrace($"NEW {pkt.dp_Arg1 << 2:X8}");
							var fi = new MyFileInfo();

							FileAccess access = FileAccess.ReadWrite;
							FileMode mode = FileMode.Create;

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

								return DosTrue(regs);
							}
							catch (Exception ex)
							{
								logger.LogTrace($"Exception: {ex}");
								return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
							}
						}

						logger.LogTrace("NOT FOUND");
						return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
					}

				case ACTION_READ:
					{
						logger.LogTrace($"ACTION_READ {pkt.dp_Arg1} {pkt.dp_Arg2:X8} {pkt.dp_Arg3}");
						if (!files.TryGetValue((uint)pkt.dp_Arg1, out var file))
						{
							logger.LogTrace("CAN'T FIND FILE");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}
						logger.LogTrace("READ FILE");
						var tmp = new byte[pkt.dp_Arg3];
						uint r = (uint)file.stream.Read(tmp, 0, pkt.dp_Arg3);
						for (uint i = 0; i < r; i++)
							memory.UnsafeWrite8((uint)pkt.dp_Arg2 + i, tmp[i]);
						logger.LogTrace($"READ {r}");
						memory.UnsafeWrite32(regs.A[4] + 12, r);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						return true;
					}

				case ACTION_WRITE:
					{
						logger.LogTrace($"ACTION_WRITE {pkt.dp_Arg1} {pkt.dp_Arg2:X8} {pkt.dp_Arg3}");
						if (!files.TryGetValue((uint)pkt.dp_Arg1, out var file))
						{
							logger.LogTrace("CAN'T FIND FILE");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}
						var tmp = new byte[pkt.dp_Arg3];
						for (uint i = 0; i < pkt.dp_Arg3; i++)
							tmp[i] = memory.UnsafeRead8((uint)pkt.dp_Arg2 + i);
						file.stream.Write(tmp, 0, pkt.dp_Arg3);

						memory.UnsafeWrite32(regs.A[4] + 12, (uint)pkt.dp_Arg3);
						memory.UnsafeWrite32(regs.A[4] + 16, 0);
						return true;
					}

				case ACTION_SEEK:
					{
						logger.LogTrace($"ACTION_SEEK {pkt.dp_Arg1} {pkt.dp_Arg2} {pkt.dp_Arg3}");
						if (!files.TryGetValue((uint)pkt.dp_Arg1, out var file))
						{
							logger.LogTrace("CAN'T FIND FILE");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}

						var origin = pkt.dp_Arg3 == -1 ? SeekOrigin.Begin :
									(pkt.dp_Arg3 == 1 ? SeekOrigin.End : SeekOrigin.Current);

						file.stream.Seek(pkt.dp_Arg2, origin);

						return DosTrue(regs);
					}

				case ACTION_END:
					{
						logger.LogTrace($"ACTION_END {pkt.dp_Arg1}");
						if (!files.TryGetValue((uint)pkt.dp_Arg1, out var file))
						{
							logger.LogTrace("CAN'T FIND FILE");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}
						file.stream.Close();
						file.stream.Dispose();
						files.Remove((uint)pkt.dp_Arg1);

						return DosTrue(regs);
					}

				case ACTION_DELETE_OBJECT:
					{
						logger.LogTrace($"ACTION_DELETE_OBJECT {pkt.dp_Arg1 << 2:X8}");

						string filename = string.Empty;

						var parent = LockFromBPtr(regs, pkt.dp_Arg1);
						if (parent == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						logger.LogTrace($"DELETE {parent.FullPath}");

						filename = AmigaPathCombine(parent.FullPath, ReadDOSString((uint)pkt.dp_Arg2 << 2));
						if (filename == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						try
						{
							string hostPath = MakeHostPath(filename);
							if (hostPath == null) return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

							if (File.Exists(hostPath))
							{
								try
								{ 
									File.Delete(hostPath);
								}
								catch (IOException)
								{
									return DosFail(regs, ERROR_OBJECT_IN_USE);
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
									return DosFail(regs, ERROR_DIRECTORY_NOT_EMPTY);
								}
							}
							else
							{
								return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
							}
							return DosTrue(regs);
						}
						catch (Exception ex)
						{
							logger.LogTrace($"Exception: {ex}");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}
					}

				case ACTION_CREATE_DIR:
					{
						logger.LogTrace($"ACTION_CREATE_DIR {pkt.dp_Arg1 << 2:X8}");

						string filename = string.Empty;

						var lok = new FileLock();
						var parent = LockFromBPtr(regs, pkt.dp_Arg1, lok);
						if (parent == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						logger.LogTrace($"CREATE DIR {parent.FullPath}");

						filename = AmigaPathCombine(parent.FullPath, ReadDOSString((uint)pkt.dp_Arg2 << 2));
						if (filename == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						try
						{
							string hostPath = MakeHostPath(filename);
							if (hostPath == null) return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

							if (Path.Exists(hostPath))
							{
								return DosFail(regs, ERROR_OBJECT_EXISTS);
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

							memory.UnsafeWrite32(regs.A[4] + 12, BPtr(mem));
							memory.UnsafeWrite32(regs.A[4] + 16, 0);
							return true;							
						}
						catch (Exception ex)
						{
							logger.LogTrace($"Exception: {ex}");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}
					}

				case ACTION_RENAME_OBJECT:
					{
						logger.LogTrace($"ACTION_RENAME_OBJECT {pkt.dp_Arg1 << 2:X8}");

						string filename = string.Empty;
						string dstFilename = string.Empty;
						var parent = LockFromBPtr(regs, pkt.dp_Arg1);
						if (parent == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						filename = AmigaPathCombine(parent.FullPath, ReadDOSString((uint)pkt.dp_Arg2 << 2));
						if (filename == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						var parentTo = LockFromBPtr(regs, pkt.dp_Arg3);
						if (parentTo == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						dstFilename = AmigaPathCombine(parentTo.FullPath, ReadDOSString((uint)pkt.dp_Arg4 << 2));
						if (dstFilename == null)
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

						try
						{
							logger.LogTrace($"RENAME");
							string from = MakeHostPath(filename);
							if (from == null) return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
							logger.LogTrace("->");
							string to = MakeHostPath(dstFilename);
							if (to == null) return DosFail(regs, ERROR_OBJECT_NOT_FOUND);

							if (File.Exists(from))
								File.Move(from, to);
							else if (Directory.Exists(from))
								Directory.Move(from, to);
							else
							{
								return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
							}
							return DosTrue(regs);
						}
						catch (Exception ex)
						{
							logger.LogTrace($"Exception: {ex}");
							return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
						}
					}

				case ACTION_SAME_LOCK:
					logger.LogTrace($"ACTION_SAME_LOCK");
					var lock1 = LockFromBPtr(regs, pkt.dp_Arg1);
					var lock2 = LockFromBPtr(regs, pkt.dp_Arg2);
					if (lock1 == null || lock2 == null)
					{
						return DosFail(regs, ERROR_OBJECT_NOT_FOUND);
					}
					if (lock1.FullPath.ToUpper() != lock2.FullPath.ToUpper())
					{
						return DosFail(regs, 0);//todo: what error should this be?
					}
					return DosTrue(regs);

				case ACTION_SET_PROTECT:
					logger.LogTrace($"ACTION_SET_PROTECT");
					return DosTrue(regs);

				case ACTION_SET_COMMENT:
					logger.LogTrace($"ACTION_SET_COMMENT");
					return DosTrue(regs);

				case ACTION_SET_DATE:
					logger.LogTrace($"ACTION_SET_DATE");
					return DosTrue(regs);

				case ACTION_FLUSH:
					logger.LogTrace($"ACTION_FLUSH");
					return DosTrue(regs);

				case ACTION_MORE_CACHE:
					logger.LogTrace($"ACTION_MORE_CACHE");
					return DosTrue(regs);

				case ACTION_IS_FILESYSTEM:
					logger.LogTrace($"ACTION_IS_FILESYSTEM");
					return DosTrue(regs);

				case ACTION_DIE:
					logger.LogTrace($"ACTION_DIE");
					Cleanup();
					return DosTrue(regs);

				case ACTION_RENAME_DISK:
					{ 
					logger.LogTrace($"ACTION_RENAME_DISK");
					string name = ReadDOSString((uint)pkt.dp_Arg1<<2);
					if (name.Length > 30)
					{
						return DosFail(regs, ERROR_INVALID_COMPONENT_NAME);
					}
					uint nameMem = AllocMem((uint)name.Length+2, 0x10001);
					WriteDOSString(nameMem, name);

					uint oldName = memory.UnsafeRead32((myVolumeNodeBPTR<<2)+40)<<2;
					uint size = memory.UnsafeRead8(oldName);
					memory.UnsafeWrite32((myVolumeNodeBPTR<<2)+40, BPtr(nameMem));

					//todo: race condition
					FreeMem(oldName, size);

					//update this device's volume name
					volumeName = name;

					return DosTrue(regs);
					}

				case ACTION_NIL:
					logger.LogTrace("ACTION_NIL");
					return DosTrue(regs);

				case > ACTION_MAXIMUM_VALUE:

					logger.LogTrace($"ACTION_IGNORED** {pkt.dp_Type} {pkt.dp_Type:X8} {pkt.dp_Type << 2:X8}");

					//possible mapping for first message
					//var f = new FileSysStartupMsg();
					//objectMapper.Deserialize(regs.A[4], f);

					return false;

				default:
					logger.LogTrace($"ACTION_UNHANDLED** {pkt.dp_Type} {pkt.dp_Type:X8} {pkt.dp_Type << 2:X8}");

					return DosFail(regs, ERROR_ACTION_NOT_KNOWN);
			}
		}

		private bool DosFail(Regs regs, uint code)
		{
			memory.UnsafeWrite32(regs.A[4] + 12, DOSFAIL);
			memory.UnsafeWrite32(regs.A[4] + 16, code);
			return false;
		}

		private bool DosTrue(Regs regs)
		{
			memory.UnsafeWrite32(regs.A[4] + 12, DOSTRUE);
			memory.UnsafeWrite32(regs.A[4] + 16, 0);
			return true;
		}

		private uint BPtr(uint mem) { return mem >> 2; }
		private uint CPtr(uint mem) { return mem << 2; }

		private void Cleanup()
		{
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

			dircache.Clear();
			locks.Clear();

			deviceIsDead = true;
		}

		private readonly string[] bad3 = { "CON", "PRN", "AUX", "NUL" };
		private readonly string[] bad4 = { "COM", "LPT" };

		private string AmigaPathCombine(string root, string fragment)
		{
			//disallow . and ..
			var bits = fragment.Split('/', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
			if (bits.Any(x => x == "." || x == "..")) return null;

			//special filenames on Windows
			if (OperatingSystem.IsWindows())
			{
				foreach (var bit in bits)
				{ 
					var winBit = bit.Split([':','.'], 1)[0].ToUpper();
					if (bad3.Contains(winBit)) return null;
					if (winBit.Length == 4 && bad4.Contains(winBit.Substring(0, 3)) && char.IsAsciiDigit(winBit[3])) return null;
				}
			}

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
				return null;
			}

			objectMapper.Deserialize((uint)arg << 2, @lock);

			if (locks.TryGetValue((uint)@lock.fl_Key, out var parent))
				return parent;

			logger.LogTrace($"Lock not found {(uint)arg << 2:X8} {@lock.fl_Key:X8}");
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

		private void LockDOSLists()
		{
			Forbid();
		}

		private void UnlockDOSLists()
		{
			Permit();
		}

		//todo: should examine DOS library version, if it's >= 36, this should be
		//AttemptLockDosList() / LockDosList() LDF_WRITE
		//MakeDosEntry()
		//AddDosEntry()
		//UnLockDosList()
		private void Forbid()
		{
			CallExec(-132, string.Empty);
		}

		private void Permit()
		{
			CallExec(-138, string.Empty);
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

		private string ReadCString(uint namePtr)
		{
			var sb = new StringBuilder();
			for (;;)
			{
				byte b = memory.UnsafeRead8(namePtr++);
				if (b == 0) break;
				sb.Append((char)b);
			}
			return sb.ToString();
		}

		private void WriteDOSString(uint address, string str)
		{
			memory.UnsafeWrite8(address++, (byte)str.Length);
			for (int i = 0; i < str.Length; i++)
				memory.UnsafeWrite8(address++, (byte)str[i]);
			memory.UnsafeWrite8(address++,0);
		}

		private void WriteCString(uint address, string str)
		{
			for (int i = 0; i < str.Length; i++)
				memory.UnsafeWrite8(address++, (byte)str[i]);
			memory.UnsafeWrite8(address++, 0);
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
		//we're inside a call to the expansion ROM from dos.library, so it can't be that bad
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

			//logger.LogTrace($"EXEC {lvo}");

			//half-way through our 64KB memory bank
			uint baseAddress = configuration.BaseAddress + 32768;
			uint i = baseAddress;
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

			cpu.SetPC(baseAddress);
			do
			{
				cpu.Emulate();
				cpu.GetRegs(regs);
			} while (regs.PC != 0);

			uint rv = regs.D[0];

			cpu.SetRegs(saved);

			//logger.LogTrace("EXEC DONE");

			return rv;
		}
	}
}
