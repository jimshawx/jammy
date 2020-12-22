using System.Linq;
using System.Text;

namespace RunAmiga.Types
{
	public class DAsm
	{
		public string Asm { get; set; }
		public uint Address { get; set; }

		public byte[] Bytes { get;set;}

		public override string ToString()
		{
			var s = new StringBuilder();
			s.Append($"{Address:X8} ");

			for (int i = 0; i < Bytes.Length; i++)
				s.Append($"{Bytes[i]:X2} ");
			for (int i = Bytes.Length; i < 8; i++)
				s.Append("   ");

			string[] sp = Asm.Split(' ');
			s.Append($"{sp[0], -8} {string.Join(" ", sp.Skip(1))}");

			return s.ToString();
		}
	}
}
