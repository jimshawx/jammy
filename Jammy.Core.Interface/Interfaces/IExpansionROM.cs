using Jammy.Core.Types.Types;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface IExpansionROM
	{
		ZorroConfiguration GetConfiguration();
		void PopulateROM(IDebuggableMemory zorroRAM, ZorroConfiguration configuration);
	}

	public interface IZorroExpansionRegistry
	{
		void RegisterExpansion(ZorroConfiguration configuration);
		ZorroConfiguration GetExpansion(uint serial);
		void RegisterHandler(uint ser, IZorroDebugHandler handler);
	}

	public interface IZorroDebugHandler
	{
		void Init(ZorroConfiguration configuration);
	}
}
