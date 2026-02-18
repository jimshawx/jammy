using System;
using System.Collections.Generic;
using System.Linq;
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

		public void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Execute, int counter = 0,
			Size size = Size.Word, ulong? value = null, Func<Breakpoint, bool> callback = null)
		{
			breakpoints[address] = new Breakpoint { Address = address, Active = true, Type = type, Counter = counter,
				CounterReset = counter, Size = size, Value = value, Callback = callback };
		}

		public void RemoveBreakpoint(uint address)
		{
			breakpoints.Remove(address);
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && Matches(bp, value, size) && bp.Active)
				if (bp.Type == BreakpointType.Write || bp.Type == BreakpointType.ReadOrWrite)
					if (bp.Callback != null || bp.Callback(bp))
						SignalBreakpoint(insaddr);
		}

		public void Read(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && Matches(bp, value, size) && bp.Active)
				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
					if (bp.Callback != null || bp.Callback(bp))
						SignalBreakpoint(insaddr);
		}

		public void Fetch(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && bp.Active)
				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
					if (bp.Callback != null || bp.Callback(bp))
						SignalBreakpoint(insaddr);
		}

		private bool Matches(Breakpoint bp, ulong value, Size size)
		{
			return (bp.Value == null || bp.Value == value) 
			       && bp.Size == size;
		}

		public bool IsBreakpoint(uint pc)
		{
			if (breakpoints.TryGetValue(pc, out Breakpoint bp))
			{
				if (bp.Callback != null && !bp.Callback(bp))
					return false;

				if (bp.Type == BreakpointType.Execute)
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

				if (bp.Type == BreakpointType.Write)
					return false;
				if (bp.Type == BreakpointType.Read)
					return false;
				if (bp.Type == BreakpointType.ReadOrWrite)
					return false;

				return bp.Active;
			}
			return false;
		}

		public void ToggleBreakpoint(uint pc)
		{
			if (breakpoints.TryGetValue(pc, out var breakpoint))
				breakpoint.Active ^= true;
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

		public void DumpBreakpoints()
		{
			foreach (var bp in breakpoints.OrderBy(x => x.Key))
				logger.LogTrace($"{bp.Key:X8} {(bp.Value.Active?"X":"-")} {bp.Value.Type} {bp.Value.Size} {bp.Value.Value:X8}");
		}
	}
}
