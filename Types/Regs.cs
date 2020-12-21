using System.Collections.Generic;

namespace runamiga.Types
{
	public class Regs
	{
		public uint[] D { get; private set; }
		public uint[] A { get; private set; }
		public uint PC { get; set; }
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


			items.Add($"PC {PC:X8}");
			items.Add($"SR {PC:X4}");
			items.Add("X N Z V C");
			items.Add($"{(SR >> 4) & 1} {(SR >> 3) & 1} {(SR >> 2) & 1} {(SR >> 1) & 1} {SR & 1}");
			return items;
		}
	}
}
