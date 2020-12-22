using System;
using System.Text;

namespace RunAmiga.Types
{
	public class Memory
	{
		private byte[] memory = new byte[16 * 1024 * 1024];

		public Memory(byte[] src)
		{
			Array.Copy(src, memory, 16 * 1024 * 1024);
		}

		private void BlockToString(uint start, uint size, StringBuilder sb)
		{
			for (uint i = start; i < start+size; i += 32)
			{
				sb.Append($"{i:X6} ");
				for (int k = 0; k < 4; k++)
				{
					for (int j = 0; j < 8; j++)
					{
						sb.Append($"{memory[i + k * 8 + j]:X2}");
					}
					sb.Append(" ");
				}

				sb.Append("  ");

				for (int k = 0; k < 32; k++)
				{

					byte c = memory[i + k];
					if (c < 31 || c >= 127) c = (byte)'.';
					sb.Append($"{Convert.ToChar(c)}");
				}

				sb.Append("\n");
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			BlockToString(0x000000, 0x4000, sb);
			BlockToString(0xc00000, 0x4000, sb);
			BlockToString(0xfc0000, 0x4000, sb);

			return sb.ToString();
		}
	}
}
