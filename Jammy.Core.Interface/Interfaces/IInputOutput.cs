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
	}

	public interface IInputOutput
	{
		public InputOutput GetInputOutput();
	}
}
