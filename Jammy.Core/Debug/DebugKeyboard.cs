using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Debug
{
	public class DebugKeyboard : IDebugKeyboard
	{
		public DebugKeyboard(IEmulationWindow emulationWindow, IEnumerable<IDebugKeys> keyHandlers,
			IOptions<EmulationSettings> settings)
		{
			if (!settings.Value.Debugger.IsEnabled()) return;

			foreach (var keys in keyHandlers)
				emulationWindow.SetKeyHandlers(keys.DebugKeyDown, keys.DebugKeyUp);
		}
	}
}
