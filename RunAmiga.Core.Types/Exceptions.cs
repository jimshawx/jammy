using System;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Core.Types
{
	public class MC68000Exception : ApplicationException { }

	public class UnknownInstructionException : MC68000Exception
	{
		private uint pc;
		private ushort instruction;

		public UnknownInstructionException(uint pc, int instruction)
		{
			this.pc = pc;
			this.instruction = (ushort)instruction;
		}

		public override string ToString()
		{
			return $"Unknown Instruction @{pc:X6} {instruction:X4}. {base.ToString()}";
		}
	}

	public class UnknownEffectiveAddressException : MC68000Exception
	{
		private uint pc;
		private ushort instruction;

		public UnknownEffectiveAddressException(uint pc, int instruction)
		{
			this.pc = pc;
			this.instruction = (ushort)instruction;
		}

		public override string ToString()
		{
			return $"Unknown Effective Address @{pc:X6} {instruction:X4}. {base.ToString()}";
		}
	}

	public class UnknownInstructionSizeException : MC68000Exception
	{
		private uint pc;
		private ushort instruction;

		public UnknownInstructionSizeException(uint pc, int instruction)
		{
			this.pc = pc;
			this.instruction = (ushort)instruction;
		}

		public override string ToString()
		{
			return $"Unknown Instruction Size @{pc:X6} {instruction:X4}. {base.ToString()}";
		}
	}

	public class InstructionAlignmentException : MC68000Exception
	{
		private uint pc;
		private ushort instruction;
		private uint address;

		public InstructionAlignmentException(uint pc, uint address, int instruction)
		{
			this.pc = pc;
			this.instruction = (ushort)instruction;
			this.address = address;
		}

		public override string ToString()
		{
			return $"Unknown Instruction Alignment @{pc:X6} {instruction:X4} {address:X8}. {base.ToString()}";
		}
	}

	public class MemoryAlignmentException : MC68000Exception
	{
		private uint address;

		public MemoryAlignmentException(uint address)
		{
			this.address = address;
		}

		public override string ToString()
		{
			return $"Unknown Memory Alignment @{address:X6}. {base.ToString()}";
		}
	}

	public class InvalidCustomRegisterSizeException : MC68000Exception
	{
		private uint pc;
		private uint reg;
		private Size size;

		public InvalidCustomRegisterSizeException(uint pc, uint reg, Size size)
		{
			this.pc = pc;
			this.reg = reg;
			this.size = size;
		}

		public override string ToString()
		{
			return $"Invalid Custom Register Size @{pc:X6} {reg:X6} {size}. {base.ToString()}";
		}
	}
}
