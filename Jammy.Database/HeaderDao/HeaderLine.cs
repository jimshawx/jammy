using Jammy.Database.Types;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.HeaderDao
{
	internal class HeaderLine : BaseObject
	{
		public Guid HeaderId { get; set; }
		public uint Line { get; set; }
		public string Text { get; set; }	
	}
}
