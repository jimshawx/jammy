using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

		private static bool exiting = false;
		static Logger()
		{
			//form = new Form {ClientSize = new Size(480, 480)};
			//text = new RichTextBox{
			//						Multiline = true,
			//						Size = new Size(480,480),
			//						Font = new Font(FontFamily.GenericMonospace, 8.0f),
			//						Anchor = AnchorStyles.Left| AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
			//						ScrollBars = RichTextBoxScrollBars.ForcedBoth,
			//						ReadOnly = true,
			//						};
			//form.Controls.Add(text);
			//form.Show();
			//form.Closing += (sender, args) =>
			//{
			//	exiting = true;
			//	while (exiting)
			//	{
			//		Application.DoEvents();
			//		Thread.Sleep(500);
			//	}
			//};

			//Thread t = new Thread(Dump);
			//t.Start();
		}

		private static void Dump()
		{
			string s = null;
			
			while (!exiting)
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
					Trace.Write(s);
					//string t = s;
					//text.Invoke((Action) delegate()
					//{
					//	text.AppendText(t);
					//});
					s = null;
				}
			}

			Trace.WriteLine(form.Text);

			exiting = false;
		}

		public static void Write(string s)
		{
			//lock (locker)
			//{
			//	sb.Append(s);
			//}
			Trace.Write(s);
		}

		public static void WriteLine(string s)
		{
			//lock (locker)
			//{
			//	sb.AppendLine(s);
			//}
			Trace.WriteLine(s);
		}

	}
}