namespace Jammy.AmigaTypes;

public struct WBStartup
{
	public Message sm_Message { get; set; }
	public MsgPortPtr sm_Process { get; set; }
	public BPTR sm_Segment { get; set; }
	public LONG sm_NumArgs { get; set; }
	public charPtr sm_ToolWindow { get; set; }
	public WBArgPtr sm_ArgList { get; set; }
}

public struct WBArg
{
	public BPTR wa_Lock { get; set; }
	public BYTEPtr wa_Name { get; set; }
}

