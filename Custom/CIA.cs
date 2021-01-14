using System;
using System.Collections.Generic;
using System.Diagnostics;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	//https://www.amigacoding.com/index.php?title=CIA_Memory_Map

	public class CIAA : IEmulate, IMemoryMappedDevice
	{
		private readonly Dictionary<int, Tuple<string,string>> debug = new Dictionary<int, Tuple<string,string>>
		{
			{0,new Tuple<string,string>("pra", "") },
			{1,new Tuple<string,string>("prb", "Parallel port data") },
			{2,new Tuple<string,string>("ddra", "Direction for Port A (BFE001), bit set = output") },
			{3,new Tuple<string,string>("ddrb", "Direction for Port B (BFE101), bit set = output") },
			{4,new Tuple<string,string>("talo", "Timer A low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)") },
			{5,new Tuple<string,string>("tahi", "Timer A high byte") },
			{6,new Tuple<string,string>("tblo", "Timer B low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)") },
			{7,new Tuple<string,string>("tbhi", "Timer B high byte") },
			{8,new Tuple<string,string>("todlo", "Vertical sync event counter bits 7-0 (50/60Hz)") },
			{9,new Tuple<string,string>("todmid", "Vertical sync event counter bits 15-8") },
			{0xa,new Tuple<string,string>("todhi", "Vertical sync event counter bits 23-16") },
			{0xb,new Tuple<string,string>("", "Not used") },
			{0xc,new Tuple<string,string>("sdr", "Serial data register (used for keyboard)") },
			{0xd,new Tuple<string,string>("icr", "Interrupt control register") },
			{0xe,new Tuple<string,string>("cra", "Control register A") },
			{0xf,new Tuple<string,string>("crb", "Control register B") },
		};

		//BFE001 - BFEF01
		private byte[] regs = new byte[16];

		public CIAA(Debugger debugger)
		{
		}

		private ulong beamTime;
		private ulong timerTime;

		private uint vblankCount;
		public void Emulate(ulong ns)
		{
			beamTime += ns;

			//every 50Hz, reset the copper list
			if (beamTime > 140_000) // 50Hz = 1/50th cpu clock = 7MHz/50 = 140k 
			{
				beamTime -= 140_000;

				vblankCount++;

				regs[8] = (byte)vblankCount;
				regs[9] = (byte)(vblankCount >> 8);
				regs[10] = (byte)(vblankCount >> 16);
			}

			timerTime += ns;
			if (timerTime > 10)// timers tick at 1/10th cpu clock
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
					if (regs[4] == 0 && regs[5] == 0 && (regs[0xe]&(1<<3))==1 )
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
			return (address&1)==1;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address,0);

			byte reg = (byte)((address>>8)&0xf);
			//Logger.WriteLine($"CIAA Read {address:X8} {regs[reg]:X2} {regs[reg]} {size} {debug[reg].Item1} {debug[reg].Item2}");
			return (uint)regs[reg];
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address,0);

			byte reg = (byte)((address >> 8) & 0xf);
			regs[reg] = (byte)value;
			Logger.WriteLine($"CIAA Write {address:X8} {debug[reg].Item1} {value:X8} {value} {Convert.ToString(value,2).PadLeft(8,'0')}");

			if (reg == 0)
			{
				UI.PowerLight = (regs[0]&2)==0;
			}
		}

		public bool PowerLight()
		{
			return (regs[0]&1)!=0;
		}
	}

	public class CIAB : IEmulate, IMemoryMappedDevice
	{
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

		public CIAB(Debugger debugger)
		{
		}

		private ulong beamTime;
		private ulong timerTime;

		private uint hblankCount;
		public void Emulate(ulong ns)
		{
			beamTime += ns;

			//every 50Hz, reset the copper list
			if (beamTime > 140_000/312)
			{
				beamTime -= 140_000/312;

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
				throw new UnknownInstructionSizeException(address,0);

			byte reg = (byte)((address >> 8) & 0xf);
			Logger.WriteLine($"CIAB Read {address:X8} {regs[reg]:X2} {regs[reg]} {size} {debug[reg].Item1} {debug[reg].Item2}");
			return (uint)regs[reg];
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address, 0);

			byte reg = (byte)((address >> 8) & 0xf);
			regs[reg] = (byte)value;
			Logger.WriteLine($"CIAB Write {address:X8} {debug[reg].Item1} {value:X8} {value} {Convert.ToString(value, 2).PadLeft(8, '0')}");
		}
	}

	public class CIA : IEmulate, IMemoryMappedDevice
	{
		private CIAA ciaA;
		private CIAB ciaB;

		public CIA(Debugger debugger)
		{
			ciaA = new CIAA(debugger);
			ciaB = new CIAB(debugger);
		}

		public void Emulate(ulong ns)
		{
			ciaA.Emulate(ns);
			ciaB.Emulate(ns);
		}

		public void Reset()
		{
			ciaA.Reset();
			ciaB.Reset();
		}

		public bool IsMapped(uint address)
		{
			return (address>>16)==0xbf;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (ciaA.IsMapped(address)) return ciaA.Read(insaddr, address, size);
			if (ciaB.IsMapped(address)) return ciaB.Read(insaddr, address, size);
			return 0;
		}
		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (ciaA.IsMapped(address)) { ciaA.Write(insaddr, address, value, size); return; }
			if (ciaB.IsMapped(address)) { ciaB.Write(insaddr, address, value, size); return; }
		}
	}
}
