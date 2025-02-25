namespace Jammy.AmigaTypes;

public struct DiscResourceUnit
{
	public Message dru_Message { get; set; }
	public Interrupt dru_DiscBlock { get; set; }
	public Interrupt dru_DiscSync { get; set; }
	public Interrupt dru_Index { get; set; }
}

public struct DiscResource
{
	public Library dr_Library { get; set; }
	public DiscResourceUnitPtr dr_Current { get; set; }
	public UBYTE dr_Flags { get; set; }
	public UBYTE dr_pad { get; set; }
	public LibraryPtr dr_SysLib { get; set; }
	public LibraryPtr dr_CiaResource { get; set; }
	[AmigaArraySize(4)]
	public ULONG[] dr_UnitID { get; set; }
	public List dr_Waiting { get; set; }
	public Interrupt dr_DiscBlock { get; set; }
	public Interrupt dr_DiscSync { get; set; }
	public Interrupt dr_Index { get; set; }
	public TaskPtr dr_CurrTask { get; set; }
}

