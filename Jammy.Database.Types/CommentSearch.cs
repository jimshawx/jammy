/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Types
{
	public class CommentSearch : DbSearch
	{
		public string Text { get; set; }
		public AddressRange AddressRange { get; } = new AddressRange();
	}
}
