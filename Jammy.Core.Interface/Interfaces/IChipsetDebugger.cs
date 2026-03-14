using Jammy.Core.Types.Types;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface IChipsetDebugger : IEmulate
	{
		char[] fetch { get; }
		char[] write { get; }
		int dbugLine { get; }
		byte bitplaneMask { get; }
		byte bitplaneMod { get; }
		bool dbug { get; set; }
		int dma { get; set; }
		int ddfSHack { get; }
		int ddfEHack { get; }
		int diwSHack { get; }
		int diwEHack { get; }
		int ddfStrtFix { get; set; }
		int ddfStopFix { get; set; }
		int bplDelayHack { get; set; }
		void SetDMAActivity(DMAActivity activity);
		DMAEntry[] GetDMASummary();
		void Init(IChips chips);
		void SetColor(int index, uint rgb);
		int GetDebugLocation();
		uint[] GetDebugPalette();
		string GetOverlayText();
		void dbug_Keyup(int obj);
		void dbug_Keydown(int obj);
	}

	public interface IDebugKeyboard { }
}
