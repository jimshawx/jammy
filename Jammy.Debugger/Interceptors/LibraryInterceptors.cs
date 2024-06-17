using Jammy.Core.Interface.Interfaces;
using Jammy.Interface;
using Jammy.Types.Kickstart;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Jammy.Debugger.Interceptors
{
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
			ILibraryBases libraryBases, ILogger<OldOpenLibraryLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
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
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs();
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X8}");
			}, memory.UnsafeRead32(regs.SP)));
		}
	}

	public class OpenDeviceLogger : LVOLoggerBase, ILVOInterceptorAction
	{
		public OpenDeviceLogger(ICPU cpu, IDebugMemoryMapper memory, IReturnValueSnagger returnValueSnagger, IAnalyser analyser,
			ILibraryBases libraryBases, ILogger<OpenDeviceLogger> logger) : base(cpu, memory, returnValueSnagger, analyser, libraryBases, logger)
		{
		}

		public string Library => "exec.library";
		public string VectorName => "OpenDevice";

		public void Intercept(LVO lvo, uint pc)
		{
			var regs = cpu.GetRegs();
			logger.LogTrace($"@{pc:X8} {lvo.Name}() deviceName: {regs.A[0]:X8} {memory.GetString(regs.A[0])} unitNumber: {regs.D[0]} ioRq:{regs.A[1]:X8} flags:{regs.D[1]:X8}");
			returnValueSnagger.AddSnagger(new ReturnAddressSnagger(() =>
			{
				var regs = cpu.GetRegs();
				logger.LogTrace($"{lvo.Name} returned: {regs.D[0]:X2} {((regs.D[0]&0xff)==0?"Success":"Failed")} @{regs.PC:X8}");
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
}
