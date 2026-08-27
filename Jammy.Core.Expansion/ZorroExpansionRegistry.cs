using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Expansion
{
	public class ZorroExpansionRegistry : IZorroExpansionRegistry
	{
		private readonly Dictionary<uint, ZorroConfiguration> expansionROMs = new Dictionary<uint, ZorroConfiguration>();
		private readonly  Dictionary<uint, IZorroDebugHandler> expansionHandlers = new Dictionary<uint, IZorroDebugHandler>();


		public ZorroConfiguration GetExpansion(uint serial)
		{
			return expansionROMs.GetValueOrDefault(serial);
		}

		public void RegisterExpansion(ZorroConfiguration configuration)
		{
			expansionROMs.Add(configuration.GetSerial(), configuration);
			expansionHandlers[configuration.GetSerial()].Init(configuration);
		}

		public void RegisterHandler(uint serial, IZorroDebugHandler handler)
		{
			expansionHandlers.Add(serial, handler);
		}
	}

	public abstract class ZorroDebugHandler : IZorroDebugHandler
	{
		public ZorroDebugHandler(IZorroExpansionRegistry registry, string serial)
		{
			registry.RegisterHandler(ZorroConfiguration.MakeSerial(serial), this);
		}

		public abstract void Init(ZorroConfiguration configuration);
	}
}
