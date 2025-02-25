namespace Jammy.AmigaTypes;

public struct NewBroker
{
	public BYTE nb_Version { get; set; }
	public STRPTR nb_Name { get; set; }
	public STRPTR nb_Title { get; set; }
	public STRPTR nb_Descr { get; set; }
	public WORD nb_Unique { get; set; }
	public WORD nb_Flags { get; set; }
	public BYTE nb_Pri { get; set; }
	public MsgPortPtr nb_Port { get; set; }
	public WORD nb_ReservedChannel { get; set; }
}

public struct InputXpression
{
	public UBYTE ix_Version { get; set; }
	public UBYTE ix_Class { get; set; }
	public UWORD ix_Code { get; set; }
	public UWORD ix_CodeMask { get; set; }
	public UWORD ix_Qualifier { get; set; }
	public UWORD ix_QualMask { get; set; }
	public UWORD ix_QualSame { get; set; }
}

