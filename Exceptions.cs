using System;

namespace runamiga
{
	public class MC68000Exception : ApplicationException { }

	public class UnknownInstructionException : MC68000Exception
	{
		private int instruction;

		public UnknownInstructionException(int instruction)
		{
			this.instruction = instruction;
		}
	}

	public class UnknownEffectiveAddressException : MC68000Exception
	{
		private int instruction;

		public UnknownEffectiveAddressException(int instruction)
		{
			this.instruction = instruction;
		}
	}

	public class UnknownInstructionSizeException : MC68000Exception
	{
		private int instruction;

		public UnknownInstructionSizeException(int instruction)
		{
			this.instruction = instruction;
		}
	}

	public class InstructionAlignmentException : MC68000Exception
	{
		private int instruction;

		public InstructionAlignmentException(int instruction)
		{
			this.instruction = instruction;
		}
	}
}
