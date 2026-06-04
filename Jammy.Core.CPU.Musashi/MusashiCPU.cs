using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.CPU.Musashi
{
	public enum MusashiCPUType
	{
		M68K_CPU_TYPE_INVALID,
		M68K_CPU_TYPE_68000,
		M68K_CPU_TYPE_68010,
		M68K_CPU_TYPE_68EC020,
		M68K_CPU_TYPE_68020,
		M68K_CPU_TYPE_68EC030,
		M68K_CPU_TYPE_68030,
		M68K_CPU_TYPE_68EC040,
		M68K_CPU_TYPE_68LC040,
		M68K_CPU_TYPE_68040,
		M68K_CPU_TYPE_SCC68070
	};

	public class MusashiCPU : MusashiCPUInternal
	{	
		public MusashiCPU(IInterrupt interrupt, IMemoryMapper memoryMapper, IBreakpointCollection breakpoints, ITracer tracer, IOptions<EmulationSettings> settings, ILogger<MusashiCPU> logger) :
			base(MusashiCPUType.M68K_CPU_TYPE_68000, interrupt, memoryMapper, breakpoints, tracer, settings, logger)
		{
		}
	}

	public class Musashi68EC020CPU : MusashiCPUInternal
	{
		public Musashi68EC020CPU(IInterrupt interrupt, IMemoryMapper memoryMapper, IBreakpointCollection breakpoints, ITracer tracer, IOptions<EmulationSettings> settings, ILogger<MusashiCPU> logger) :
			base(MusashiCPUType.M68K_CPU_TYPE_68EC020, interrupt, memoryMapper, breakpoints, tracer, settings, logger)
		{
		}
	}

	public class Musashi68030CPU : MusashiCPUInternal
	{
		public Musashi68030CPU(IInterrupt interrupt, IMemoryMapper memoryMapper, IBreakpointCollection breakpoints, ITracer tracer, IOptions<EmulationSettings> settings, ILogger<MusashiCPU> logger) :
			base(MusashiCPUType.M68K_CPU_TYPE_68030, interrupt, memoryMapper, breakpoints, tracer, settings, logger)
		{
		}
	}

	public class Musashi68040CPU : MusashiCPUInternal
	{
		public Musashi68040CPU(IInterrupt interrupt, IMemoryMapper memoryMapper, IBreakpointCollection breakpoints, ITracer tracer, IOptions<EmulationSettings> settings, ILogger<MusashiCPU> logger) :
			base(MusashiCPUType.M68K_CPU_TYPE_68040, interrupt, memoryMapper, breakpoints, tracer, settings, logger)
		{
		}
	}

	public class MusashiCPUInternal : ICPU, IMusashiCPU, IStatePersister
	{
		private readonly IInterrupt interrupt;
		private readonly IMemoryMapper memoryMapper;
		private readonly IBreakpointCollection breakpoints;
		private readonly ITracer tracer;
		private readonly EmulationSettings settings;
		private readonly MusashiCPUType cpuType;

		[DllImport("Musashi.dll")]
		private static extern unsafe void Musashi_init(
			IntPtr ctx,
			uint cputype,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint> r32,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint> r16,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint> r8,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint, void> w32,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint, void> w16,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint, void> w8
		);

		[DllImport("Musashi.dll")]
		private static extern uint Musashi_execute(ref int cycles);

		[DllImport("Musashi.dll")]
		private static extern void Musashi_get_regs(Musashi_regs regs);

		[DllImport("Musashi.dll")]
		private static extern void Musashi_set_regs(Musashi_regs regs);

		[DllImport("Musashi.dll")]
		private static extern void Musashi_set_pc(uint pc);

		[DllImport("Musashi.dll")]
		private static extern void Musashi_set_irq(uint levels);

		[DllImport("Musashi.dll")]
		private static extern void Musashi_pulse_reset();

		public MusashiCPUInternal(MusashiCPUType cpuType, IInterrupt interrupt, IMemoryMapper memoryMapper,
			IBreakpointCollection breakpoints, ITracer tracer, IOptions<EmulationSettings> settings, ILogger<MusashiCPU> logger)
		{
			this.cpuType = cpuType;
			this.interrupt = interrupt;
			this.memoryMapper = memoryMapper;
			this.breakpoints = breakpoints;
			this.tracer = tracer;
			this.settings = settings.Value;
			logger.LogTrace($"Starting Musashi C {cpuType.ToString().Substring(14)} CPU");
		}

		public unsafe void Initialise()
		{
			var handle = GCHandle.Alloc(this, GCHandleType.Normal);
			IntPtr contextPtr = GCHandle.ToIntPtr(handle);

			Musashi_init(
				contextPtr,
				(uint)cpuType,
				&Musashi_read32_thunk,
				&Musashi_read16_thunk,
				&Musashi_read8_thunk,
				&Musashi_write32_thunk,
				&Musashi_write16_thunk,
				&Musashi_write8_thunk
			);
		}

		private void CheckInterrupt()
		{
			ushort interruptLevel = interrupt.GetInterruptLevel();
			Musashi_set_irq(interruptLevel);
		}
		
		//tracer
		private readonly Regs traceRegs = new Regs();
		//tracer

		private uint instructionStartPC = 0;
		private int cycles=0;

		public uint GetCycles()
		{
			return (uint)cycles;
		}

		public void Emulate()
		{
			CheckInterrupt();

			//tracer
			ushort ins = 0;
			uint ipc = 0;
			if (settings.Tracer.IsEnabled())
			{
				GetRegs(traceRegs);
				tracer.TraceAsm(traceRegs);
				ins = ((IDebugMemoryMapper)memoryMapper).UnsafeRead16(traceRegs.PC);
				ipc = traceRegs.PC; traceRegs.PC += 2;
			}
			//tracer

			uint pc = Musashi_execute(ref cycles);

			//tracer
			if (settings.Tracer.IsEnabled())
				tracer.TracePost(traceRegs, pc, ipc, ins);
			//tracer

			instructionStartPC = pc;

			breakpoints.ExecutionBreakpoint(pc);
		}

		public void Reset()
		{
			Musashi_pulse_reset();
		}

		public Regs GetRegs()
		{
			var regs = new Regs();
			return GetRegs(regs);
		}

		public Regs GetRegs(Regs regs)
		{
			var musashiRegs = new Musashi_regs();
			Musashi_get_regs(musashiRegs);

			regs.D[0] = musashiRegs.d0;
			regs.D[1] = musashiRegs.d1;
			regs.D[2] = musashiRegs.d2;
			regs.D[3] = musashiRegs.d3;
			regs.D[4] = musashiRegs.d4;
			regs.D[5] = musashiRegs.d5;
			regs.D[6] = musashiRegs.d6;
			regs.D[7] = musashiRegs.d7;

			regs.A[0] = musashiRegs.a0;
			regs.A[1] = musashiRegs.a1;
			regs.A[2] = musashiRegs.a2;
			regs.A[3] = musashiRegs.a3;
			regs.A[4] = musashiRegs.a4;
			regs.A[5] = musashiRegs.a5;
			regs.A[6] = musashiRegs.a6;
			regs.A[7] = musashiRegs.a7;

			regs.PC = musashiRegs.pc;
			regs.SR = musashiRegs.sr;

			regs.SSP = musashiRegs.ssp;
			regs.SP = musashiRegs.usp;

			return regs;
		}


		public void SetRegs(Regs regs)
		{
			var musashiRegs = new Musashi_regs();

			musashiRegs.d0 = regs.D[0];
			musashiRegs.d1 = regs.D[1];
			musashiRegs.d2 = regs.D[2];
			musashiRegs.d3 = regs.D[3];
			musashiRegs.d4 = regs.D[4];
			musashiRegs.d5 = regs.D[5];
			musashiRegs.d6 = regs.D[6];
			musashiRegs.d7 = regs.D[7];

			musashiRegs.a0 = regs.A[0];
			musashiRegs.a1 = regs.A[1];
			musashiRegs.a2 = regs.A[2];
			musashiRegs.a3 = regs.A[3];
			musashiRegs.a4 = regs.A[4];
			musashiRegs.a5 = regs.A[5];
			musashiRegs.a6 = regs.A[6];
			musashiRegs.a7 = regs.A[7];

			musashiRegs.pc = regs.PC;
			musashiRegs.sr = regs.SR;

			musashiRegs.ssp = regs.SSP;
			musashiRegs.usp = regs.SP;

			Musashi_set_regs(musashiRegs);
		}

		public void SetPC(uint pc)
		{
			Musashi_set_pc(pc);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static MusashiCPU FromIntPtr(IntPtr context)
		{
			return Unsafe.As<MusashiCPU>(GCHandle.FromIntPtr(context).Target);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static uint Musashi_read32_thunk(IntPtr context, uint address)
		{
			return FromIntPtr(context).Musashi_read32(address);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static uint Musashi_read16_thunk(IntPtr context, uint address)
		{
			return FromIntPtr(context).Musashi_read16(address);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static uint Musashi_read8_thunk(IntPtr context, uint address)
		{
			return FromIntPtr(context).Musashi_read8(address);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static void Musashi_write32_thunk(IntPtr context, uint address, uint value)
		{
			FromIntPtr(context).Musashi_write32(address, value);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static void Musashi_write16_thunk(IntPtr context, uint address, uint value)
		{
			FromIntPtr(context).Musashi_write16(address, value);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static void Musashi_write8_thunk(IntPtr context, uint address, uint value)
		{
			FromIntPtr(context).Musashi_write8(address, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint Musashi_read32(uint address)
		{
			return memoryMapper.Read(instructionStartPC, address, Size.Long);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint Musashi_read16(uint address)
		{
			//word read at instruction address is instruction fetch
			if (address == instructionStartPC)
			{
				uint m = memoryMapper.Fetch(instructionStartPC, address, Size.Word);
				return m;
			}
			uint n = memoryMapper.Read(instructionStartPC, address, Size.Word);
			return n;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint Musashi_read8(uint address)
		{
			return memoryMapper.Read(instructionStartPC, address, Size.Byte);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Musashi_write32(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Long);
			if (settings.Tracer.IsEnabled())
				tracer.Flush(address);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Musashi_write16(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Word);
			if (settings.Tracer.IsEnabled())
				tracer.Flush(address);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Musashi_write8(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Byte);
			if (settings.Tracer.IsEnabled())
				tracer.Flush(address & 0xfffffffe);
		}

		public void Save(JArray obj)
		{
			var regs = GetRegs();
			var jo = JObject.FromObject(regs);
			jo["id"] = "cpuregs";
			obj.Add(jo);
		}

		public void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "cpuregs")) return;

			var regs = new Regs();
			obj.GetValue("A").Select(x => uint.Parse((string)x)).ToArray().CopyTo(regs.A, 0);
			obj.GetValue("D").Select(x => uint.Parse((string)x)).ToArray().CopyTo(regs.D, 0);
			regs.PC = uint.Parse((string)obj["PC"]);
			regs.SP = uint.Parse((string)obj["SP"]);
			regs.SSP = uint.Parse((string)obj["SSP"]);
			regs.SR = ushort.Parse((string)obj["SR"]);
			SetRegs(regs);
		}
	}
}