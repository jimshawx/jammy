using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;
using System;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface IBreakpointCollection : IMemoryInterceptor
	{
		bool IsBreakpoint(uint address);
		//cpu interface
		void MemoryBreakpoint(Breakpoint bp, uint address);
		bool ExecutionBreakpoint(uint pc);

		//machine interface
		void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Execute, int counter = 0, 
			Size size = Size.Long, ulong? value = null, Func<Breakpoint, bool> callback = null);
		void RemoveBreakpoint(uint address);
		void ToggleBreakpoint(uint pc);
		bool BreakpointHit();
		void DumpBreakpoints();
	}
}