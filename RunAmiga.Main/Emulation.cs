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

		public Emulation(IMachine machine, IDebugger debugger,
			IOptions<EmulationSettings> settings, IDisassembly disassembly)
		{
			this.machine = machine;
			//var labeller = new Labeller(settings.Value);
			//var tracer = new Tracer(disassembly, labeller);

			//debugger.SetTracer(tracer);
		}

		public void Reset()
		{
			machine.Reset();
		}

		public void Start()
		{
			machine.Start();
		}
	}
}
