using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.CPU.Moira
{
	public class MoiraCPU : ICPU, IMoiraCPU, IStatePersister
	{
		private readonly IInterrupt interrupt;
		private readonly IMemoryMapper memoryMapper;
		private readonly IBreakpointCollection breakpoints;
		private readonly ITracer tracer;
		private readonly ILogger logger;
		private readonly EmulationSettings settings;

		[DllImport("moira.dll", CallingConvention = CallingConvention.Cdecl)]
		private static extern unsafe void Moira_init(
			IntPtr ctx,
			int model,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint> r16,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint> r8,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint, void> w16,
			delegate* unmanaged[Cdecl]<IntPtr, uint, uint, void> w8,
			delegate* unmanaged[Cdecl]<IntPtr, int, void> sync
		);

		[DllImport("Moira.dll")]
		static extern uint Moira_execute(ref int cycles);

		[DllImport("Moira.dll")]
		static extern void Moira_get_regs(Moira_regs regs);

		[DllImport("Moira.dll")]
		static extern void Moira_set_regs(Moira_regs regs);

		[DllImport("Moira.dll")]
		static extern void Moira_set_pc(uint pc);

		[DllImport("Moira.dll")]
		static extern void Moira_set_irq(uint levels);

		public MoiraCPU(IInterrupt interrupt, IMemoryMapper memoryMapper,
			IBreakpointCollection breakpoints, ITracer tracer,
			IOptions<EmulationSettings> settings,
			ILogger<MoiraCPU> logger)
		{
			this.interrupt = interrupt;
			this.memoryMapper = memoryMapper;
			this.breakpoints = breakpoints;
			this.tracer = tracer;
			this.logger = logger;
			this.settings = settings.Value;

			if (settings.Value.Sku == CPUSku.MC68000)
				logger.LogTrace("Starting Moira C++ 68000 CPU");
			else
				logger.LogTrace("Starting Moira C++ 68EC020 CPU");
		}

		private enum Model : int
		{
			M68000,                 // Cycle-exact emulation
			M68010,                 // Cycle-exact emulation
			M68EC020,               // Non-cycle exaxt emulation
			M68020,                 // Non-cycle exaxt emulation
			M68EC030,               // Disassembler only
			M68030,                 // Disassembler only
			M68EC040,               // Disassembler only
			M68LC040,               // Disassembler only
			M68040                  // Disassembler only
		};

		public unsafe void Initialise()
		{
			var model = settings.Sku == CPUSku.MC68000 ? Model.M68000 : Model.M68EC020;

			var handle = GCHandle.Alloc(this, GCHandleType.Normal);
			IntPtr contextPtr = GCHandle.ToIntPtr(handle);

			Moira_init(
				contextPtr,
				(int)model,
				&Moira_read16_thunk,
				&Moira_read8_thunk,
				&Moira_write16_thunk,
				&Moira_write8_thunk,
				&Moira_sync_thunk
			);
		}

		private void CheckInterrupt()
		{
			ushort interruptLevel = interrupt.GetInterruptLevel();
			Moira_set_irq(interruptLevel);
		}

		//tracer
		private readonly Regs traceRegs = new Regs();
		//tracer

		private uint instructionStartPC = 0;
		private int cycles = 0;

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

			uint pc = Moira_execute(ref cycles);

			//tracer
			if (settings.Tracer.IsEnabled())
				tracer.TracePost(traceRegs, pc, ipc, ins);
			//tracer

			instructionStartPC = pc;

			breakpoints.ExecutionBreakpoint(pc);
		}

		public void Reset()
		{
			var r = new Moira_regs();
			uint sp = memoryMapper.Read(0, 0, Size.Long);
			uint pc = memoryMapper.Read(0, 4, Size.Long);

			//supervisor mode
			r.sr = 0x2704;
			r.ssp = sp;
			r.pc = pc;

			Moira_set_regs(r);
		}

		public Regs GetRegs()
		{
			var regs = new Regs();
			return GetRegs(regs);
		}

		public Regs GetRegs(Regs regs)
		{
			var MoiraRegs = new Moira_regs();
			Moira_get_regs(MoiraRegs);

			regs.D[0] = MoiraRegs.d0;
			regs.D[1] = MoiraRegs.d1;
			regs.D[2] = MoiraRegs.d2;
			regs.D[3] = MoiraRegs.d3;
			regs.D[4] = MoiraRegs.d4;
			regs.D[5] = MoiraRegs.d5;
			regs.D[6] = MoiraRegs.d6;
			regs.D[7] = MoiraRegs.d7;

			regs.A[0] = MoiraRegs.a0;
			regs.A[1] = MoiraRegs.a1;
			regs.A[2] = MoiraRegs.a2;
			regs.A[3] = MoiraRegs.a3;
			regs.A[4] = MoiraRegs.a4;
			regs.A[5] = MoiraRegs.a5;
			regs.A[6] = MoiraRegs.a6;
			regs.A[7] = MoiraRegs.a7;

			regs.PC = MoiraRegs.pc;
			regs.SR = MoiraRegs.sr;

			regs.SSP = MoiraRegs.ssp;
			regs.SP = MoiraRegs.usp;

			return regs;
		}


		public void SetRegs(Regs regs)
		{
			var MoiraRegs = new Moira_regs();

			MoiraRegs.d0 = regs.D[0];
			MoiraRegs.d1 = regs.D[1];
			MoiraRegs.d2 = regs.D[2];
			MoiraRegs.d3 = regs.D[3];
			MoiraRegs.d4 = regs.D[4];
			MoiraRegs.d5 = regs.D[5];
			MoiraRegs.d6 = regs.D[6];
			MoiraRegs.d7 = regs.D[7];

			MoiraRegs.a0 = regs.A[0];
			MoiraRegs.a1 = regs.A[1];
			MoiraRegs.a2 = regs.A[2];
			MoiraRegs.a3 = regs.A[3];
			MoiraRegs.a4 = regs.A[4];
			MoiraRegs.a5 = regs.A[5];
			MoiraRegs.a6 = regs.A[6];
			MoiraRegs.a7 = regs.A[7];

			MoiraRegs.pc = regs.PC;
			MoiraRegs.sr = regs.SR;

			MoiraRegs.ssp = regs.SSP;
			MoiraRegs.usp = regs.SP;

			Moira_set_regs(MoiraRegs);
		}

		public void SetPC(uint pc)
		{
			Moira_set_pc(pc);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static MoiraCPU FromIntPtr(IntPtr context)
		{
			return Unsafe.As<MoiraCPU>(GCHandle.FromIntPtr(context).Target);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static uint Moira_read16_thunk(IntPtr context, uint address)
		{
			return FromIntPtr(context).Moira_read16(address);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static uint Moira_read8_thunk(IntPtr context, uint address)
		{
			return FromIntPtr(context).Moira_read8(address);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static void Moira_write16_thunk(IntPtr context, uint address, uint value)
		{
			FromIntPtr(context).Moira_write16(address, value);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static void Moira_write8_thunk(IntPtr context, uint address, uint value)
		{
			FromIntPtr(context).Moira_write8(address, value);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
		private static void Moira_sync_thunk(IntPtr context, int cycles)
		{
			FromIntPtr(context).Moira_sync(cycles);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint Moira_read16(uint address)
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
		private uint Moira_read8(uint address)
		{
			return memoryMapper.Read(instructionStartPC, address, Size.Byte);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Moira_write16(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Word);
			if (settings.Tracer.IsEnabled())
				tracer.Flush(address);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Moira_write8(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Byte);
			if (settings.Tracer.IsEnabled())
				tracer.Flush(address & 0xfffffffe);
		}

		private Action<int> syncChipset = NullSync;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Moira_sync(int cycles)
		{
			syncChipset(cycles);
		}

		public void SetSync(Action<int> syncChipset)
		{
			this.syncChipset = syncChipset;
		}

		private static void NullSync(int _) {}

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