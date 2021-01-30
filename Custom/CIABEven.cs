using System;
using System.Collections.Generic;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class CIABEven : IEmulate, IMemoryMappedDevice
	{
		private readonly DiskDrives diskDrives;

		private readonly Dictionary<int, Tuple<string, string>> debug = new Dictionary<int, Tuple<string, string>>
		{
			{0,new Tuple<string,string>("pra", "") },
			{1,new Tuple<string,string>("prb", "") },
			{2,new Tuple<string,string>("ddra", "Direction for Port A (BFD000), bit set = output") },
			{3,new Tuple<string,string>("ddrb", "Direction for Port B (BFD100), bit set = output") },
			{4,new Tuple<string,string>("talo", "Timer A low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)") },
			{5,new Tuple<string,string>("tahi", "Timer A high byte") },
			{6,new Tuple<string,string>("tblo", "Timer B low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)") },
			{7,new Tuple<string,string>("tbhi", "Timer B high byte") },
			{8,new Tuple<string,string>("todlo", "Horizontal sync event counter bits 7-0") },
			{9,new Tuple<string,string>("todmid", "Horizontal sync event counter bits 15-8") },
			{0xa,new Tuple<string,string>("todhi", "Horizontal sync event counter bits 23-16") },
			{0xb,new Tuple<string,string>("", "Not used") },
			{0xc,new Tuple<string,string>("sdr", "Serial data register (not used)") },
			{0xd,new Tuple<string,string>("icr", "Interrupt control register") },
			{0xe,new Tuple<string,string>("cra", "Control register A") },
			{0xf,new Tuple<string,string>("crb", "Control register B") },
		};

		//BFD000 - BFDF00
		private byte[] regs = new byte[16];

		public CIABEven(Debugger debugger, DiskDrives diskDrives)
		{
			this.diskDrives = diskDrives;
		}

		private ulong beamTime;
		private ulong timerTime;

		private uint hblankCount;
		public void Emulate(ulong ns)
		{
			beamTime += ns;

			//every 50Hz, reset the copper list
			if (beamTime > 140_000 / 312)
			{
				beamTime -= 140_000 / 312;

				hblankCount++;

				regs[8] = (byte)hblankCount;
				regs[9] = (byte)(hblankCount >> 8);
				regs[10] = (byte)(hblankCount >> 16);
			}

			timerTime += ns;
			if (timerTime > 10)
			{
				timerTime -= 10;

				//timer A running
				if ((regs[0xe] & 1) != 0)
				{
					//timer A
					regs[4]--;
					if (regs[4] == 0xff)
						regs[5]--;

					//one shot mode?
					if (regs[4] == 0 && regs[5] == 0 && (regs[0xe] & (1 << 3)) == 1)
						regs[0xe] &= 0xfe;
				}

				//timer B running
				if ((regs[0xf] & 1) != 0)
				{
					//timer B
					regs[6]--;
					if (regs[6] == 0xff)
						regs[7]--;

					//one shot mode?
					if (regs[6] == 0 && regs[7] == 0 && (regs[0xf] & (1 << 3)) == 1)
						regs[0xe] &= 0xfe;
				}
			}

		}

		public void Reset()
		{
		}

		public bool IsMapped(uint address)
		{
			return (address & 1) == 0;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address, 0);

			byte reg = (byte)((address >> 8) & 0xf);

			if (reg == 1)
				return diskDrives.ReadPRB(insaddr);

			//Logger.WriteLine($"CIAB Read {address:X8} {regs[reg]:X2} {regs[reg]} {size} {debug[reg].Item1} {debug[reg].Item2}");
			return (uint)regs[reg];
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address, 0);

			byte reg = (byte)((address >> 8) & 0xf);
			regs[reg] = (byte)value;
			//Logger.WriteLine($"CIAB Write {address:X8} {debug[reg].Item1} {value:X8} {value} {Convert.ToString(value, 2).PadLeft(8, '0')}");

			//if (reg == 1)
			//{
			//	UI.DiskLight = (regs[1] & 0x80) == 0;
			//}

			if (reg == 1)
				diskDrives.WritePRB(insaddr, (byte)value);
		}
	}

}