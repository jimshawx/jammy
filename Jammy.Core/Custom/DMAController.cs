using System;
using System.Threading;
using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom;

public enum DMAActivityType
{
	None,
	Read,
	Write,
	Consume,
	CPU
}

public class DMAActivity
{
	public DMAActivity()
	{
		Type = DMAActivityType.None;
	}

	public DMAActivityType Type { get; set; }
	public uint Address { get; set; }
	public ulong Value { get; set; }
	public Size Size { get; set; }
	public DMA Priority { get; set; }
	public uint ChipReg { get; set; }

	public override string ToString()
	{
		switch (Priority)
		{
			case 0: return "c"; //CPU
			case DMA.BPLEN: return "B";
			case DMA.COPEN: return "C";
			case DMA.BLTEN: return "b";
			case DMA.SPREN: return "S";
			case DMA.DSKEN: return "D";
			case DMA.AUD0EN: return "A";
			case DMA.AUD1EN: return "A";
			case DMA.AUD2EN: return "A";
			case DMA.AUD3EN: return "A";
		}
		return "x";
	}
}

public class DMAController : IDMA
{
	private readonly IChipsetClock clock;
	private readonly IChipsetDebugger debugger;
	private readonly IChipRAM memory;
	private readonly IChips custom;
	private readonly ILogger<DMAController> logger;

	private readonly DMAActivity[] activities;

	private volatile int cpuMemTick;

	public DMAController(IChipRAM memory, IChips custom, IChipsetClock clock,
		IChipsetDebugger debugger, ILogger<DMAController> logger)
	{
		this.memory = memory;
		this.custom = custom;
		this.logger = logger;
		this.clock = clock;
		this.debugger = debugger;

		activities = new DMAActivity[(int)DMASource.NumDMASources];
		for (int i = 0; i < (int)DMASource.NumDMASources; i++)
			activities[i] = new DMAActivity();
	}

	public void Reset()
	{
		//unlock all the DMA channels
		foreach (var activity in activities)
		{
			activity.Type = DMAActivityType.None;
		}
	}

	public void TriggerHighestPriorityDMA()
	{
		var dmacon = (DMA)custom.Read(0, ChipRegs.DMACONR, Size.Word);

		DMAActivity slotTaken = null;

		//check if ANY DMA is enabled
		if ((dmacon & DMA.DMAEN) == DMA.DMAEN)
		{
			for (int i = 0; i < (int)DMASource.NumDMASources-1; i++)
			{
				if (activities[i].Type == DMAActivityType.None)
				{
					//if no DMA required, continue
				}
				else if (slotTaken == null && (activities[i].Priority & dmacon) != 0)
				{
					//check this DMA channel is enabled and the DMA slot hasn't been taken
					slotTaken = activities[i];
				}
			}
		}

		if (slotTaken == null && activities[(int)DMASource.CPU].Type == DMAActivityType.CPU)
		{
			//CPU memory access required
			slotTaken = activities[(int)DMASource.CPU];
		}

		if (slotTaken == null)
		{
			debugger.SetDMAActivity('-');
			return;
		}

		debugger.SetDMAActivity(slotTaken.ToString()[0]);

		//DMA required, execute the transaction
		ExecuteDMATransfer(slotTaken);
	}

	public bool IsWaitingForDMA(DMASource source)
	{
		return activities[(int)source].Type != DMAActivityType.None;
	}

	public void ClearWaitingForDMA(DMASource source)
	{
		activities[(int)source].Type = DMAActivityType.None;
	}

	public void WaitForChipRamDMASlot()
	{
		activities[(int)DMASource.CPU].Priority = 0;
		activities[(int)DMASource.CPU].Type = DMAActivityType.CPU;

		while (cpuMemTick == 0) /*extreme busy wait, anything else is far too slow*/ ;
		cpuMemTick = 0;
	}

	private void ExecuteDMATransfer(DMAActivity activity)
	{
		if (activity.Type == DMAActivityType.Consume)
		{
			activity.Type = DMAActivityType.None;
			return;
		}

		if (activity.Type == DMAActivityType.CPU)
		{
			activity.Type = DMAActivityType.None;
			cpuMemTick = 1;
			return;
		}

		if (activity.Type == DMAActivityType.Write)
		{
			activity.Type = DMAActivityType.None;
			memory.Write(0, activity.Address, (uint)activity.Value, activity.Size);
			return;
		}

		if (activity.Type == DMAActivityType.Read)
		{
			activity.Type = DMAActivityType.None;
			if (activity.Size == Size.QWord)
			{	
				ulong value = memory.Read(0, activity.Address, Size.Long);
				value = (value << 32) | memory.Read(0, activity.Address+4, Size.Long);
				custom.WriteWide(activity.ChipReg, value);
			}
			else
			{
				uint value = memory.Read(0, activity.Address, activity.Size);
				custom.Write(0, activity.ChipReg, value, activity.Size);
			}
			return;
		}

		throw new ArgumentOutOfRangeException();
	}

	public void NeedsDMA(DMASource source, DMA priority)
	{
		var activity = activities[(int)source];
		activity.Type = DMAActivityType.Consume;
		activity.Priority = priority;
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
		activity.Type = DMAActivityType.Read;
		activity.Address = address;
		activity.Priority = priority;
		activity.Size = size;
		activity.ChipReg = chipReg;
	}

	public void Write(DMASource source, uint address, DMA priority, ushort value, Size size)
	{
		var activity = activities[(int)source];
		activity.Type = DMAActivityType.Write;
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
