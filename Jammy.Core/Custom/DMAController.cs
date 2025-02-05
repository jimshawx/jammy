using System;
using System.Linq;
using Jammy.Core.Debug;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom;

public class DMAController : IDMA
{
	private readonly IChipsetDebugger debugger;
	private IAudio audio;
	//private readonly IChips custom;
	private readonly IChipsetClock chipsetClock;
	private IMemoryMapper memoryMapper;
	private readonly ILogger<DMAController> logger;
	private readonly DMAActivity[] activities;

	public DMAController(
		//IAudio audio,// IChips custom,
		IChipsetClock chipsetClock,
		//IMemoryMapper memoryMapper,
		IChipsetDebugger debugger, ILogger<DMAController> logger)
	{
		//this.audio = audio;
		//this.custom = custom;
		this.chipsetClock = chipsetClock;
		//this.memoryMapper = memoryMapper;
		this.logger = logger;
		this.debugger = debugger;

		activities = new DMAActivity[(int)DMASource.NumDMASources];
		for (int i = 0; i < (int)DMASource.NumDMASources; i++)
			activities[i] = new DMAActivity();
	}

	public void Init(IAudio audio, IMemoryMapper memoryMapper)
	{
		this.audio = audio;
		this.memoryMapper = memoryMapper;
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

	public void TriggerHighestPriorityDMA()
	{
		//var dmacon = (DMA)custom.Read(0, ChipRegs.DMACONR, Size.Word);

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

					if ((DMASource)i == DMASource.Copper)
					{
						//copper can only use odd-numbered slots
						if ((chipsetClock.HorizontalPos & 1)!=0)
							slotTaken = null;
					}

					if ((DMASource)i == DMASource.Blitter)
					{
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

					}
				}
				if (slotTaken != null)
					break;
			}
		}

		lastDMASlot = slotTaken;

		////cpu can only use even-numbered slots
		//if (slotTaken == null && 
		//	(activities[(int)DMASource.CPU].Type == DMAActivityType.CPU 
		//	|| activities[(int)DMASource.CPU].Type == DMAActivityType.ReadCPU
		//	|| activities[(int)DMASource.CPU].Type == DMAActivityType.WriteCPU ) 
		//	/*&& (chipsetClock.HorizontalPos & 1) == 0*/)
		//{
		//	//CPU memory access required
		//	slotTaken = activities[(int)DMASource.CPU];
		//}

		debugger.SetDMAActivity(slotTaken);

		if (slotTaken == null)
		{
			//if (chipsetClock.VerticalPos == 100)
			//	logger.LogTrace($"None {chipsetClock.HorizontalPos}");
			return;
		}

		//DMA required, execute the transaction
		ExecuteDMATransfer(slotTaken);
	}

	private DMAActivity lastDMASlot = null;
	public bool LastDMASlotWasUsedByChipset()
	{
		return lastDMASlot != null;
	}

	public void ExecuteCPUDMASlot()
	{
		//debugger.SetDMAActivity(activities[(int)DMASource.CPU]);
		ExecuteDMATransfer(activities[(int)DMASource.CPU]);
		lastDMASlot = activities[(int)DMASource.CPU];
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
		//if (chipsetClock.VerticalPos == 100)
		//	logger.LogTrace($"{activity.Type} {chipsetClock.HorizontalPos}");

		switch (activity.Type)
		{
			case DMAActivityType.Consume:break;
			case DMAActivityType.CPU: break;

			case DMAActivityType.Write:
				memoryMapper.Write(0, activity.Address, (uint)activity.Value, activity.Size);
				break;

			case DMAActivityType.WriteReg:
				memoryMapper.ImmediateWrite(0, activity.ChipReg, (uint)activity.Value, Size.Word);
				break;

			case DMAActivityType.Read:
				if (activity.Size == Size.QWord)
				{
					ulong value = memoryMapper.ImmediateRead(0, activity.Address, Size.Long);
					value = (value << 32) | memoryMapper.ImmediateRead(0, activity.Address + 4, Size.Long);
					throw new NotImplementedException();
					//custom.WriteWide(activity.ChipReg, value);
				}
				else
				{
					uint value = memoryMapper.ImmediateRead(0, activity.Address, activity.Size);
					memoryMapper.ImmediateWrite(0, activity.ChipReg, value, activity.Size);
				}
				break;

			case DMAActivityType.ReadCPU:
				if (activity.Target == CPUTarget.ChipRAM)
					LastRead = (ushort)memoryMapper.ImmediateRead(0, activity.Address, activity.Size);
				else if (activity.Target == CPUTarget.SlowRAM)
					LastRead = (ushort)memoryMapper.ImmediateRead(0, activity.Address, activity.Size);
				else if (activity.Target == CPUTarget.ChipReg)
					LastRead = (ushort)memoryMapper.ImmediateRead(0, activity.Address, activity.Size);
				else if (activity.Target == CPUTarget.KickROM)
					LastRead = (ushort)memoryMapper.ImmediateRead(0, activity.Address, activity.Size);
				//if (chipsetClock.VerticalPos == 100)
				//	logger.LogTrace($"R {chipsetClock.HorizontalPos}");
				break;

			case DMAActivityType.WriteCPU:
				if (activity.Target == CPUTarget.ChipRAM)
					memoryMapper.ImmediateWrite(0, activity.Address, (uint)activity.Value, activity.Size);
				else if (activity.Target == CPUTarget.SlowRAM)
					memoryMapper.ImmediateWrite(0, activity.Address, (uint)activity.Value, activity.Size);
				else if (activity.Target == CPUTarget.ChipReg)
					memoryMapper.ImmediateWrite(0, activity.Address, (uint)activity.Value, activity.Size);
				//if (chipsetClock.VerticalPos == 100)
				//	logger.LogTrace($"W {chipsetClock.HorizontalPos}");
				break;

			case DMAActivityType.None: break;

			default:
				throw new ArgumentOutOfRangeException(nameof(activity.Type));
		}
		Consume(activity);
	}

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

	public void WriteReg(DMASource source, uint chipReg, DMA priority, ushort value)
	{
		var activity = activities[(int)source];
		activity.Type = DMAActivityType.WriteReg;
		activity.ChipReg = chipReg;
		activity.Priority = priority;
		activity.Value = value;
	}

	public void ReadCPU(CPUTarget target, uint address, Size size)
	{
		var activity = activities[(int)DMASource.CPU];
		activity.Type = DMAActivityType.ReadCPU;
		activity.Address = address;
		activity.Size = size;
		activity.Target = target;
		activity.Priority = DMA.DMAEN;
	}

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

			if ((dmacon & (int)DMA.COPEN) != (p & (int)DMA.COPEN))
				logger.LogTrace($"COPEN {((dmacon & (int)DMA.COPEN) != 0 ? "on" : "off")} @{insaddr:X8} {chipsetClock.TimeStamp()}");
			if ((dmacon & (int)DMA.BLTEN) != (p & (int)DMA.BLTEN))
				logger.LogTrace($"BLTEN {((dmacon & (int)DMA.BLTEN) != 0 ? "on" : "off")} @{insaddr:X8} {chipsetClock.TimeStamp()}");

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
