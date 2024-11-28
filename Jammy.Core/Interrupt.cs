using System;
using System.Collections;
using Jammy.Core.Custom;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core
{
	public class Interrupt : IInterrupt
	{
		private IChips custom;

		private readonly Queue q = new Queue();

		public static uint[] priority = new uint[]{ 1,1,1,2,3,3,3,4,4,4,4,5,5,6,6,7};

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
			gayleInterruptLevel = 0;
		}

		//in chipset clocks (Blitter Miracle wants this > 1)
		private const int PAULA_INTERRUPT_LATENCY = 0;
		//minimum value
		//private const int PAULA_INTERRUPT_LATENCY = 1;

		private int intreqPending = 0;
		public void Emulate()
		{
			if (PAULA_INTERRUPT_LATENCY == 0) return;

			q.Enqueue(paulaInterruptLevel);
			if (q.Count == PAULA_INTERRUPT_LATENCY)
				paulaInterruptLevelLagged = (uint)q.Dequeue();

			//if (intreqPending > 0)
			//{
			//	intreqPending--;
			//	if (intreqPending == 0)
			//	{
			//		SetPaulaInterruptLevelReal(intenaStash, intreqStash);
			//	}
			//}
		}

		private uint paulaInterruptLevelLagged;

		//level is the IPLx interrupt bits in SR

		private uint paulaInterruptLevel;
		private uint gayleInterruptLevel;

		//private readonly object locker = new object();

		public ushort GetInterruptLevel()
		{
			//lock (locker)
			{
				if (PAULA_INTERRUPT_LATENCY == 0)
					return (ushort)Math.Max(paulaInterruptLevel, gayleInterruptLevel);
				return (ushort)Math.Max(paulaInterruptLevelLagged, gayleInterruptLevel);
			}
		}

		public void AssertInterrupt(uint intreq, bool asserted = true)
		{
			//lock (locker)
			{
				uint mask = (1u << (int)intreq);
				if (asserted) mask |= 0x8000;
				custom.Write(0, ChipRegs.INTREQ, mask, Size.Word);
			}
		}

		public void SetPaulaInterruptLevel(uint intreq, uint intena)
		{
			//lock (locker)
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
		}

		public void SetGayleInterruptLevel(uint level)
		{
			//set CPU level outside of Paula

			//gayleInterruptLevel = 0;
			//for (int i = 6; i >= 0; i--)
			//{
			//	if ((level & (1 << i)) != 0)
			//	{
			//		gayleInterruptLevel = (uint)i;
			//		break;
			//	}
			//}

			//set CPU level using Paula INTREQ
			if ((level & (1 << 2)) != 0) AssertInterrupt(Types.Interrupt.PORTS);
			if ((level & (1 << 3)) != 0) AssertInterrupt(Types.Interrupt.COPPER);
			if ((level & (1 << 6)) != 0) AssertInterrupt(Types.Interrupt.EXTER);
		}
	}
}
