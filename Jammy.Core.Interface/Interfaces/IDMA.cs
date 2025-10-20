using Jammy.Core.Types.Types;
using System;

namespace Jammy.Core.Interface.Interfaces;

public interface IDMA : ICustomReadWrite, IDebugChipsetRead, IStatePersister
{
	void ReadReg(DMASource source, uint address, DMA priority, Size size, uint chipReg);
	uint DebugRead(uint address, Size size);
	void WriteChip(DMASource source, uint address, DMA priority, ushort value, Size size);
	void NoDMA(DMASource source);
	void NeedsDMA(DMASource source, DMA priority);
	bool IsDMAEnabled(DMA source);
	void TriggerHighestPriorityDMA();
	bool IsWaitingForDMA(DMASource source);
	void ClearWaitingForDMA(DMASource source);
	void SetCPUWaitingForDMA();
	void WriteDMACON(ushort bits);
	ushort ReadDMACON();
	void DebugExecuteDMAActivity(DMASource source);
	void DebugExecuteAllDMAActivity();
	void ReadCPU(CPUTarget target, uint address, Size size);
	void WriteCPU(CPUTarget target, uint address, ushort value, Size size);
	ushort LastRead { get; }
	bool LastDMASlotWasUsedByChipset();
	void ExecuteCPUDMASlot();
	void Init(IAudio audio, IMemoryMapper memoryMapper, IChipRAM chipRAM);
	uint ChipsetSync();
	void SetSync(Func<ushort> runChipsetEmulation);
}