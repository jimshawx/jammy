namespace Jammy.AmigaTypes;

public struct Device
{
	public Library dd_Library { get; set; }
}

public struct Unit
{
	public MsgPort unit_MsgPort { get; set; }
	public UBYTE unit_flags { get; set; }
	public UBYTE unit_pad { get; set; }
	public UWORD unit_OpenCnt { get; set; }
}

