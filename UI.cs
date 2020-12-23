using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RunAmiga
{
	public sealed class UI
	{
		private static UI instance = null;
		private static readonly object padlock = new object();

		private static SemaphoreSlim uiSemaphore = new SemaphoreSlim(1);

		private static void Lock()
		{
			uiSemaphore.Wait();
		}

		private static void Unlock()
		{
			uiSemaphore.Release();
		}

		private static bool powerLight;
		public static bool PowerLight
		{
			set
			{
				powerLight = value;
			}
			get
			{
				return powerLight;
			}
		}

		private static uint [] colours = new uint[256];
		public static void SetColour(int index, ushort value)
		{
			Lock();
			uint colour = value;
			colours[index] = ((colour&0xf)*0x11)+((colour&0xf0)*0x110)+((colour&0xf00)*0x1100);
			Unlock();
		}

		public static void GetColours(uint[] dst)
		{
			Lock();
			Array.Copy(colours, dst, colours.Length);
			Unlock();
		}
	}
}

