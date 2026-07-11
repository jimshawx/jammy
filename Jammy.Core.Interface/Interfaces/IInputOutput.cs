/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

using System;

namespace Jammy.Core.Interface.Interfaces
{
	public class InputOutput
	{
		[Flags]
		public enum MouseButton
		{
			MouseLeft = 1<<0,
			MouseMiddle = 1<<1,
			MouseRight = 1<<2
		}

		public MouseButton MouseButtons { get; set; }
		public int MouseX { get; set; }
		public int MouseY { get; set; }
		public int MouseDX { get; set; }
		public int MouseDY { get; set; }

		public bool[] Keyboard = new bool[256];

		public void Reset()
		{
			Array.Clear(Keyboard);
			MouseDX = 0;
			MouseDY = 0;
			MouseButtons = 0;
		}
	}

	public interface IInputOutput
	{
		public InputOutput GetInputOutput();
	}
}
