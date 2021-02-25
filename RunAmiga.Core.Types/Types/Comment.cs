using System.Collections.Generic;

namespace RunAmiga.Core.Types.Types
{
	public class Comment
	{
		public uint Address { get; set; }
		public string Text { get; set; }
	}

	public class Header
	{
		public Header()
		{
			TextLines = new List<string>();
		}

		public uint Address { get; set; }
		public List<string> TextLines { get; }
	}
}