using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Jammy.Core.Types
{
	public class Regs
	{
		public uint[] D { get; private set; }
		public uint[] A { get; private set; }
		public uint PC { get; set; }
		public uint SP { get; set; }
		public uint SSP { get; set; }
		//T.S..210...XNZVC
		public ushort SR { get; set; }

		public Regs()
		{
			D = new uint[8];
			A = new uint[8];
		}

		public Regs Clone()
		{
			var regs = new Regs();
			for (int i = 0; i < 8; i++)
			{
				regs.A[i] = this.A[i];
				regs.D[i] = this.D[i];
			}

			regs.PC = this.PC;
			regs.SP = this.SP;
			regs.SSP = this.SSP;
			regs.SR = this.SR;

			return regs;
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

			sb.Append($"SR {SR:X4} PC {PC:X8}");

			return sb.ToString();
		}

		public bool Compare(Regs other)
		{
			return 
				((this.PC != other.PC)
				||(this.D[0] != other.D[0])
				||(this.D[1] != other.D[1])
				||(this.D[2] != other.D[2])
				||(this.D[3] != other.D[3])
				||(this.D[4] != other.D[4])
				||(this.D[5] != other.D[5])
				||(this.D[6] != other.D[6])
				||(this.D[7] != other.D[7])

				||(this.A[0] != other.A[0])
				||(this.A[1] != other.A[1])
				||(this.A[2] != other.A[2])
				||(this.A[3] != other.A[3])
				||(this.A[4] != other.A[4])
				||(this.A[5] != other.A[5])
				||(this.A[6] != other.A[6])
				//||(this.A[7] != other.A[7])
				//||(this.SSP != other.SSP)
				||(this.SP != other.SP)
				||(this.SR != other.SR)
				);
		}

		public List<string> CompareSummary(Regs other)
		{
			var rv = new List<string>();

			if (this.PC != other.PC) { rv.Add($"PC Drift at {this.PC:X8} {other.PC:X8}");  }
			if (this.D[0] != other.D[0]) { rv.Add($"reg D0 differs {this.D[0]:X8} {other.D[0]:X8}");  }
			if (this.D[1] != other.D[1]) { rv.Add($"reg D1 differs {this.D[1]:X8} {other.D[1]:X8}");  }
			if (this.D[2] != other.D[2]) { rv.Add($"reg D2 differs {this.D[2]:X8} {other.D[2]:X8}");  }
			if (this.D[3] != other.D[3]) { rv.Add($"reg D3 differs {this.D[3]:X8} {other.D[3]:X8}");  }
			if (this.D[4] != other.D[4]) { rv.Add($"reg D4 differs {this.D[4]:X8} {other.D[4]:X8}");  }
			if (this.D[5] != other.D[5]) { rv.Add($"reg D5 differs {this.D[5]:X8} {other.D[5]:X8}");  }
			if (this.D[6] != other.D[6]) { rv.Add($"reg D6 differs {this.D[6]:X8} {other.D[6]:X8}");  }
			if (this.D[7] != other.D[7]) { rv.Add($"reg D7 differs {this.D[7]:X8} {other.D[7]:X8}");  }

			if (this.A[0] != other.A[0]) { rv.Add($"reg A0 differs {this.A[0]:X8} {other.A[0]:X8}");  }
			if (this.A[1] != other.A[1]) { rv.Add($"reg A1 differs {this.A[1]:X8} {other.A[1]:X8}");  }
			if (this.A[2] != other.A[2]) { rv.Add($"reg A2 differs {this.A[2]:X8} {other.A[2]:X8}");  }
			if (this.A[3] != other.A[3]) { rv.Add($"reg A3 differs {this.A[3]:X8} {other.A[3]:X8}");  }
			if (this.A[4] != other.A[4]) { rv.Add($"reg A4 differs {this.A[4]:X8} {other.A[4]:X8}");  }
			if (this.A[5] != other.A[5]) { rv.Add($"reg A5 differs {this.A[5]:X8} {other.A[5]:X8}");  }
			if (this.A[6] != other.A[6]) { rv.Add($"reg A6 differs {this.A[6]:X8} {other.A[6]:X8}");  }
			//if (this.A[7] != other.A[7]) { rv.Add($"reg A7 differs {this.A[7]:X8} {other.A[7]:X8}");  }
			//if (this.SSP != other.SSP) { rv.Add($"reg SSP differs {this.SSP:X8} {other.SSP:X8}");  }
			if (this.SP != other.SP) { rv.Add($"reg SP differs {this.SP:X8} {other.SP:X8}");  }
			if (this.SR != other.SR) { rv.Add($"reg SR differs {this.SR:X8} {other.SR:X8}");  }

			return rv;
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
