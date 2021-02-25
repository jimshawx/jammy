using System;
using System.Collections.Generic;
using System.Linq;

namespace RunAmiga.Extensions.Extensions
{
	public static class StringExtensions
	{
		public static string[] SplitSmart(this string s, char sep, StringSplitOptions sso)
		{
			return s.SplitSmart(new[] { sep }, sso);
		}

		//same as string.Split() except not allowing splits inside quotes
		public static string[] SplitSmart(this string s, char[] sep, StringSplitOptions sso)
		{
			bool instr = false;
			var bits = new List<string>();

			int start = 0;
			for (int end = 0; end < s.Length; end++)
			{
				if (s[end] == '"' && !instr) instr = true;
				else if (s[end] == '"' && instr) instr = false;
				else if (sep.Contains(s[end]) && !instr)
				{
					if (sso != StringSplitOptions.RemoveEmptyEntries || end - start != 0)
						bits.Add(s.Substring(start, end - start));
					start = end + 1;
				}
			}
			if (sso != StringSplitOptions.RemoveEmptyEntries || s.Length - start != 0)
				bits.Add(s.Substring(start, s.Length - start));
			return bits.ToArray();
		}
	}
}
