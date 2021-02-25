using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Debugger;
using RunAmiga.Disassembler;

namespace RunAmiga.Main
{
	public class Emulation : IEmulation
	{
		private readonly IMachine machine;
		private readonly IDebugger debugger;

		public Emulation(IMachine machine, IDebugger debugger, IMemory memory, IMemoryMapper memoryMapper, IBreakpointCollection breakpointCollection)
		{
			this.machine = machine;
			this.debugger = debugger;

			memoryMapper.AddMapper(debugger);

			var disassembly = new Disassembly(memory.GetMemoryArray(), breakpointCollection);
			var labeller = new Labeller();
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
