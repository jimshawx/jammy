namespace Jammy.AmigaTypes;

public struct ExAllData
{
	public ExAllDataPtr ed_Next { get; set; }
	public UBYTEPtr ed_Name { get; set; }
	public LONG ed_Type { get; set; }
	public ULONG ed_Size { get; set; }
	public ULONG ed_Prot { get; set; }
	public ULONG ed_Days { get; set; }
	public ULONG ed_Mins { get; set; }
	public ULONG ed_Ticks { get; set; }
	public UBYTEPtr ed_Comment { get; set; }
}

public struct ExAllControl
{
	public ULONG eac_Entries { get; set; }
	public ULONG eac_LastKey { get; set; }
	public UBYTEPtr eac_MatchString { get; set; }
	public HookPtr eac_MatchFunc { get; set; }
}

