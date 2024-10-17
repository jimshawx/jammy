using System;
using Jammy.Core.Types.Types;

namespace Jammy.Core.Interface.Interfaces;

[Flags]
public enum DMA : ushort
{
	SETCLR = 0x8000,
	BBUSY = 0x4000,
	BZERO = 0x2000,
	unused0 = 0x1000,
	unused1 = 0x0800,
	BLTPRI = 0x0400,
	DMAEN = 0x0200,
	BPLEN = 0x00100,
	COPEN = 0x0080,
	BLTEN = 0x0040,
	SPREN = 0x0020,
	DSKEN = 0x0010,
	AUD3EN = 0x0008,
	AUD2EN = 0x0004,
	AUD1EN = 0x0002,
	AUD0EN = 0x0001,
}
public enum DMASource
{
	Agnus,
	Copper,
	Blitter,

	//needs to be last
	CPU,

	NumDMASources,
	None,
}

public interface IDMA : ICustomReadWrite, IDebugChipsetRead
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