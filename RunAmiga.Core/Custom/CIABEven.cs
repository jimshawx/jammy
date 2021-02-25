using System;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interfaces;
using RunAmiga.Core.Types;

namespace RunAmiga.Core.Custom
{
	public class CIABEven : CIA, ICIABEven
	{
		private readonly IDiskDrives diskDrives;

		private readonly Tuple<string, string>[] debug = new Tuple<string, string>[]
		{
			new Tuple<string,string>("pra", ""),
			new Tuple<string,string>("prb", ""),
			new Tuple<string,string>("ddra", "Direction for Port A (BFD000), bit set = output"),
			new Tuple<string,string>("ddrb", "Direction for Port B (BFD100), bit set = output"),
			new Tuple<string,string>("talo", "Timer A low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)"),
			new Tuple<string,string>("tahi", "Timer A high byte"),
			new Tuple<string,string>("tblo", "Timer B low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)"),
			new Tuple<string,string>("tbhi", "Timer B high byte"),
			new Tuple<string,string>("todlo", "Horizontal sync event counter bits 7-0"),
			new Tuple<string,string>("todmid", "Horizontal sync event counter bits 15-8"),
			new Tuple<string,string>("todhi", "Horizontal sync event counter bits 23-16"),
			new Tuple<string,string>("", "Not used"),
			new Tuple<string,string>("sdr", "Serial data register (not used)"),
			new Tuple<string,string>("icr", "Interrupt control register"),
			new Tuple<string,string>("cra", "Control register A"),
			new Tuple<string,string>("crb", "Control register B")
		};

		//BFD000 - BFDF00

		public CIABEven(IDiskDrives diskDrives, IInterrupt interrupt, ILogger<CIABEven> logger)
		{
			this.diskDrives = diskDrives;
			this.interrupt = interrupt;
			this.logger = logger;
		}

		protected override uint interruptLevel => Interrupt.EXTER;
		protected override char cia => 'B';

		private ulong beamTime;
		public override void Emulate(ulong cycles)
		{
			beamTime += cycles;

			if (beamTime > 140_000 / 312)
			{
				beamTime -= 140_000 / 312;

				IncrementTODTimer();
			}

			base.Emulate(cycles);
		}

		public override bool IsMapped(uint address)
		{
			return base.IsMapped(address) && (address & 1) == 0;
		}

		public override uint Read(uint insaddr, uint address, Size size)
		{
			byte reg = GetReg(address, size);

			if (reg == CIA.PRB)
			{
				return diskDrives.ReadPRB(insaddr);
			}

			//logger.LogTrace($"CIAB Read {address:X8} {regs[reg]:X2} {regs[reg]} {size} {debug[reg].Item1} {debug[reg].Item2}");
			return base.Read(reg);
		}

		public override void Write(uint insaddr, uint address, uint value, Size size)
		{
			byte reg = GetReg(address, size);

			if (reg == CIA.PRB)
			{
				diskDrives.WritePRB(insaddr, (byte)value);
				return;
			}
			
			//logger.LogTrace($"CIAB Write {address:X8} {debug[reg].Item1} {value:X8} {value} {Convert.ToString(value, 2).PadLeft(8, '0')}");
			base.Write(reg, value);
		}
	}
}