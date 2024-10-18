using System;
using System.Collections.Generic;
using System.Linq;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.CIA
{
	public class CIABEven : CIA, ICIABEven
	{
		private readonly IDiskDrives diskDrives;
		private readonly IChipsetClock clock;

		private static readonly Tuple<string, string>[] debug = new Tuple<string, string>[]
		{
			new Tuple<string,string>("pra", "BFD000 /DTR  /RTS  /CD   /CTS  /DSR   SEL   POUT  BUSY"),
			new Tuple<string,string>("prb", "BFD100 /MTR  /SEL3 /SEL2 /SEL1 /SEL0 /SIDE  DIR  /STEP"),
			new Tuple<string,string>("ddra", "BFD200 Direction for Port A (BFD000), bit set = output"),
			new Tuple<string,string>("ddrb", "BFD300 Direction for Port B (BFD100), bit set = output"),
			new Tuple<string,string>("talo", "BFD400 Timer A low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)"),
			new Tuple<string,string>("tahi", "BFD500 Timer A high byte"),
			new Tuple<string,string>("tblo", "BFD600 Timer B low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)"),
			new Tuple<string,string>("tbhi", "BFD700 Timer B high byte"),
			new Tuple<string,string>("todlo", "BFD800 Horizontal sync event counter bits 7-0"),
			new Tuple<string,string>("todmid", "BFD900 Horizontal sync event counter bits 15-8"),
			new Tuple<string,string>("todhi", "BFDA00 Horizontal sync event counter bits 23-16"),
			new Tuple<string,string>("", "BFDB00 Not used"),
			new Tuple<string,string>("sdr", "BFDC00 Serial data register (not used)"),
			new Tuple<string,string>("icr", "BFDD00 Interrupt control register FLAG SERIAL TODALARM TIMERB TIMERA"),
			new Tuple<string,string>("cra", "BFDE00 Control register A"),
			new Tuple<string,string>("crb", "BFDF00 Control register B")
		};

		//BFD000 - BFDF00

		public CIABEven(IDiskDrives diskDrives, IInterrupt interrupt, IChipsetClock clock,
			IOptions<EmulationSettings> settings, ILogger<CIABEven> logger)
		{
			this.diskDrives = diskDrives;
			this.clock = clock;
			this.interrupt = interrupt;
			this.logger = logger;
		}

		protected override uint interruptLevel => Types.Interrupt.EXTER;
		protected override char cia => 'B';

		private int divisor = 0;
		private readonly object locker = new object();
		public override void Emulate()
		{
			clock.WaitForTick();

			//lock (locker)
			{

				if ((clock.ClockState & ChipsetClockState.EndOfLine) != 0)
					IncrementTODTimer();

				divisor++;
				if (divisor == 5)
				{
					divisor = 0;
					base.Emulate();
				}
			}

			clock.Ack();
		}

		public override void Reset()
		{
			base.Reset();
			regs[PRA] = 0x8c;
			divisor = 0;
		}

		public override bool IsMapped(uint address)
		{
			return base.IsMapped(address) && (address & 1) == 0;
		}

		public override uint ReadByte(uint insaddr, uint address)
		{
			//lock (locker)
			{
				byte reg = GetReg(address, Size.Byte);

				if (reg == PRB)
				{
					return diskDrives.ReadPRB(insaddr);
				}

				if (reg == ICR)
					diskDrives.ReadICR(SnoopICRR());

				//logger.LogTrace($"CIAB Read {address:X8} {regs[reg]:X2} {regs[reg]} {size} {debug[reg].Item1} {debug[reg].Item2}");
				return base.Read(reg);
			}
		}

		public override void WriteByte(uint insaddr, uint address, uint value)
		{
			//lock (locker)
			{
				byte reg = GetReg(address, Size.Byte);

				if (reg == PRB)
				{
					diskDrives.WritePRB(insaddr, (byte)value);
					return;
				}

				//logger.LogTrace($"CIAB Write {address:X8} {debug[reg].Item1} {value:X8} {value} {Convert.ToString(value, 2).PadLeft(8, '0')}");
				base.Write(reg, value);
			}
		}

		public static List<string> GetCribSheet()
		{
			return new List<string> { "CIAB Even" }.Concat(debug.Select(x => $"{x.Item1.ToUpper(),-6} {x.Item2}")).ToList();
		}
	}
}