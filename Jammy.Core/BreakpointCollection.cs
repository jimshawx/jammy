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
				CounterReset = counter, Size = size, Value = value, BreakpointHit = callback };
		}

		public void RemoveBreakpoint(uint address)
		{
			breakpoints.Remove(address);
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && Matches(bp, value, size) && bp.Active)
				if (bp.Type == BreakpointType.Write || bp.Type == BreakpointType.ReadOrWrite)
					MemoryBreakpoint(bp,insaddr);
		}

		public void Read(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && Matches(bp, value, size) && bp.Active)
				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
					MemoryBreakpoint(bp, insaddr);
		}

		public void Fetch(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && bp.Active)
				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
					MemoryBreakpoint(bp, insaddr);
		}

		private bool Matches(Breakpoint bp, ulong value, Size size)
		{
			return (bp.Value == null || bp.Value == value) 
			       && bp.Size == size;
		}

		private bool ShouldBreakpointTrigger(uint pc, Breakpoint bp)
		{
			//does it have a function to call when hit? if so, call it
			if (bp.BreakpointHit != null)
			{
				//returns true if we are to stop
				return bp.BreakpointHit(bp);
			}

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
				return false;
			}

			if (bp.Type == BreakpointType.OneShot)
			{
				if (!bp.Active) return false;
				bp.Active = false;
				return true;
			}

			return bp.Active;
		}

		public void ToggleBreakpoint(uint pc)
		{
			if (breakpoints.TryGetValue(pc, out var breakpoint))
				breakpoint.Active ^= true;
			else
				AddBreakpoint(pc);
		}

		private bool IsExecutable(Breakpoint bp)
		{
			return bp.Type == BreakpointType.Execute ||
				bp.Type == BreakpointType.OneShot ||
				bp.Type == BreakpointType.Counter;
		}

		//here is where memory reads/writes/fetches call to signal a breakpoint
		public void MemoryBreakpoint(Breakpoint bp, uint address)
		{
			Breakpoint(bp, address);
		}

		//here is where the CPUs call at the end of an instruction to check for a breakpoint at new pc
		public bool ExecutionBreakpoint(uint pc)
		{
			if (breakpoints.TryGetValue(pc, out var bp) && IsExecutable(bp))
			{
				Breakpoint(bp, pc);
				return true;
			}

			return false;
		}

		private Breakpoint breakpointHit = null;

		//here is where emulation loop checks whether a breakpoint was hit and resets the hit
		//we are between instructions so emulation state is consistent
		public bool BreakpointHit()
		{
			if (breakpointHit == null) return false;

			var bp = breakpointHit;
			breakpointHit = null;

			return ShouldBreakpointTrigger(bp.Address, bp);
		}

		//signal a breakpoint (bp) hit
		private void Breakpoint(Breakpoint bp, uint pc)
		{
			logger.LogTrace($"Breakpoint @{pc:X8} {bp.Type}");
			//nb. there could be multiple breakpoints on the same instruction, read/write/execute
			breakpointHit = bp;
		}

		//is there any breakpoint here? currently only used by the disassembler
		public bool IsBreakpoint(uint address)
		{
			return breakpoints.ContainsKey(address);
		}

		public void DumpBreakpoints()
		{
			foreach (var bp in breakpoints.OrderBy(x => x.Key))
				logger.LogTrace($"{bp.Key:X8} {(bp.Value.Active?"X":"-")} {bp.Value.Type} {bp.Value.Size} {bp.Value.Value:X8}");
		}
	}
}
