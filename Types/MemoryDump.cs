using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace RunAmiga.Types
{
	public class MemoryDump
	{
		private byte[] memory = new byte[16 * 1024 * 1024];

		public MemoryDump(byte[] src)
		{
			Array.Copy(src, memory, 16 * 1024 * 1024);
		}

		private Dictionary<uint, int> addressToLine = new Dictionary<uint, int>();
		private string BlockToString(List<Tuple<uint, uint>> ranges)
		{
			var sb = new StringBuilder();

			int line = 0;
			foreach (var range in ranges)
			{
				uint start = range.Item1;
				uint size = range.Item2;

				for (uint i = start; i < start + size; i += 32)
				{
					addressToLine.Add(i, line++);
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
			return sb.ToString();
		}

		public int AddressToLine(uint address)
		{
			for (uint i = address; i >= address - 32; i--)
			{
				if (addressToLine.TryGetValue(i, out int v))
					return v;
			}
			return 0;
		}

		public byte Read8(uint address)
		{
			if (address >= 0x1000000) { Logger.WriteLine($"Memory Read Byte from {address:X8}"); return 0; }
			return memory[address];
		}

		public ushort Read16(uint address)
		{
			if (address >= 0xfffffe) { Logger.WriteLine($"Memory Read Word from ${address:X8}"); return 0; }
			if ((address & 1) != 0) { Logger.WriteLine($"Memory Read Unaligned Word from ${address:X8}"); return 0; }
			return (ushort)(((ushort)memory[address] << 8) +
							(ushort)memory[(address + 1) ]);
		}

		public uint Read32(uint address)
		{
			if (address >= 0xfffffc) { Logger.WriteLine($"Memory Read Int from ${address:X8}"); return 0; }
			if ((address & 1) != 0) { Logger.WriteLine($"Memory Read Unaligned Int from ${address:X8}"); return 0; }
			return ((uint)memory[address] << 24) +
					((uint)memory[(address + 1) ] << 16) +
					((uint)memory[(address + 2)] << 8) +
					(uint)memory[(address + 3) ];
		}

		public override string ToString()
		{
			return BlockToString(new List<Tuple<uint, uint>>
				{
					new Tuple<uint, uint> ( 0x000000, 0xc000),
					new Tuple<uint, uint> ( 0xc00000, 0x4000),
					new Tuple<uint, uint> ( 0xf80000, 0x40000),
					new Tuple<uint, uint> ( 0xfc0000, 0x40000)
				});
		}
	}
}
