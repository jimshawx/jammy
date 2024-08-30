using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Size = System.Drawing.Size;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main
{
	public class StringScan
	{
		private readonly ILogger logger;
		private readonly IMemoryMapper memory;
		private Form emulation;

		public StringScan(ILogger<StringScan> logger, IMemoryMapper memory)
		{
			this.logger = logger;
			this.memory = memory;

			var ram = ((IDebugMemoryMapper)memory).GetBulkRanges();
			string s = GetStrings(ram, 4);

			var ss = new SemaphoreSlim(1);
			ss.Wait();
			var t = new Thread(() =>
			{
				emulation = new Form {Name = "Strings", Text = "Strings", ControlBox = false, FormBorderStyle = FormBorderStyle.FixedSingle, MinimizeBox = true, MaximizeBox = true};

				if (emulation.Handle == IntPtr.Zero)
					throw new ApplicationException();

				ss.Release();

				SetStrings(s);

				emulation.Show();

				Application.Run(emulation);
			});
			t.SetApartmentState(ApartmentState.STA);
			t.Start();
			ss.Wait();
		}

		private bool IsString(byte b)
		{
			char c = (char)b;
			//return char.IsLetterOrDigit(c) || c == ' ';
			return c>= ' ' && c < 128;
		}

		private string GetStrings(List<BulkMemoryRange> ram, int minW)
		{
			long startI;
			var sb = new StringBuilder();

			foreach (var r in ram)
			{
				startI = -1;

				for (uint i = 0; i < r.Length; i++)
				{
					bool isPrint = IsString(r.Memory[i]);
					if (isPrint && startI == -1)
					{
						startI = i;
					}
					else if (!isPrint && startI != -1)
					{
						long len = i-startI;
						if (len >= minW)
							sb.AppendLine(Encoding.ASCII.GetString(r.Memory.AsSpan((int)startI, (int)len)));
						startI = -1;
					}
				}
			}
			return sb.ToString();
		}

		private RichTextBox textBox;
		private VScrollBar slider;

		public void SetStrings(string s)
		{
			if (emulation.IsDisposed) return;

			emulation.Invoke((Action)delegate
			{
				textBox = new RichTextBox();
				textBox.Multiline = true;
				textBox.Text = s;
				emulation.Controls.Add(textBox);

				emulation.ClientSize = new Size(800, 600);
				textBox.Size = emulation.ClientSize;
				emulation.Show();
			});
		}
	}
}