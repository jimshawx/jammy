using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
	Consume
}

public class DMAActivity
{
	//public AutoResetEvent Channel { get; set; } = new AutoResetEvent(false);
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
	private readonly ILogger<DMAController> logger;

	private readonly DMAActivity[] activities;

	private readonly AutoResetEvent cpuMemLock = new AutoResetEvent(false);

	public DMAController(IChipRAM memory, IChips custom, IChipsetClock clock,
		IAgnus agnus, ICopper copper, IBlitter blitter, ILogger<DMAController> logger)
	{
		this.memory = memory;
		this.custom = custom;
		this.logger = logger;
		this.clock = clock;

		activities = new DMAActivity[(int)DMASource.NumDMASources];
		for (int i = 0; i < (int)DMASource.NumDMASources; i++)
			activities[i] = new DMAActivity();

		//all threads begin blocked
		activities[0].Thread = new Thread(() => EmulateWrapper(agnus.Emulate, activities[0]));
		activities[1].Thread = new Thread(() => EmulateWrapper(copper.Emulate, activities[1]));
		activities[2].Thread = new Thread(() => EmulateWrapper(blitter.Emulate, activities[2]));

		activities[0].Thread.Name = "Agnus";
		activities[1].Thread.Name = "Copper";
		activities[2].Thread.Name = "Blitter";

		foreach (var activity in activities.Take(3))
			activity.Thread.Start();
	}

	public void Reset()
	{
		//unlock all the DMA channels
		foreach (var activity in activities)
		{
			activity.Type = DMAActivityType.None;
		}
	}

	private void EmulateWrapper(Action<ulong> emulate, DMAActivity activity)
	{
		clock.RegisterThread();
		for(;;)
		{
			clock.WaitForTick();
			//if (activity.Type == DMAActivityType.None)
				emulate(0);
			clock.Ack();
		}
	}

	//public void Emulate(ulong cycles)
	//{
	//	clock.WaitForTick();
	//	TriggerHighestPriorityDMA();
	//	clock.Ack();
	//}

	public void TriggerHighestPriorityDMA()
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
				}
				else if (!slotTaken && activities[i].Type == DMAActivityType.Consume)
				{
					activities[i].Type = DMAActivityType.None;
					slotTaken = true;
				}
				else if (!slotTaken && (activities[i].Priority & dmacon) != 0)
				{
					//check this DMA channel is enabled and the DMA slot hasn't been taken
					//DMA required, execute the transaction
					ExecuteDMATransfer(activities[i]);
					activities[i].Type = DMAActivityType.None;
					//continue
					slotTaken = true;
				}
			}
		}
		if (slotTaken)
			return;

		//reads to Agnus memory will be waiting for Chipset tick
		if (activities[(int)DMASource.CPU].Type != DMAActivityType.None)
		{
			//can do CPU chip mem now
			//ExecuteDMATransfer(activities[(int)DMASource.CPU]);
			activities[(int)DMASource.CPU].Type = DMAActivityType.None;

			cpuMemLock.Set();
		}
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
		activities[(int)DMASource.CPU].Type = DMAActivityType.Consume;
		cpuMemLock.WaitOne();
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
