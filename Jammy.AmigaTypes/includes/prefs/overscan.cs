namespace Jammy.AmigaTypes;

public class OverscanPrefs
{
	[AmigaArraySize(4)]
	public ULONG[] os_Reserved { get; set; }
	public ULONG os_DisplayID { get; set; }
	public Point os_ViewPos { get; set; }
	public Point os_Text { get; set; }
	public Rectangle os_Standard { get; set; }
}

