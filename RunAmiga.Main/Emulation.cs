using Microsoft.Extensions.Options;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types;
using RunAmiga.Debugger;
using RunAmiga.Disassembler;

namespace RunAmiga.Main
{
	public class Emulation : IEmulation
	{
		private readonly IMachine machine;
		private readonly IDebugger debugger;

		public Emulation(IMachine machine, IDebugger debugger, IMemory memory, 
			IBreakpointCollection breakpointCollection, IOptions<EmulationSettings> settings)
		{
			this.machine = machine;
			this.debugger = debugger;

			//memoryMapper.AddMapper(debugger);

			var disassembly = new Disassembly(memory.GetMemoryArray(), breakpointCollection, settings.Value);
			var labeller = new Labeller(settings.Value);
			var tracer = new Tracer(disassembly, labeller);

			debugger.SetTracer(tracer);
		}

		public void Reset()
		{
			machine.Reset();
		}

		public void Start()
		{
			machine.Start();
		}

		public IDebugger GetDebugger()
		{
			return debugger;
		}
	}
}
