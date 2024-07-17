using System;
using System.Threading;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom;

public enum DMAPriority
{
	Refresh,
	Disk,
	Audio0,
	Audio1,
	Audio2,
	Audio3,
	Bitplane,

	Sprite,

	Copper,
	
	Blitter,
	
	CPU
}

public interface IDMA : IEmulate
{
	public ulong Read(uint address, DMAPriority priority, Size size);
	uint DebugRead(uint address, Size size);
	void Write(uint address, DMAPriority priority, ushort value, Size size);
	void ReliquishDMASlot();
	void Refresh();
	bool IsDMAEnabled(ChipRegs.DMA source);
}

public class DMA : IDMA
{
	private readonly IChipsetClock clock;
	private readonly IChipRAM memory;
	private readonly IChips custom;

	private readonly ManualResetEventSlim[] events = new ManualResetEventSlim[4];
	private readonly bool[] priorities = new bool[4];

	public DMA(IChipsetClock clock, IChipRAM memory, IChips custom)
	{
		this.clock = clock;
		this.memory = memory;
		this.custom = custom;

		events[0] = new ManualResetEventSlim();
		events[1] = new ManualResetEventSlim();
		events[2] = new ManualResetEventSlim();
		events[3] = new ManualResetEventSlim();
	}

	public void Reset() { }

	public void Emulate(ulong cycles)
	{
		TriggerHighestPriorityDMA();
	}

	private void TriggerHighestPriorityDMA()
	{
		for (int i = 0; i < 4; i++)
		{
			if (priorities[i])
			{
				priorities[i] = false;
				events[i].Set();
				return;
			}
		}
	}

	public bool IsDMAEnabled(ChipRegs.DMA source)
	{
		//todo stash away this dmacon value at the beginning of the chipset tick
		var dmacon = (ChipRegs.DMA)custom.Read(0, ChipRegs.DMACONR, Size.Word);
		var mask = ChipRegs.DMA.DMAEN | source;
		return (dmacon & mask) == mask;
	}

	public ulong Read(uint address, DMAPriority priority, Size size)
	{
		BlockWaitingForDMA(priority);

		ulong value = memory.Read(0, address, size);
		if (size == Size.QWord)
			value = (value << 32) | memory.Read(0, address, size);

		BlockWaitingForChipsetTick();

		return value;
	}

	public void BlockWaitingForChipsetTick()
	{
		throw new NotImplementedException();
	}

	private void BlockWaitingForDMA(DMAPriority priority)
	{
		switch (priority)
		{
			case DMAPriority.Refresh:
			case DMAPriority.Disk:
			case DMAPriority.Audio0:
			case DMAPriority.Audio1:
			case DMAPriority.Audio2:
			case DMAPriority.Audio3:
			case DMAPriority.Bitplane:
			case DMAPriority.Sprite:
				priorities[0] = true;
				events[0].Wait();
				break;
			case DMAPriority.Copper:
				events[1].Wait();
				break;
			case DMAPriority.Blitter:
				events[2].Wait();
				break;
			case DMAPriority.CPU:
				events[3].Wait();
				break;
		}
	}

	public void Refresh()
	{
		BlockWaitingForDMA(DMAPriority.Refresh);
	}

	public void Write(uint address, DMAPriority priority, ushort value, Size size)
	{
		BlockWaitingForDMA(priority);
		memory.Write(0, address, value, size);
		BlockWaitingForChipsetTick();
	}

	public void ReliquishDMASlot()
	{
		
	}

	public uint DebugRead(uint address, Size size)
	{
		return (uint)memory.Read(0, address, size);
	}
}
