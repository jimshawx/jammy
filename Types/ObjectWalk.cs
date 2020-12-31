using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RunAmiga.Types
{
	public class ObjectWalk
	{
		private void DumpObj(object obj, StringBuilder sb, int depth)
		{
			if (depth > 5) return;
			if (obj == null)
			{
				sb.Remove(sb.Length-1, 1);
				sb.Append("\t\t\t\t\t(null)");
				return;
			}

			foreach (var p in obj.GetType().GetProperties().OrderBy(x=>x.MetadataToken))
			{
				for (int j = 0; j < depth; j++)
					sb.Append("\t");

				sb.Append($"{p.Name} ");

				if (p.PropertyType == typeof(string))
				{
					sb.Remove(sb.Length - 1, 1);
					sb.Append($"{p.GetValue(obj)}");
				}
				else if (p.PropertyType.BaseType == typeof(Array))
				{
					sb.Append("\n");
					var array = (Array)p.GetValue(obj);
					for (int i = 0; i < array.Length; i++)
					{
						for (int j = 0; j < depth; j++)
							sb.Append("\t");
						sb.Append($"[{i}]\n");
						DumpObj(array.GetValue(i), sb, depth + 1);
					}
					sb.Remove(sb.Length - 1, 1);
				}
				else if (p.PropertyType.BaseType == typeof(object))
				{
					sb.Append("\n");
					DumpObj(p.GetValue(obj), sb, depth + 1);
					//sb.Remove(sb.Length - 1, 1);
				}
				else
				{
					sb.Append($"\t\t\t\t\t{p.GetValue(obj):X8} {p.GetValue(obj)}");
				}
				sb.Append("\n");
			}
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			//try { 
			DumpObj(this, sb, 0);
			//}
			//catch { /* yum */ };
			return sb.ToString();
		}
	}
}
