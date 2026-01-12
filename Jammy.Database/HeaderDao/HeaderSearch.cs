using Jammy.Database.Types;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.HeaderDao
{
	public class HeaderSearch : DbSearch
	{
		public string Text { get; set; }
		public AddressRange AddressRange { get; } = new AddressRange();
	}
}
