using Jammy.Core.Types;

namespace Jammy.Core.Interface.Interfaces
{
	public interface ITracer
	{
		void Trace(uint pc);
		void Trace(string v, uint pc, Regs regs);
		void DumpTrace();
		void TraceAsm(uint pc, Regs regs);
		void WriteTrace();
	}
}