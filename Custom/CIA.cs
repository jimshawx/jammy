using RunAmiga.Types;

namespace RunAmiga.Custom
{
	//https://www.amigacoding.com/index.php?title=CIA_Memory_Map

	public abstract class CIA : IEmulate, IMemoryMappedDevice
	{
		public const int PRA = 0;
		public const int PRB = 1;
		public const int DDRA = 2;
		public const int DDRB = 3;
		public const int TALO = 4;
		public const int TAHI = 5;
		public const int TBLO = 6;
		public const int TBHI = 7;
		public const int TODLO = 8;
		public const int TODMID = 9;
		public const int TODHI = 10;
		public const int NA = 11;
		public const int SDR = 12;
		public const int ICR = 13;
		public const int CRA = 14;
		public const int CRB = 15;

		protected readonly byte[] regs = new byte[16];

		protected byte icrr;
		//writing to regs[0xd] sets icr which controls which interrupts TO trigger.
		//icrr has the equivalent bits set for which interrupt WAS triggered. reset to 0 after read.

		protected Interrupt interrupt;

		private ulong timerTime;

		public virtual void Emulate(ulong ns)
		{

			timerTime += ns;
			if (timerTime > 10)// timers tick at 1/10th cpu clock
			{
				timerTime -= 10;

				//timer A running
				if ((regs[CIA.CRA] & 1) != 0)
				{
					//timer A
					regs[CIA.TALO]--;
					if (regs[CIA.TALO] == 0xff)
						regs[CIA.TAHI]--;

					if (regs[CIA.TALO] == 0 && regs[CIA.TAHI] == 0)
					{
						if ((regs[CIA.ICR] & (1 << 0)) != 0)
						{
							icrr |= (1 << 0) + 0x80;
							interrupt.TriggerInterrupt(Interrupt.PORTS);
						}

						//one shot mode?
						if ((regs[CIA.CRA] & (1 << 3)) != 0)
							regs[CIA.CRA] &= 0xfe;
					}
				}

				//timer B running
				if ((regs[CIA.CRB] & 1) != 0)
				{
					//timer B
					regs[CIA.TBLO]--;
					if (regs[CIA.TBLO] == 0xff)
						regs[CIA.TBHI]--;

					if (regs[CIA.TBLO] == 0 && regs[CIA.TBHI] == 0)
					{
						if ((regs[CIA.ICR] & (1 << 1)) != 0)
						{
							icrr |= (1 << 1) + 0x80;
							interrupt.TriggerInterrupt(Interrupt.PORTS);
						}

						//one shot mode?
						if ((regs[CIA.CRB] & (1 << 3)) != 0)
							regs[CIA.CRB] &= 0xfe;
					}
				}
			}
		}

		public virtual void Reset()
		{
			for (int i = 0; i < 16; i++)
				regs[i] = 0;
			regs[CIA.TAHI] = regs[CIA.TALO] = regs[CIA.TBHI] = regs[CIA.TBLO] = 0xff;

		}

		public virtual bool IsMapped(uint address)
		{
			return (address >> 16) == 0xbf;
		}

		public abstract uint Read(uint insaddr, uint address, Size size);

		public abstract void Write(uint insaddr, uint address, uint value, Size size);
	}
}
