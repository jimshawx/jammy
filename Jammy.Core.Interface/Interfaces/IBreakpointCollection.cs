using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;

namespace Jammy.Core.Interface.Interfaces
{
	public interface IBreakpointCollection : IMemoryInterceptor
	{
		bool IsBreakpoint(uint pc);
		//cpu interface
		void SignalBreakpoint(uint address);
		bool CheckBreakpoints(uint address);

		//machine interface
		void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Permanent, int counter = 0, Size size = Size.Long);
		void ToggleBreakpoint(uint pc);
		bool BreakpointHit();
	}
}