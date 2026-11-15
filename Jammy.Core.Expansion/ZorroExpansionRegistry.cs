using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Types;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Expansion
{
	public class ZorroExpansionRegistry : IZorroExpansionRegistry
	{
		private readonly  Dictionary<uint, IZorroDebugHandler> expansionHandlers = new Dictionary<uint, IZorroDebugHandler>();

		public IZorroDebugHandler GetHandler(uint serial)
		{
			return expansionHandlers.GetValueOrDefault(serial);
		}

		public void RegisterHandler(uint serial, IZorroDebugHandler handler)
		{
			expansionHandlers.Add(serial, handler);
		}

		public void Reset()
		{
			foreach (var ex in expansionHandlers.Values)
				ex.Reset();
		}
	}

	public abstract class ZorroDebugHandler : IZorroDebugHandler
	{
		public ZorroDebugHandler(IZorroExpansionRegistry registry, string serial)
		{
			registry.RegisterHandler(ZorroConfiguration.MakeSerial(serial), this);
		}

		public abstract void Init(ZorroConfiguration configuration);
		public abstract void Reset();
	}
}
