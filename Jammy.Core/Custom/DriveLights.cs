using Jammy.Core.Interface.Interfaces;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public class DriveLights : IDriveLights
	{
		public bool PowerLight { get; set; }
		public bool DiskLight { get; set; }
	}
}
