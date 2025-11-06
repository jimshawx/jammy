using Jammy.Core.Interface.Interfaces;
using Jammy.Debugger.Types;
using Jammy.Interface;
using Jammy.Types;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

namespace Jammy.Debugger
{
	public interface IFlowAnalyser
	{
		PC_TRACE start_pc_trace(uint pc);
	}

	public class FlowAnalyser : IFlowAnalyser
	{
		private readonly IDisassembler disassembler;
		private readonly ILogger logger;
		private readonly IDebugMemoryMapper memory; 

		public FlowAnalyser(IMemoryMapper memory, IDisassembler disassembler, ILogger<FlowAnalyser> logger)
		{
			this.disassembler = disassembler;
			this.logger = logger;
			this.memory = (IDebugMemoryMapper)memory;
		}

		public PC_TRACE start_pc_trace(uint pc)
		{
			var pctrace = new PC_TRACE();
			pctrace.Start = pc;
			trace(new_node(pc, BRANCH_TYPE.BT_FALLTHROUGH, pctrace, null), 0, pctrace);
			return pctrace;
		}

		private ushort peek_fast(uint pc) { return memory.UnsafeRead16(pc); }

		private byte[] peek_longest(uint pc)
		{
			var b = new byte[Disassembler.Disassembler.LONGEST_X86_INSTRUCTION];
			for (uint p = 0; p < b.Length; p++)
				b[p] = memory.UnsafeRead8(pc+p);
			return b;
		}

		private void trace(BRANCH_NODE curr, int depth, PC_TRACE pctrace)
		{
			if (curr == null) return;
			if (curr.visited) return;
			if (depth > 200) { logger.LogTrace("[TRACE] too deep!\n"); curr.end = curr.start + 1; return; }

			uint pc = curr.start;

			for (;;)
			{
				ushort ins = peek_fast(pc);

				// returns - if we hit a return, that's the end of the block

				if (ins == 0b0100111001110011 ||//rte
				    ins == 0b0100111001110101 ||//rts
					ins == 0b0100111001110111)  //rtr
				{
					curr.ret = pc;
					curr.end = pc+2;
					return;
				}

				// dead ends - if we hit a TRAP or RESET
				if ((ins & 0b111111111111_0000) == 0b010011100100_0000 || //trap
					ins == 0b0100111001110110 || //trapv
					ins == 0b0100111001110000)   //reset
				{
					curr.end = pc + 2;
					return;
				}

				var dasm = disassembler.Disassemble(pc, peek_longest(pc));//todo: need to add type and target to DAsm
				uint size = (uint)dasm.Bytes.Length;
				uint target = dasm.ea;//target of jump/call/branch, zero if none available
				M_TYPE type = dasm.type;//extended code

				// dead ends - if we hit a jmp where the address is computed
				if (type == M_TYPE.M_JMP && target == 0)
				{
					curr.end = pc + size;
					return;
				}

				pc += size;

				switch (type)
				{
					case M_TYPE.M_BRA:
					case M_TYPE.M_JMP:

						//jmp/bra abs
						curr.from = pc - size;
						curr.to = target;
						curr.end = pc;

						Debug.Assert(target != 0);

						curr.taken = new_node(target, BRANCH_TYPE.BT_JP, pctrace, curr);
						trace(curr.taken, depth + 1, pctrace);
						return;

					case M_TYPE.M_Bcc:
					case M_TYPE.M_DBcc:

						//bcc/dbcc abs
						curr.from = pc - size;
						curr.to = target;
						curr.end = pc;

						Debug.Assert(target != 0);

						curr.taken = new_node(target, BRANCH_TYPE.BT_JP, pctrace, curr);
						trace(curr.taken, depth + 1, pctrace);

						curr.nottaken = new_node(pc, BRANCH_TYPE.BT_FALLTHROUGH, pctrace, curr);
						trace(curr.nottaken, depth + 1, pctrace);
						return;

					case M_TYPE.M_BSR:
					case M_TYPE.M_JSR:
						//bsr/jsr abs

						//don't know where the branch is going, eg jsr -12(A6) , so treat it as just another instruction
						if (target == 0)
							break;
						
						curr.from = pc - size;
						curr.to = target;
						curr.end = pc;
						curr.taken = new_node(target, BRANCH_TYPE.BT_CALL, pctrace, curr);
						trace(curr.taken, depth + 1, pctrace);

						curr.nottaken = new_node(pc, BRANCH_TYPE.BT_FALLTHROUGH, pctrace, curr);
						trace(curr.nottaken, depth + 1, pctrace);
						return;
				}
			}
		}

		private bool IsInROM(uint pc)
		{
			return false;
		}

		private BRANCH_NODE new_node(uint start, BRANCH_TYPE branchtype, PC_TRACE pctrace, BRANCH_NODE from)
		{
			//if we end up at the ROM START routine, stop tracing
			//don't bother tracing into the ROM
			if (IsInROM(start)) return null;

			var node = pctrace.nodes.FirstOrDefault(x => start == x.start);
			if (node != null)
			{
				node.visited = true;
				if (branchtype > node.branchtype)
					node.branchtype = branchtype;
				return node;
			}

			var c = new BRANCH_NODE
			{
				start = start,
				branchtype = branchtype,
				parent = from
			};
			pctrace.nodes.Add(c);
			return c;
		}
	}
}
