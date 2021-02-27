using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;
using RunAmiga.Disassembler;

namespace RunAmiga.Debugger
{
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

		private readonly Disassembly disassembly;
		private readonly Labeller labeller;
		private readonly ILogger logger;

		public Tracer(Disassembly disassembly, Labeller labeller)
		{
			this.disassembly = disassembly;
			this.labeller = labeller;
			this.logger = ServiceProviderFactory.ServiceProvider.GetRequiredService<ILogger<Tracer>>();
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
			traces.Add(new TraceEntry { Type = v, FromPC = pc, FromLabel = labeller.LabelName(pc), Regs = regs });
		}

		public void DumpTrace()
		{
			foreach (var t in traces.TakeLast(64))
			{
				logger.LogTrace($"{t}");
			}
			traces.Clear();
		}

		public void TraceAsm(uint pc, Regs regs)
		{
			Trace(disassembly.DisassembleAddress(pc), pc, regs);
		}
	}
}