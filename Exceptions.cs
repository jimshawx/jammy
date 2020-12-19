using System;

namespace runamiga
{
	public class UnknownInstructionException : ApplicationException
	{
		private int instruction;

		public UnknownInstructionException(int instruction)
		{
			this.instruction = instruction;
		}
	}

	public class UnknownEffectiveAddressException : ApplicationException
	{
		private int instruction;

		public UnknownEffectiveAddressException(int instruction)
		{
			this.instruction = instruction;
		}
	}

	public class UnknownInstructionSizeException : ApplicationException
	{
		private int instruction;

		public UnknownInstructionSizeException(int instruction)
		{
			this.instruction = instruction;
		}
	}

	public class InstructionAlignmentException : ApplicationException
	{
		private int instruction;

		public InstructionAlignmentException(int instruction)
		{
			this.instruction = instruction;
		}
	}

}
