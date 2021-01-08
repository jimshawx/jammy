using RunAmiga.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RunAmiga
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

		public void Emulate(ulong ns)
		{
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
			//Trace.WriteLine($"CIAA Read {address:X8} {size} {debug[reg].Item1} {debug[reg].Item2}");
			return (uint)regs[reg];
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address,0);

			byte reg = (byte)((address >> 8) & 0xf);
			regs[reg] = (byte)value;
			//Trace.WriteLine($"CIAA Write {address:X8} {value:X8} {size} {debug[reg].Item1} {debug[reg].Item2}");

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

		public void Emulate(ulong ns)
		{
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
			//Trace.WriteLine($"CIAB Read {address:X8} {size} {debug[reg].Item1} {debug[reg].Item2}");
			return (uint)regs[reg];
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (size != Size.Byte)
				throw new UnknownInstructionSizeException(address, 0);

			byte reg = (byte)((address >> 8) & 0xf);
			regs[reg] = (byte)value;
			//Trace.WriteLine($"CIAB Write {address:X8} {value:X8} {size} {debug[reg].Item1} {debug[reg].Item2}");
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
