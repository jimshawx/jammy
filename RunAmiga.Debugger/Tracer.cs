using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Debugger
{
	public class NullTracer : ITracer
	{
		private readonly ILogger logger;

		public NullTracer(ILogger<NullTracer> logger)
		{
			this.logger = logger;
		}

		public void Trace(uint pc) { }

		public void Trace(string v, uint pc, Regs regs) { }

		public void DumpTrace()
		{
			logger.LogTrace("There is no trace being recorded");
		}

		public void TraceAsm(uint pc, Regs regs) { }

		public void WriteTrace() { }
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
		private readonly ILogger logger;
		private readonly Disassembler.Disassembler disassembler;

		public Tracer(IDebugMemoryMapper memory, ILabeller labeller, ILogger<Tracer> logger)
		{
			this.mem = memory;
			this.labeller = labeller;
			this.logger = logger;
			this.disassembler = new Disassembler.Disassembler();
		}

		public void Trace(uint pc)
		{
			if (traces.Any())
			{
				traces.Last().ToPC = pc;
				traces.Last().ToLabel = labeller.LabelName(pc);
			}
		}

		public void Trace(string v, uint pc, Regs regs)
		{
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

		public void TraceAsm(uint pc, Regs regs)
		{
			Trace(DisassembleAddress(pc), pc, regs.Clone());
		}

		private string DisassembleAddress(uint pc)
		{
			if (pc >= mem.Length) return "";
			var dasm = disassembler.Disassemble(pc, mem.GetEnumerable((int)pc, 20));
			return dasm.ToString();
		}
	}
}