using System.Collections.Generic;

namespace RunAmiga.Core.Types.Types
{
	public enum BreakpointType
	{
		Permanent,
		Counter,
		OneShot,
		Read,
		Write,
		ReadOrWrite
	}

	public class Breakpoint
	{
		public uint Address { get; set; }
		public bool Active { get; set; }
		public BreakpointType Type { get; set;}

		public int CounterReset { get; set; }
		public int Counter { get; set; }

		public Size Size { get; set; }

		public Breakpoint()
		{
			Type = BreakpointType.Permanent;
		}
	}

	public class BreakpointCollection
	{
		private Dictionary<uint, Breakpoint> breakpoints = new Dictionary<uint, Breakpoint>();

		public void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Permanent, int counter = 0, Size size = Size.Long)
		{
			breakpoints[address] = new Breakpoint { Address = address, Active = true, Type = type, Counter = counter, CounterReset = counter, Size = size };
		}

		public bool IsMemoryBreakpoint(uint pc, BreakpointType type)
		{
			//for (uint i = 0; i < 4; i++)
			uint i = 0;
			{
				if (breakpoints.TryGetValue(pc + i, out Breakpoint bp))
				{
					if (type == BreakpointType.Write)
					{
						if (bp.Type == BreakpointType.Write || bp.Type == BreakpointType.ReadOrWrite)
							return bp.Active;
					}
					else if (type == BreakpointType.Read)
					{
						if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
							return bp.Active;
					}
				}
			}
			return false;
		}

		public bool IsBreakpoint(uint pc)
		{
			//var regs = cpu.GetRegs();

			//if (pc == 0xfc165a && string.Equals(GetString(regs.A[1]), "ciaa.resource"))
			//	return true;

			if (breakpoints.TryGetValue(pc, out Breakpoint bp))
			{
				if (bp.Type == BreakpointType.Permanent)
					return bp.Active;
				if (bp.Type == BreakpointType.Counter)
				{
					if (bp.Active)
					{
						bp.Counter--;
						if (bp.Counter == 0)
						{
							bp.Counter = bp.CounterReset;
							return true;
						}
					}
				}

				if (bp.Type == BreakpointType.OneShot)
					breakpoints.Remove(pc);
				return bp.Active;
			}
			return false;
		}


		public void RemoveBreakpoint(uint address)
		{
			breakpoints.Remove(address);
		}

		public void SetBreakpoint(uint address, bool active)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp))
				bp.Active = active;
		}

		public Breakpoint GetBreakpoint(uint address, bool active)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp))
				return bp;
			return new Breakpoint { Address = address, Active = false };
		}

		public void ToggleBreakpoint(uint pc)
		{
			if (IsBreakpoint(pc))
				breakpoints[pc].Active ^= true;
			else
				AddBreakpoint(pc);
		}

		private bool signalled = false;
		public void SignalBreakpoint()
		{
			signalled = true;
		}

		public void AckBreakpoint()
		{
			signalled = false;
		}

		public bool IsBreakpointSignalled()
		{
			return signalled;
		}
	}
}
