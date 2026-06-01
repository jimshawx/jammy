/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Types
{
	public class MemTypeSearch : DbSearch
	{
		public AddressRange AddressRange { get; } = new AddressRange();
	}
}
