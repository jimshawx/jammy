using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Interface;
using Jammy.Types;
using Jammy.Types.Debugger;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Debugger
{
	public class NullTracer : ITracer
	{
		private readonly ILogger logger;

		public NullTracer(ILogger<NullTracer> logger)
		{
			this.logger = logger;
		}

		public void Flush(uint address) { }

		public void TraceTo(uint pc) { }

		public void TraceFrom(string v, uint pc, Regs regs) { }

		public void DumpTrace()
		{
			logger.LogTrace("There is no trace being recorded");
		}

		public void TraceAsm(Regs regs) { }

		public void WriteTrace() { }

		public void Enable(bool enabled) { }

		public void TracePost(Regs traceRegs, uint pc, uint ipc, ushort ins) { }
	}

	public class Tracer : ITracer
	{
		private class TraceEntry
		{
			public string Type { get; set; }
			public uint FromPC { get; set; }
			public string FromLabel { get; set; }
			public string ToLabel { get; set; }
			public uint ToPC { get; set; }
			public Regs Regs { get; set; }

			public override string ToString()
			{
				return $"{Type,-80} {FromPC:X8}{(!string.IsNullOrEmpty(FromLabel) ? " " + FromLabel : "")}->{ToPC:X8}{(!string.IsNullOrEmpty(ToLabel) ? " " + ToLabel : "")} {Regs.RegString()}";
			}
		}

		private readonly List<TraceEntry> traces = new List<TraceEntry>();

		private readonly IDebugMemoryMapper mem;
		private readonly ILabeller labeller;
		private readonly IDisassembler disassembler;
		private readonly IInstructionAnalysisDatabase instructionAnalysisDatabase;
		private readonly ICPUAnalyser cpuAnalyser;
		private readonly IAnalysis analysis;
		private readonly ILogger logger;

		public Tracer(IDebugMemoryMapper memory, ILabeller labeller, IDisassembler disassembler,
			IInstructionAnalysisDatabase instructionAnalysisDatabase,
			ICPUAnalyser cpuAnalyser, IAnalysis analysis,
			ILogger<Tracer> logger)
		{
			this.mem = memory;
			this.labeller = labeller;
			this.disassembler = disassembler;
			this.instructionAnalysisDatabase = instructionAnalysisDatabase;
			this.cpuAnalyser = cpuAnalyser;
			this.analysis = analysis;
			this.logger = logger;
		}

		private readonly HashSet<uint> seen = new HashSet<uint>();
		private bool enabled = true;

		public void Flush(uint address)
		{
			seen.Remove(address);
		}

		private bool ShouldTrace(uint pc)
		{
			if (!enabled) return false;
			if (pc >= 0xf00000) return false;
			if (seen.Contains(pc)) return false;
			return true;
		}

		public void TraceTo(uint pc)
		{
			if (!ShouldTrace(0xffffffff)) return;

			if (traces.Any())
			{
				traces.Last().ToPC = pc;
				traces.Last().ToLabel = labeller.LabelName(pc);
			}
		}

		public void TraceFrom(string type, uint pc, Regs regs)
		{
			if (!ShouldTrace(pc)) return;

			seen.Add(pc);
			traces.Add(new TraceEntry { Type = type, FromPC = pc, FromLabel = labeller.LabelName(pc), Regs = regs.Clone() });
		}

		public void DumpTrace()
		{
			foreach (var t in traces.TakeLast(64))
			{
				logger.LogTrace($"{t}");
			}
			traces.Clear();
		}

		public void WriteTrace()
		{
			using var f = File.OpenWrite($"trace{DateTime.Now:yyyy-MM-dd-HHmmss}.txt");
			using var s = new StreamWriter(f, Encoding.UTF8);

			foreach (var t in traces)
				s.WriteLine(t);
		}

		public void TraceAsm(Regs regs)
		{
			//for now, only call this once, it's dead slow
			if (!instructionAnalysisDatabase.Has(regs.PC))
			{
				var rv = new InstructionAnalysis { PC = regs.PC };
				rv.EffectiveAddresses.AddRange(cpuAnalyser.Analyse(regs));
				instructionAnalysisDatabase.Add(rv);
				analysis.SetMemType(regs.PC, MemType.Code);
				if (rv.EffectiveAddresses.Count >= 1)
					analysis.SetMemType(regs.PC + 2, MemType.Code);
				if (rv.EffectiveAddresses.Count >= 2)
					analysis.SetMemType(regs.PC + 4, MemType.Code);
			}

			//early check of ShouldTrace to avoid disassembling code we're not tracing
			if (!ShouldTrace(regs.PC)) return;
				TraceFrom(DisassembleAddress(regs.PC), regs.PC, regs);
		}

		private string DisassembleAddress(uint pc)
		{
			if (pc >= mem.Length) return "";
			var dasm = disassembler.Disassemble(pc, mem.GetEnumerable(pc, Disassembler.Disassembler.LONGEST_68K_INSTRUCTION));
			return dasm.ToString();
		}

		public void Enable(bool enabled)
		{ 
			this.enabled = enabled;	
		}

		public void TracePost(Regs traceRegs, uint pc, uint ipc, ushort ins)
		{
			if ((ins & 0xff00) == 0x6100)
			{
				//bsr
				uint disp = (uint)(sbyte)ins & 0xff;
				if (disp == 0) { traceRegs.PC += 2; }
				else if (disp == 0xff) { traceRegs.PC += 4; }

				TraceFrom("bsr", ipc, traceRegs);
				TraceTo(pc);
			}
			else if ((ins & 0xf000) == 0x6000)
			{
				//bcc
				uint inssize = 2;
				uint disp = (uint)(sbyte)ins & 0xff;
				if (disp == 0) { inssize += 2; traceRegs.PC += 2; }
				else if (disp == 0xff) { inssize += 4; traceRegs.PC += 4; }
				if (pc != ipc + inssize)
				{
					TraceFrom("bra", ipc, traceRegs);
					TraceTo(pc);
				}
			}
			else if ((ins & 0xffc0) == 0x4e80)
			{
				//jsr
				TraceFrom("jsr", ipc, traceRegs);
				TraceTo(pc);
			}
			else if ((ins & 0xffc0) == 0x4ec0)
			{
				//jmp
				TraceFrom("jmp", ipc, traceRegs);
				TraceTo(pc);
			}
			else if (ins == 0x4e75)
			{
				//rts
				TraceFrom("rts", ipc, traceRegs);//rts
				TraceTo(pc);
			}
			else if (ins == 0x4e73)
			{
				//rte
				TraceFrom("rte", ipc, traceRegs);//rte
				TraceTo(pc);
			}
		}
	}
}