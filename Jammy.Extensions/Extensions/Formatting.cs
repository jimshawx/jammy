using System;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Extensions.Extensions
{
	public static class Formatting
	{
		//public static string ToBin(this byte v) { return ((uint)v).ToBin(8); }
		//public static string ToBin(this ushort v) { return ((uint)v).ToBin(16); }
		//public static string ToBin(this uint v) { return v.ToBin(32); }
		//public static string ToBin(this uint v, int cnt) { return Convert.ToString(v, 2).PadLeft(cnt, '0'); }

		public static string ToBin(this byte v) { return v.ToString("b8"); }
		public static string ToBin(this ushort v) { return v.ToString("b16"); }
		public static string ToBin(this uint v) { return v.ToString("b32"); }
		public static string ToBin(this uint v, int cnt) { return v.ToString($"b{cnt}"); }
		public static string ToBin(this ulong v) { return v.ToString("b64"); }
		public static string ToBin(this ulong v, int cnt) { return v.ToString($"b{cnt}"); }
	}
}