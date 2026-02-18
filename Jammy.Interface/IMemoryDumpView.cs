using Jammy.Core.Types.Types;
using System.Collections.Generic;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IMemoryDump
	{
		int AddressToLine(uint address);
		string GetString(List<AddressRange> rng);
		string GetString(IMemoryDumpRanges rng);
		string GetString(uint start, ulong length);
		void ClearMapping();
	}

	public interface IMemoryDumpView
	{
		int AddressToLine(uint address);
		string Text { get; }
	}
}
