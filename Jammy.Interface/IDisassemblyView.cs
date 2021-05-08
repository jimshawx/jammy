/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IDisassemblyView
	{
		int GetAddressLine(uint address);
		uint GetLineAddress(int line);
		string Text { get; }
	}
}