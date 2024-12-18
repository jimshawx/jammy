using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Concurrent;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core
{
	public class Interrupt : IInterrupt
	{
		private IChips custom;

		public static readonly uint[] priority = [1, 1, 1, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 6, 6, 7];
		private readonly ConcurrentQueue<uint> interruptLevelQueue;
		private readonly ILogger logger;

		public Interrupt(ILogger<Interrupt> logger)
		{
			this.logger = logger;
			logger.LogTrace("Interrupt Construction");
			interruptLevelQueue = new ConcurrentQueue<uint>();
		}

		public static uint CPUPriority(uint interrupt)
		{
			return priority[interrupt];
		}

		public void Init(IChips custom)
		{
			this.custom = custom;
		}

		public void Reset()
		{
			paulaInterruptLevel = 0;
		}

		//delay interrupt by N cpu instructions (Blitter Miracle wants this > 1)
		private const int PAULA_INTERRUPT_LATENCY = 1;

		public void Emulate()
		{
		}

		private uint paulaInterruptLevel;

		//This is called once per instruction
		public ushort GetInterruptLevel()
		{
			interruptLevelQueue.Enqueue(paulaInterruptLevel);
			if (interruptLevelQueue.Count <= PAULA_INTERRUPT_LATENCY)
				return 0;
			interruptLevelQueue.TryDequeue(out uint i);
			return (ushort)(uint)i;
			//return (ushort)(uint)interruptLevelQueue.Dequeue();
		}

		public void AssertInterrupt(uint intreq, bool asserted = true)
		{
			uint mask = (1u << (int)intreq);
			if (asserted) mask |= 0x8000;
			custom.Write(0, ChipRegs.INTREQ, mask, Size.Word);
		}

		public void SetPaulaInterruptLevel(uint intreq, uint intena)
		{
			paulaInterruptLevel = 0;

			//all interrupts disabled
			if ((intena & (1 << (int)Types.Interrupt.INTEN)) == 0) return;

			intreq &= intena;
			if (intreq == 0) return;

			for (int i = (int)Types.Interrupt.EXTER; i >= 0; i--)
			{
				if ((intreq & (1u << i)) != 0)
				{
					paulaInterruptLevel = CPUPriority((uint)i);
					break;
				}
			}
		}

		public void SetGayleInterruptLevel(uint level)
		{
			//set CPU level using Paula INTREQ
			if ((level & (1 << 2)) != 0) AssertInterrupt(Types.Interrupt.PORTS);
			if ((level & (1 << 3)) != 0) AssertInterrupt(Types.Interrupt.COPPER);
			if ((level & (1 << 6)) != 0) AssertInterrupt(Types.Interrupt.EXTER);
		}
	}
}
