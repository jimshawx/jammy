using System;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Types.Types
{

	[Flags]
	public enum ChipsetClockState
	{
		StartOfFrame = 1,
		EndOfFrame = 2,
		EndOfLine = 4,
		StartOfLine = 8
	}
}
