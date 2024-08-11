using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Core.CPU.Microcode
{
	public class CPUWrapper : ICPU, ICSharpCPU
	{
		public CPUWrapper(IInterrupt interrupt, IMemoryMapper memoryMapper,
			IBreakpointCollection breakpoints, ITracer tracer,
			IOptions<EmulationSettings> settings,
			ILogger<CPUWrapper> logger)
		{
			CPU.CPUInit(interrupt, memoryMapper, breakpoints, tracer, settings, logger);
		}

		public void Emulate()
		{
			CPU.Emulate();
		}

		public Regs GetRegs()
		{
			return CPU.GetRegs();
		}

		public Regs GetRegs(Regs regs)
		{
			return CPU.GetRegs(regs);
		}

		public void Reset()
		{
			CPU.Reset();
		}

		public void SetPC(uint pc)
		{
			CPU.SetPC(pc);
		}

		public void SetRegs(Regs regs)
		{
			CPU.SetRegs(regs);
		}
	}
}
