using System;
using System.Text;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types
{
	public class Vectors
	{
		public Tuple<string, uint>[] Items { get; } = new Tuple<string, uint>[256];

		public override string ToString()
		{
			var sb = new StringBuilder();
			for (int i = 0; i < Items.Length; i++)
				sb.AppendLine($"{i*4:X8} {Items[i].Item1,-35} {Items[i].Item2:X8}");
			return sb.ToString();
		}

		public readonly static string [] vectorNames = [

			"Initial SSP",
			"Initial PC",
			"Bus Error",
			"Address Error",
			"Illegal Instruction",
			"Zero Divide",
			"CHK Instruction",
			"TRAPV Instruction",
			"Privilege Violation",
			"Trace",
			"Line 1010 Emulator",
			"Line 1111 Emulator",
			"Reserved",
			"Reserved",
			"Format Error (MC68010)",
			"Unitialized Interrupt Vector",

			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",

			"Spurious Interrupt",

			"Level 1 (TBE, DSKBLK, SOFTINT)",
			"Level 2 (PORTS (CIAA))",
			"Level 3 (COPER, VERTB, BLIT)",
			"Level 4 (AUD0/1/2/3)",
			"Level 5 (RBF, DSKSYNC)",
			"Level 6 (EXTER (CIAB))",
			"Level 7 (NMI)",
			 
			"Trap 0",
			"Trap 1",
			"Trap 2",
			"Trap 3",
			"Trap 4",
			"Trap 5",
			"Trap 6",
			"Trap 7",
			"Trap 8",
			"Trap 9",
			"Trap A",
			"Trap B",
			"Trap C",
			"Trap D",
			"Trap E",
			"Trap F",

			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			"Reserved",
			];
	}
}
