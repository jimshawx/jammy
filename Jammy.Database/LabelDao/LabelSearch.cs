using Jammy.Database.Types;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.LabelDao
{
	public class LabelSearch : DbSearch
	{
		public string Name { get; set; }
		public AddressRange AddressRange { get; } = new AddressRange();
	}
}
