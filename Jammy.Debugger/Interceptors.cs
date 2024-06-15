using Jammy.Types.Kickstart;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Jammy.Core.Types.Types;
using Jammy.Core.Interface.Interfaces;
using Jammy.Interface;
using Jammy.Core.Memory;
using Jammy.Core.Types;

namespace Jammy.Debugger
{
	public class LVOLoggerBase
	{
		protected readonly ICPU cpu;
		protected readonly IDebugMemoryMapper memory;
		protected readonly IReturnValueSnagger returnValueSnagger;
		protected readonly IAnalyser analyser;
		protected readonly ILibraryBases libraryBases;
		protected readonly ILogger logger;

		public LVOLoggerBase(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger logger)
		{
			this.cpu = cpu;
			this.memory = memory;
			this.returnValueSnagger = returnValueSnagger;
			this.analyser = analyser;
			this.libraryBases = libraryBases;
			this.logger = logger;
		}
	}

	public interface IOpenFileTracker
	{
		public void Open(uint handle, string name);
		public void Close(uint handle);
		public string GetFileName(uint handle);
	}

	public class OpenFileTracker : IOpenFileTracker
	{
		private Dictionary<uint, string> openFiles = new Dictionary<uint, string>();

		public void Open(uint handle, string name)
		{
			openFiles[handle] = name;
		}

		public void Close(uint handle)
		{
			if (openFiles.ContainsKey(handle))
				openFiles.Remove(handle);
		}

		public string GetFileName(uint handle)
		{
			if (!openFiles.ContainsKey(handle)) return "unknown";
			return openFiles[handle];
		}
	}

	public class ReadLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private readonly IOpenFileTracker fileTracker;

		public ReadLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IOpenFileTracker fileTracker, ILogger<ReadLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.fileTracker = fileTracker;
		}

		public string Library => "dos.library";
		public string VectorName => "Read";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() file: {fileTracker.GetFileName(regs.D[1])}:{regs.D[1]:X8} buffer: {regs.D[2]:X8} length: {regs.D[3]:X8}");
		}
	}

	public class OpenLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private readonly IOpenFileTracker fileTracker;

		public OpenLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IOpenFileTracker fileTracker, ILogger<ReadLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.fileTracker = fileTracker;
		}

		public string Library => "dos.library";
		public string VectorName => "Open";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			string filename = memory.GetString(regs.D[1]);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() name:{filename}:{regs.D[1]:X8} flags: {regs.D[2]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
				{
					var regs = cpu.GetRegs();
					logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
					fileTracker.Open(regs.D[0], filename);
				}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class CloseLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private readonly IOpenFileTracker fileTracker;

		public CloseLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, IOpenFileTracker fileTracker, ILogger<ReadLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
			this.fileTracker = fileTracker;
		}

		public string Library => "dos.library";
		public string VectorName => "Close";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() file: {regs.D[1]:X8}");
			fileTracker.Close(regs.D[1]);
		}
	}

	public class LoadSegLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public LoadSegLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<ReadLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "dos.library";
		public string VectorName => "LoadSeg";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() name:{memory.GetString(regs.D[1])}:{regs.D[1]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
				{
					var regs = cpu.GetRegs();
					logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
				}, memory.UnsafeRead32(regs.SP)));
		}
	}
	public class AllocMemLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public AllocMemLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<AllocMemLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		[Flags]
		enum MEMF
		{
			MEMF_ANY = (0),    /* Any type of memory will do */
			MEMF_PUBLIC = (1 << 0),
			MEMF_CHIP = (1 << 1),
			MEMF_FAST = (1 << 2),
			MEMF_LOCAL = (1 << 8), /* Memory that does not go away at RESET */
			MEMF_24BITDMA = (1 << 9),  /* DMAable memory within 24 bits of address */
			MEMF_KICK = (1 << 10), /* Memory that can be used for KickTags */

			MEMF_CLEAR = (1 << 16),    /* AllocMem: NULL out area before return */
			MEMF_LARGEST = (1 << 17),  /* AvailMem: return the largest chunk size */
			MEMF_REVERSE = (1 << 18),  /* AllocMem: allocate from the top down */
			MEMF_TOTAL = (1 << 19),    /* AvailMem: return total size of memory */

			MEMF_NO_EXPUNGE = (1<<31), /*AllocMem: Do not cause expunge on failure */
		}

		public string Library => "exec.library";
		public string VectorName => "AllocMem";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() size: {regs.D[0]:X8} flags: {regs.D[1]:X8} {(MEMF)regs.D[1]}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(()=>
				{
					var regs = cpu.GetRegs();
					logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
				}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class OpenLibraryLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public OpenLibraryLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<OpenLibraryLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "exec.library";
		public string VectorName => "OpenLibrary";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			string libraryName = memory.GetString(regs.A[1]);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() libname {regs.A[1]:X8} {libraryName} version: {regs.D[0]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
				{
					var regs = cpu.GetRegs();
					logger.LogTrace($"{libraryName} {regs.D[0]:X8}");
					libraryBases.SetLibraryBaseaddress(libraryName, regs.D[0]);
				}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class OldOpenLibraryLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public OldOpenLibraryLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<OpenLibraryLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "exec.library";
		public string VectorName => "OldOpenLibrary";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			string libraryName = memory.GetString(regs.A[1]);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() libname {regs.A[1]:X8} {libraryName}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
				{
					var regs = cpu.GetRegs();
					logger.LogTrace($"{libraryName} {regs.D[0]:X8}");
					libraryBases.SetLibraryBaseaddress(libraryName, regs.D[0]);
				}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class OpenResourceLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public OpenResourceLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<OpenResourceLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}
		
		public string Library => "exec.library";
		public string VectorName => "OpenResource";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() resName: {regs.A[1]:X8} {memory.GetString(regs.A[1])}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(()=>
				{
					var regs = cpu.GetRegs();
					logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
				}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class MakeLibraryLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		private HashSet<uint> librariesMade = new HashSet<uint>();

		public MakeLibraryLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<MakeLibraryLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}
		
		public string Library => "exec.library";
		public string VectorName => "MakeLibrary";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			uint returnAddress = memory.UnsafeRead32(regs.SP);
			logger.LogTrace($"@{pc:X8} {lvo.Name}() vectors: {regs.A[0]:X8} structure: {regs.A[1]:X8} init: {regs.A[2]:X8} dataSize: {regs.D[0]:X8} segList: {regs.D[1]:X8}");

			if (!librariesMade.Contains(regs.A[0]))
			{
				librariesMade.Add(regs.A[0]);
				if (regs.A[0] != 0) analyser.ExtractFunctionTable(regs.A[0], NT_Type.NT_LIBRARY, $"unknown_{regs.A[0]:X8}");
				if (regs.A[1] != 0) analyser.ExtractStructureInit(regs.A[1]);
				if (regs.A[2] != 0) analyser.ExtractFunction(regs.A[2], "init");
			}
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
				{
					var regs = cpu.GetRegs();
					logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
				}, returnAddress));

			//snag the call to init
			if (regs.A[2] != 0)
			{
				returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
				{
					var regs = cpu.GetRegs();

					//D0 points to Library structure

					/*
					*  List Node Structure.  Each member in a list starts with a Node
					*/

					//struct Node
					//{
					//	struct Node *ln_Succ;	/* Pointer to next (successor) */
					//	struct Node *ln_Pred;	/* Pointer to previous (predecessor) */
					//	UBYTE ln_Type;
					//	BYTE ln_Pri;        /* Priority, for sorting */
					//	char* ln_Name;      /* ID string, null terminated */
					//};  /* Note: word aligned */

					/*------ Library Base Structure ----------------------------------*/
					/* Also used for Devices and some Resources */
					//struct Library
					//{
					//	struct Node lib_Node;
					//	UBYTE lib_Flags;
					//	UBYTE lib_pad;
					//	UWORD lib_NegSize;      /* number of bytes before library */
					//	UWORD lib_PosSize;      /* number of bytes after library */
					//	UWORD lib_Version;      /* major */
					//	UWORD lib_Revision;     /* minor */
					//	APTR lib_IdString;      /* ASCII identification */
					//	ULONG lib_Sum;          /* the checksum itself */
					//	UWORD lib_OpenCnt;      /* number of current opens */
					//};  /* Warning: size is not a longword multiple! */

					uint library = regs.D[0];
					//Node is 14, 10 bytes more until IdString
					uint idStringAddress = memory.UnsafeRead32(library + 24);
					string idString = memory.GetString(idStringAddress);

					logger.LogTrace($"{lvo.Name} init: libaddr: {regs.D[0]:X8} seglist: {regs.A[0]:X8} execbase: {regs.A[6]:X8}, init {idString}");

				}, regs.A[2]));
			}
		}
	}

	public interface ILVOInterceptorAction
	{
		public void Intercept(LVO lvo, uint pc);

		public string Library { get; }
		public string VectorName { get; }
	}

	public interface ILibraryBases
	{
		Dictionary<string, uint> Addresses { get; }
		void SetLibraryBaseaddress(string libraryName, uint address);
	}

	public class LibraryBases : ILibraryBases
	{
		private Dictionary<string, uint> libraryBaseAddresses = new Dictionary<string, uint>();

		public LibraryBases(IDebugMemoryMapper memory)
		{
			SetLibraryBaseaddress("exec.library", memory.UnsafeRead32(4));
		}

		public Dictionary<string, uint> Addresses => libraryBaseAddresses;

		public void SetLibraryBaseaddress(string libraryName, uint address)
		{
			if (address == 0) return;
			libraryBaseAddresses[libraryName] = address;
		}
	}

	public interface ILVOInterceptors
	{
		void CheckLVOAccess(uint address, Size size);
	}

	public class LVOInterceptor
	{
		public string Library { get; set; }
		public LVO LVO { get; set; }
		public ILVOInterceptorAction Action { get; set; }
	}

	public class LVOInterceptors : ILVOInterceptors
	{
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memory;
		private readonly IAnalysis analysis;
		private readonly ILibraryBases libraryBases;

		private List<LVOInterceptor> lvoInterceptors { get; } = new List<LVOInterceptor>();

		public LVOInterceptors(IEnumerable<ILVOInterceptorAction> actions, ICPU cpu, IDebugMemoryMapper memory, IAnalysis analysis, ILibraryBases libraryBases)
		{
			this.cpu = cpu;
			this.memory = memory;
			this.analysis = analysis;
			this.libraryBases = libraryBases;
			foreach (var act in actions)
				AddLVOIntercept(act.Library, act.VectorName, act);
		}

		private void AddLVOIntercept(string library, string vectorName, ILVOInterceptorAction action)
		{
			var lvos = analysis.GetLVOs();
			if (lvos.TryGetValue(library, out var lib))
			{
				var vector = lib.LVOs.SingleOrDefault(x => x.Name == vectorName);
				if (vector != null)
				{
					lvoInterceptors.Add(new LVOInterceptor
					{
						Library = library,
						LVO = vector,
						Action = action
					});
				}
			}
		}

		public void CheckLVOAccess(uint pc, Size size)
		{
			if (size == Size.Word)
			{
				//todo: speed this up!
				var lvo = lvoInterceptors
					.Where(x => libraryBases.Addresses.ContainsKey(x.Library))
					.SingleOrDefault(x => memory.UnsafeRead32((uint)(libraryBases.Addresses[x.Library] + x.LVO.Offset + 2)) == pc);
				if (lvo != null)
				{
					//breakpoints.SignalBreakpoint(pc);
					lvo.Action.Intercept(lvo.LVO, pc);
				}
			}
		}
	}

	public interface ISnagger
	{
		bool IsHit(IDebugMemoryMapper memoryMapper, uint pc, uint sp);
		void Act();
	}

	public class RtsSnagger : ISnagger
	{
		private Action act;
		private uint sp;
		public RtsSnagger(Action action, uint sp)
		{
			this.act = action;
			this.sp = sp;
		}

		public bool IsHit(IDebugMemoryMapper memoryMapper, uint pc, uint sp)
		{
			ushort ins = memoryMapper.UnsafeRead16(pc);
			if (ins != 0x4e75 && ins != 0x4e73) return false;//rts or rte
			return sp == this.sp;
		}

		public void Act()
		{
			act();
		}
	}

	public class ReturnAddressSnagger :ISnagger
	{
		private Action act;
		private uint address;

		public ReturnAddressSnagger(Action action, uint address)
		{
			this.act = action;
			this.address = address;
		}

		public bool IsHit(IDebugMemoryMapper memoryMapper, uint pc, uint sp)
		{
			return pc == address;
		}

		public void Act()
		{
			act();
		}
	}

	public interface IReturnValueSnagger
	{
		void AddSnagger(ISnagger snagger);
		void CheckSnaggers(uint pc, uint sp);
	}

	public class ReturnValueSnagger : IReturnValueSnagger
	{
		private readonly ICPU cpu;
		private readonly IDebugMemoryMapper memoryMapper;
		private readonly List<ISnagger> snaggers = new List<ISnagger>();

		public ReturnValueSnagger(ICPU cpu, IDebugMemoryMapper memoryMapper)
		{
			this.cpu = cpu;
			this.memoryMapper = memoryMapper;
		}

		public void AddSnagger(ISnagger snagger)
		{
			snaggers.Add(snagger);
		}

		public void CheckSnaggers(uint pc, uint sp)
		{
			if (snaggers.Count == 0) return;

			List<ISnagger> completed = null;
			foreach (var snagger in snaggers)
			{
				if (snagger.IsHit(memoryMapper, pc, sp))
				{
					snagger.Act();
					if (completed == null)
						completed = new List<ISnagger>();
					completed.Add(snagger);
				}
			}
			if (completed != null)
				snaggers.RemoveAll(x=>completed.Contains(x));
		}
	}
}
