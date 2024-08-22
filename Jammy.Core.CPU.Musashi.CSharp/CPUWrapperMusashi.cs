using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using m68kcpu;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Core.CPU.Musashi.CSharp
{
	public class CPUWrapperMusashi : ICPU, IMusashiCSharpCPU
	{
		private readonly IInterrupt interrupt;
		private readonly IBreakpointCollection breakpoints;

		public CPUWrapperMusashi(IInterrupt interrupt, IMemoryMapper memoryMapper,
			IBreakpointCollection breakpoints, ITracer tracer,
			IOptions<EmulationSettings> settings,
			ILogger<CPUWrapperMusashi> logger)
		{
			M68KCPU.Init(memoryMapper);
			this.interrupt = interrupt;
			this.breakpoints = breakpoints;

			M68KCPU.m68k_init();
			M68KCPU.m68k_set_cpu_type(M68KCPU.M68K_CPU_TYPE.M68K_CPU_TYPE_68000);
			M68KCPU.m68k_pulse_reset();
		}

		private void CheckInterrupt()
		{
			ushort interruptLevel = interrupt.GetInterruptLevel();
			M68KCPU.m68k_set_irq(interruptLevel);
		}

		public void Emulate()
		{
			CheckInterrupt();
			int cycles = M68KCPU.m68k_execute(1);
			
			uint pc = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_PC);
			M68KCPU.SetInstructionStartPC(pc);

			breakpoints.CheckBreakpoints(pc);
		}

		public Regs GetRegs()
		{
			var regs = new Regs();
			return GetRegs(regs);
		}

		public Regs GetRegs(Regs regs)
		{
			regs.D[0] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_D0);
			regs.D[1] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_D1);
			regs.D[2] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_D2);
			regs.D[3] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_D3);
			regs.D[4] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_D4);
			regs.D[5] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_D5);
			regs.D[6] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_D6);
			regs.D[7] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_D7);

			regs.A[0] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_A0);
			regs.A[1] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_A1);
			regs.A[2] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_A2);
			regs.A[3] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_A3);
			regs.A[4] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_A4);
			regs.A[5] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_A5);
			regs.A[6] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_A6);
			regs.A[7] = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_A7);

			regs.PC = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_PC);
			regs.SP = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_USP);
			regs.SSP = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_ISP);
			regs.SR = (ushort)M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_SR);
			return regs;
		}

		public void Reset()
		{
		}

		public void SetPC(uint pc)
		{
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_PC, pc);
		}

		public void SetRegs(Regs regs)
		{
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_D0, regs.D[0]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_D1, regs.D[1]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_D2, regs.D[2]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_D3, regs.D[3]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_D4, regs.D[4]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_D5, regs.D[5]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_D6, regs.D[6]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_D7, regs.D[7]);

			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_A0, regs.A[0]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_A1, regs.A[1]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_A2, regs.A[2]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_A3, regs.A[3]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_A4, regs.A[4]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_A5, regs.A[5]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_A6, regs.A[6]);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_A7, regs.A[7]);

			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_PC, regs.PC);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_ISP, regs.SSP);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_USP, regs.SP);
			M68KCPU.m68k_set_reg(M68KCPU.m68k_register_t.M68K_REG_SR, regs.SR);
		}
	}
}

namespace m68kcpu
{
	public static partial class M68KCPU
	{
		private static IMemoryMapper memoryMapper;

		public static void Init(IMemoryMapper memoryMapper)
		{
			M68KCPU.memoryMapper = memoryMapper;
		}

		private static uint instructionStartPC = 0;
		public static void SetInstructionStartPC(uint pc)
		{
			instructionStartPC = pc;
		}

		static void m68040_fpu_op0() { }
		static void m68040_fpu_op1() { }
		static void m68881_mmu_ops() { }
		static uint m68k_read_memory_8(uint A) { return memoryMapper.Read(0, A, Size.Byte); }
		static uint m68k_read_memory_16(uint A) {
			if (A == instructionStartPC)
				return memoryMapper.Fetch(instructionStartPC, A, Size.Word);
			return memoryMapper.Read(0, A, Size.Word); }
		static uint m68k_read_memory_32(uint A) { return memoryMapper.Read(0, A, Size.Long); }
		static void m68k_write_memory_8(uint A, uint v) { memoryMapper.Write(0, A, v, Size.Byte); }
		static void m68k_write_memory_16(uint A, uint v) { memoryMapper.Write(0, A, v, Size.Word); }
		static void m68k_write_memory_32(uint A, uint v) { memoryMapper.Write(0, A, v, Size.Long); }
		static void m68ki_check_bus_error_trap() { }
	}
}

