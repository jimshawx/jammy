using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom;

public class DMAController : IDMA
{
	private readonly IChipsetDebugger debugger;
	private IAudio audio;
	private readonly IChipsetClock chipsetClock;
	private readonly IChips chips;
	private IContendedMemoryMappedDevice chipRAM;
	private IMemoryMapper memoryMapper;
	private readonly ILogger<DMAController> logger;
	private readonly DMAActivity[] activities;

	public DMAController(IChipsetClock chipsetClock, IChips chips,
		IChipsetDebugger debugger, ILogger<DMAController> logger)
	{
		this.chipsetClock = chipsetClock;
		this.chips = chips;
		this.logger = logger;
		this.debugger = debugger;

		activities = new DMAActivity[(int)DMASource.NumDMASources];
		for (int i = 0; i < (int)DMASource.NumDMASources; i++)
			activities[i] = new DMAActivity();
	}

	public void Init(IAudio audio, IMemoryMapper memoryMapper, IChipRAM chipRAM)
	{
		this.audio = audio;
		this.memoryMapper = memoryMapper;
		this.chipRAM = (IContendedMemoryMappedDevice)chipRAM;
	}

	public void Reset()
	{
		//unlock all the DMA channels
		foreach (var activity in activities)
		{
			activity.Type = DMAActivityType.None;
		}
	}

	private Func<ushort> chipsetSync = NullSync;
	public void SetSync(Func<ushort> chipsetSync)
	{
		this.chipsetSync = chipsetSync;
	}
	private static ushort NullSync() { return 0; }

	public uint ChipsetSync()
	{
		return chipsetSync();
	}

	private int blitHogCount = 0;

	public DMAActivity TriggerHighestPriorityDMA()
	{
		DMAActivity slotTaken = null;

		//check if ANY DMA is enabled
		if (((DMA)dmacon & DMA.DMAEN) == DMA.DMAEN)
		{
			bool blitHog = ((DMA)dmacon & DMA.BLTPRI) == DMA.BLTPRI;

			for (int i = 0; i < (int)DMASource.NumDMASources-1; i++)
			{
				if (activities[i].Type == DMAActivityType.None)
				{
					//if no DMA required, continue
					continue;
				}
				else if ((activities[i].Priority & (DMA)dmacon) != 0)
				{
					//check this DMA channel is enabled and the DMA slot hasn't been taken
					slotTaken = activities[i];

					var src = (DMASource)i;
					switch (src)
					{
						case DMASource.Copper:
							//copper can only use even-numbered slots
							
							//if it's odd, it can't be used
							if ((chipsetClock.HorizontalPos & 1) == 1)
								slotTaken = null;
							break;

						case DMASource.Blitter:
							if (blitHog)
							{
								blitHogCount = 0;
							}
							else
							{
								blitHogCount++;
								if (blitHogCount > 3)
								{
									slotTaken = null;
									blitHogCount = 0;
								}
							}
							break;

						case DMASource.Agnus:
							//Agnus can only use odd-numbered slots EXCEPT when it's doing bitplane DMA
							//that requires even slots too (e.g. low-res > 4bpp, hi-res > 2bpp, sh-res)

							//if it's even, it can't be used
							if ((chipsetClock.HorizontalPos & 1) == 0)
							{
								//EXCEPT when BR is set and it's bitplane DMA
								if (!slotTaken.BR || slotTaken.Priority != DMA.BPLEN)
									slotTaken = null;
							}
							break;
					}
				}
				if (slotTaken != null)
					break;
			}
		}
		else
		{
			//it could still be memory refresh, even if DMA is disabled : Source = Agnus, Priority = DMAEN
			var agnusActivity = activities[(int)DMASource.Agnus];
			if (agnusActivity.Type == DMAActivityType.Consume && agnusActivity.Priority == (DMA)DMA.DMAEN)
				slotTaken = agnusActivity;
		}

		lastDMASlot = slotTaken;

		if (slotTaken == null)
		{
			debugger.SetDMAActivity(null);
			return null;
		}

		//DMA required, execute the transaction
		ExecuteDMATransfer(slotTaken);
		return slotTaken;
	}

	private DMAActivity lastDMASlot = null;
	public bool LastDMASlotWasUsedByChipset()
	{
		return lastDMASlot != null;
	}

	public void ClearSlot()
	{
		lastDMASlot = null;
	}

	public void ExecuteCPUDMASlot()
	{
		ExecuteDMATransfer(activities[(int)DMASource.CPU]);
	}

	public bool IsWaitingForDMA(DMASource source)
	{
		return activities[(int)source].Type != DMAActivityType.None;
	}

	public void ClearWaitingForDMA(DMASource source)
	{
		activities[(int)source].Type = DMAActivityType.None;
	}

	public void SetCPUWaitingForDMA()
	{
		activities[(int)DMASource.CPU].Priority = 0;
		activities[(int)DMASource.CPU].Type = DMAActivityType.CPU;
	}

	private void Consume(DMAActivity activity)
	{
		activity.Type = DMAActivityType.None;
		//todo: debugging, remove
		activity.Address = 0;
		activity.Value = 0;
		activity.Size = Size.Byte;
		activity.Priority = 0;
		activity.ChipReg = 0;
		activity.Target = CPUTarget.None;
	}

	public ushort LastRead { get; private set; }

	private void ExecuteDMATransfer(DMAActivity activity)
	{
		switch (activity.Type)
		{
			case DMAActivityType.Consume:break;
			case DMAActivityType.CPU: break;

			case DMAActivityType.WriteChip:
				chipRAM.ImmediateWrite(0, activity.Address, (uint)activity.Value, activity.Size);
				break;

			case DMAActivityType.ReadChip:
				if (activity.Size == Size.QWord)
				{
					ulong value = chipRAM.ImmediateRead(0, activity.Address, Size.Long);
					value = (value << 32) | chipRAM.ImmediateRead(0, activity.Address + 4, Size.Long);
					activity.Value = value;
					chips.ImmediateWriteWide(activity.ChipReg, value);
				}
				else if (activity.Size == Size.LWord)
				{
					ulong value = chipRAM.ImmediateRead(0, activity.Address, Size.Long);
					activity.Value = value;
					chips.ImmediateWriteWide(activity.ChipReg, value);
				}
				else
				{
					uint value = chipRAM.ImmediateRead(0, activity.Address, activity.Size);
					activity.Value = value;
					chips.ImmediateWrite(0, activity.ChipReg, value, activity.Size);
				}
				break;

			case DMAActivityType.ReadCPU:
				LastRead = (ushort)memoryMapper.ImmediateRead(0, activity.Address, activity.Size);
				activity.Value = LastRead;
				break;

			case DMAActivityType.WriteCPU:
				memoryMapper.ImmediateWrite(0, activity.Address, (uint)activity.Value, activity.Size);
				break;

			case DMAActivityType.None: break;

			default:
				throw new ArgumentOutOfRangeException(nameof(activity.Type));
		}
		debugger.SetDMAActivity(activity);
		if (logit) logger.LogTrace($"DMA  {chipsetClock} {activity.Type} {activity.Address:X8} {activity.ChipReg:X8}");
		Consume(activity);
	}
	bool logit = false;

	//requires to be the highest priority DMA, but does not eat the memory cycle (CPU can still have it)
	public void NeedsDMA(DMASource source, DMA priority)
	{
		var activity = activities[(int)source];
		activity.Type = DMAActivityType.Consume;
		activity.Priority = priority;
	}

	public void DebugExecuteDMAActivity(DMASource source)
	{
		var activity = activities[(int)source];
		ExecuteDMATransfer(activity);
	}

	public void DebugExecuteAllDMAActivity()
	{
		foreach (var activity in activities)
			ExecuteDMATransfer(activity);
	}

	public void FullSpeedExecuteAllDMAActivity()
	{
		for (int i = 0; i < activities.Length; i++)
		{
			//don't run the copper TOO fast
			if ((DMASource)i == DMASource.Copper && (chipsetClock.HorizontalPos & 1) != 0)
				continue;
			ExecuteDMATransfer(activities[i]);
		}
	}

	public void NoDMA(DMASource source)
	{
		activities[(int)source].Type = DMAActivityType.None;
	}

	public bool IsDMAEnabled(DMA source)
	{
		//todo stash away this dmacon value at the beginning of the chipset tick
		//var dmacon = (DMA)custom.Read(0, ChipRegs.DMACONR, Size.Word);
		var mask = DMA.DMAEN | source;
		return ((DMA)dmacon & mask) == mask;
	}

	//used when the chipset wants to read from chip memory into a chip register
	public void ReadReg(DMASource source, uint address, DMA priority, Size size, uint chipReg)
	{
		var activity = activities[(int)source];
		activity.Type = DMAActivityType.ReadChip;
		activity.Address = address;
		activity.Priority = priority;
		activity.Size = size;
		activity.ChipReg = chipReg;

		//this (chipsetClock.HorizontalPos&1) is currently 1 for the first 4 planes, then 0 for the >5th one
		//so when it's 0, we want a special case
		activity.BR = (chipsetClock.HorizontalPos&1)==0 && priority == DMA.BPLEN;
	}

	//used when chipset itself wants to write to chip memory
	public void WriteChip(DMASource source, uint address, DMA priority, ushort value, Size size)
	{
		var activity = activities[(int)source];
		activity.Type = DMAActivityType.WriteChip;
		activity.Address = address;
		activity.Priority = priority;
		activity.Value = value;
		activity.Size = size;
	}

	//used when the CPU wants to read from chip memory or chip register
	public void ReadCPU(CPUTarget target, uint address, Size size)
	{
		var activity = activities[(int)DMASource.CPU];
		activity.Type = DMAActivityType.ReadCPU;
		activity.Address = address;
		activity.Size = size;
		activity.Target = target;
		activity.Priority = DMA.DMAEN;
	}

	//used when the CPU wants to write to chip memory or chip register
	public void WriteCPU(CPUTarget target, uint address, ushort value, Size size)
	{
		var activity = activities[(int)DMASource.CPU];
		activity.Type = DMAActivityType.WriteCPU;
		activity.Address = address;
		activity.Size = size;
		activity.Target = target;
		activity.Value = value;
		activity.Priority = DMA.DMAEN;
	}

	public ushort Read(uint insaddr, uint address)
	{
		if (address == ChipRegs.DMACONR)
			return (ushort)(dmacon&0x7fff);

		return 0;
	}

	private ushort dmacon;

	public void Write(uint insaddr, uint address, ushort value)
	{
		if (address == ChipRegs.DMACON)
		{
			var p = dmacon;

			if ((value & 0x8000) != 0)
				dmacon |= (ushort)(value & 0x9fff); //can't set BBUSY or BZERO
			else
				dmacon &= (ushort)(~value | 0x6000); //can't clear BBUSY or BZERO

			//if ((dmacon & (int)DMA.COPEN) != (p & (int)DMA.COPEN))
			//	logger.LogTrace($"COPEN {((dmacon & (int)DMA.COPEN) != 0 ? "on" : "off")} @{insaddr:X8} {chipsetClock}");
			//if ((dmacon & (int)DMA.BLTEN) != (p & (int)DMA.BLTEN))
			//	logger.LogTrace($"BLTEN {((dmacon & (int)DMA.BLTEN) != 0 ? "on" : "off")} @{insaddr:X8} {chipsetClock}");

			audio.WriteDMACON((ushort)(dmacon & 0x7fff));
		}
	}

	//this is how to set/clear BBUSY/BZERO
	public void WriteDMACON(ushort bits)
	{
		if ((bits & 0x8000) != 0)
			dmacon |= bits;
		else
			dmacon &= (ushort)~bits;
	}

	public ushort ReadDMACON()
	{
		return dmacon;
	}

	public uint DebugChipsetRead(uint address, Size size)
	{
		if (address == ChipRegs.DMACON) return dmacon;
		if (address == ChipRegs.DMACONR) return (uint)(dmacon&0x7fff);
		return 0;
	}

	public uint DebugRead(uint address, Size size)
	{
		return memoryMapper.ImmediateRead(0, address, size);
	}

	public void Save(JArray obj)
	{
		var jo = new JObject();
		jo.Add("activities", JToken.FromObject(activities));
		jo.Add("id", "dma");
		obj.Add(jo);
	}

	public void Load(JObject obj)
	{
		if (!PersistenceManager.Is(obj, "dma")) return;

		obj.GetValue("activities")
			.Select(x =>
				new DMAActivity
				{
					Address = uint.Parse((string)x["Address"]),
					ChipReg = uint.Parse((string)x["ChipReg"]),
					Priority = (DMA)Enum.Parse(typeof(DMA), (string)x["Priority"]),
					Size = (Size)Enum.Parse(typeof(Size), (string)x["Size"]),
					Type = (DMAActivityType)Enum.Parse(typeof(DMAActivityType), (string)x["Type"]),
					Value = ulong.Parse((string)x["Value"]),
				})
			.ToArray()
			.CopyTo(activities, 0);
	}

}
