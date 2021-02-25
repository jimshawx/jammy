using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.CPU.Musashi
{
	public class MusashiCPU : ICPU, IMusashiCPU
	{
		private readonly IInterrupt interrupt;
		private readonly IMemoryMapper memoryMapper;
		private readonly IBreakpointCollection breakpoints;
		private readonly ILogger logger;

		[DllImport("Musashi.dll")]
		static extern void Musashi_init(IntPtr r32, IntPtr r16, IntPtr r8, IntPtr w32, IntPtr w16, IntPtr w8);

		[DllImport("Musashi.dll")]
		static extern uint Musashi_execute(ref int cycles);

		[DllImport("Musashi.dll")]
		static extern void Musashi_get_regs(Musashi_regs regs);

		[DllImport("Musashi.dll")]
		static extern void Musashi_set_regs(Musashi_regs regs);

		[DllImport("Musashi.dll")]
		static extern void Musashi_set_pc(uint pc);

		[DllImport("Musashi.dll")]
		static extern void Musashi_set_irq(uint levels);

		private void CheckInterrupt()
		{
			ushort interruptLevel = interrupt.GetInterruptLevel();
			Musashi_set_irq(interruptLevel);
		}

		private ulong totalTicks = 0;
		private ulong tickCount = 0;

		private uint instructionStartPC = 0;
		public void Emulate(ulong cycles)
		{
			CheckInterrupt();
			
			int ticks = 0;
			uint pc = Musashi_execute(ref ticks);

			totalTicks += (ulong)ticks;
			tickCount++;

			if ((tickCount&0xffffff)==0)
				logger.LogTrace($"{(double)totalTicks/tickCount}");

			instructionStartPC = pc;

			if (breakpoints.IsBreakpoint(pc))
				Breakpoint(pc);
		}

		private void Breakpoint(uint pc)
		{
			//debugger.DumpTrace();
			logger.LogTrace($"Breakpoint @{pc:X8}");
			//Machine.SetEmulationMode(EmulationMode.Stopped, true);
			breakpoints.SignalBreakpoint();
			UI.UI.IsDirty = true;
		}

		public void Reset()
		{
			//Musashi_set_pc(4);
		}

		private Musashi_Reader r32;
		private Musashi_Reader r16;
		private Musashi_Reader r8;
		private Musashi_Writer w32;
		private Musashi_Writer w16;
		private Musashi_Writer w8;

		public MusashiCPU(IInterrupt interrupt, IMemoryMapper memoryMapper, IBreakpointCollection breakpoints, ILogger<MusashiCPU> logger)
		{
			this.interrupt = interrupt;
			this.memoryMapper = memoryMapper;
			this.breakpoints = breakpoints;
			this.logger = logger;

			r32 = new Musashi_Reader(Musashi_read32);
			r16 = new Musashi_Reader(Musashi_read16);
			r8 = new Musashi_Reader(Musashi_read8);
			w32 = new Musashi_Writer(Musashi_write32);
			w16 = new Musashi_Writer(Musashi_write16);
			w8 = new Musashi_Writer(Musashi_write8);

			Musashi_init(
				Marshal.GetFunctionPointerForDelegate(r32),
				Marshal.GetFunctionPointerForDelegate(r16),
				Marshal.GetFunctionPointerForDelegate(r8),
				Marshal.GetFunctionPointerForDelegate(w32),
				Marshal.GetFunctionPointerForDelegate(w16),
				Marshal.GetFunctionPointerForDelegate(w8)
			);
		}

		public Regs GetRegs()
		{
			var musashiRegs = new Musashi_regs();
			Musashi_get_regs(musashiRegs);
			var regs = new Regs();

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

		private delegate uint Musashi_Reader(uint address);
		private delegate void Musashi_Writer(uint address, uint value);

		private uint Musashi_read32(uint address)
		{
			return memoryMapper.Read(instructionStartPC, address, Size.Long);
		}
		private uint Musashi_read16(uint address)
		{
			return memoryMapper.Read(instructionStartPC, address, Size.Word);
		}
		private uint Musashi_read8(uint address)
		{
			return memoryMapper.Read(instructionStartPC, address, Size.Byte);
		}
		private void Musashi_write32(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Long);
		}
		private void Musashi_write16(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Word);
		}
		private void Musashi_write8(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Byte);
		}
	}
}