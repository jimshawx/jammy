using RunAmiga.Core.Interfaces;

namespace RunAmiga.Core
{
	public class Emulation : IEmulation
	{
		private readonly IMachine machine;
		private readonly IDebugger debugger;

		public Emulation(IMachine machine, IDebugger debugger, IMemory memory, IMemoryMapper memoryMapper)
		{
			this.machine = machine;
			this.debugger = debugger;

			memoryMapper.AddMapper(debugger);

			var disassembly = new Disassembly(memory.GetMemoryArray(), debugger.GetBreakpoints());
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
