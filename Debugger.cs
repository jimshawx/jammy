using RunAmiga.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using RunAmiga.Custom;

namespace RunAmiga
{
	public class Debugger : IMemoryMappedDevice
	{
		private Dictionary<uint, Breakpoint> breakpoints = new Dictionary<uint, Breakpoint>();

		private readonly Disassembler disassembler;
		private Memory memory;
		private CPU cpu;
		private Chips custom;
		private CIA cia;
		private Labeller labeller;

		public Debugger(Labeller labeller)
		{
			disassembler = new Disassembler();

			AddBreakpoint(0xfc0af0);//InitCode
			//AddBreakpoint(0xfc0afe);
			//AddBreakpoint(0xfc0af0);
			//AddBreakpoint(0xfc14ec);//MakeLibrary
			//AddBreakpoint(0xfc0900);
			//AddBreakpoint(0xfc096c);
			//AddBreakpoint(0xfc0bc8);//InitStruct
			//AddBreakpoint(0xfc1c34);//OpenResource
			AddBreakpoint(0xfc1438);//OpenLibrary
			//AddBreakpoint(0xfe9180);
			//AddBreakpoint(0xfc30e4);//setup LastAlert
			//AddBreakpoint(0xfc19ea);//AddMemList
			//AddBreakpoint(0xfc165a);//FindName

			//AddBreakpoint(0xfc02b0);//initialize exec lists
			//AddBreakpoint(0xFC125C);//initialize exec interrupts

			//AddBreakpoint(0xfc01ee);//relocate ExecBase to $C00276
			//AddBreakpoint(0xfc0240);
			//AddBreakpoint(0xfc033e);
			//AddBreakpoint(0xfcac92);

			//AddBreakpoint(0xfc1798);
			
			//AddBreakpoint(0xfcac92);

			//AddBreakpoint(0x00fcac82);//copper list
			//AddBreakpoint(0x00fc0e60);//ExitIntr
			//AddBreakpoint(0x00fc0c4c);//Interrupt Bail Out
			//AddBreakpoint(0x00fc0ca6);//L2 Autovector
			//AddBreakpoint(0x00fc0cdc);//L3 Autovector
			//AddBreakpoint(0xfc0e8a);//Schedule()+4

			//AddBreakpoint(0xfc0b28);//InitResident
			//AddBreakpoint(0xFC1C28);//AddResource
			//AddBreakpoint(0xFC0ca2);//sw interrupt

			//AddBreakpoint(0xfc13ec);

			AddBreakpoint(0xfcabe4);//Init Graphics Library

			AddBreakpoint(0x00FE930E);//

			//AddBreakpoint(0xfc0e86);//Schedule().
			//AddBreakpoint(0xfc0ee0);//Correct version of Switch() routine.
			AddBreakpoint(0xfc108A);//Incorrect version of Switch() routine. Shouldn't be here, this one handles 68881.
			AddBreakpoint(0xfc2fb4);//Task Crash Routine
			AddBreakpoint(0xfc2fd6);//Alert()
			AddBreakpoint(0xfc305e);//Irrecoverable Crash

			for (uint i = 0; i < 12; i++)
				AddBreakpoint(0xc004d2 + 4 * i, BreakpointType.Write);
			this.labeller = labeller;
		}

		public void Initialise(Memory memory, CPU cpu, Custom.Chips custom, CIA cia)
		{
			this.memory = memory;
			this.cpu = cpu;
			this.custom = custom;
			this.cia = cia;
		}

		public bool IsMapped(uint address)
		{
			return true;
		}

		private bool isROM(uint address)
		{
			return address >= 0xf80000 && address <= 0xffffff;
		}

		private bool isOutOfRange(uint address)
		{
			return address >= 0x1000000;
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (isOutOfRange(address))
			{
				DumpTrace();
				System.Diagnostics.Trace.WriteLine($"Trying to read a {size} from {address:X8} @{insaddr:X8}");
				Machine.SetEmulationMode(EmulationMode.Stopped, true);
			}

			//if (IsMemoryBreakpoint(address, BreakpointType.Read))
			//	Machine.SetEmulationMode(EmulationMode.Stopped, true);

			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			//if (address >= 0xc004d2 && address < 0xc004d2+48) 
			//{
			//	DumpTrace();
			//	Trace.WriteLine($"Wrote to {address:X8}");
			//	Machine.SetEmulationMode(EmulationMode.Stopped, true);
			//}

			if (isROM(address) || isOutOfRange(address))
			{
				DumpTrace();
				System.Diagnostics.Trace.WriteLine($"Trying to write a {size} ({value:X8} {value}) to {address:X8} @{insaddr:X8}");
				Machine.SetEmulationMode(EmulationMode.Stopped, true);
			}

			//if (IsMemoryBreakpoint(address, BreakpointType.Write))
			//	Machine.SetEmulationMode(EmulationMode.Stopped, true);
		}

		public bool IsMemoryBreakpoint(uint pc, BreakpointType type)
		{
			//for (uint i = 0; i < 4; i++)
			uint i = 0;
			{
				if (breakpoints.TryGetValue(pc + i, out Breakpoint bp))
				{
					if (type == BreakpointType.Write)
					{
						if (bp.Type == BreakpointType.Write || bp.Type == BreakpointType.ReadOrWrite)
							return bp.Active;
					}
					else if (type == BreakpointType.Read)
					{
						if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
							return bp.Active;
					}
				}
			}
			return false;
		}

		private string GetString(uint str)
		{
			var sb = new StringBuilder();
			for (; ; )
			{
				byte c = memory.Read8(str);
				if (c == 0)
					return sb.ToString();

				sb.Append(Convert.ToChar(c));
				str++;
			}
			return null;
		}

		public bool IsBreakpoint(uint pc)
		{
			var regs = cpu.GetRegs();

			//if (pc == 0xfc165a && string.Equals(GetString(regs.A[1]), "ciaa.resource"))
			//	return true;

			if (breakpoints.TryGetValue(pc, out Breakpoint bp))
			{
				if (bp.Type == BreakpointType.Permanent)
					return bp.Active;
				if (bp.Type == BreakpointType.Counter)
				{
					if (bp.Active)
					{
						bp.Counter--;
						if (bp.Counter == 0)
						{
							bp.Counter = bp.CounterReset;
							return true;
						}
					}
				}

				if (bp.Type == BreakpointType.OneShot)
					breakpoints.Remove(pc);
				return bp.Active;
			}
			return false;
		}

		public void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Permanent, int counter = 0, Size size = Size.Long)
		{
			breakpoints[address] = new Breakpoint { Address = address, Active = true, Type = type, Counter = counter, CounterReset = counter, Size = size };
		}

		public void RemoveBreakpoint(uint address)
		{
			breakpoints.Remove(address);
		}

		public void SetBreakpoint(uint address, bool active)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp))
				bp.Active = active;
		}

		public Breakpoint GetBreakpoint(uint address, bool active)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp))
				return bp;
			return new Breakpoint { Address = address, Active = false };
		}

		public void BreakAtNextPC()
		{
			uint pc = cpu.GetRegs().PC;
			int line = GetAddressLine(pc) + 1;
			AddBreakpoint(GetLineAddress(line), BreakpointType.OneShot);
		}


		public void Disassemble(uint address)
		{
			var memorySpan = new ReadOnlySpan<byte>(memory.GetMemoryArray());

			using (var file = File.OpenWrite("kick12.rom.asm"))
			{
				using (var txtFile = new StreamWriter(file, Encoding.UTF8))
				{
					while (address < 0x1000000)
					{
						var dasm = disassembler.Disassemble(address, memorySpan.Slice((int)address, Math.Min(12, (int)(0x1000000 - address))));
						//Trace.WriteLine(dasm);
						txtFile.WriteLine(dasm);

						address += (uint)dasm.Bytes.Length;
					}
				}
			}
		}

		private Dictionary<uint, int> addressToLine = new Dictionary<uint, int>();
		private Dictionary<int, uint> lineToAddress = new Dictionary<int, uint>();

		public string DisassembleTxt(List<Tuple<uint, uint>> ranges)
		{
			addressToLine.Clear();
			lineToAddress.Clear();

			var memorySpan = new ReadOnlySpan<byte>(memory.GetMemoryArray());
			var txt = new StringBuilder();

			int line = 0;

			foreach (var range in ranges)
			{
				uint address = range.Item1;
				uint size = range.Item2;
				uint addressEnd = address + size;
				while (address < addressEnd)
				{
					if (labeller.HasLabel(address))
					{
						txt.Append($"{labeller.LabelName(address)}:\n");
						line++;
					}
					addressToLine.Add(address, line);
					lineToAddress.Add(line, address);
					line++;
					txt.Append(IsBreakpoint(address)?'*':' ');
					var dasm = disassembler.Disassemble(address, memorySpan.Slice((int)address, Math.Min(12, (int)(0x1000000 - address))));
					txt.Append($"{dasm}\n");
					address += (uint)dasm.Bytes.Length;
				}
			}
			return txt.ToString();
		}

		public int GetAddressLine(uint address)
		{
			if (addressToLine.TryGetValue(address, out int line))
				return line;

			uint inc = 1;
			int sign = 1;
			while (Math.Abs(inc) < 16)
			{
				address += (uint)(sign * inc);
				if (addressToLine.TryGetValue(address, out int linex))
					return linex;
				if (sign == -1)
					inc++;
				sign = -sign;
			}

			return 0;
		}

		public uint GetLineAddress(int line)
		{
			if (lineToAddress.TryGetValue(line, out uint address))
				return address;
			return 0;
		}

		private class Tracer
		{
			public string type { get; set; }
			public uint fromPC { get; set; }
			public string fromLabel { get;set;}
			public string toLabel { get;set;}
			public uint toPC { get; set; }
			public Regs regs { get; set; }

			public override string ToString()
			{
				return $"{type,-80} {fromPC:X8}{(!string.IsNullOrEmpty(fromLabel)?" "+fromLabel:"")}->{toPC:X8}{(!string.IsNullOrEmpty(toLabel)? " " + toLabel : "")} {regs.RegString()}";
			}
		}

		List<Tracer> traces = new List<Tracer>();

		public void Trace(uint pc)
		{
			if (traces.Any()) { 
				traces.Last().toPC = pc;
				traces.Last().toLabel = labeller.LabelName(pc);
			}
		}

		public void Trace(string v, uint pc)
		{
			traces.Add(new Tracer { type = v, fromPC = pc, fromLabel = labeller.LabelName(pc), regs = cpu.GetRegs() });
		}

		public void DumpTrace()
		{
			foreach (var t in traces.TakeLast(64))
			{
				System.Diagnostics.Trace.WriteLine($"{t}");
			}
			traces.Clear();
		}

		public string DisassembleAddress(uint pc)
		{
			var dasm = disassembler.Disassemble(pc, new ReadOnlySpan<byte>(memory.GetMemoryArray()).Slice((int)pc, Math.Min(12, (int)(0x1000000 - pc))));
			return dasm.ToString();
		}

		public string UpdateExecBase()
		{
			return new ExecBaseMapper(memory).FromAddress();
		}

		public MemoryDump GetMemory()
		{
			return new MemoryDump(memory.GetMemoryArray());
		}

		public Regs GetRegs()
		{
			return cpu.GetRegs();
		}

		public void ToggleBreakpoint(uint pc)
		{
			if (IsBreakpoint(pc))
				breakpoints[pc].Active ^= true;
			else
				AddBreakpoint(pc);
		}

		public void SetPC(uint pc)
		{
			cpu.SetPC(pc);
		}
	}
}
