using System;
using System.Threading;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom;

public interface IDMA : IEmulate
{
	void Read(DMASource source, uint address, DMA priority, Size size, uint chipReg);
	uint DebugRead(uint address, Size size);
	void Write(DMASource source, uint address, DMA priority, ushort value, Size size);
	void NoDMA(DMASource source);
	void NeedsDMA(DMASource source);
	bool IsDMAEnabled(DMA source);
	void WaitForChipRamDMASlot();
}

public enum DMAActivityType
{
	None,
	Read,
	Write,
	Consume
}

public enum DMASource
{
	Agnus,
	Copper,
	Blitter,

	//needs to be last
	CPU,

	NumDMASources
}

public class DMAActivity
{
	public AutoResetEvent Channel { get; set; } = new AutoResetEvent(false);
	public Thread Thread { get; set; }
	public DMAActivityType Type { get; set; }
	public uint Address { get; set; }
	public ulong Value { get; set; }
	public Size Size { get; set; }
	public DMA Priority { get; set; }
	public uint ChipReg { get; set; }
}

public class DMAController : IDMA
{
	private readonly IChipsetClock clock;
	private readonly IChipRAM memory;
	private readonly IChips custom;

	private readonly DMAActivity[] activities;

	public DMAController(IChipsetClock clock, IChipRAM memory, IChips custom,
		IAgnus agnus, ICopper copper, IBlitter blitter)
	{
		this.clock = clock;
		this.memory = memory;
		this.custom = custom;

		activities = new DMAActivity[(int)DMASource.NumDMASources];
		for (int i = 0; i < (int)DMASource.NumDMASources; i++)
			activities[i] = new DMAActivity();

		//all threads begin blocked
		activities[0].Thread = new Thread(() => EmulateWrapper(agnus.Emulate, activities[0]));
		activities[1].Thread = new Thread(() => EmulateWrapper(copper.Emulate, activities[1]));
		activities[2].Thread = new Thread(() => EmulateWrapper(blitter.Emulate, activities[2]));
	}

	public void Reset() { }

	private void EmulateWrapper(Action<ulong> emulate, DMAActivity activity)
	{
		for(;;)
		{
			//wait for the Emulate() code below to release one (or more) of the DMA activities
			activity.Channel.WaitOne();
			emulate(0);
		}
	}

	public void Emulate(ulong cycles)
	{
		clock.WaitForTick();
		TriggerHighestPriorityDMA();
	}

	private void TriggerHighestPriorityDMA()
	{
		var dmacon = (DMA)custom.Read(0, ChipRegs.DMACONR, Size.Word);

		bool slotTaken = false;

		//check if ANY DMA is enabled
		if ((dmacon & DMA.DMAEN) == DMA.DMAEN)
		{
			for (int i = 0; i < (int)DMASource.NumDMASources - 1; i++)
			{
				if (activities[i].Type == DMAActivityType.None)
				{
					//if no DMA required, continue
					activities[i].Channel.Set();
				}
				else if (!slotTaken && (activities[i].Priority & dmacon) != 0)
				{
					//check this DMA channel is enabled and the DMA slot hasn't been taken
					//DMA required, execute the transaction
					ExecuteDMATransfer(activities[i]);
					//continue
					activities[i].Channel.Set();
					slotTaken = true;
				}
			}
		}
		if (slotTaken)
			return;

		//reads to Agnus memory will be waiting for Chipset tick
		if (activities[(int)DMASource.CPU].Type != DMAActivityType.None)
			ExecuteDMATransfer(activities[(int)DMASource.CPU]);
	}

	public void WaitForChipRamDMASlot()
	{
		activities[(int)DMASource.CPU].Channel.WaitOne();
	}

	private void ExecuteDMATransfer(DMAActivity activity)
	{
		if (activity.Type == DMAActivityType.Consume)
			return;
		if (activity.Type == DMAActivityType.Write)
		{
			memory.Write(0, activity.Address, (uint)activity.Value, activity.Size);
			return;
		}
		if (activity.Type == DMAActivityType.Read)
		{
			if (activity.Size == Size.QWord)
			{	
				ulong value = memory.Read(0, activity.Address, Size.QWord);
				value = (value << 32) | memory.Read(0, activity.Address+4, Size.QWord);
				custom.WriteWide(activity.ChipReg, value);
			}
			else
			{
				uint value = memory.Read(0, activity.Address, activity.Size);
				custom.Write(0, activity.ChipReg, value, activity.Size);
			}
			
		}
	}

	public void NeedsDMA(DMASource source)
	{
		activities[(int)source].Type = DMAActivityType.Consume;
	}

	public void NoDMA(DMASource source)
	{
		activities[(int)source].Type = DMAActivityType.None;
	}

	public bool IsDMAEnabled(DMA source)
	{
		//todo stash away this dmacon value at the beginning of the chipset tick
		var dmacon = (DMA)custom.Read(0, ChipRegs.DMACONR, Size.Word);
		var mask = DMA.DMAEN | source;
		return (dmacon & mask) == mask;
	}

	public void Read(DMASource source, uint address, DMA priority, Size size, uint chipReg)
	{
		var activity = activities[(int)source];
		activity.Address = address;
		activity.Priority = priority;
		activity.Size = size;
		activity.ChipReg = chipReg;
	}



	public void Write(DMASource source, uint address, DMA priority, ushort value, Size size)
	{
		var activity = activities[(int)source];
		activity.Address = address;
		activity.Priority = priority;
		activity.Value = value;
		activity.Size = size;
	}

	public uint DebugRead(uint address, Size size)
	{
		return memory.Read(0, address, size);
	}
}
