using System;

namespace runamiga
{
	public class MC68000Exception : ApplicationException { }

	public class UnknownInstructionException : MC68000Exception
	{
		private ushort instruction;

		public UnknownInstructionException(int instruction)
		{
			this.instruction = (ushort)instruction;
		}

		public override string ToString()
		{
			return $"Unknown Instruction {instruction:X4}. {base.ToString()}";
		}
	}

	public class UnknownEffectiveAddressException : MC68000Exception
	{
		private ushort instruction;

		public UnknownEffectiveAddressException(int instruction)
		{
			this.instruction = (ushort)instruction;
		}

		public override string ToString()
		{
			return $"Unknown Effective Address {instruction:X4}. {base.ToString()}";
		}
	}

	public class UnknownInstructionSizeException : MC68000Exception
	{
		private ushort instruction;

		public UnknownInstructionSizeException(int instruction)
		{
			this.instruction = (ushort)instruction;
		}

		public override string ToString()
		{
			return $"Unknown Instruction Size {instruction:X4}. {base.ToString()}";
		}
	}

	public class InstructionAlignmentException : MC68000Exception
	{
		private ushort instruction;

		public InstructionAlignmentException(int instruction)
		{
			this.instruction = (ushort)instruction;
		}

		public override string ToString()
		{
			return $"Unknown Instruction Alignment {instruction:X4}. {base.ToString()}";
		}
	}
}
