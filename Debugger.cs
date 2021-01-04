using RunAmiga.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace RunAmiga
{
	public class Debugger : IMemoryMappedDevice
	{
		private Dictionary<uint, Breakpoint> breakpoints = new Dictionary<uint, Breakpoint>();

		private readonly Disassembler disassembler;
		private Memory memory;
		private CPU cpu;
		private Custom custom;
		private CIA cia;

		public Debugger()
		{
			disassembler = new Disassembler();

			//AddBreakpoint(0xfc0500);//InitCode
			//AddBreakpoint(0xfc0af0);
			//AddBreakpoint(0xfc14ec);//MakeLibrary
			//AddBreakpoint(0xfc0900);
			//AddBreakpoint(0xfc096c);
			//AddBreakpoint(0xfc0bc8);//InitStruct
			AddBreakpoint(0xfc1c34);//OpenResource
			AddBreakpoint(0xfe9174);
			AddBreakpoint(0xfc30e4);//setup LastAlert
			AddBreakpoint(0xfc19ea);//AddMemList

			AddBreakpoint(0xfc02b0);//initialize exec lists
			AddBreakpoint(0xFC125C);//initialize exec interrupts

			AddBreakpoint(0xfc01ee);//relocate ExecBase to $C00276
			AddBreakpoint(0xfc0240);
			AddBreakpoint(0xfc033e);

			//AddBreakpoint(0xfc0e86);//Schedule().
			AddBreakpoint(0xfc0ee0);//Correct version of Switch() routine.
			AddBreakpoint(0xfc108A);//Incorrect version of Switch() routine. Shouldn't be here, this one handles 68881.
			AddBreakpoint(0xfc2fb4);//Task Crash Routine
			AddBreakpoint(0xfc2fd6);//Alert()
			AddBreakpoint(0xfc305e);//Irrecoverable Crash


			for (uint i = 0; i < 12; i++)
				AddBreakpoint(0xc004d2 + 4 * i, BreakpointType.Write);

			ExecLabels();
			MiscLabels();
		}

		public void Initialise(Memory memory, CPU cpu, Custom custom, CIA cia)
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
			return address >= 0xfc0000 && address <= 0xffffff;
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
				Trace.WriteLine($"Trying to read a {size} from {address:X8} @{insaddr:X8}");
				Machine.SetEmulationMode(EmulationMode.Stopped, true);
			}

			if (IsMemoryBreakpoint(address, BreakpointType.Read))
				Machine.SetEmulationMode(EmulationMode.Stopped, true);

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
				Trace.WriteLine($"Trying to write a {size} ({value:X8} {value}) to {address:X8} @{insaddr:X8}");
				Machine.SetEmulationMode(EmulationMode.Stopped, true);
			}

			if (IsMemoryBreakpoint(address, BreakpointType.Write))
				Machine.SetEmulationMode(EmulationMode.Stopped, true);
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

		public bool IsBreakpoint(uint pc)
		{
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

		private string[] fns = {
			"Supervisor",
			"ExitIntr",
			"Schedule",
			"Reschedule",
			"Switch",
			"Dispatch",
			"Exception",
			"InitCode",
			"InitStruct",
			"MakeLibrary",
			"MakeFunctions",
			"FindResident",
			"InitResident",
			"Alert",
			"Debug",
			"Disable",
			"Enable",
			"Forbid",
			"Permit",
			"SetSR",
			"SuperState",
			"UserState",
			"SetIntVector",
			"AddIntServer",
			"RemIntServer",
			"Cause",
			"Allocate",
			"Deallocate",
			"AllocMem",
			"AllocAbs",
			"FreeMem",
			"AvailMem",
			"AllocEntry",
			"FreeEntry",
			"Insert",
			"AddHead",
			"AddTail",
			"Remove",
			"RemHead",
			"RemTail",
			"Enqueue",
			"FindName",
			"AddTask",
			"RemTask",
			"FindTask",
			"SetTaskPri",
			"SetSignal",
			"SetExcept",
			"Wait",
			"Signal",
			"AllocSignal",
			"FreeSignal",
			"AllocTrap",
			"FreeTrap",
			"AddPort",
			"RemPort",
			"PutMsg",
			"GetMsg",
			"ReplyMsg",
			"WaitPort",
			"FindPort",
			"AddLibrary",
			"RemLibrary",
			"OldOpenLibrary",
			"CloseLibrary",
			"SetFunction",
			"SumLibrary",
			"AddDevice",
			"RemDevice",
			"OpenDevice",
			"CloseDevice",
			"DoIO",
			"SendIO",
			"CheckIO",
			"WaitIO",
			"AbortIO",
			"AddResource",
			"RemResource",
			"OpenResource",
			"RawIOInit",
			"RawMayGetChar",
			"RawPutChar",
			"RawDoFmt",
			"GetCC",
			"TypeOfMem",
			"Procure",
			"Vacate",
			"OpenLibrary",
			"InitSemaphore",
			"ObtainSemaphore",
			"ReleaseSemaphore",
			"AttemptSemaphore",
			"ObtainSemaphoreList",
			"ReleaseSemaphoreList",
			"FindSemaphore",
			"AddSemaphore",
			"RemSemaphore",
			"SumKickData",
			"AddMemList",
			"CopyMem",
			"CopyMemQuick",
			"CacheClearU",
			"CacheClearE",
			"CacheControl",
			"CreateIORequest",
			"DeleteIORequest",
			"CreateMsgPort",
			"DeleteMsgPort",
			"ObtainSemaphoreShared",
			"AllocVec",
			"FreeVec",
			"CreatePrivatePool",
			"DeletePrivatePool",
			"AllocPooled",
			"FreePooled",
			"AttemptSemaphoreShared",
			"ColdReboot",
			"StackSwap",
			"ChildFree",
			"ChildOrphan",
			"ChildStatus",
			"ChildWait",
			"CachePreDMA",
			"CachePostDMA",
			"ExecReserved01",
			"ExecReserved02",
			"ExecReserved03",
			"ExecReserved04",
		};

		uint fnbase = 0xFC1A40;

		ushort[] fnoffs = {
			0x08A0, 0x08A8,
			0x08AC, 0x08AC,
			0xEE6A, 0xF420,
			0xF446, 0x04F8,
			0xF4A0, 0xF4EA,
			0xF58E, 0xF0B0,
			0xF188, 0xFAAC,
			0xFB36, 0xF080,
			0xF0E8, 0x1596,
			0x08EE, 0xF9AC,
			0xF9BA, 0x051A,
			0x0520, 0xF6E2,
			0xF708, 0xF734,
			0xF74E, 0xF794,
			0xF7D4, 0xF8E0,
			0xFC5C, 0xFCC4,
			0xFD54, 0xFE00,
			0xFDB0, 0xFE90,
			0xFEDE, 0xFF6C,
			0xFB6C, 0xFB98,
			0xFBA8, 0xFBC0,
			0xFBCE, 0xFBDE,
			0xFBF4, 0xFC1A,
			0x0208, 0x02B4,
			0x0334, 0x0388,
			0x03E2, 0x03D8,
			0x0490, 0x0408,
			0x0584, 0x05BC,
			0x054E, 0x0574,
			0x00D8, 0x00F0,
			0x00F4, 0x016E,
			0x019C, 0x01B6,
			0x01DE, 0xF9CC,
			0xF9DA, 0xF9F0,
			0xFA26, 0xFA3A,
			0xFA58, 0xEC14,
			0xEC22, 0xEC26,
			0xEC74, 0xEC9C,
			0xEC8A, 0xED0E,
			0xECB2, 0xED2A,
			0x01E8, 0x01F0,
			0x01F4, 0x07B8,
			0x07C2, 0x07EE,
			0x06A8, 0xF700,
			0xFDDA, 0x131C,
			0x1332, 0xF9F8,
			0x1354, 0x1374,
			0x13C4, 0x1428,
			0x1458, 0x14CE,
			0x14F4, 0x14E4,
			0x14F0, 0xEFFC,
			0xFFAA, 0x1504,
			0x1500};

		Dictionary<uint, Label> asmLabels = new Dictionary<uint, Label>();

		private void MiscLabels()
		{
			asmLabels.Add(0xfc2fb4, new Label { Address = 0xfc2fb4, Name = "TaskCrash" });
			//asmLabels.Add(0xfc2fd6, new Label { Address = 0xfc2fd6, Name = "Alert" });
			asmLabels.Add(0xfc305e, new Label { Address = 0xfc305e, Name = "IrrecoverableCrash" });
			asmLabels.Add(0xfc0ee0, new Label { Address = 0xfc0ee0, Name = "Switch" });
			asmLabels.Add(0xfc108A, new Label { Address = 0xfc108A, Name = "SwitchFPU" });

			asmLabels.Add(0xFC125C, new Label { Address = 0xFC125C, Name = "InitInterruptHandlers" });

			asmLabels.Add(0xfc19ea, new Label { Address = 0xfc19ea, Name = "AddMemList" });
			asmLabels.Add(0xFC191E, new Label { Address = 0xFC191E, Name = "AllocEntry" });
			asmLabels.Add(0xFC19AC, new Label { Address = 0xFC19AC, Name = "FreeEntry" });
			asmLabels.Add(0xFC18D0, new Label { Address = 0xFC18D0, Name = "AvailMem" });
		}

		private void ExecLabels()
		{
			for (int i = 4; i < fnoffs.Length; i++)
				asmLabels[fnbase + fnoffs[i]] = new Label { Address = fnbase + fnoffs[i], Name = fns[i - 4] };

			//foreach (var e in asmLabels)
			//	Trace.WriteLine($"{e.Key:X6} {e.Value.Name}");
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
					if (asmLabels.ContainsKey(address))
					{
						txt.Append($"{asmLabels[address].Name}:\n");
						line++;
					}
					addressToLine.Add(address, line);
					lineToAddress.Add(line, address);
					line++;
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
			public uint toPC { get; set; }
			public Regs regs { get; set; }

			public override string ToString()
			{
				return $"{type,-80} {fromPC:X8}->{toPC:X8} {regs.RegString()}";
			}
		}

		List<Tracer> traces = new List<Tracer>();
		public void tracePC(uint pc)
		{
			if (traces.Any())
				traces.Last().toPC = pc;
		}

		public void tracePC(string v, uint pc)
		{
			traces.Add(new Tracer { type = v, fromPC = pc, regs = cpu.GetRegs() });
		}

		public void DumpTrace()
		{
			foreach (var t in traces.TakeLast(64))
			{
				Trace.WriteLine($"{t}");
			}
			traces.Clear();
		}

		public void CurrentLabel(uint pc)
		{
			if (asmLabels.ContainsKey(pc))
				Trace.WriteLine($"{asmLabels[pc].Address:X6} {asmLabels[pc].Name}");
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
	}
}
