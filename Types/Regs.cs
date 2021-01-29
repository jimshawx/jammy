using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace RunAmiga.Types
{
	public class Regs
	{
		public uint[] D { get; private set; }
		public uint[] A { get; private set; }
		public uint PC { get; set; }
		public uint SP { get; set; }
		public uint SSP { get; set; }
		//T.S..210...XNZVC
		public ushort SR { get; set;}

		public Regs()
		{
			D = new uint[8];
			A = new uint[8];
		}

		public List<string> Items()
		{
			var items = new List<string>();

			//for (int i = 0; i < 8; i++)
			//	items.Add($"D{i} {D[i]:X8}");
			//for (int i = 0; i < 7; i++)
			//	items.Add($"A{i} {A[i]:X8}");
			//items.Add($"SP {A[7]:X8}");

			for (int i = 0; i < 8; i++)
			{
				if (i == 7)
					items.Add($"D{i} {D[i]:X8}  SP {A[i]:X8}");
				else
					items.Add($"D{i} {D[i]:X8}  A{i} {A[i]:X8}");
			}

			items.Add($"PC {PC:X8} SSP {SSP:X8}");
			items.Add($"X N Z V C    SR {SR:X4}");
			items.Add($"{(SR >> 4) & 1} {(SR >> 3) & 1} {(SR >> 2) & 1} {(SR >> 1) & 1} {SR & 1}");
			return items;
		}

		public string RegString()
		{
			var sb = new StringBuilder();
			
			sb.Append("D ");
			for (int i = 0; i < 8; i++)
				sb.Append($"{D[i]:X8} ");
			
			sb.Append(" A ");
			for (int i = 0; i < 8; i++)
				sb.Append($"{A[i]:X8} ");
			
			sb.Append($" PC {PC:X8}");

			return sb.ToString();
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public class Musashi_regs
	{
		public uint d0, d1, d2, d3, d4, d5, d6, d7;
		public uint a0, a1, a2, a3, a4, a5, a6, a7;
		public uint pc, sp, usp, ssp;
		public ushort sr;
	}

}
