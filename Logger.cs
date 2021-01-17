using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RunAmiga
{
	public static class Logger
	{
		private static StringBuilder sb = new StringBuilder();

		private static object locker = new object();

		private static Form form;
		private static RichTextBox text;

		static Logger()
		{
			form = new Form {ClientSize = new Size(640, 480)};
			text = new RichTextBox{Multiline = true, Size = new Size(640,480), Font = new Font(FontFamily.GenericMonospace, 8.0f), Anchor = AnchorStyles.Left| AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right, ScrollBars = RichTextBoxScrollBars.ForcedBoth};
			form.Controls.Add(text);
			form.Show();
			Thread t = new Thread(Dump);
			t.Start();
		}

		private static void Dump()
		{
			string s = null;
			
			for (; ; )
			{
				Thread.Sleep(1000);

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
					//Trace.Write(s);
					string t = s;
					text.Invoke((Action)delegate () { text.AppendText(t); });
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