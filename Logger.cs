using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms.VisualStyles;
using RunAmiga.Types;

namespace RunAmiga
{
	public static class Logger
	{
		private static StringBuilder sb = new StringBuilder();

		public static void Write(string s)
		{
			Debug.Write(s);
			//sb.Append(s);
			//Dump();
		}

		public static void WriteLine(string s)
		{
			Debug.WriteLine(s);
			//sb.AppendLine(s);
			//Dump();
		}

		//private static void Dump()
		//{
		//	if (sb.Length > 1000)
		//	{
		//		Trace.Write(sb.ToString());
		//		sb = new StringBuilder();
		//	}
		//}
	}
}