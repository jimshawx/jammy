using Jammy.Core.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

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