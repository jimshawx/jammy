using System.Collections.Generic;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core
{
	public class BreakpointCollection : IBreakpointCollection
	{
		private readonly ILogger logger;
		private readonly Dictionary<uint, Breakpoint> breakpoints = new Dictionary<uint, Breakpoint>();

		public BreakpointCollection(ILogger<BreakpointCollection> logger)
		{
			this.logger = logger;
		}

		public void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Permanent, int counter = 0, Size size = Size.Long)
		{
			breakpoints[address] = new Breakpoint { Address = address, Active = true, Type = type, Counter = counter, CounterReset = counter, Size = size };
		}

		public void RemoveBreakpoint(uint address)
		{
			breakpoints.Remove(address);
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp))
				if (bp.Type == BreakpointType.Write || bp.Type == BreakpointType.ReadOrWrite)
					SignalBreakpoint(insaddr);
		}

		public void Read(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp))
				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
					SignalBreakpoint(insaddr);
		}

		public void Fetch(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp))
				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
					SignalBreakpoint(insaddr);
		}

		//private bool IsMemoryBreakpoint(uint pc, BreakpointType type)
		//{
		//	//for (uint i = 0; i < 4; i++)
		//	uint i = 0;
		//	{
		//		if (breakpoints.TryGetValue(pc + i, out Breakpoint bp))
		//		{
		//			if (type == BreakpointType.Write)
		//			{
		//				if (bp.Type == BreakpointType.Write || bp.Type == BreakpointType.ReadOrWrite)
		//					return bp.Active;
		//			}
		//			else if (type == BreakpointType.Read)
		//			{
		//				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
		//					return bp.Active;
		//			}
		//		}
		//	}
		//	return false;
		//}

		public bool IsBreakpoint(uint pc)
		{
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

		public void ToggleBreakpoint(uint pc)
		{
			if (IsBreakpoint(pc))
				breakpoints[pc].Active ^= true;
			else
				AddBreakpoint(pc);
		}

		public void SignalBreakpoint(uint address)
		{
			Breakpoint(address);
		}

		public bool CheckBreakpoints(uint address)
		{
			if (IsBreakpoint(address))
			{
				Breakpoint(address);
				return true;
			}

			return false;
		}

		private bool breakpointHit;

		public bool BreakpointHit()
		{
			bool hit = breakpointHit;
			breakpointHit = false;
			return hit;
		}

		private void Breakpoint(uint pc)
		{
			logger.LogTrace($"Breakpoint @{pc:X8}");
			breakpointHit = true;
		}
	}
}