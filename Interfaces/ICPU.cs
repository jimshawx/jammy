using RunAmiga.Types;

namespace RunAmiga
{
	public interface ICPU
	{
		public Regs GetRegs();
		public void SetRegs(Regs regs);
		public void SetPC(uint pc);
	}
}