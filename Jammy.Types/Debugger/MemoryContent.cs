using Jammy.Core.Types.Types;
using System.Collections.Generic;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types.Debugger
{
	public class MemoryContent
	{
		public List<BulkMemoryRange> Contents { get; } = new List<BulkMemoryRange>();
	}
}
