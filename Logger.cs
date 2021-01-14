using System.Diagnostics;
using System.Text;
using System.Threading;

namespace RunAmiga
{
	public static class Logger
	{
		private static StringBuilder sb = new StringBuilder();

		private static object locker = new object();

		static Logger()
		{
			Thread t = new Thread(Dump);
			t.Start();
		}

		private static void Dump()
		{
			string s = null;
			
			for (; ; )
			{
				Thread.Sleep(2000);

				lock (locker)
				{
					if (sb.Length > 0)
					{
						s = sb.ToString();
						sb.Clear();
					}
				}

				if (s != null)
				{
					Trace.Write(s);
					s = null;
				}
			}
		}

		public static void Write(string s)
		{
			lock (locker)
			{
				sb.Append(s);
			}
		}

		public static void WriteLine(string s)
		{
			lock (locker)
			{
				sb.AppendLine(s);
			}
		}

	}
}