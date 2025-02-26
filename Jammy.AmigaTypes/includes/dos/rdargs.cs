namespace Jammy.AmigaTypes;

public class CSource
{
	public UBYTEPtr CS_Buffer { get; set; }
	public LONG CS_Length { get; set; }
	public LONG CS_CurChr { get; set; }
}

public class RDArgs
{
	public CSource RDA_Source { get; set; }
	public LONG RDA_DAList { get; set; }
	public UBYTEPtr RDA_Buffer { get; set; }
	public LONG RDA_BufSiz { get; set; }
	public UBYTEPtr RDA_ExtHelp { get; set; }
	public LONG RDA_Flags { get; set; }
}

