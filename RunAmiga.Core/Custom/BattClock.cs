using System;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Custom
{
	public class BattClock : IBattClock
	{
		private readonly ILogger logger;

		public BattClock(ILogger<BattClock> logger)
		{
			this.logger = logger;
		}

		public void Reset()
		{
			for (int i = 0; i < 16; i++)
				regs[i] = 0;
		}

		readonly MemoryRange memoryRange = new MemoryRange(0xdc0000, 0x10000);
		//readonly MemoryRange memoryRange = new MemoryRange(0xd80000, 0x60000);

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		// the RTC is an OKI Semiconductor MSM6242B.
		// datasheet msm6242b.pdf

		//There are 16 4-bit registers
		// S1 S10  M1 M10  H1 H10  D1 D10 Mo0 Mo10  Y1 Y10 DOW  Cd  Ce  Cf
		//  0   1   2   3   4   5   6   7   8    9  10  11  12  13  14  15
		// $3  $7  $B  $F $13 $17 $1B $1F $23  $27 $2B $2F $33 $37 $3B $3F

		//They are mapped like this
		//DC0000 xx x0 xx x0 xx x1 xx x1 ...
		//So each register appears twice on odd addresses.
		//battclock.resource uses the second one, i.e. 3,7,11...

		//Amiga epoch is 00:00:00 January 1, 1978

		private readonly byte[] regs = new byte [16];

		private int REG(uint address)
		{
			if ((address & 3) == 3)
				return (int)((address & 0xfffc) >> 2);
			return -1;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size == Size.Long)
			{
				if ((address & 3) != 0) throw new MemoryAlignmentException(address);
				uint v = Read(insaddr, address + 3, Size.Byte);
				return v * 0x00010001;
			}
			if (size == Size.Word)
			{
				if ((address & 1) != 0) throw new MemoryAlignmentException(address);
				uint v = Read(insaddr, address + 1, Size.Byte);
				return v;
			}

			int reg = REG(address); 
			byte value = 0;
			if (reg >= 0 && reg < 16)
			{
				//if it's a clock register and the clock isn't held, map in the latest time
				if (reg <= 12 && (regs[0xd] & 1) == 0)
				{
					var t = DateTime.Now;

					regs[0] = (byte)(t.Second % 10);
					regs[1] = (byte)(t.Second / 10);
					regs[2] = (byte)(t.Minute % 10);
					regs[3] = (byte)(t.Minute / 10);

					int hour = t.Hour;
					if (false)
					{
						byte h24 = (byte)(regs[0xf] & 2); //12H, 24H
						if (h24 == 0) hour %= 12; //AM/PM clock
						regs[4] = (byte)(hour % 10);
						regs[5] = (byte)((hour / 10) | (t.Hour >= 12 ? ((h24 ^ 2) << 1) : 0));
					}
					else
					{
						regs[4] = (byte)(hour % 10);
						regs[5] = (byte)(hour / 10);
					}

					int day = t.Day;// - 1;
					regs[6] = (byte)(day % 10);
					regs[7] = (byte)(day / 10);

					int month = t.Month;//1-based
					regs[8] = (byte)(month % 10);
					regs[9] = (byte)(month / 10);

					//kickstart 2/3 battclock is clever enough to say:
					// year = 1900 + clock value
					// if (year < 1978) year += 100
					// which means it'll work 'til 2078.
					//kickstart 1.2/1.3 unfortunately...
					// year = 1900 + clock value
					// if (year < 1978) year = 1978
					int year = t.Year % 100;
					regs[10] = (byte)(year % 10);
					regs[11] = (byte)(year / 10);

					switch (t.DayOfWeek)
					{
						case DayOfWeek.Sunday: regs[12] = 0; break;
						case DayOfWeek.Monday: regs[12] = 1; break;
						case DayOfWeek.Tuesday: regs[12] = 2; break;
						case DayOfWeek.Wednesday: regs[12] = 3; break;
						case DayOfWeek.Thursday: regs[12] = 4; break;
						case DayOfWeek.Friday: regs[12] = 5; break;
						case DayOfWeek.Saturday: regs[12] = 6; break;
					}
				}
				value = regs[reg];
			}

			logger.LogTrace($"[BATTCLOCK] R {address:X8} @ {insaddr:X8} {size} {value:X2} R{reg}");
			
			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size == Size.Long)
			{
				if ((address & 3) != 0) throw new MemoryAlignmentException(address);
				Write(insaddr, address+3, value & 0xf, Size.Byte);
				return;
			}
			if (size == Size.Word)
			{
				if ((address & 1) != 0) throw new MemoryAlignmentException(address);
				Write(insaddr, address + 1, value & 0xf, Size.Byte);
				return;
			}

			int reg = REG(address);
			logger.LogTrace($"[BATTCLOCK] W {address:X8} @ {insaddr:X8} {size} {value:X2} R{reg}");
			if (reg >= 0 && reg < 16)
			{
				regs[reg] = (byte)value;
			}
		}
	}
}
