using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Enums;
using RunAmiga.Types;
using RunAmiga.Types.Debugger;

namespace RunAmiga.Interface
{
	public interface IDebugger : IMemoryInterceptor
	{
		void ToggleBreakpoint(uint pc);
		MemoryDump GetMemory();
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
	}
}