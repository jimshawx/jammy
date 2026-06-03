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
				emulation = new Form {Name = "Strings", Text = "Strings", ControlBox = true, FormBorderStyle = FormBorderStyle.SizableToolWindow, MinimizeBox = true, MaximizeBox = true};

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

		private static readonly int[] charScore =
		[
			2,//space
			-1,//!
			-1,//"
			-1,//#
			-1,//$
			-3,//%
			-3,//&
			-1,//'
			-2,//(
			-2,//)
			-3,//*
			-3,//+
			-2,//,
			-3,//-
			-1,//.
			-1,//forward slash
			1,1,1,1,1,1,1,1,1,1,//0-9
			-1,//:
			-3,//;
			-3,//<
			-3,//=
			-3,//>
			-1,//?
			-2,//@
			2,2,2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2,2,2, 2,2,2,2,2,2,//A-Z
			-3,//[
			-2,//backslash
			-3,//]
			-3,//^
			-3,//_
			-3,//`
			2,2,2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2,2,2, 2,2,2,2,2,2,//a-z
			-3,//{
			-3,//|
			-3,//},
			-3,//~
		];

		private bool Filter(string s)
		{
			//return true if the string is to be filtered out

			//Nu is a really common misinterpretation of a CPU instruction
			//if it's prefixed by anything other than ' ', it's unlikely to be a string
			int nu = s.IndexOf("Nu");
			if (nu > 0 && s[nu-1] != ' ') return true;
			//if it's at the start but not followed by a letter, it's unlikely to be a string
			if (nu == 0 && s.Length > 2 && !char.IsAsciiLetter(s[nu+2])) return true;

			//if it's whitespace, it's not a string
			if (string.IsNullOrWhiteSpace(s)) return true;

			//not filtered
			return false;
		}

		private int CharScore(byte b)
		{
			int c = b;
			c -= 32;
			if (c < 0 || c >= charScore.Length) return 0;
			return charScore[c];
		}

		private string GetStrings(List<BulkMemoryRange> ram, int minW)
		{
			long startI;
			var sb = new StringBuilder();
			int currentScore = 0;

			foreach (var r in ram)
			{
				startI = -1;

				for (uint i = 0; i <= r.Length; i++)
				{
					//force a terminating null at the end of the buffer
					int score = i == r.Length ? 0: CharScore(r.Memory[i]);
					if (score != 0 && startI == -1)
					{
						startI = i;
						currentScore = score;
					}
					else if (score == 0 && startI != -1)
					{
						long len = i-startI;
						if (len >= minW && currentScore >= 0)
						{
							string s = Encoding.ASCII.GetString(r.Memory.AsSpan((int)startI, (int)len));
							if (!Filter(s))
								sb.AppendLine(s);
						}
						startI = -1;
					}
					else
					{
						currentScore += score;
					}
				}
			}
			return sb.ToString();
		}

		private RichTextBox textBox;

		public void SetStrings(string s)
		{
			if (emulation.IsDisposed) return;

			emulation.Invoke((Action)delegate
			{
				textBox = new RichTextBox();
				textBox.Multiline = true;
				textBox.Text = s;
				textBox.Dock = DockStyle.Fill;
				emulation.Controls.Add(textBox);

				emulation.ClientSize = new Size(800, 600);
				textBox.Size = emulation.ClientSize;
				emulation.Show();
			});
		}
	}
}