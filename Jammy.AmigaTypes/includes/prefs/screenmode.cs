namespace Jammy.AmigaTypes;

public struct ScreenModePrefs
{
	[AmigaArraySize(4)]
	public ULONG[] sm_Reserved { get; set; }
	public ULONG sm_DisplayID { get; set; }
	public UWORD sm_Width { get; set; }
	public UWORD sm_Height { get; set; }
	public UWORD sm_Depth { get; set; }
	public UWORD sm_Control { get; set; }
}

