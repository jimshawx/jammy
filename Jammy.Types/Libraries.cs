using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types
{
	public class Libraries
	{
		public List<Tuple<string, uint>> Items {get; } = new List<Tuple<string, uint>>();

		public override string ToString()
		{
			var sb = new StringBuilder();
			foreach (var item in Items.OrderBy(x=>x.Item1))
				sb.AppendLine($"{item.Item1,-25}\t{item.Item2:X8}");
			return sb.ToString();
		}
	}
}
