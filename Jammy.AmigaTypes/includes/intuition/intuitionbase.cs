namespace Jammy.AmigaTypes;

public class IntuitionBase
{
	public Library LibNode { get; set; }
	public View ViewLord { get; set; }
	public WindowPtr ActiveWindow { get; set; }
	public ScreenPtr ActiveScreen { get; set; }
	public ScreenPtr FirstScreen { get; set; }
	public ULONG Flags { get; set; }
	public WORD MouseY { get; set; }
	public WORD MouseX { get; set; }
	public ULONG Seconds { get; set; }
	public ULONG Micros { get; set; }
}

