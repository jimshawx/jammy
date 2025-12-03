using System;
using System.Collections.Generic;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Extensions.Extensions
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

		public static IEnumerable<ushort> AsUWord(this Memory<byte> src)
		{
			for (int i = 0; i < src.Length; i += 2)
			{
				var span = src.Span;
				ushort b = (ushort)(((ushort)span[i] << 8)
									| (ushort)span[i + 1]);
				yield return b;
			}
		}

		public static ushort FirstUWord(this Memory<byte> src)
		{
			var span = src.Span;
			ushort w = (ushort)(((ushort)span[0] << 8)
								| (ushort)span[1]);
			return w;
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
			var copy = src;//.ToArray();

			foreach (var s in copy)
				yield return (byte)((s >> 1) & 0x55);
			foreach (var s in copy)
				yield return (byte)(s & 0x55);
		}

		//all the odd bits, followed by all the even bits
		public static IEnumerable<byte> OddEven(this Span<byte> src)
		{
			//return src.ToArray().OddEven();
			//hack - this is required to fix a problem with .net 10.0 where it's recursively calling
			//this function instead of the IEnumerable<byte> version
			IEnumerable<byte> rv = src.ToArray();
			return rv.OddEven();
		}

		public static string DiffSummary(this byte[] m0, byte[] m1)
		{
			var diffs = new List<Tuple<int, uint, uint>>();
			foreach (var p in m0.AsULong().Zip(m1.AsULong().Zip(Enumerable.Range(0, int.MaxValue))))
			{
				if (p.First != p.Second.First)
					diffs.Add(new Tuple<int, uint, uint>(p.Second.Second, p.First, p.Second.First));
			}
			return string.Join(Environment.NewLine, diffs.Select(x => $"{x.Item1*4:X6} {x.Item2:X8} {x.Item3:X8}"));
		}

		public static IEnumerable<byte> AsByte(this ushort[] src)
		{
			foreach (var s in src)
			{
				yield return (byte)(s >> 8);
				yield return (byte)s;
			}
		}
	}
}
