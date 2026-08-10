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
			Size? size = null, ulong? value = null, Func<BreakpointHitInfo, bool> callback = null)
		{
			breakpoints[address] = new Breakpoint { Address = address, Active = true, Type = type, Counter = counter,
				CounterReset = counter, Size = size, Value = value, BreakpointHit = callback };
		}

		public void RemoveBreakpoint(uint address)
		{
			breakpoints.Remove(address);
		}

		public void RemoveBreakpoint(Breakpoint bp)
		{
			breakpoints.Remove(bp.Address);
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && Matches(bp, value, size) && bp.Active)
				if (bp.Type == BreakpointType.Write || bp.Type == BreakpointType.ReadOrWrite)
					MemoryBreakpoint(bp,insaddr, size);
		}

		public void Read(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && Matches(bp, value, size) && bp.Active)
				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
					MemoryBreakpoint(bp, insaddr, size);
		}

		public void Fetch(uint insaddr, uint address, uint value, Size size)
		{
			if (breakpoints.TryGetValue(address, out Breakpoint bp) && bp.Active)
				if (bp.Type == BreakpointType.Read || bp.Type == BreakpointType.ReadOrWrite)
					MemoryBreakpoint(bp, insaddr, size);
		}

		private bool Matches(Breakpoint bp, ulong value, Size size)
		{
			return (bp.Value == null || bp.Value == value) 
			       && (!bp.Size.HasValue || bp.Size.Value == size);
		}

		private bool ShouldBreakpointTrigger(uint pc, BreakpointHitInfo bphi)
		{
			var bp = bphi.Bp;

			//does it have a function to call when hit? if so, call it
			if (bp.BreakpointHit != null)
			{
				//returns true if we are to stop
				return bp.BreakpointHit(bphi);
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
				//bp.Active = false;
				breakpoints.Remove(bp.Address);
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
		public void MemoryBreakpoint(Breakpoint bp, uint address, Size size)
		{
			Breakpoint(bp, address, size);
		}

		//here is where the CPUs call at the end of an instruction to check for a breakpoint at new pc
		public bool ExecutionBreakpoint(uint pc)
		{
			if (breakpoints.TryGetValue(pc, out var bp) && IsExecutable(bp))
			{
				Breakpoint(bp, pc, Size.Word);
				return true;
			}

			return false;
		}

		//just one of these, reused to save allocating
		private readonly BreakpointHitInfo hitbp = new BreakpointHitInfo();

		private BreakpointHitInfo breakpointHit;

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
		private void Breakpoint(Breakpoint bp, uint pc, Size size)
		{
			logger.LogTrace($"Breakpoint @{pc:X8} {bp.Type}");

			//nb. there could be multiple breakpoints on the same instruction, read/write/execute
			if (breakpointHit != null)
			{
				logger.LogTrace($"Multiple Breakpoints hit @{pc:X8} {bp.Type}");
				return;
			}

			hitbp.Bp = bp;
			hitbp.Address = pc;
			hitbp.Size = size;
			breakpointHit = hitbp;
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
