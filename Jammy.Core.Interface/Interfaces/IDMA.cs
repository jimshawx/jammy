using Jammy.Core.Types.Types;

namespace Jammy.Core.Interface.Interfaces;

public interface IDMA : ICustomReadWrite, IDebugChipsetRead, IStatePersister
{
	void Read(DMASource source, uint address, DMA priority, Size size, uint chipReg);
	uint DebugRead(uint address, Size size);
	void Write(DMASource source, uint address, DMA priority, ushort value, Size size);
	void WriteReg(DMASource source, uint chipReg, DMA priority, ushort value);
	void NoDMA(DMASource source);
	void NeedsDMA(DMASource source, DMA priority);
	bool IsDMAEnabled(DMA source);
	//void WaitForChipRamDMASlot();
	void TriggerHighestPriorityDMA();
	bool IsWaitingForDMA(DMASource source);
	void ClearWaitingForDMA(DMASource source);
	void SetCPUWaitingForDMA();
	void WriteDMACON(ushort bits);
	void DebugExecuteDMAActivity(DMASource source);
	void DebugExecuteAllDMAActivity();
}