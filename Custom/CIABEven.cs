using System;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class CIABEven : CIA
	{
		private readonly DiskDrives diskDrives;

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

		public CIABEven(Debugger debugger, DiskDrives diskDrives, Interrupt interrupt)
		{
			this.diskDrives = diskDrives;
			this.interrupt = interrupt;
		}

		private ulong beamTime;
		private uint hblankCount;
		public override void Emulate(ulong ns)
		{
			beamTime += ns;

			//every 50Hz, reset the copper list
			if (beamTime > 140_000 / 312)
			{
				beamTime -= 140_000 / 312;

				hblankCount++;

				regs[CIA.TODLO] = (byte)hblankCount;
				regs[CIA.TODMID] = (byte)(hblankCount >> 8);
				regs[CIA.TODHI] = (byte)(hblankCount >> 16);
			}

			base.Emulate(ns);
		}

		public override void Reset()
		{
			base.Reset();
		}

		public override bool IsMapped(uint address)
		{
			return base.IsMapped(address) && (address & 1) == 0;
		}

		public override uint Read(uint insaddr, uint address, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address, 0);

			byte reg = (byte)((address >> 8) & 0xf);

			if (reg == CIA.PRB)
			{
				return diskDrives.ReadPRB(insaddr);
			}
			else if (reg == CIA.ICR)
			{
				byte p = icrr;
				icrr = 0;
				return p;
				//return regs[CIA.ICR];
			}
			else
			{
				//Logger.WriteLine($"CIAB Read {address:X8} {regs[reg]:X2} {regs[reg]} {size} {debug[reg].Item1} {debug[reg].Item2}");
				return (uint)regs[reg];
			}
		}

		public override void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address, 0);

			byte reg = (byte)((address >> 8) & 0xf);

			if (reg == CIA.ICR)
			{
				if ((value & 0x80) != 0)
					regs[CIA.ICR] |= (byte)value;
				else
					regs[CIA.ICR] &= (byte)~value;
			}
			else if (reg == CIA.PRB)
			{
				diskDrives.WritePRB(insaddr, (byte)value);
			}
			else
			{
				//Logger.WriteLine($"CIAB Write {address:X8} {debug[reg].Item1} {value:X8} {value} {Convert.ToString(value, 2).PadLeft(8, '0')}");
				regs[reg] = (byte)value;
			}

			if (reg == CIA.TAHI)
			{
				regs[CIA.CRA] |= 1;
			}
			else if (reg == CIA.TBHI)
			{
				regs[CIA.CRB] |= 1;
			}
		}
	}
}