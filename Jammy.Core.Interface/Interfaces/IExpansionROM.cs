/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

using Jammy.Core.Types.Types;

namespace Jammy.Core.Interface.Interfaces
{
	public interface IExpansionROM
	{
		ZorroConfiguration GetConfiguration();
		void PopulateROM(IDebuggableMemory zorroRAM);
	}
}
