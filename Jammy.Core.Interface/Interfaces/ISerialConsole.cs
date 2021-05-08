/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface ISerialConsole
	{
		int ReadChar();
		void WriteChar(int c);
		void Reset();
	}
}