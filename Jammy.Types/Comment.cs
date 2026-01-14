using Jammy.Database.Types;
using System.Collections.Generic;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types
{
	public class Comment : BaseDbObject
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

	public class Header : BaseDbObject
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
		public class LVOParm
		{
			public string Name { get; set; }
			public string Reg { get; set; }	
		}

		public string Name { get; set; }
		public int Offset { get; set; }
		public uint Address { get; set; }
		public int Index { get { return Offset/-6-1;} }
		public List<LVOParm> parms { get; } = new List<LVOParm>();

		public LVO() { }

		public LVO(string name, int offset)
		{
			Name = name;
			Offset = offset;
		}

		public string GetFnSignature()
		{
			return $"{Name}({(string.Join(',', parms.Select(x => $"{x.Name}/{x.Reg}")))})";
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
				LVOs.Add(new LVO("LibOpen", -6));
				LVOs.Add(new LVO("LibClose", -12));
				LVOs.Add(new LVO("LibExpunge", -18));
				LVOs.Add(new LVO("LibReserved", -24));
			}
			else if (type == LVOType.Device)
			{
				LVOs.Add(new LVO("DevOpen", -6));
				LVOs.Add(new LVO("DevClose", -12));
				LVOs.Add(new LVO("DevExpunge", -18));
				LVOs.Add(new LVO("DevReserved", -24));
				LVOs.Add(new LVO("DevBeginIO", -30));
				LVOs.Add(new LVO("DevAbortIO", -36));
			}
		}
	}
}
