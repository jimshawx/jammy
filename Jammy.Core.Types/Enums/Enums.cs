using System;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Types.Enums
{
	[Flags]
	public enum CR
	{
		START = 1,
		PBON = 2,
		OUTMODE = 4,
		RUNMODE = 8,
		LOAD = 16,
		INMODE0 = 32,
		CRB_INMODE1 = 64,
		CRA_SPMODE = 64,
		CRB_ALARM = 128
	}

	[Flags]
	public enum ICRB
	{
		TIMERA = 1,
		TIMERB = 2,
		TODALARM = 4,
		SERIAL = 8,
		FLAG = 16,

		IR = 128,
	}
}