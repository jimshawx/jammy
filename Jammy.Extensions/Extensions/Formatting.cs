using System;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Extensions.Extensions
{
	public static class Formatting
	{
		public static string ToBin(this byte v) { return ((uint)v).ToBin(8); }
		public static string ToBin(this ushort v) { return ((uint)v).ToBin(16); }
		public static string ToBin(this uint v) { return v.ToBin(32); }
		public static string ToBin(this uint v, int cnt) { return Convert.ToString(v, 2).PadLeft(cnt, '0'); }
	}
}