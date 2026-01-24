using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;
using Jammy.Types;
using Jammy.Types.Debugger;
using System.Collections.Generic;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IDebugger : IMemoryInterceptor, IDebuggableMemory
	{
		void ToggleBreakpoint(uint pc);
		IMemoryDump GetMemory();
		ChipState GetChipRegs();
		string GetCopperDisassembly();
		ushort GetInterruptLevel();
		Regs GetRegs();
		void BreakAtNextPC();
		void SetPC(uint pc);
		uint FindMemoryText(string txt);
		uint FindMemory(byte[] seq);
		void InsertDisk(int df);
		void RemoveDisk(int df);
		void ChangeDisk(int df, string fileName);
		void ReadyDisk();
		void CIAInt(ICRB icr);
		void IRQ(uint irq);
		void INTENA(uint irq);
		void INTDIS(uint irq);
		void WriteTrace();
		uint KickstartSize();
		void IDEACK();
		void ClearBBUSY();
		uint Read32(uint address);
		void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Execute, int counter = 0, Size size = Size.Long);
		void RemoveBreakpoint(uint address);
		void DumpBreakpoints();
		ClockInfo GetChipClock();
		void GenerateDisassemblies();
		Vectors GetVectors();
		Libraries GetLibraries();
		InstructionAnalysis Analyse();
	}

	public interface ICPUAnalyser
	{
		public EAAnalysis Analyse(Regs regs);
	}
}