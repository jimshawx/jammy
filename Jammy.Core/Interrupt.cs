using System;
using Jammy.Core.Custom;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core
{
	public class Interrupt : IInterrupt
	{
		private IChips custom;

		public const uint NMI = 15;
		public const uint INTEN = 14;
		public const uint EXTER = 13;
		public const uint DSKSYNC = 12;
		public const uint RBF = 11;
		public const uint AUD3 = 10;
		public const uint AUD2 = 9;
		public const uint AUD1 = 8;
		public const uint AUD0 = 7;
		public const uint BLIT = 6;
		public const uint VERTB = 5;
		public const uint COPPER = 4;
		public const uint PORTS = 3;
		public const uint SOFTINT = 2;
		public const uint DSKBLK = 1;
		public const uint TBE = 0;

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

		//level is the IPLx interrupt bits in SR

		private uint paulaInterruptLevel;
		private uint gayleInterruptLevel;

		//private readonly object locker = new object();

		public ushort GetInterruptLevel()
		{
			//lock (locker)
			{
				return (ushort)Math.Max(paulaInterruptLevel, gayleInterruptLevel);
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
				if ((intena & (1 << (int)Interrupt.INTEN)) == 0) return;

				intreq &= intena;
				if (intreq == 0) return;

				for (int i = (int)Interrupt.EXTER; i >= 0; i--)
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
			if ((level & (1 << 2)) != 0) AssertInterrupt(PORTS);
			if ((level & (1 << 3)) != 0) AssertInterrupt(COPPER);
			if ((level & (1 << 6)) != 0) AssertInterrupt(EXTER);
		}
	}
}
