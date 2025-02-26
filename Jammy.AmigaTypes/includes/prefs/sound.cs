namespace Jammy.AmigaTypes;

public class SoundPrefs
{
	[AmigaArraySize(4)]
	public LONG[] sop_Reserved { get; set; }
	public BOOL sop_DisplayQueue { get; set; }
	public BOOL sop_AudioQueue { get; set; }
	public UWORD sop_AudioType { get; set; }
	public UWORD sop_AudioVolume { get; set; }
	public UWORD sop_AudioPeriod { get; set; }
	public UWORD sop_AudioDuration { get; set; }
	[AmigaArraySize(256)]
	public char[] sop_AudioFileName { get; set; }
}

