using Jammy.Core.Interface.Interfaces;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Debug
{
	public class DebugKeyboard : IDebugKeyboard
	{
		public DebugKeyboard(IEmulationWindow emulationWindow, IChipsetDebugger chipsetDebugger)
		{
			emulationWindow.SetKeyHandlers(chipsetDebugger.dbug_Keydown, chipsetDebugger.dbug_Keyup);
		}
	}
}
