using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using m68kcpu;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Jammy.Core.CPU.Musashi.CSharp
{
	public class CPUWrapperMusashi : ICPU, IMusashiCSharpCPU, IStatePersister
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

			M68KCPU.M68K_EMULATE_PREFETCH = M68KCPU.OPT_OFF;
			M68KCPU.m68k_init();
			switch(settings.Value.Sku)
			{ 
				case CPUSku.MC68000: M68KCPU.m68k_set_cpu_type(M68KCPU.M68K_CPU_TYPE.M68K_CPU_TYPE_68000);
					if (settings.Value.Prefetch.IsEnabled())
						M68KCPU.M68K_EMULATE_PREFETCH = M68KCPU.OPT_ON;
					break;
				case CPUSku.MC68EC020: M68KCPU.m68k_set_cpu_type(M68KCPU.M68K_CPU_TYPE.M68K_CPU_TYPE_68EC020); break;
				case CPUSku.MC68030: M68KCPU.m68k_set_cpu_type(M68KCPU.M68K_CPU_TYPE.M68K_CPU_TYPE_68030); break;
				case CPUSku.MC68040: M68KCPU.m68k_set_cpu_type(M68KCPU.M68K_CPU_TYPE.M68K_CPU_TYPE_68040); break;
				default: throw new ArgumentOutOfRangeException(nameof(settings.Value.Sku));
			}
			logger.LogTrace($"Starting Musashi C# {settings.Value.Sku.ToString().Split('.').Last().Substring(2)} CPU");

			M68KCPU.m68k_pulse_reset();
		}

		private void CheckInterrupt()
		{
			ushort interruptLevel = interrupt.GetInterruptLevel();
			M68KCPU.m68k_set_irq(interruptLevel);
		}

		private int cycles=0;
		public void Emulate()
		{
			CheckInterrupt();
			cycles = M68KCPU.m68k_execute(1);
			
			uint pc = M68KCPU.m68k_get_reg(null, M68KCPU.m68k_register_t.M68K_REG_PC);
			M68KCPU.SetInstructionStartPC(pc);

			breakpoints.CheckBreakpoints(pc);
		}

		public uint GetCycles()
		{
			return (uint)cycles;
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

		//static void m68040_fpu_op0() { throw new NotImplementedException("m68040_fpu_op0()"); }
		//static void m68040_fpu_op1() { throw new NotImplementedException("m68040_fpu_op1()"); }
		//static void m68881_mmu_ops() { throw new NotImplementedException("m68881_mmu_ops()"); }
		static uint m68k_read_memory_8(uint A) { return memoryMapper.Read(instructionStartPC, A, Size.Byte); }
		static uint m68k_read_memory_16(uint A) {
			if (A == instructionStartPC)
				return memoryMapper.Fetch(instructionStartPC, A, Size.Word);
			return memoryMapper.Read(instructionStartPC, A, Size.Word); }
		static uint m68k_read_memory_32(uint A) { return memoryMapper.Read(instructionStartPC, A, Size.Long); }
		static void m68k_write_memory_8(uint A, uint v) { memoryMapper.Write(instructionStartPC, A, v, Size.Byte); }
		static void m68k_write_memory_16(uint A, uint v) { memoryMapper.Write(instructionStartPC, A, v, Size.Word); }
		static void m68k_write_memory_32(uint A, uint v) { memoryMapper.Write(instructionStartPC, A, v, Size.Long); }
	}

}

