using System;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class CIAAOdd : CIA
	{
		private readonly DiskDrives diskDrives;
		private readonly Mouse mouse;

		private readonly Tuple<string, string>[] debug = new Tuple<string, string>[]
		{
			new Tuple<string,string>("pra", ""),
			new Tuple<string,string>("prb", "Parallel port data"),
			new Tuple<string,string>("ddra", "Direction for Port A (BFE001), bit set = output"),
			new Tuple<string,string>("ddrb", "Direction for Port B (BFE101), bit set = output"),
			new Tuple<string,string>("talo", "Timer A low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)"),
			new Tuple<string,string>("tahi", "Timer A high byte"),
			new Tuple<string,string>("tblo", "Timer B low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)"),
			new Tuple<string,string>("tbhi", "Timer B high byte"),
			new Tuple<string,string>("todlo", "Vertical sync event counter bits 7-0 (50/60Hz)"),
			new Tuple<string,string>("todmid", "Vertical sync event counter bits 15-8"),
			new Tuple<string,string>("todhi", "Vertical sync event counter bits 23-16"),
			new Tuple<string,string>("", "Not used"),
			new Tuple<string,string>("sdr", "Serial data register (used for keyboard)"),
			new Tuple<string,string>("icr", "Interrupt control register"),
			new Tuple<string,string>("cra", "Control register A"),
			new Tuple<string,string>("crb", "Control register B")
		};

		//BFE001 - BFEF01

		public CIAAOdd(Debugger debugger, DiskDrives diskDrives, Mouse mouse, Interrupt interrupt)
		{
			this.diskDrives = diskDrives;
			this.mouse = mouse;
			this.interrupt = interrupt;
		}

		private ulong beamTime;
		private uint vblankCount;
		public override void Emulate(ulong ns)
		{
			beamTime += ns;

			//every 50Hz, reset the copper list
			if (beamTime > 140_000) // 50Hz = 1/50th cpu clock = 7MHz/50 = 140k 
			{
				beamTime -= 140_000;

				vblankCount++;

				regs[CIA.TODLO] = (byte)vblankCount;
				regs[CIA.TODMID] = (byte)(vblankCount >> 8);
				regs[CIA.TODHI] = (byte)(vblankCount >> 16);
			}
			base.Emulate(ns);
		}

		public override void Reset()
		{
			base.Reset();
		}

		public override bool IsMapped(uint address)
		{
			return base.IsMapped(address) && (address & 1) == 1;
		}

		public override uint Read(uint insaddr, uint address, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address, 0);

			byte reg = (byte)((address >> 8) & 0xf);

			if (reg == CIA.PRA)
			{
				byte p = 0;
				p |= diskDrives.ReadPRA(insaddr);
				p |= mouse.ReadPRA(insaddr);
				return p;
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
				//Logger.WriteLine($"CIAA Read {address:X8} {regs[reg]:X2} {regs[reg]} {size} {debug[reg].Item1} {debug[reg].Item2}");
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
			else if (reg == CIA.PRA)
			{
				UI.PowerLight = (regs[CIA.PRA] & 2) == 0;

				diskDrives.WritePRA(insaddr, (byte)value);
				mouse.WritePRA(insaddr, (byte)value);
			}
			else
			{
				//Logger.WriteLine($"CIAA Write {address:X8} {debug[reg].Item1} {value:X8} {value} {Convert.ToString(value, 2).PadLeft(8, '0')}");
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