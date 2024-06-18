/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IMemoryDumpView
	{
		int AddressToLine(uint address);
		string Text { get; }
	}
}
