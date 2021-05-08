/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types.Options
{
	public class DisassemblyOptions
	{
		public bool IncludeBytes { get; set; }
		public bool CommentPad { get; set; }
		public bool IncludeBreakpoints { get; set; }
		public bool IncludeComments { get; set; }
		public bool Full32BitAddress { get; set; }
	}
}