using System;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class BattClock : IBattClock
	{
		public void Emulate(ulong cycles)
		{
			
		}

		public void Reset()
		{
			for (int i = 0; i < 16; i++)
				regs[i] = 0;
			//regs[15] = 4;//24/12
		}

		public bool IsMapped(uint address)
		{
			//return (address >= 0xdc0000 && address < 0xdd0000) || // 2.04 ROM looks here
			//	   (address >= 0xd80000 && address < 0xd90000);
			return address >= 0xdc0000 && address < 0xdc0040;
			//return false;
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

		private byte[] regs = new byte [16];

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint reg = ((address & 0xffff)-3)/4;
			if (reg < 16)
			{
				Logger.WriteLine($"[BATTCLOCK] R {address:X8} @ {insaddr:X8}");

				//if it's a clock register and the clock isn't held, map in the latest time
				if (reg <= 12 && (regs[0xd] & 1) == 0)
				{
					var t = DateTime.Now;

					regs[0] = (byte)(t.Second % 10);
					regs[1] = (byte)(t.Second / 10);
					regs[2] = (byte)(t.Minute % 10);
					regs[3] = (byte)(t.Minute / 10);

					int hour = t.Hour;
					byte h24 = (byte)(regs[0xf] & 2); //12H, 24H
					if (h24 != 0)
						hour %= 12;
					regs[4] = (byte)(hour % 10);
					regs[5] = (byte)((hour / 10) | (t.Hour >= 12 ? 4 : 0));

					int day = t.Day - 1;
					regs[6] = (byte)(day % 10);
					regs[7] = (byte)(day / 10);

					int month = t.Month - 1;
					regs[8] = (byte)(month % 10);
					regs[9] = (byte)(month / 10);

					int year = (t.Year - 1900) % 100;
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
				return regs[reg];
			}

			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			uint reg = ((address & 0xffff) - 3) / 4;
			if (reg < 16)
			{
				Logger.WriteLine($"[BATTCLOCK] W {address:X8} {value:X8} @ {insaddr:X8}");

				regs[reg] = (byte)value;
			}
		}
	}
}
