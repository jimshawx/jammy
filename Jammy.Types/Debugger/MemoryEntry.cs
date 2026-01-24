using System;
using System.Collections.Generic;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types.Debugger
{
	[Flags]
	public enum MEMF
	{
		MEMF_ANY = (0),    /* Any type of memory will do */
		MEMF_PUBLIC = (1 << 0),
		MEMF_CHIP = (1 << 1),
		MEMF_FAST = (1 << 2),
		MEMF_LOCAL = (1 << 8), /* Memory that does not go away at RESET */
		MEMF_24BITDMA = (1 << 9),  /* DMAable memory within 24 bits of address */
		MEMF_KICK = (1 << 10), /* Memory that can be used for KickTags */

		MEMF_CLEAR = (1 << 16),    /* AllocMem: NULL out area before return */
		MEMF_LARGEST = (1 << 17),  /* AvailMem: return the largest chunk size */
		MEMF_REVERSE = (1 << 18),  /* AllocMem: allocate from the top down */
		MEMF_TOTAL = (1 << 19),    /* AvailMem: return total size of memory */

		MEMF_NO_EXPUNGE = (1 << 31), /*AllocMem: Do not cause expunge on failure */
	}

	public class MemoryAllocations
	{
		public List<MemoryEntry> Allocations { get; } = new List<MemoryEntry>();
	}

	public class MemoryEntry
	{
		public uint Address { get; set; }
		public uint Size { get; set; }
		public MEMF Type { get; set; }
	}
}
