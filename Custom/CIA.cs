using System;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	//https://www.amigacoding.com/index.php?title=CIA_Memory_Map
	//http://www.theflatnet.de/pub/cbm/amiga/AmigaDevDocs/hard_f.html#f-2-3
	[Flags]
	public enum CR
	{
		START=1,
		PBON=2,
		OUTMODE=4,
		RUNMODE=8,
		LOAD=16,
		INMODE0=32,
		CRB_INMODE1 = 64,
		CRA_SPMODE = 64,
		CRB_ALARM=128
	}

	[Flags]
	public enum ICRB
	{
		TIMERA=1,
		TIMERB=2,
		TODALARM=4,
		SERIAL=8,
		FLAG=16,

		IR=128,
	}

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

		protected readonly uint[] regs = new uint[16];

		protected byte icrr;
		//writing to regs[ICR] sets icr which controls which interrupts TO trigger.
		//icrr has the equivalent bits set for which interrupt WAS triggered. reset to 0 after read.

		private ushort timerA;
		private ushort timerB;

		private ushort timerAreset;
		private ushort timerBreset;

		protected Interrupt interrupt;

		private ulong timerTime;

		private uint todAlarm;
		private uint todLatch;

		private uint todTimer;

		private bool todStopped;
		private bool todLatched;

		public virtual void Emulate(ulong cycles)
		{
			timerTime += cycles;
			if (timerTime > 10)// timers tick at 1/10th cpu clock
			{
				timerTime -= 10;

				//timer A running
				if ((regs[CIA.CRA] & (uint)CR.START) != 0)
				{
					//timer A
					timerA--;

					if (timerA == 0xffff)
					{
						if ((regs[CIA.ICR] & (uint)ICRB.TIMERA) != 0)
						{
							icrr |= (byte)(ICRB.TIMERA | ICRB.IR);
							interrupt.TriggerInterrupt(Interrupt.PORTS);
						}

						//one shot mode?
						if ((regs[CIA.CRA] & (uint)CR.RUNMODE) != 0)
							regs[CIA.CRA] &= ~(uint)CR.START;
						else
							timerA = timerAreset;
					}
				}

				//timer B running
				if ((regs[CIA.CRB] & (uint)CR.START) != 0)
				{
					//timer B
					timerB--;

					if (timerB == 0xffff)
					{
						if ((regs[CIA.ICR] & (uint)ICRB.TIMERB) != 0)
						{
							icrr |= (byte)(ICRB.TIMERB | ICRB.IR);
							interrupt.TriggerInterrupt(Interrupt.PORTS);
						}

						//one shot mode?
						if ((regs[CIA.CRB] & (uint)CR.RUNMODE) != 0)
							regs[CIA.CRB] &= ~(uint)CR.START;
						else
							timerB = timerBreset;
					}
				}
			}
		}

		protected void IncrementTODTimer()
		{
			if (!todStopped)
			{
				todTimer++;
				todTimer &= 0xffffff;

				CheckTODAlarm();
			}
		}

		private void CheckTODAlarm()
		{
			if (todTimer == todAlarm && (regs[CIA.ICR] & (uint)ICRB.TODALARM) != 0)
			{
				icrr |= (byte)(ICRB.TODALARM | ICRB.IR);
				interrupt.TriggerInterrupt(Interrupt.PORTS);
			}
		}

		public virtual void Reset()
		{
			for (int i = 0; i < 16; i++)
				regs[i] = 0;
			
			regs[CIA.PRA] = 0xff;
			
			timerA = 0xffff;
			timerB = 0xffff;
			timerAreset = timerA;
			timerBreset = timerB;

			todStopped = false;
			todLatched = false;
			todTimer = 0;
			todAlarm = 0;

			icrr = 0;
		}

		public virtual bool IsMapped(uint address)
		{
			return (address >> 16) == 0xbf;
		}

		protected byte GetReg(uint address, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address, 0);

			return (byte)((address >> 8) & 0xf);
		} 

		public abstract uint Read(uint insaddr, uint address, Size size);

		protected uint Read(byte reg)
		{
			switch (reg)
			{
				case CIA.ICR: { byte p = icrr; icrr = 0; return p; }
				case CIA.TAHI: return (uint)(timerA >> 8);
				case CIA.TALO: return timerA;
				case CIA.TBHI: return (uint)(timerB >> 8);
				case CIA.TBLO: return timerB;
				case CIA.TODLO:
					uint rv;
					if (todLatched) rv = todLatch & 0xff;
					else rv = todTimer & 0xff;
					todLatched = false;
					return rv;
				case CIA.TODMID:
					if (todLatched) return (todLatch >> 8) & 0xff;
					else return (todTimer >> 8) & 0xff;
				case CIA.TODHI://reading TODHI latches the values read from TOD until TODLO is read.  HRM p344.
					todLatch = todTimer;
					todLatched = true;
					return todLatch >> 16;
				default: return (uint)regs[reg];
			}
		}

		public abstract void Write(uint insaddr, uint address, uint value, Size size);

		protected void Write(byte reg, uint value)
		{
			switch (reg)
			{
				case CIA.ICR:
					if ((value & 0x80) != 0)
						regs[CIA.ICR] |= (byte)value;
					else
						regs[CIA.ICR] &= (byte)~value;
					break;

				case CIA.TAHI:
					timerAreset = (ushort)((timerAreset & 0x00ffu) | (value << 8));
					if ((regs[CIA.CRA] & (uint)CR.START) == 0)
					{
						timerA = timerAreset;
						regs[CIA.CRA] |= (uint)CR.START; //start the timer
					}
					break;
				case CIA.TALO:
					timerAreset = (ushort)((timerAreset & 0xff00u) | value);
					break;

				case CIA.TBHI:
					timerBreset = (ushort)((timerBreset & 0x00ffu) | (value << 8));
					if ((regs[CIA.CRB] & (uint)CR.START) == 0)
					{
						timerB = timerBreset;
						regs[CIA.CRB] |= (uint)CR.START;//start the timer
					}
					break;
				case CIA.TBLO:
					timerBreset = (ushort)((timerBreset & 0xff00u) | value);
					break;

				case CIA.CRA:
					if ((value & (uint)CR.LOAD) != 0)
						timerA = timerAreset;
					value &= ~(uint)CR.LOAD;

					if (((value >> 5) & 1) != 0)
						Logger.WriteLine($"A inmode: {(value >> 5) & 1}");

					regs[CIA.CRA] = (byte)value;
					break;

				case CIA.CRB:
					if ((value & (uint)CR.LOAD) != 0)
						timerB = timerBreset;
					value &= ~(uint)CR.LOAD;

					if (((value >> 5) & 3) != 0)
						Logger.WriteLine($"B inmode: {(value >> 5) & 3}");

					regs[CIA.CRB] = (byte)value;
					break;

				case CIA.TODLO:
					if ((regs[CIA.CRB] & (uint)CR.CRB_ALARM) != 0)
					{
						todAlarm = (todAlarm & 0xffff00) | (value & 0xff);
					}
					else
					{
						todTimer = (todTimer & 0xffff00) | (value & 0xff);
						todStopped = false;
					}
					//CheckTODAlarm();
					//todStopped = false;
					break;

				case CIA.TODMID:
					if ((regs[CIA.CRB] & (uint)CR.CRB_ALARM) != 0)
					{
						todAlarm = (todAlarm & 0xff00ff) | ((value & 0xff) << 8);
					}
					else
					{
						todTimer = (todTimer & 0xff00ff) | ((value & 0xff) << 8);
						todStopped = true;
					}
					//CheckTODAlarm();
					//todStopped = true;
					break;

				case CIA.TODHI:
					if ((regs[CIA.CRB] & (uint)CR.CRB_ALARM) != 0)
					{
						todAlarm = (todAlarm & 0x00ffff) | ((value & 0xff) << 16);
					}
					else
					{
						todTimer = (todTimer & 0x00ffff) | ((value & 0xff) << 16);
						todStopped = true;//writing TODHI/TODMID stops the TOD timer until TODLO is written. HRM p344.
					}
					//CheckTODAlarm();
					//todStopped = true;//todo: is the TOD stopped when writing the alarm?
					break;

				default:
					regs[reg] = (byte)value;
					break;
			}
		}

		public void SetICR(ICRB i)
		{
			//enable the interrupt mask
			regs[ICR] |= (byte)i;
			//flag the interrupt
			icrr |= (byte)(ICRB.IR | i);
		}
	}
}
