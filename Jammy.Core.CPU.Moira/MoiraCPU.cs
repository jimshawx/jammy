using System;
using System.Linq;
using System.Runtime.InteropServices;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
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

		[DllImport("Moira.dll")]
		static extern void Moira_init(IntPtr r16, IntPtr r8, IntPtr w16, IntPtr w8, IntPtr sync);

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

		private Moira_Reader r16;
		private Moira_Reader r8;
		private Moira_Writer w16;
		private Moira_Writer w8;
		private Moira_Sync sync;

		public MoiraCPU(IInterrupt interrupt, IMemoryMapper memoryMapper,
			IBreakpointCollection breakpoints, ITracer tracer, ILogger<MoiraCPU> logger)
		{
			this.interrupt = interrupt;
			this.memoryMapper = memoryMapper;
			this.breakpoints = breakpoints;
			this.tracer = tracer;
			this.logger = logger;
			logger.LogTrace("Starting Moira C 68000 CPU");

			r16 = new Moira_Reader(Moira_read16);
			r8 = new Moira_Reader(Moira_read8);
			w16 = new Moira_Writer(Moira_write16);
			w8 = new Moira_Writer(Moira_write8);
			sync = new Moira_Sync(Moira_sync);

			Moira_init(
				Marshal.GetFunctionPointerForDelegate(r16),
				Marshal.GetFunctionPointerForDelegate(r8),
				Marshal.GetFunctionPointerForDelegate(w16),
				Marshal.GetFunctionPointerForDelegate(w8),
				Marshal.GetFunctionPointerForDelegate(sync)
			);
		}

		private void CheckInterrupt()
		{
			ushort interruptLevel = interrupt.GetInterruptLevel();
			Moira_set_irq(interruptLevel);
		}

		private uint instructionStartPC = 0;
		private int cycles=0;

		public uint GetCycles()
		{
			return (uint)cycles;
		}

		public void Emulate()
		{
			CheckInterrupt();

			/*

			//tracer
			var regs = GetRegs();
			tracer.TraceAsm(regs.PC, regs);
			ushort ins = (ushort)memoryMapper.Read(0, regs.PC, Size.Word);
			//bsr, bra, jmp, jsr, rts, rte
			uint ipc = regs.PC; regs.PC += 2;
			//tracer

			*/

			uint pc = Moira_execute(ref cycles);

			/*

			//tracer
			if ((ins & 0xff00) == 0x6100)
			{
				uint disp = (uint)(sbyte)ins & 0xff;
				if (disp == 0) { regs.PC += 2; }
				else if (disp == 0xff) { regs.PC += 4; }

				tracer.Trace("bsr", ipc, regs); //bsr
				tracer.Trace(pc); //bsr
			}
			else if ((ins & 0xf000) == 0x6000)
			{
				uint inssize = 2;
				uint disp = (uint)(sbyte)ins & 0xff;
				if (disp == 0) { inssize += 2; regs.PC += 2; }
				else if (disp == 0xff) { inssize+=4; regs.PC += 4;}
				if (pc != ipc+inssize)
				{
					tracer.Trace("bra", ipc, regs); //bcc
					tracer.Trace(pc); //bsr
				}
			}
			else if ((ins & 0xffc0) == 0x4e80)
			{
				tracer.Trace("jsr", ipc, regs);//jsr
				tracer.Trace(pc); //bsr
			}
			else if ((ins & 0xffc0) == 0x4ec0)
			{
				tracer.Trace("jmp", ipc, regs);//jmp
				tracer.Trace(pc); //bsr
			}
			else if (ins == 0x4e75)
			{
				tracer.Trace("rts", ipc, regs);//rts
				tracer.Trace(pc); //bsr
			}
			else if (ins == 0x4e73)
			{
				tracer.Trace("rte", ipc, regs);//rte
				tracer.Trace(pc); //bsr
			}
			//tracer
			
			*/

			instructionStartPC = pc;

			breakpoints.CheckBreakpoints(pc);
		}

		public void Reset()
		{
			//Moira_set_pc(4);
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

		private delegate uint Moira_Reader(uint address);
		private delegate void Moira_Writer(uint address, uint value);
		private delegate void Moira_Sync(int cycles);

		private uint Moira_read16(uint address)
		{
			//word read at instruction address is instruction fetch
			if (address == instructionStartPC)
				return memoryMapper.Fetch(instructionStartPC, address, Size.Word);
			return memoryMapper.Read(instructionStartPC, address, Size.Word);
		}
		private uint Moira_read8(uint address)
		{
			return memoryMapper.Read(instructionStartPC, address, Size.Byte);
		}
		private void Moira_write16(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Word);
		}
		private void Moira_write8(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Byte);
		}

		private Action<int> syncChipset = NullSync;

		private void Moira_sync(int cycles)
		{
			syncChipset(cycles>>1);
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