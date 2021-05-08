using System.Collections.Generic;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types
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

	public class LVO
	{
		public string Name { get; set; }
		public int Offset { get; set; }
		public uint Address { get; set; }
		public int Index { get { return Offset/-6-1;} }
	}

	public class LVOCollection
	{
		public string Name { get; set; }
		public uint BaseAddress { get; set; }
		public List<LVO> LVOs { get; } = new List<LVO>();
	}
}
