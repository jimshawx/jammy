namespace Jammy.AmigaTypes;

public struct InputPrefs
{
	[AmigaArraySize(16)]
	public char[] ip_Keymap { get; set; }
	public UWORD ip_PointerTicks { get; set; }
	public timeval ip_DoubleClick { get; set; }
	public timeval ip_KeyRptDelay { get; set; }
	public timeval ip_KeyRptSpeed { get; set; }
	public WORD ip_MouseAccel { get; set; }
}

