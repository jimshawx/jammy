namespace Jammy.AmigaTypes;

public class Device
{
	public Library dd_Library { get; set; }
}

public class Unit
{
	public MsgPort unit_MsgPort { get; set; }
	public UBYTE unit_flags { get; set; }
	public UBYTE unit_pad { get; set; }
	public UWORD unit_OpenCnt { get; set; }
}

