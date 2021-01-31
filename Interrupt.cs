using System.Runtime.InteropServices;
using RunAmiga.Custom;
using RunAmiga.Types;

namespace RunAmiga
{
	public class Interrupt : IEmulate
	{
		private Chips custom;
		public const uint NMI = 15;
		public const uint INTEN = 14;
		public const uint EXTER = 13;
		public const uint DSKSYNC = 12;
		public const uint RBF = 11;
		public const uint AUD1 = 10;
		public const uint AUD3 = 9;
		public const uint AUD0 = 8;
		public const uint AUD2 = 7;
		public const uint BLIT = 6;
		public const uint VERTB = 5;
		public const uint COPPER = 4;
		public const uint PORTS = 3;
		public const uint TBE = 2;
		public const uint DSKBLK = 1;
		public const uint SOFTINT = 0;

		public static uint[] priority = new uint[]{ 1,1,1,2,3,3,3,4,4,4,4,5,5,6,6,7};

		public static uint CPUPriority(uint interrupt)
		{
			return priority[interrupt];
		}

		public void Init(Chips custom)
		{
			this.custom = custom;
		}

		public void Emulate(ulong cycles)
		{
			
		}

		public void Reset()
		{
			interruptPending = 0;
		}

		//level is the IPLx interrupt bits in SR

		private uint interruptPending;

		public bool InterruptPending()
		{
			return interruptPending != 0;
		}

		public void ResetInterrupt()
		{
			interruptPending = 0;
		}

		public ushort GetInterruptLevel()
		{
			return (ushort)interruptPending;
		}

		//public void EnableSchedulerAttention()
		//{
		//	//enable scheduler attention
		//	uint execBase = memory.Read(0, 4, Size.Long);
		//	uint sysflags = memory.Read(0, execBase + 0x124, Size.Byte);
		//	sysflags |= 0x80;
		//	memory.Write(0, execBase + 0x124, sysflags, Size.Byte);
		//	musashiMemory.Write(0, execBase + 0x124, sysflags, Size.Byte);
		//}


		public void TriggerInterrupt(uint interrupt)
		{
			custom.Write(0, ChipRegs.INTREQ, 0x8000 + (1u << (int)interrupt), Size.Word);
			interruptPending = CPUPriority(interrupt);
			//Musashi_set_irq(interruptPending);
		}

		private void EnableInterrupt(uint interrupt)
		{
			uint intenar = custom.Read(0, ChipRegs.INTENAR, Size.Word);
			//only write the bit if necessary
			if ((intenar & (1u << (int)interrupt)) == 0)
				custom.Write(0, ChipRegs.INTENA, 0x8000 + (1u << (int)interrupt), Size.Word);
		}
	}
}
