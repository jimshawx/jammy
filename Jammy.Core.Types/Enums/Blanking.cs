using System;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Types.Enums
{
	[Flags]
	public enum Blanking
	{
		None = 0,
		VerticalBlank=1,
		HorizontalBlank=2,
		OutsideDisplayWindow=4
	}
}
