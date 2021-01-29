using RunAmiga.Types;

namespace RunAmiga
{
	public interface ICPU
	{
		public Regs GetRegs();
		public void SetPC(uint pc);
	}
}