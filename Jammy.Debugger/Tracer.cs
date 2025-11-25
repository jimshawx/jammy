using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Interface;
using Jammy.Types.Debugger;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
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

		public void TraceTo(uint pc) { }

		public void TraceFrom(string v, uint pc, Regs regs) { }

		public void DumpTrace()
		{
			logger.LogTrace("There is no trace being recorded");
		}

		public void TraceAsm(Regs regs) { }

		public void WriteTrace() { }

		public void Enable(bool enabled) { }
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
		private readonly ILogger logger;

		public Tracer(IDebugMemoryMapper memory, ILabeller labeller, IDisassembler disassembler,
			IInstructionAnalysisDatabase instructionAnalysisDatabase,
			ICPUAnalyser cpuAnalyser,
			ILogger<Tracer> logger)
		{
			this.mem = memory;
			this.labeller = labeller;
			this.disassembler = disassembler;
			this.instructionAnalysisDatabase = instructionAnalysisDatabase;
			this.cpuAnalyser = cpuAnalyser;
			this.logger = logger;
		}

		private readonly HashSet<uint> seen = new HashSet<uint>();
		private bool enabled = false;

		private bool ShouldTrace(uint pc)
		{
			if (!enabled) return false;
			if (pc >= 0xf00000) return false;
			if (seen.Contains(pc)) return false;
			return true;
		}

		public void TraceTo(uint pc)
		{
			if (!ShouldTrace(pc)) return;

			if (traces.Any())
			{
				traces.Last().ToPC = pc;
				traces.Last().ToLabel = labeller.LabelName(pc);
			}
		}

		public void TraceFrom(string v, uint pc, Regs regs)
		{
			if (!ShouldTrace(pc)) return;

			seen.Add(pc);
			traces.Add(new TraceEntry { Type = v, FromPC = pc, FromLabel = labeller.LabelName(pc), Regs = regs.Clone() });
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

		//[MethodImpl(MethodImplOptions.NoOptimization|MethodImplOptions.NoInlining)]
		public void TraceAsm(Regs regs)
		{
			if (!ShouldTrace(regs.PC)) return;

			//for now, only call this once, it's dead slow
			if (!instructionAnalysisDatabase.Has(regs.PC))
			{ 
				var ana = cpuAnalyser.Analyse(regs);
				if (ana.Count > 0)				
				{ 
					var rv = new InstructionAnalysis();
					rv.PC = regs.PC;
					rv.EffectiveAddresses.AddRange(ana);
					instructionAnalysisDatabase.Add(rv);
				}
			}

			TraceFrom(DisassembleAddress(regs.PC), regs.PC, regs);
		}

		private string DisassembleAddress(uint pc)
		{
			if (pc >= mem.Length) return "";
			var dasm = disassembler.Disassemble(pc, mem.GetEnumerable(pc, Disassembler.Disassembler.LONGEST_X86_INSTRUCTION));
			return dasm.ToString();
		}

		public void Enable(bool enabled)
		{ 
			this.enabled = enabled;	
		}
	}
}