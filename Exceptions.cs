using System;

namespace runamiga
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

		public InstructionAlignmentException(uint pc, int instruction)
		{
			this.pc = pc;
			this.instruction = (ushort)instruction;
		}

		public override string ToString()
		{
			return $"Unknown Instruction Alignment @{pc:X6} {instruction:X4}. {base.ToString()}";
		}
	}
}
