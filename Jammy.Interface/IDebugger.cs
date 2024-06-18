using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Types;
using Jammy.Types.Debugger;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IDebugger : IMemoryInterceptor
	{
		void ToggleBreakpoint(uint pc);
		IMemoryDump GetMemory();
		ChipState GetChipRegs();
		ushort GetInterruptLevel();
		Regs GetRegs();
		void BreakAtNextPC();
		void SetPC(uint pc);
		uint FindMemoryText(string txt);
		void InsertDisk(int df);
		void RemoveDisk(int df);
		void ChangeDisk(int df, string fileName);
		void CIAInt(ICRB icr);
		void IRQ(uint irq);
		void INTENA(uint irq);
		void WriteTrace();
		uint KickstartSize();
		void IDEACK();
		void ClearBBUSY();
		uint Read32(uint address);
	}
}