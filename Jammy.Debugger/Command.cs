using Jammy.Core;
using Jammy.Core.Types.Types;
using Jammy.Core.Types.Types.Breakpoints;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Debugger
{
	public class DebugCommand : IDebugCommand
	{
		private readonly IDebugger debugger;
		private readonly IAnalysis analysis;
		private readonly IDisassemblyRanges disassemblyRanges;
		private readonly IMemoryDumpRanges memoryDumpRanges;
		private readonly ILogger<DebugCommand> logger;

		public DebugCommand(IDebugger debugger, IAnalysis analysis,
			IDisassemblyRanges disassemblyRanges, IMemoryDumpRanges memoryDumpRanges,
			ILogger<DebugCommand> logger)
		{
			this.debugger = debugger;
			this.analysis = analysis;
			this.disassemblyRanges = disassemblyRanges;
			this.memoryDumpRanges = memoryDumpRanges;
			this.logger = logger;
		}
		
		public bool ProcessCommand(string cmd)
		{
			string[] parm = cmd.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parm.Length == 0)
				return false;

			uint A(int i) { string s = P(i); return uint.Parse(s, NumberStyles.HexNumber); }
			uint AD(int i, uint def) { string s = P(i); return string.IsNullOrEmpty(s) ? def : uint.Parse(s, NumberStyles.HexNumber); }
			uint? N(int i) { string s = P(i); return string.IsNullOrWhiteSpace(s) ? null : A(i); }
			Core.Types.Types.Size? S(int i)
			{
				string s = P(i);
				if (s.Length != 1) return null;
				if (char.ToLower(s[0]) == 'b') return Core.Types.Types.Size.Byte;
				if (char.ToLower(s[0]) == 'w') return Core.Types.Types.Size.Word;
				if (char.ToLower(s[0]) == 'l') return Core.Types.Types.Size.Long;
				return null;
			}
			string P(int i) { return (i < parm.Length) ? parm[i] : string.Empty; }
			string R(int i) { return (i < parm.Length) ? string.Join(' ', parm[i..]) : string.Empty; }
			MemType? M(int i)
			{
				string s = P(i);
				if (s.Length != 1) return null;
				if (char.ToLower(s[0]) == 'c') return MemType.Code;
				if (char.ToLower(s[0]) == 'b') return MemType.Byte;
				if (char.ToLower(s[0]) == 'w') return MemType.Word;
				if (char.ToLower(s[0]) == 'l') return MemType.Long;
				if (char.ToLower(s[0]) == 's') return MemType.Str;
				if (char.ToLower(s[0]) == 'u') return MemType.Unknown;
				return null;
			}

			Amiga.LockEmulation();
			bool refresh = false;
			var regs = debugger.GetRegs();

			try
			{
				switch (P(0))
				{
					case "b":
						debugger.AddBreakpoint(AD(1, regs.PC));
						break;

					case "bw":
						debugger.AddBreakpoint(A(1), BreakpointType.Write, 0, S(2) ?? Core.Types.Types.Size.Word);
						break;
					case "br":
						debugger.AddBreakpoint(A(1), BreakpointType.Read, 0, S(2) ?? Core.Types.Types.Size.Word);
						break;
					case "brw":
						debugger.AddBreakpoint(A(1), BreakpointType.ReadOrWrite, 0, S(2) ?? Core.Types.Types.Size.Word);
						break;
					case "bl":
						debugger.DumpBreakpoints();
						break;

					case "bc":
						debugger.RemoveBreakpoint(AD(1, regs.PC));
						break;

					case "t":
						debugger.ToggleBreakpoint(AD(1, regs.PC));
						break;

					case "d":
						disassemblyRanges.Add(new AddressRange(A(1), N(2) ?? 0x1000));
						refresh = true;
						break;

					case "m":
						memoryDumpRanges.Add(new AddressRange(A(1), N(2) ?? 0x1000));
						refresh = true;
						break;

					case "w":
						debugger.DebugWrite(A(1), N(2) ?? 0, S(3) ?? Core.Types.Types.Size.Word);
						break;

					case "r":
						uint v = debugger.DebugRead(A(1), S(2) ?? Core.Types.Types.Size.Word);
						logger.LogTrace($"{v:X8} ({v})");
						break;

					case "a":
						for (uint i = 0; i < (N(3) ?? 1); i++)
							analysis.SetMemType(A(1) + i, M(2) ?? MemType.Code);
						refresh = true;
						break;

					case "c":
						analysis.AddComment(A(1), R(2));
						refresh = true;
						break;

					case "h":
						analysis.AddHeader(A(1), $"\t{R(2)}");
						refresh = true;
						break;

					case "g":
						Amiga.SetEmulationMode(EmulationMode.Running, true);
						break;
					case "so":
						Amiga.SetEmulationMode(EmulationMode.StepOut, true);
						break;
					case "s":
						Amiga.SetEmulationMode(EmulationMode.Step, true);
						break;
					case "x":
						Amiga.SetEmulationMode(EmulationMode.Stopped, true);
						break;

					case "?":
						logger.LogTrace("b address - breakpoint on execute at address");
						logger.LogTrace("bw address [size(W)] - breakpoint on write at address");
						logger.LogTrace("br address [size(W)] - breakpoint on read at address");
						logger.LogTrace("brw address [size(W)] - breakpoint on read/write at address");
						logger.LogTrace("bc address - remove breakpoint at address");
						logger.LogTrace("t address - toggle breakpoint at address");
						logger.LogTrace("bl - list all breakpoints");
						logger.LogTrace("d address [length(1000h)] - add an address range to the debugger");
						logger.LogTrace("m address [length(1000h)] - add an address range to the memory dump");
						logger.LogTrace("w address [value(0)] [size(W)] - write a value to memory");
						logger.LogTrace("r address [size(W)] - read a value from memory");
						logger.LogTrace("a address [type(C)] [length(1)] - set memory type C,B,W,L,S,U");
						logger.LogTrace("c address text - add a comment");
						logger.LogTrace("h address text - add a header");
						logger.LogTrace("g - emulation Go");
						logger.LogTrace("s - emulation Step");
						logger.LogTrace("so - emulation Step Out");
						logger.LogTrace("x - emulation Stop");
						break;
				}
				//AddHistory(cmd);
			}
			catch
			{
				logger.LogTrace($"Can't execute \"{cmd}\"");
			}

			Amiga.UnlockEmulation();
			return refresh;
		}
	}
}
