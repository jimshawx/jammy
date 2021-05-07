using Jammy.Core.Types;

namespace Jammy.Core.Interface.Interfaces
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