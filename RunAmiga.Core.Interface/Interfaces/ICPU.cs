using RunAmiga.Core.Types;

namespace RunAmiga.Core.Interfaces
{
	public interface ICPU : IEmulate
	{
		public Regs GetRegs();
		public void SetRegs(Regs regs);
		public void SetPC(uint pc);
	}

	public interface IMusashiCPU { }
	public interface ICSharpCPU { }
}