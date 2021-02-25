using System.Collections.Generic;
using System.Linq;

namespace RunAmiga.Extensions.Extensions
{
	public static class Enumerables
	{
		public static IEnumerable<uint> AsULong(this byte[] src)
		{
			for (int i = 0; i < src.Length; i+=4)
			{
				uint b = ((uint)src[i] << 24)
				         | ((uint)src[i+1] << 16)
				         | ((uint)src[i+2] << 8)
				         | (uint)src[i+3];
				yield return b;
			}
		}

		public static IEnumerable<ushort> AsUWord(this byte[] src)
		{
			for (int i = 0; i < src.Length; i+=2)
			{
				ushort b = (ushort)( ((ushort)src[i] << 8)
				                    | (ushort)src[i+1]);
				yield return b;
			}
		}

		public static IEnumerable<byte> AsByte(this uint[] src)
		{
			for (int i = 0; i < src.Length; i++)
			{
				yield return (byte)src[i];
				yield return (byte)(src[i]>>8);
				yield return (byte)(src[i]>>16);
				yield return (byte)(src[i]>>24);
			}
		}

		public static IEnumerable<byte> AsByte(this uint v)
		{
			yield return (byte)(v >> 24);
			yield return (byte)(v >> 16);
			yield return (byte)(v >> 8);
			yield return (byte)v;
		}

		//all the odd bits, followed by all the even bits
		public static IEnumerable<byte> OddEven(this IEnumerable<byte> src)
		{
			byte[] copy = src.ToArray();

			foreach (var s in copy)
				yield return (byte)((s >> 1) & 0x55);
			foreach (var s in copy)
				yield return (byte)(s & 0x55);
		}
	}
}
