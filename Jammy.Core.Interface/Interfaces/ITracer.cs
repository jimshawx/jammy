using Jammy.Core.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface ITracer
	{
		void TraceTo(uint pc);
		void TraceFrom(string v, uint pc, Regs regs);
		void DumpTrace();
		void TraceAsm(Regs regs);
		void WriteTrace();
		void Enable(bool enabled);
	}
}