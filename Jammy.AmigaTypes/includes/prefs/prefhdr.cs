namespace Jammy.AmigaTypes;

public struct PrefHeader
{
	public UBYTE ph_Version { get; set; }
	public UBYTE ph_Type { get; set; }
	public ULONG ph_Flags { get; set; }
}

