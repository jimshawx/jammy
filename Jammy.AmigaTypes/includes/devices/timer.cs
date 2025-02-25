namespace Jammy.AmigaTypes;

public struct timeval
{
	public ULONG tv_secs { get; set; }
	public ULONG tv_micro { get; set; }
}

public struct EClockVal
{
	public ULONG ev_hi { get; set; }
	public ULONG ev_lo { get; set; }
}

public struct timerequest
{
	public IORequest tr_node { get; set; }
	public timeval tr_time { get; set; }
}

