namespace Jammy.AmigaTypes;

public class BootBlock
{
	[AmigaArraySize(4)]
	public UBYTE[] bb_id { get; set; }
	public LONG bb_chksum { get; set; }
	public LONG bb_dosblock { get; set; }
}

