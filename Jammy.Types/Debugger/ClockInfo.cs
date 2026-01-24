using Jammy.Core.Types.Types;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types.Debugger
{
	public class ClockInfo
	{
		public uint HorizontalPos;
		public uint VerticalPos;
		public uint Tick;
		public ChipsetClockState State;

		public override string ToString()
		{
			return $"v:{VerticalPos} h:{HorizontalPos} t:{Tick} {State}";
		}
	}
}
