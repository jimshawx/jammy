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

		public Comment() { }
		public Comment(uint address, string text)
		{
			Address = address;
			Text = text;
		}
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
		
		public LVO() { }

		public LVO(string name, int offset)
		{
			Name = name;
			Offset = offset;
		}
	}

	public enum LVOType
	{
		Library,
		Resource,
		Device,
		Empty
	}

	public class LVOCollection
	{
		public string Name { get; set; }
		public uint BaseAddress { get; set; }
		public List<LVO> LVOs { get; } = new List<LVO>();

		public LVOCollection(LVOType type)
		{
			if (type == LVOType.Library)
			{
				LVOs.Add(new LVO("_LVOLibOpen", -6));
				LVOs.Add(new LVO("_LVOLibClose", -12));
				LVOs.Add(new LVO("_LVOLibExpunge", -18));
				LVOs.Add(new LVO("_LVOLibReserved", -24));
			}
			else if (type == LVOType.Device)
			{
				LVOs.Add(new LVO("_LVODevOpen", -6));
				LVOs.Add(new LVO("_LVODevClose", -12));
				LVOs.Add(new LVO("_LVODevExpunge", -18));
				LVOs.Add(new LVO("_LVODevReserved", -24));
				LVOs.Add(new LVO("_LVODevBeginIO", -30));
				LVOs.Add(new LVO("_LVODevAbortIO", -36));
			}
		}
	}
}
