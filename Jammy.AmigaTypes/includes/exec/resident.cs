namespace Jammy.AmigaTypes;

public struct Resident
{
	public UWORD rt_MatchWord { get; set; }
	public ResidentPtr rt_MatchTag { get; set; }
	public APTR rt_EndSkip { get; set; }
	public UBYTE rt_Flags { get; set; }
	public UBYTE rt_Version { get; set; }
	public UBYTE rt_Type { get; set; }
	public BYTE rt_Pri { get; set; }
	public charPtr rt_Name { get; set; }
	public charPtr rt_IdString { get; set; }
	public APTR rt_Init { get; set; }
}

