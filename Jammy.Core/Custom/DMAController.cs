using System;
using System.Linq;
using System.Text;
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
	Consume,
	CPU
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

		var slotTaken = DMASource.None;

		//check if ANY DMA is enabled
		if ((dmacon & DMA.DMAEN) == DMA.DMAEN)
		{
			for (int i = 0; i < (int)DMASource.NumDMASources-1; i++)
			{
				if (activities[i].Type == DMAActivityType.None)
				{
					//if no DMA required, continue
				}
				else if (slotTaken == DMASource.None && activities[i].Type == DMAActivityType.Consume)
				{
					//DMA engine is required
					slotTaken = (DMASource)i;
				}
				else if (slotTaken == DMASource.None && (activities[i].Priority & dmacon) != 0)
				{
					//check this DMA channel is enabled and the DMA slot hasn't been taken
					slotTaken = (DMASource)i;
				}
			}
		}

		if (slotTaken == DMASource.None && activities[(int)DMASource.CPU].Type == DMAActivityType.CPU)
		{
			//CPU memory access required
			slotTaken = DMASource.CPU;
		}

		if (slotTaken == DMASource.None)
		{
			sb.Append('x');
			return;
		}

		if (slotTaken != DMASource.CPU)
			sb.Append(slotTaken.ToString()[0]);
		else
			sb.Append('c');

		//DMA required, execute the transaction
		ExecuteDMATransfer(activities[(int)slotTaken]);
	}

	private StringBuilder sb = new StringBuilder();
	public void DebugStartOfLine()
	{
		sb.Clear();
	}

	public void DebugEndOfLine()
	{
		//logger.LogTrace(sb.ToString());
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
		activities[(int)DMASource.CPU].Type = DMAActivityType.CPU;
		cpuMemLock.WaitOne();
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
			cpuMemLock.Set();
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
