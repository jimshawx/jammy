using Jammy.Database.Types;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types
{
	public class Label : BaseDbObject
	{
		public Label( uint address, string name)
		{
			Name = name;
			Address = address;
		}

		public Label()
		{
		}

		public string Name { get; set; }
		public uint Address { get; set; }
	}
}
